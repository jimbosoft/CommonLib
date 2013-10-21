using System;
using System.Reflection;
using System.Collections;
using System.Xml;
using System.Configuration;
using System.Collections.Generic;

namespace CommonLib.Utilities
{
    public class ConfigReader
    {
        public class ConfigStringResult
        {
            public string resultVal = "";
            public bool valid = false;
            public void SetValue(string val)
            {
                resultVal = val;
                valid = true;
            }
        }
        public class ConfigIntResult
        {
            public int resultVal = 0;
            public bool valid = false;
            public void SetValue(int val)
            {
                resultVal = val;
                valid = true;
            }
        }
        private XmlDocument mDoc;
        private IDictionary mSettings;
        private string mNodeName = "appSettings"; //"configuration"; //appSettings
        private ILogger mErrorLog;
        private bool mRecordReads = false;

        public ConfigReader()
        {
            mSettings = ReadConfig(Assembly.GetCallingAssembly());
        }
        public void RegisterLogfile(ILogger logger, bool recordReads)
        {
            mErrorLog = logger;
            mRecordReads = recordReads;
        }
        public ConfigStringResult GetStringValue(string key)
        {
            ConfigStringResult val = new ConfigStringResult();

            try
            {
                if (mSettings.Contains(key))
                {
                    val.SetValue((string)mSettings[key]);
                    if (mRecordReads && mErrorLog != null)
                    {
                        WriteError("Key=" + key + " Value=" + val.resultVal, LogLevel.Info);
                    }
                }
                else
                {
                        WriteError("Key=" + key + " was NOT FOUND in the configuration", LogLevel.Error);
                }
            }
            catch (Exception e)
            {
                if (!WriteError("GetStringValue failed: " + e.ToString(), LogLevel.Error))
                {
                    throw e;
                }
            }
            return val;
        }
        public ConfigIntResult GetIntValue(string key)
        {
            ConfigIntResult val = new ConfigIntResult();

            try
            {
                if (mSettings.Contains(key))
                {
                    int res = int.Parse((string)mSettings[key]);
                    val.SetValue(res);
                    if (mRecordReads && mErrorLog != null)
                    {
                        WriteError("Key=" + key + " Value=" + val.resultVal.ToString(), LogLevel.Info);
                    }
                }
                else
                {
                    WriteError("Key=" + key + " was NOT FOUND in the configuration", LogLevel.Error);
                }
            }
            catch (Exception e)
            {
                if (!WriteError("Key=" + key + " value was not an integer: " + e.ToString(), LogLevel.Error))
                {
                    throw e;
                }
            }
            return val;
        }
        //----------------------------------------------------------------
        public List<string> GetNode(string nodeName)
        {
            List<string> valueList = new List<string>();
            XmlNodeList nodes = mDoc.GetElementsByTagName(nodeName);
            //now we need to loop through all the
            //nodes in the XML document
            foreach (XmlNode node in nodes)
            {
                //now check to see if the name of the node
                //in this iteration is the same as our global
                //nodeName variable
                if (node.LocalName == nodeName)
                {
                    foreach (XmlNode cnode in node.ChildNodes)
                    {
                        valueList.Add(cnode.InnerText.Trim());
                    }
                }
            }
            return valueList;
        }
        //----------------------------------------------------------------
        // <summary>
        /// method to open and parse the config
        /// file for the provided assembly
        /// </summary>
        /// <param name="asm">Assembly's config file to parse</param>
        /// <returns></returns>
        public IDictionary ReadConfig(Assembly asm)
        {
            try
            {
                //string to hold the name of the
                //config file for the assembly
                string assemblyName =  asm.CodeBase;
                int idx = assemblyName.IndexOf('.');
                string cfgFile = assemblyName.Substring(0, idx) + ".config";

                //create a new XML Document
                mDoc = new XmlDocument();
                //load an XML document by using the
                //XMLTextReader class of the XML Namespace
                //yo open the sfgFile
                mDoc.Load(new XmlTextReader(cfgFile));
                //retrieve a list of nodes in the document
                XmlNodeList nodes = mDoc.GetElementsByTagName(mNodeName);
                //now we need to loop through all the
                //nodes in the XML document
                foreach (XmlNode node in nodes)
                {
                    //now check to see if the name of the node
                    //in this iteration is the same as our global
                    //nodeName variable
                    if (node.LocalName == mNodeName)
                    {
                        //since they match we need to use the
                        //DictionarySectionHandler and create a
                        //new handler and add it to the collection
                        DictionarySectionHandler handler = new DictionarySectionHandler();
                        object o = handler.Create(null, null, node);
                        //rerutn the new handler
                        return (IDictionary)o;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return (null);
        }
        private bool WriteError(string msg, LogLevel level )
        {
            if (mErrorLog != null)
            {
                mErrorLog.Write(0, "ConfigReader", msg, level);
                return true;
            }
            return false;
        }
    }
}
