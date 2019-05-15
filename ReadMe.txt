The Skill Client example source code are developed using Microsoft Visual Studio 2017.

Directory Structure
--------------------
PCSkills Framework   --- Root Directory
IntentRouterClient --- Dll Directory containing the Framework
PCSkillsHello               --- Project for HelloWorld PC Skill Example. It contains the Hello world sample application. This is the starting point where you can start developing the code to create your own PC Skill. It also contains the setup project to create an MSI Installer.
PCSkillExample            --- Source code for the Hello world sample PC skill
PCSkillInstallerExample   --- Project to create the Installer for the HelloWorld PC Skill. The same project can be used to create the installer for your own PC Skill.
PCSkillsRelease	          --- Project for the three reference skills. It contains reference source code for Arrange Windows PC Skill, PC Automation skill and PC Content search skill. It also contains the setup project to create an MSI Installer.
PCSkillExample            --- Reference source code for Arrange Windows Skill, PC Automation skill and PC Content search skill.
PCSkillInstallerExample   --- Project to create the Installer for the three PC Skills. This is also for your reference.

To Compile the HelloWorld PC Skill. Open the PCSkillDevKit.sln file in the PCSkillsHello directory using Microsoft Visual Studio. Follow the instruction mentioned in the AWS Setup document to configure the cloud infrastructure and fill in the infrastructure information into the IntentRouterClient.dll.config file. And build the project and run it. Detailed instruction can be found in the Intel Developer Zone portal.
