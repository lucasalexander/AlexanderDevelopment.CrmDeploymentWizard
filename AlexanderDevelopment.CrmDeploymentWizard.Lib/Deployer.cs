using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Discovery;
using log4net;


namespace AlexanderDevelopment.CrmDeploymentWizard.Lib
{
    public class Deployer
    {
        public string TargetString { get; set; }
        public string ManifestData { get; set; }
        public string RootDirectory { get; set; }

        private const string _cSolutionsDir = "solutions";
        private const string _cImportLogsDir = "logs";
        private const string _cImportDataDir = "data";

        string _targetString;
        CrmServiceClient _targetClient;
        List<JObject> _jobSteps;
        string _targetVersion;
        string _rootDir;
        int _errorCount;

        int _timeoutHours = 0;
        int _timeoutMinutes = 30;
        int _timeoutSeconds = 30;

        /// <summary>
        /// log4net logger
        /// </summary>
        private ILog logger;


        public Deployer()
        {
            _targetString = string.Empty;
            _targetClient = null;
            _jobSteps = new List<JObject>();
            _targetVersion = "0.0.0.0";
            _rootDir = string.Empty;
            _errorCount = 0;
            log4net.Config.XmlConfigurator.Configure();
        }

        /// <summary>
        /// used to report progress and log status via a single method
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        private void LogMessage(string level, string message)
        {
            switch (level.ToUpper())
            {
                case "INFO":
                    logger.Info(message);
                    break;
                case "ERROR":
                    logger.Error(message);
                    break;
                case "WARN":
                    logger.Warn(message);
                    break;
                case "DEBUG":
                    logger.Debug(message);
                    break;
                case "FATAL":
                    logger.Fatal(message);
                    break;
                default:
                    logger.Info(message); //default to info
                    break;
            }
        }

        public void Process()
        {
            //set up logging
            logger = LogManager.GetLogger(typeof(Deployer));
            LogMessage("INFO", "starting job");

            if (!string.IsNullOrEmpty(TargetString))
            {
                _targetString = TargetString;
                ParseCrmConnection();
            }
            else
            {
                string errormsg = "no target connection specified";
                LogMessage("ERROR", errormsg);
                throw new InvalidOperationException(errormsg);
            }

            if (!string.IsNullOrEmpty(ManifestData))
            {
                ParseManifest(ManifestData);
            }
            else
            {
                string errormsg = "no manifest data specified";
                LogMessage("ERROR", errormsg);
                throw new InvalidOperationException(errormsg);
            }

            if (!string.IsNullOrEmpty(RootDirectory))
            {
                _rootDir = RootDirectory;
            }
            else
            {
                string errormsg = "no root directory specified";
                LogMessage("ERROR", errormsg);
                throw new InvalidOperationException(errormsg);
            }

            //do some basic validations
            if (!(_jobSteps.Count > 0))
            {
                string errormsg = "no steps in job";
                LogMessage("ERROR", errormsg);
                throw new InvalidOperationException(errormsg);
            }
            LogMessage("INFO", "executing steps");
            for (int i = 0; i < _jobSteps.Count; i++)
            {
                LogMessage("INFO", string.Format("Step #{0}", i + 1));
                var step = _jobSteps[i];
                switch (step["type"].ToString().ToUpper())
                {
                    case "SOLUTIONIMPORT":
                        ImportSolution(step);
                        break;
                    case "DATAIMPORT":
                        ImportData(step);
                        break;
                    case "COMMAND":
                        RunCommand(step);
                        break;
                }
            }
        }

        void ParseManifest(string json)
        {
            JObject manifestJson = JObject.Parse(json);
            JArray steps = ((JArray)manifestJson["steps"]);
            foreach (JObject step in steps)
            {
                _jobSteps.Add(step);
            }
        }

