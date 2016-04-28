using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace AlexanderDevelopment.CrmDeploymentWizard
{
    public class Options
    {
        [Option('m', "manifest", Required = true, HelpText = "JSON manifest file")]
        public string Manifest { get; set; }

        [Option('t', "target", Required = false, HelpText = "Simplified CRM connection string to CRM target org")]
        public string Target { get; set; }

        [Option('v', "verbose", HelpText = "Print details during execution")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("AlexanderDevelopment.ConfigDataMover.Cli", "1.0.0.0"),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.Copyright = @"
Copyright 2016 Lucas Alexander

This program comes with ABSOLUTELY NO WARRANTY. This is free software, and you are welcome to redistribute it and/or modify it under the terms of the Apache License, Version 2.0. You may obtain a copy at http://www.apache.org/licenses/LICENSE-2.0.
";

            help.AddPreOptionsLine("Usage: AlexanderDevelopment.CrmDeploymentWizard.Cli.exe -m manifest.json -t \"Url=https://xxxx; Domain=xxxx; Username=xxxx; Password=xxxx;\"");
            help.AddOptions(this);
            return help;
        }
    }
}
