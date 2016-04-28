using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlexanderDevelopment.ConfigDataMover.Lib;
using System.Xml;
using System.IO;


namespace AlexanderDevelopment.CrmDeploymentWizard.Lib
{
    public class DataImport
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public string ConfigFile { get; set; }

        static string _sourceString = null;
        static string _targetString = null;
        static bool _mapBaseBu = false;
        static bool _mapBaseCurrency = false;
        static List<GuidMapping> _guidMappings = new List<GuidMapping>();
        static List<JobStep> _jobSteps = new List<JobStep>();

        public void Execute()
        {
            //parse the config file
            ParseConfig(ConfigFile);

            //set source/target connection from parameters if specified - this will overwrite connections from the config file
            if (!string.IsNullOrEmpty(Source))
            {
                _sourceString = Source;
            }
            if (!string.IsNullOrEmpty(Target))
            {
                _targetString = Target;
            }

            //do some basic validations
            if (string.IsNullOrEmpty(_sourceString))
            {
                throw new InvalidOperationException("no data source connection specified");
            }
            if (string.IsNullOrEmpty(_targetString))
            {
                throw new InvalidOperationException("no data target connection specified");
            }
            if (!(_jobSteps.Count > 0))
            {
                throw new InvalidOperationException("no steps in data job step");
            }

            Importer importer = new Importer();
            importer.GuidMappings = _guidMappings;
            importer.JobSteps = _jobSteps;
            importer.SourceString = _sourceString;
            importer.TargetString = _targetString;
            importer.MapBaseBu = _mapBaseBu;
            importer.MapBaseCurrency = _mapBaseCurrency;
            importer.StopLogging = false;
            importer.Process();
            int errorCount = importer.ErrorCount;

            importer = null;

            ////show a message to the user
            //if (errorCount == 0)
            //{
            //    Console.WriteLine("Job finished with no errors.");
            //}
            //else
            //{
            //    Console.WriteLine("Job finished with errors. See the RecordError.log file for more details.");
            //}
        }
        

        void ParseConfig(string filepath)
        {
            StreamReader sr = new StreamReader(filepath);
            string jobdata = (sr.ReadToEnd());
            sr.Close();

            XmlDocument xml = new XmlDocument();
            try
            {
                xml.LoadXml(jobdata);
                _jobSteps.Clear();
                _guidMappings.Clear();

                XmlNodeList stepList = xml.GetElementsByTagName("Step");
                foreach (XmlNode xn in stepList)
                {
                    JobStep step = new JobStep();
                    step.StepName = xn.SelectSingleNode("Name").InnerText;
                    step.StepFetch = xn.SelectSingleNode("Fetch").InnerText;
                    step.UpdateOnly = Convert.ToBoolean(xn.Attributes["updateOnly"].Value);

                    _jobSteps.Add(step);
                }

                XmlNodeList configData = xml.GetElementsByTagName("JobConfig");
                _mapBaseBu = Convert.ToBoolean(configData[0].Attributes["mapBuGuid"].Value);
                _mapBaseCurrency = Convert.ToBoolean(configData[0].Attributes["mapCurrencyGuid"].Value);

                XmlNodeList mappingList = xml.GetElementsByTagName("GuidMapping");
                foreach (XmlNode xn in mappingList)
                {
                    Guid sourceGuid = new Guid(xn.Attributes["source"].Value);
                    Guid targetGuid = new Guid(xn.Attributes["target"].Value);
                    _guidMappings.Add(new GuidMapping { sourceId = sourceGuid, targetId = targetGuid });
                }
                XmlNodeList connectionNodes = xml.GetElementsByTagName("ConnectionDetails");
                if (connectionNodes.Count > 0)
                {
                    _sourceString = connectionNodes[0].Attributes["source"].Value;
                    _targetString = connectionNodes[0].Attributes["target"].Value;
                    //Console.WriteLine(connectionNodes[0].InnerText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Could not parse job configuration data in {0} - exiting", filepath));
            }
        }
    }
}

