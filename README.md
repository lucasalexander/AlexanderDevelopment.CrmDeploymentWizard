# AlexanderDevelopment.CrmDeploymentWizard
The CRM Deployment Wizard automates Dynamics CRM deployments. The tool supports three different operations:

1. Importing one or more CRM solutions
1. Executing one or more [Alexander Development Dynamics CRM Configuration Data Mover](http://alexanderdevelopment.net/tag/configuration-data-mover/) jobs
1. Executing arbitrary commands using the .Net System.Diagnostics.Process.Start method

###Setting up a deployment package
In order to use the tool, you need to do the following:

1. Create a directory to hold the deployment manifest and all supporting CRM solution and configuration data files.
1. Create the following subdirectories in the main deployment package directory:
a. solutions
b. data
c. logs

1. Place solutions to be imported in "solutions" directory.
1. Place Configuration Data Mover job XML files and any JSON data import files in "data" directory.

1. Create a manifest file in root of package directory. This file is a JSON array of steps for the tool to execute. Here is the content of a sample Deployment Wizard manifest:

```
{
  "steps": [
    {
      "type": "solutionimport",
      "solutionpath": "dog_0_0_0_1.zip",
      "options": {
        "publishworkflows": true,
		"publishcomponents": true
      }
    },
    {
      "type": "dataimport",
      "configpath": "team-import.xml",
	  "datasource": "teams.json"
    },
    {
      "type": "command",
      "path": "C:\\git-repos\\AlexanderDevelopment.CrmDeploymentWizard\\sln-work\\test.bat",
	  "arguments": ""
    }
  ]
}
```

Each of the configuration parameters is described in more detail below. A sample deployment package can be found in the "samples" directory.

####CRM solution import step parameters
- type: solutionimport
- solutionpath: Path to the solution relative to the "solutions" directory.
- options
 - publishworkflows: Boolean value for whether publish SDK messages when the solution is imported.
 - publishcomponents: Boolean value for whether to publish the entities, dashboards, application ribbon modifications, sitemap, global optionsets and web resources included in the solution after it is imported.
  

####Configuration Data Mover step parameters
 - type: dataimport
 - configpath: Path to the job XML file relative to the "data" directory.
 - datasource: Source for the data import. If the source is a Configuration Data Mover JSON data export file, the path is relative to the "data" directory. Unlike when running the Configuration Data Mover as a standalone tool, this utility will not read the source connection from the job XML file.

The target for the data import is not specified in the step parameters. The data import target is always set to the target for the overall Deployment Wizard job, even if a target is specified in the Configuration Data Mover job XML file.


####Command step parameters
 - type: command
 - path: Absolute path to the command to run (EXE, BAT, etc.). Any backslashes in the path must be escaped.
 - arguments: Arguments to be passed to the command at runtime.

When a command step is executed, as soon as the process starts, the Deployment Wizard will immediately move to the next step in the manifest or finalize the job if no next step is specified. You should not create jobs that require a command to complete prior to executing a subsequent step.


###Deploying the package
Once you have completed the preparation steps above, you can open a command prompt and run the tool. You just need to supply the path to the manifest file and connection details for the target CRM system as a simplified connection string. Here's an example of how to do that:

```
AlexanderDevelopment.CrmDeploymentWizard.exe 
-m "C:\git-repos\AlexanderDevelopment.CrmDeploymentWizard\samples\package01\package-manifest.json" -t "url=http://192.168.65.101/LucasTest01;username=administrator;domain=companyx;password=XXXXXXXX"
```

###Logging
The CRM Deployment Wizard uses the Apache [log4net](https://logging.apache.org/log4net/) library for logging. The standard configuration generates two log files in the directory where the Deployment Wizard executable runs:

 - CrmDeploymentWizard.log - This is a verbose log of each step in the process. 
 - CrmDeploymentWizard-Error.log - This records every error encountered in the process. It is particularly useful for troubleshooting jobs that include a Configuration Data Import step because it includes the GUID and details for each record that could not be imported.

You can modify the format of the logs by updating the log4net configuration in the app.config files. An overview of logging configuration options can be found [here](https://logging.apache.org/log4net/release/config-examples.html).

Results of CRM solution import steps are saved to the "logs" subdirectory in the main deployment package directory as yyyy-MM-dd_hhmmss_solution-[SOLUTION NAME].xml