        void ImportSolution(JObject solutionstep)
        {
            LogMessage("INFO", string.Format("Starting solution import step"));
            using (OrganizationServiceProxy service = _targetClient.OrganizationServiceProxy)
            {
                service.Timeout = new TimeSpan(_timeoutHours, _timeoutMinutes, _timeoutSeconds);
                string solutionpath = string.Format(@"{0}\{1}\{2}", _rootDir, _cSolutionsDir, solutionstep["solutionpath"].ToString());
                byte[] fileBytes = File.ReadAllBytes(solutionpath);

                ImportSolutionRequest impSolReq = new ImportSolutionRequest()
                {
                    CustomizationFile = fileBytes,
                    PublishWorkflows = Convert.ToBoolean(solutionstep["options"]["publishworkflows"]),
                    ImportJobId = Guid.NewGuid()
                };

                service.Execute(impSolReq);
                LogMessage("INFO", string.Format("Imported Solution from {0}", solutionpath));
                Entity job = service.Retrieve("importjob", impSolReq.ImportJobId, new ColumnSet(new System.String[] { "data", "solutionname" }));
                LogMessage("INFO", string.Format("Solution name: {0}", job["solutionname"]));

                XmlDocument importjobdoc = new XmlDocument();
                importjobdoc.LoadXml(job["data"].ToString());

                string solutionname = importjobdoc.SelectSingleNode("//solutionManifest/UniqueName").InnerText;
                string importresult = importjobdoc.SelectSingleNode("//solutionManifest/result/@result").Value;
                string solutionmanaged = importjobdoc.SelectSingleNode("//solutionManifest/Managed").InnerText;
                bool ismanaged = (solutionmanaged == "0") ? false : true;

                LogMessage("INFO", string.Format("Report from the ImportJob data"));
                LogMessage("INFO", string.Format("Solution Unique name: {0}", solutionname));
                LogMessage("INFO", string.Format("Solution Import Result: {0}", importresult));
                string importlogfilename = string.Format("{0}_solution-{1}.xml",DateTime.Now.ToString("yyyy-MM-dd_hhmmss"), solutionname);
                string importlogfilepath = string.Format(@"{0}\{1}\{2}", _rootDir, _cImportLogsDir, importlogfilename);
                LogMessage("INFO", string.Format(@"Import log file: {0}\{1}", _cImportLogsDir, importlogfilename));
                importjobdoc.Save(importlogfilepath);

                if (!ismanaged && Convert.ToBoolean(solutionstep["options"]["publishcomponents"]))
                {
                    LogMessage("INFO", "Preparing to publish solution components");
                    XmlDocument publishdoc = BuildPublishXml(importjobdoc, service);

                    PublishXmlRequest publishrequest = new PublishXmlRequest { ParameterXml = publishdoc.OuterXml };
                    PublishXmlResponse publishresponse = (PublishXmlResponse)service.Execute(publishrequest);

                    LogMessage("INFO", "Solution components published");
                }
            }
            LogMessage("INFO", string.Format("Solution import step complete"));
       }

        XmlDocument BuildPublishXml(XmlDocument importjobdoc, OrganizationServiceProxy service)
        {
            XmlDocument publishdoc = new XmlDocument();
            XmlElement rootelement = publishdoc.CreateElement(string.Empty, "importexportxml", string.Empty);
            publishdoc.AppendChild(rootelement);

            XmlNodeList entities = importjobdoc.SelectNodes("//entities/entity");
            if (entities.Count > 0)
            {
                XmlElement entitieselement = publishdoc.CreateElement(string.Empty, "entities", string.Empty);
                rootelement.AppendChild(entitieselement);
                foreach (System.Xml.XmlNode node in entities)
                {
                    string itemname = node.Attributes["id"].Value;
                    string result = node.FirstChild.Attributes["result"].Value;

                    if (result == "success")
                    {
                        XmlElement entityelement = publishdoc.CreateElement(string.Empty, "entity", string.Empty);
                        entityelement.AppendChild(publishdoc.CreateTextNode(itemname.ToLower()));
                        entitieselement.AppendChild(entityelement);
                    }
                }
            }

            XmlNodeList optionsets = importjobdoc.SelectNodes("//optionSets/optionSet");
            if (optionsets.Count > 0)
            {
                XmlElement optionsetselement = publishdoc.CreateElement(string.Empty, "optionsets", string.Empty);
                rootelement.AppendChild(optionsetselement);
                foreach (System.Xml.XmlNode node in optionsets)
                {
                    string itemname = node.Attributes["id"].Value;
                    string result = node.FirstChild.Attributes["result"].Value;

                    if (result == "success")
                    {
                        XmlElement optionsetelement = publishdoc.CreateElement(string.Empty, "optionset", string.Empty);
                        optionsetelement.AppendChild(publishdoc.CreateTextNode(itemname));
                        optionsetselement.AppendChild(optionsetelement);
                    }
                }
            }

            XmlNodeList dashboards = importjobdoc.SelectNodes("//dashboards/dashboard");
            if (dashboards.Count > 0)
            {
                XmlElement dashboardselement = publishdoc.CreateElement(string.Empty, "dashboards", string.Empty);
                rootelement.AppendChild(dashboardselement);
                foreach (System.Xml.XmlNode node in dashboards)
                {
                    string itemname = node.Attributes["id"].Value;
                    string result = node.FirstChild.Attributes["result"].Value;

                    if (result == "success")
                    {
                        XmlElement dashboardelement = publishdoc.CreateElement(string.Empty, "dashboard", string.Empty);
                        dashboardelement.AppendChild(publishdoc.CreateTextNode(itemname));
                        dashboardselement.AppendChild(dashboardelement);
                    }
                }
            }

            XmlNodeList ribbons = importjobdoc.SelectNodes("//ribbons/ribbon[result[@result='success']]");
            if (ribbons.Count > 0)
            {
                XmlElement ribbonselement = publishdoc.CreateElement(string.Empty, "ribbons", string.Empty);
                rootelement.AppendChild(ribbonselement);
                ribbonselement.AppendChild(publishdoc.CreateElement(string.Empty, "ribbon", string.Empty));
            }

            XmlNodeList sitemapnodes = importjobdoc.SelectNodes("//*[@id='sitemap']");
            if (sitemapnodes.Count > 0)
            {
                System.Xml.XmlNode sitemapnode = sitemapnodes[0];
                if (sitemapnode.FirstChild.Attributes["result"].Value == "success")
                {
                    XmlElement sitemapselement = publishdoc.CreateElement(string.Empty, "sitemaps", string.Empty);
                    rootelement.AppendChild(sitemapselement);
                    sitemapselement.AppendChild(publishdoc.CreateElement(string.Empty, "sitemap", string.Empty));
                }
            }

            XmlNodeList webresources = importjobdoc.SelectNodes("//webResources/webResource");
            if (webresources.Count > 0)
            {
                string webresourcefetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='webresource'>
                    <attribute name='webresourceid'/> 
                    <attribute name='name'/> 
                    <filter type='and'>
                    <condition attribute='name' operator='in' >{0}</condition>
                    </filter>
                    </entity>
                    </fetch>";
                StringBuilder nameSb = new StringBuilder();
                foreach (System.Xml.XmlNode webresource in webresources)
                {
                    string itemname = webresource.Attributes["id"].Value;
                    string result = webresource.FirstChild.Attributes["result"].Value;

                    if (result == "success")
                    {
                        nameSb.Append(string.Format("<value>{0}</value>", webresource.Attributes["id"].Value));
                    }
                }
                EntityCollection webresourceEntities = service.RetrieveMultiple(new FetchExpression(string.Format(webresourcefetch, nameSb.ToString())));
                if (webresourceEntities.Entities.Count > 0)
                {
                    XmlElement webresourceselement = publishdoc.CreateElement(string.Empty, "webresources", string.Empty);
                    rootelement.AppendChild(webresourceselement);
                    foreach (var entity in webresourceEntities.Entities)
                    {
                        XmlElement webresourceelement = publishdoc.CreateElement(string.Empty, "webresource", string.Empty);
                        webresourceelement.AppendChild(publishdoc.CreateTextNode(entity.Id.ToString()));
                        webresourceselement.AppendChild(webresourceelement);
                    }
                }
            }
            return publishdoc;
        }

