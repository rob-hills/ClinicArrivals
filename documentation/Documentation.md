# ClinicArrivals Documentation
 
This program is written to fit into an existing GP clinic workflow
to help a GP manage their workflow during the [COVID-19](https://en.wikipedia.org/wiki/Coronavirus_disease_2019) crisis. 

There are 2 main purposes of the application: 

* Keep Patients out of the waiting room where they are an infection risk to each other 
* Support the use of teleconsulation for patients who are an infection risk

The application uses SMS messages (due to their wide ubiquity) for patient communication,
and [OpenVidu](https://openvidu.org/) for the video consultations. 

Documentation:

* [Description of the messaging workflow](Workflow.md)
* [User Guide](UserGuide.md)
* [Installation](Installation.md)
* [Using the Simulator](Simulator.md)
* [Setting up the SMS phone # using Twilio](Twilio.md)
* [Program Settings](Settings.md)
* [Managing SMS templates](Templates.md)
* [Security Notes](Security.md)
* [Getting Support](Support.md)
* [Technical Specifications for the PMS integration](FHIRDocumentation.md)


## Patient Focused Documentation

This is documentation that is linked to directly from the SMS messages

There is one set of instructions for each different video call service that can be used:

* [OpenVidu](VideoOpenVidu.md)  (https://bit.ly/33MFitY)
* [Jitsi](VideoJitsi.md) (https://bit.ly/3bufeq9)
* [Skype](VideoSkype.md) (https://bit.ly/2WJrL4O)
