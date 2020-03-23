﻿using ClinicArrivals.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ClinicArrivals
{
    public class ViewModel : ArrivalsModel
    {
        public string Text { get { return System.IO.File.ReadAllText("about.md"); } }

        public BackgroundProcess ReadSmsMessage;
        public BackgroundProcess ScanAppointments;
        public BackgroundProcess ProcessUpcomingAppointments;
        public SimulationSmsProcessor smsProcessor { get; set; } = new SimulationSmsProcessor();

        private readonly NLogAdapter logger = new NLogAdapter();

        public ViewModel()
        {
            CreateTestDataForDebug();

            // Assign all of the implementations for the interfaces
            Storage = new ArrivalsFileSystemStorage();
            Settings.Save = new SaveSettingsCommand(Storage);
            Settings.Reload = new ReloadSettingsCommand(Storage);
            SaveRoomMappings = new SaveRoomMappingsCommand(Storage);
            ReloadRoomMappings = new ReloadRoomMappingsCommand(Storage);
            SaveTemplates = new SaveTemplatesCommand(Storage);
            ReloadTemplates = new ReloadTemplatesCommand(Storage);
            SeeTemplateDocumentation = new SeeTemplateDocumentationCommand();
            ClearUnproccessedMessages = new ClearUnproccessedMessagesCommand(Storage);

            // Simulator for the SMS message processor
            smsProcessor.ClearMessages = new SimulateProcessorCommand(smsProcessor);
            smsProcessor.QueueIncomingMessage = new SimulateProcessorCommand(smsProcessor);
        }

        private MessagingEngine PrepareMessagingEngine()
        {
            MessagingEngine engine = new MessagingEngine();
            engine.Initialise(Settings);
            engine.Logger = logger;
            engine.Storage = Storage;
            engine.RoomMappings = RoomMappings;
            engine.TemplateProcessor = new TemplateProcessor();
            engine.TemplateProcessor.Initialise(Settings);
            engine.TemplateProcessor.Templates = Templates;
            engine.AppointmentUpdater = new FhirAppointmentUpdater(MessageProcessing.GetServerConnection);
            engine.VideoManager = new VideoConferenceManager();
            engine.VideoManager.Initialize(Settings);
            engine.UnprocessableMessages = UnprocessableMessages;
            // if (!string.IsNullOrEmpty(Settings.DeveloperPhoneNumber))
            engine.SmsSender = smsProcessor;
            // engine.SmsSender = new TwilioSmsProcessor();
            engine.TimeNow = DateTime.Now;
            return engine;
        }

        public async Task Initialize(Dispatcher dispatcher)
        {
            // read the settings from storage
            Settings.CopyFrom(await Storage.LoadSettings());
            if (Settings.SystemIdentifier == Guid.Empty)
            {
                // this only occurs when the system hasn't had one allocated
                // so we can create a new one, then save the settings.
                // (this will force an empty setting file with the System Identifier if needed)
                Settings.AllocateNewSystemIdentifier();
                await Storage.SaveSettings(Settings);
            }

            // read the room mappings from storage
            RoomMappings.Clear();
            foreach (var map in await Storage.LoadRoomMappings())
                RoomMappings.Add(map);

            Templates.Clear();
            foreach (var template in await Storage.LoadTemplates())
                Templates.Add(template);
            AddMissingTemplates();

            // reload any unmatched messages
            var messages = await Storage.LoadUnprocessableMessages(DisplayingDate);
            foreach (var item in messages)
                UnprocessableMessages.Add(item);

            // setup the background worker routines
            ReadSmsMessage = new BackgroundProcess(Settings, serverStatuses.IncomingSmsReader, dispatcher, async () =>
            {
                // Logic to run on this process
                // (called every settings.interval)
                StatusBarMessage = $"Last read SMS messages at {DateTime.Now.ToLongTimeString()}";
                var engine = PrepareMessagingEngine();
                List<PmsAppointment> appts = new List<PmsAppointment>();
                appts.AddRange(Appointments);
                var messagesReceived = await engine.SmsSender.ReceiveMessages();
                engine.ProcessIncomingMessages(appts, messagesReceived);
            });

            ScanAppointments = new BackgroundProcess(Settings, serverStatuses.AppointmentScanner, dispatcher, async () =>
            {
                // Logic to run on this process
                // (called every settings.interval)
                var engine = PrepareMessagingEngine();
                List<PmsAppointment> appts = await MessageProcessing.SearchAppointments(this.DisplayingDate, RoomMappings, Storage);
                engine.ProcessTodaysAppointments(appts);

                // Now update the UI once we've processed it all
                Expecting.Clear();
                Waiting.Clear();
                Appointments.Clear();
                foreach (var appt in appts)
                {
                    Appointments.Add(appt);
                    if (appt.ArrivalStatus == Hl7.Fhir.Model.Appointment.AppointmentStatus.Booked)
                        Expecting.Add(appt);
                    else if (appt.ArrivalStatus == Hl7.Fhir.Model.Appointment.AppointmentStatus.Arrived)
                        Waiting.Add(appt);
                }
            });

            ProcessUpcomingAppointments = new BackgroundProcess(Settings, serverStatuses.UpcomingAppointmentProcessor, dispatcher, async () =>
            {
                // Logic to run on this process
                // (called every settings.intervalUpcoming)
                var engine = PrepareMessagingEngine();
                List<PmsAppointment> appts = await MessageProcessing.SearchAppointments(this.DisplayingDate.AddDays(1), RoomMappings, Storage);
                appts.AddRange(await MessageProcessing.SearchAppointments(this.DisplayingDate.AddDays(2), RoomMappings, Storage));
                engine.ProcessUpcomingAppointments(appts);
            }, true);
        }

        private void AddMissingTemplates()
        {
            DefineDefaultTemplate(MessageTemplate.MSG_REGISTRATION, MessageTemplate.DEF_MSG_REGISTRATION);
            DefineDefaultTemplate(MessageTemplate.MSG_CANCELLATION, MessageTemplate.DEF_MSG_CANCELLATION);
            DefineDefaultTemplate(MessageTemplate.MSG_UNKNOWN_PH, MessageTemplate.DEF_MSG_UNKNOWN_PH);
            DefineDefaultTemplate(MessageTemplate.MSG_TOO_MANY_APPOINTMENTS, MessageTemplate.DEF_MSG_TOO_MANY_APPOINTMENTS);
            DefineDefaultTemplate(MessageTemplate.MSG_UNEXPECTED, MessageTemplate.DEF_MSG_UNEXPECTED);
            DefineDefaultTemplate(MessageTemplate.MSG_SCREENING, MessageTemplate.DEF_MSG_SCREENING);
            DefineDefaultTemplate(MessageTemplate.MSG_SCREENING_YES, MessageTemplate.DEF_MSG_SCREENING_YES);
            DefineDefaultTemplate(MessageTemplate.MSG_SCREENING_NO, MessageTemplate.DEF_MSG_SCREENING_NO);
            DefineDefaultTemplate(MessageTemplate.MSG_DONT_UNDERSTAND_SCREENING, MessageTemplate.DEF_MSG_DONT_UNDERSTAND_SCREENING);
            DefineDefaultTemplate(MessageTemplate.MSG_VIDEO_INVITE, MessageTemplate.DEF_MSG_VIDEO_INVITE);
            DefineDefaultTemplate(MessageTemplate.MSG_VIDEO_THX, MessageTemplate.DEF_MSG_VIDEO_THX);
            DefineDefaultTemplate(MessageTemplate.MSG_DONT_UNDERSTAND_VIDEO, MessageTemplate.DEF_MSG_DONT_UNDERSTAND_VIDEO);
            DefineDefaultTemplate(MessageTemplate.MSG_ARRIVED_THX, MessageTemplate.DEF_MSG_ARRIVED_THX);
            DefineDefaultTemplate(MessageTemplate.MSG_DONT_UNDERSTAND_ARRIVING, MessageTemplate.DEF_MSG_DONT_UNDERSTAND_ARRIVING);
            DefineDefaultTemplate(MessageTemplate.MSG_APPT_READY, MessageTemplate.DEF_MSG_APPT_READY);
        }

        private void DefineDefaultTemplate(string id, string value)
        {
            foreach (var template in Templates)
            {
                if (template.MessageType == id)
                {
                    return;
                }
            }
            Templates.Add(new MessageTemplate(id, value));
        }

        private void CreateTestDataForDebug()
        {
#if DEBUG
            // This is some test data to assist in the UI Designer
            Expecting.Add(new PmsAppointment() { ArrivalTime = DateTime.Parse("10:35am"), PatientName = "Postlethwaite, Brian", PatientMobilePhone = "0423857505", ArrivalStatus = Hl7.Fhir.Model.Appointment.AppointmentStatus.Pending, AppointmentStartTime = DateTime.Parse("9:45am"), PractitionerName = "Dr Nathan Pinskier" });
            Expecting.Add(new PmsAppointment() { ArrivalTime = DateTime.Parse("10:35am"), PatientName = "Esler, Brett", PatientMobilePhone = "0423083847", ArrivalStatus = Hl7.Fhir.Model.Appointment.AppointmentStatus.Pending, AppointmentStartTime = DateTime.Parse("10:00am"), PractitionerName = "Dr Nathan Pinskier" });
            Waiting.Add(new PmsAppointment() { ArrivalTime = DateTime.Parse("10:35am"), PatientName = "Grieve, Grahame", PatientMobilePhone = "0423083847", ArrivalStatus = Hl7.Fhir.Model.Appointment.AppointmentStatus.Arrived, AppointmentStartTime = DateTime.Parse("10:00am"), PractitionerName = "Dr Nathan Pinskier" });
            RoomMappings.Add(new DoctorRoomLabelMapping()
            {
                PractitionerFhirID = "1",
                PractitionerName = "Dr Nathan",
                LocationName = "Room 1",
                LocationDescription = "Proceed through the main lobby and its the 3rd door on the right"
            });
            RoomMappings.Add(new DoctorRoomLabelMapping()
            {
                PractitionerFhirID = "1",
                PractitionerName = "Dr Smith",
                LocationName = "Room 2",
                LocationDescription = "Proceed through the main lobby and its the 2nd door on the right"
            });
            
#endif
        }
    }
}
