# AlexanderDevelopment.CrmDeploymentWizard
This is a tool used to automate Dynamics CRM deployments. The tool reads a JSON-formatted manifest of CRM solutions to deploy (and optionally publish), configuration data to import and additional commands to execute, and then it executes those steps in order.

The deployment manifest is formatted like this:

<pre>{
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
</pre>
