using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Discovery;
using AlexanderDevelopment.CrmDeploymentWizard.Lib;

namespace AlexanderDevelopment.CrmDeploymentWizard
{
    class Program
    {
        static string _targetString = string.Empty;
        private static string _rootDir = string.Empty;
        private static string _json = string.Empty;

        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // consume Options instance properties
                if (options.Verbose)
                {
                    Console.WriteLine("Manifest file: {0}", options.Manifest);
                    Console.WriteLine("Target connection string: {0}", options.Target);
                }

                if (!string.IsNullOrEmpty(options.Target))
                {
                    _targetString = options.Target;
                }
                else
                {
                    Console.WriteLine("no target connection specified - exiting");
                    return;
                }

                if (!string.IsNullOrEmpty(options.Manifest))
                {
                    //parse the config file
                    _json = File.ReadAllText(options.Manifest);

                    _rootDir = Path.GetDirectoryName(Path.GetFullPath(options.Manifest));
                    Console.WriteLine("Root directory: {0}", _rootDir);
                }
                else
                {
                    Console.WriteLine("no manifest file specified - exiting");
                    return;
                }

                Deployer deployer = new Deployer
                {
                    ManifestData = _json,
                    TargetString = _targetString,
                    RootDirectory = _rootDir
                };

                deployer.Process();
            }
        }
    }
}
