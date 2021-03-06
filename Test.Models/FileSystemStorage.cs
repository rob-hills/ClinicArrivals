﻿using ClinicArrivals.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Models
{
    [TestClass]
    public class TestFileSystemStorage
    {
        [TestMethod, TestCategory("Storage")]
        public void SaveLoadSettings() 
        {
            // delete any pre-existing settings file
            var storage = new ClinicArrivals.Models.ArrivalsFileSystemStorage(false, "ClinicArrivals.Test.SaveLoadSettings");
            string pathSettings = Path.Combine(storage.GetFolder(), "appSettings.json");
            if (File.Exists(pathSettings))
                File.Delete(pathSettings);

            // read a non existent settings file
            var settings = storage.LoadSettings().GetAwaiter().GetResult();
            Assert.IsNotNull(settings);

            // Put in some values
            settings.ACCOUNT_SID = "asdf";
            settings.AUTH_TOKEN = "qwerqwqwer";
            settings.FromTwilioMobileNumber = "+555 testme";
            settings.PollIntervalSeconds = 30;
            storage.SaveSettings(settings);

            // Now re-read the settings 
            settings = storage.LoadSettings().GetAwaiter().GetResult();
            Assert.IsNotNull(settings);
            Assert.AreEqual(30, settings.PollIntervalSeconds);
            Assert.AreEqual("qwerqwqwer", settings.AUTH_TOKEN);

            // Update some values and re-save
            settings.PollIntervalSeconds = 60;
            settings.AUTH_TOKEN = "Welcome to the test of ClinicArrivals";
            settings.Save = new SaveSettingsCommand(storage);
            storage.SaveSettings(settings);

            // Now re-read the settings 
            settings = storage.LoadSettings().GetAwaiter().GetResult();
            Assert.IsNotNull(settings);
            Assert.AreEqual(60, settings.PollIntervalSeconds);
            Assert.AreEqual("Welcome to the test of ClinicArrivals", settings.AUTH_TOKEN);
            Assert.IsNull(settings.Save);

            // Cleanup the test folder
            Directory.Delete(storage.GetFolder(), true);
        }

        [TestMethod, TestCategory("Storage")]
        public async Task SaveLoadTemplates()
        {
            // delete any pre-existing settings file
            var storage = new ClinicArrivals.Models.ArrivalsFileSystemStorage(false, "ClinicArrivals.Test.SaveLoadTemplates");
            string pathSettings = Path.Combine(storage.GetFolder(), "message-templates.json");
            if (File.Exists(pathSettings))
                File.Delete(pathSettings);

            // read a non existent settings file
            var templates = (await storage.LoadTemplates())?.ToList();
            Assert.IsNotNull(templates);

            // Add in a template
            templates.Add(new MessageTemplate("Intro"));
            templates.Add(new MessageTemplate("PleaseWait"));
            templates.Add(new MessageTemplate("ComeInside"));
            await storage.SaveTemplates(templates);

            // Check that they are in there
            templates = (await storage.LoadTemplates())?.ToList();
            Assert.AreEqual(3, templates.Count, "Expected the same number of templates to come out");

            // append another
            templates.Add(new MessageTemplate("ComeInside"));
            await storage.SaveTemplates(templates);
            templates = (await storage.LoadTemplates())?.ToList();
            Assert.AreEqual(4, templates.Count, "Expected the same number of templates to come out");

            // Update the contents of a template
            string TemplateContent = "this is the template";
            templates[2].Template = TemplateContent;

            await storage.SaveTemplates(templates);
            templates = (await storage.LoadTemplates())?.ToList();
            Assert.AreEqual(4, templates.Count, "Expected the same number of templates to come out");
            Assert.AreEqual(TemplateContent, templates[2].Template, "The updated template content was not the same");

            // Cleanup the test folder
            Directory.Delete(storage.GetFolder(), true);
        }

        [TestMethod, TestCategory("Storage")]
        public async Task SaveAppointmentExtendedData()
        {
            // delete any pre-existing test content
            var storage = new ClinicArrivals.Models.ArrivalsFileSystemStorage(false, "ClinicArrivals.Test.SaveAppointmentExtendedData");
            if (File.Exists(storage.GetFolder()))
                File.Delete(storage.GetFolder());

            DateTime testDataToday = new DateTime(2020, 3, 20);

            // read a non extended data
            PmsAppointment appt = new PmsAppointment() { AppointmentFhirID = Guid.NewGuid().ToString("D") };
            Assert.IsNotNull(appt.ExternalData);
            await storage.LoadAppointmentStatus(appt);
            Assert.IsNotNull(appt.ExternalData);

            // Put some content in there and ensure that a load will clear it out
            appt.ExternalData.LastPatientMessage = "last-message";
            Assert.IsNotNull(appt.ExternalData.LastPatientMessage);
            await storage.LoadAppointmentStatus(appt);
            Assert.IsNull(appt.ExternalData.LastPatientMessage);

            // Add some content, then save it
            appt.ExternalData.LastPatientMessage = "last-message";
            await storage.SaveAppointmentStatus(appt);
            Assert.IsNotNull(appt.ExternalData.LastPatientMessage);
            Assert.AreEqual("last-message", appt.ExternalData.LastPatientMessage);

            // Cleanup the test folder
            if (File.Exists(storage.GetFolder()))
                Directory.Delete(storage.GetFolder(), true);
        }
    }
}
