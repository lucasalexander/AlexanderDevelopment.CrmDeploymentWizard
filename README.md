# AlexanderDevelopment.CrmDeploymentWizard
This is a tool used to automate Dynamics CRM deployments. The tool reads a JSON-formatted manifest of CRM solutions to deploy (and optionally publish), configuration data to import and additional commands to execute, and then it executes those steps in order.

The deployment manifest is formatted like so:

<pre>{
  "steps": [
    {
      "steptype": "solutionimport",
      "steppath": "dog_0_0_0_1.zip",
      "options": {
        "publishworkflows": true,
		"publishcomponents": true
      }
    },
    {
      "steptype": "dataimport",
      "steppath": "PATH_TO_JOB.xml"
    },
    {
      "steptype": "command",
      "steppath": "echo",
	  "arguments": ""
    }
  ]
}</pre>
