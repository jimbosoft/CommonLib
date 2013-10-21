using System;
using System.Diagnostics;

namespace CommonLib.Utilities
{
    public class SNMPAlertConfig
    {
        public string m_path = string.Empty;
        public string m_targetNode = string.Empty;
        public string m_enterpriseID = string.Empty;
        public string m_agentAddress = string.Empty;
        public string m_genericTrap = string.Empty;
        public string m_specificTrap = string.Empty;
        public string m_timeStamp = string.Empty;
        public string m_objectID = string.Empty;
    }
	public enum AlertSeverity : byte
	{
		Fatal	= 0x46, // F
		Warning	= 0x57, // W
		Info	= 0x49  // I
	}

	/// <summary>
	/// When an error occurs which is written to error database 
	/// (WISNET_Errors), the SNMP Alert class is used to read the error message from 
	/// error database and sends an alert with error number to the log file.
	/// </summary>
    public class SNMPAlert
    {
        private ILogger m_errorLog;
        private string m_path = string.Empty;
        private string m_arguments = string.Empty;

        /// <summary>
        /// Initialises a SNMPAlert object with certain configuration values either read 
        /// from database or from a config file. If an empty SNMPAlert object is created, then 
        /// it reads configuration settings from the app.exe.config file. If the SNMPAlert object 
        /// contains the Database object then it reads the configuration settings from
        /// the database configuration table.
        /// </summary>
        /// <returns>A boolean value.</returns>
        public SNMPAlert(ILogger logger, SNMPAlertConfig config)
        {
            m_errorLog = logger;
            m_path = config.m_path;

            m_arguments = String.Format("{0} {1} {2} {3} {4} {5} {6} octetstringascii ", 
                config.m_targetNode, config.m_enterpriseID, config.m_agentAddress, config.m_genericTrap, 
                config.m_specificTrap, config.m_timeStamp, config.m_objectID);
        }
        /// <summary>
        /// Sends an SNMPAlert message with application name, severity level and a message 
        /// string.
        /// </summary>
        /// <param name="application">The application name which sends an alert message.</param>
        /// <param name="severity">The level of alert severity.</param>
        /// <param name="message">A string that contains the error message.</param>
        /// <returns> Returns true if successfully sends the message otherwise returns false.</returns>
        public bool Send(string application, AlertSeverity severity, string message)
        {
            bool bReturnValue = false;
            try
            {
                // build the message string
                string tmp = String.Format("OCM {0} {1} 0 {2}", application, (char)(byte)severity, message);

                // call the CATRAP exe
                Process.Start(m_path, m_arguments + "\"" + tmp + "\"");

                // success if we get this far
                bReturnValue = true;
            }
            catch (Exception ex)
            {
                m_errorLog.Write(0, "SNMPAlert", "Sending SNMP alert failed: " + ex.Message
                    + " app message: " + message, LogLevel.Error);
            }
            return bReturnValue;
        }
    }
}