        void ImportData(JObject datastep)
        {
            LogMessage("INFO", string.Format("Starting data import step"));
            string dataimportconfig = string.Format(@"{0}\{1}\{2}", _rootDir, _cImportDataDir, datastep["configpath"].ToString());
            LogMessage("INFO", string.Format("Data import config is {0}", dataimportconfig));

            string source = datastep["datasource"].ToString();
            if(source.ToUpper().EndsWith("JSON"))
            {
                source = string.Format(@"FILE={0}\{1}\{2}", _rootDir, _cImportDataDir, source);
            }
            LogMessage("INFO", string.Format("Data import source is {0}", source));

            DataImport import = new DataImport();
            import.ConfigFile = dataimportconfig;
            import.Source = source;
            import.Target = _targetString;

            import.Execute();
            LogMessage("INFO", string.Format("Data import step complete"));
        }

        void RunCommand(JObject commandstep)
        {
            LogMessage("INFO", string.Format("Starting command step"));
            System.Diagnostics.ProcessStartInfo startinfo = new System.Diagnostics.ProcessStartInfo();
            startinfo.FileName = commandstep["path"].ToString();
            startinfo.Arguments = commandstep["arguments"].ToString();
            startinfo.CreateNoWindow = false;// !(Convert.ToBoolean(commandstep["newwindow"].ToString()));
            System.Diagnostics.Process.Start(startinfo);
            LogMessage("INFO", string.Format("Command step complete"));
        }

        void ParseCrmConnection()
        {
            LogMessage("INFO", string.Format("Parsing CRM connection"));

            //strip out the password before we log the connection parameters
            StringBuilder targetSb = new StringBuilder();
            foreach(string item in ExtractConnectionParams(_targetString))
            {
                if(!item.ToUpper().StartsWith("PASSWORD"))
                {
                    targetSb.Append(string.Format("{0};", item));
                }
            }

            //log the connection parameters without password
            LogMessage("INFO", string.Format("target string: {0}", targetSb.ToString()));

            //create a new crm service client
            _targetClient = new CrmServiceClient(_targetString);
            
            //validate login works
            try
            {
                using (OrganizationServiceProxy service = _targetClient.OrganizationServiceProxy)
                {
                    //get the organization id
                    Guid orgId = ((WhoAmIResponse)service.Execute(new WhoAmIRequest())).OrganizationId;

                    LogMessage("INFO", "target version is - " + _targetClient.ConnectedOrgVersion.ToString());
                }
            }
            catch (Exception ex)
            {
                string errormsg = string.Format(string.Format("Could not validate target connection: {0}", ex.Message));
                LogMessage("ERROR", errormsg);
                throw new InvalidOperationException(errormsg);
            }
        }

        List<string> ExtractConnectionParams(string connstring)
        {
            string[] connectionparams = connstring.Split(";".ToCharArray());
            int counter = 0;
            List<string> paramlist = new List<string>();
            foreach (string param in connectionparams)
            {
                if (!string.IsNullOrWhiteSpace(param))
                {
                    if (param.Contains("="))
                    {
                        paramlist.Add(param);
                        counter++;
                    }
                    else
                    {
                        paramlist[counter - 1] = paramlist[counter - 1] + ";" + param;
                    }
                }
            }

            return paramlist;
        }
    }
}
