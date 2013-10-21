using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace CommonLib.Utilities
{
	/// <summary>
	/// Applications use Logger class to write messages into the log file.
	/// </summary>
	public sealed class Logger :ILogger	
	{
		public int DEFAULT_LOG_SIZE = 1024 * 1024 * 5; // 5M
		private string mstrLogPath = "";

		private string mstrLogCaller = "";
		private FileStream mLogStream = null;
		private StreamWriter mLogWriter = null;
		private LogLevel mltLogLevel = LogLevel.Info;
		private ILogger mNextLogger = null;

		/// <summary>
		/// Initializes a new instance of Logger class that contains the string value of 
		/// Log directory.
		/// </summary>
		/// <param name="strLogPath">The name of log directory.</param>
		public Logger(string strLogPath)
		{
			Construction(strLogPath, @"N/A", true, LogLevel.Info, false);
		}

		/// <summary>
		/// Initializes a new instance of Logger class that contains the string values of 
		/// Log directory and name of application that is using the logger functionality.
		/// </summary>
		/// <param name="strLogPath">The name of log directory.</param>
		/// <param name="strLogCaller">The name of application that uses the logger class.</param>
		public Logger(string strLogPath, string strLogCaller)
		{
			Construction(strLogPath, strLogCaller, true, LogLevel.Info, false);
		}

		/// <summary>
		/// Initializes a new instance of Logger class that contains the string values of 
		/// Log directory, name of application using the logging functionality and the type
		/// of Log.
		/// </summary>
		/// <param name="strLogPath">The name of log directory.</param>
		/// <param name="strLogCaller">The name of application that uses the logger class.</param>
		/// <param name="ltLogLevel">The type of log which is an enum value.</param>
		public Logger(string strLogPath, string strLogCaller, LogLevel ltLogLevel)
		{
			Construction(strLogPath, strLogCaller, true, ltLogLevel, false);
		}

		/// <summary>
		/// Initializes a new instance of Logger class that contains the string values of 
		/// Log directory, name of application using the logging functionality and a boolean
		/// value used to send buffered data to stream.
		/// </summary>
		/// <param name="strLogPath">The name of log directory.</param>
		/// <param name="strLogCaller">The name of application that uses the logger class.</param>
		/// <param name="bAutoFlush">If set to true, the buffered data is sent to stream.</param>
		public Logger(string strLogPath, string strLogCaller, bool bAutoFlush)
		{
			Construction(strLogPath, strLogCaller, bAutoFlush, LogLevel.Info, false);
		}

		/// <summary>
		/// Initializes a new instance of Logger class that contains the string values of
		/// Log directory, name of application using the logging functionality, a boolean 
		/// value to ?? and the log type ??.
		/// </summary>
		/// <param name="strLogPath"></param>
		/// <param name="strLogCaller"></param>
		/// <param name="bAutoFlush"></param>
		/// <param name="ltLogLevel"></param>
		public Logger(string strLogPath, string strLogCaller, bool bAutoFlush, LogLevel ltLogLevel)
		{
			Construction(strLogPath, strLogCaller, bAutoFlush, ltLogLevel, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="strLogPath"></param>
		/// <param name="strLogCaller"></param>
		/// <param name="bAutoFlush"></param>
		/// <param name="ltLogLevel"></param>
		/// <param name="bRollover"></param>
		public Logger(string strLogPath, string strLogCaller, bool bAutoFlush, LogLevel ltLogLevel, bool bRollover)
		{
			Construction(strLogPath, strLogCaller, bAutoFlush, ltLogLevel, bRollover);
		}

		public Logger(string strLogPath, string strLogCaller, bool bAutoFlush, 
						LogLevel ltLogLevel, bool bRollover, ILogger nextLogger)
		{
			mNextLogger = nextLogger;
			Construction(strLogPath, strLogCaller, bAutoFlush, ltLogLevel, bRollover);
		}
		private void Construction(string strLogPath, string strLogCaller, bool bAutoFlush, LogLevel ltLogLevel, bool bRollover)
		{
			// if the path doesn't end with a forward slash, add it!
			if(strLogPath.EndsWith(@"\") == false) { strLogPath += @"\"; }

			// assign logger variables...
			mstrLogPath		= strLogPath;
			mstrLogCaller	= strLogCaller.ToUpper();
			mltLogLevel		= ltLogLevel;

			// how the log starts up varies if it is in rollover...
			if(bRollover == true)
			{
				Initialise(LogState.Rollover, bAutoFlush);
			}
			else
			{
				Initialise(LogState.Startup, bAutoFlush);
			}
		}

		/// <summary>
		/// Gets or Sets the value of LogLevel.
		/// </summary>
		public LogLevel LogLevel
		{
			get { return mltLogLevel; }
			set { mltLogLevel = value; }
		}

		private void Initialise(LogState flag, bool flush)
		{	
			try
			{
				lock(this)
				{
					// generate the filename & path
					string strFilePath = GetFilePath(flag);

					// if the directory doesn't already exist, create it!
					if(!Directory.Exists(mstrLogPath)) { Directory.CreateDirectory(mstrLogPath); }

					// if already accessing a file, close off the logger
					if(mLogStream != null)
					{
						mLogWriter.Close();
						mLogStream.Close();
						mLogWriter = null;
						mLogStream = null;
					}

					// instantiate the stream and and writer...
					mLogStream = new FileStream(strFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
					mLogWriter = new StreamWriter(mLogStream);
					mLogWriter.AutoFlush = true;

					// point to the end of the file...
					mLogWriter.BaseStream.Seek(0, SeekOrigin.End);
				}
			}
			catch(Exception)
			{
				//EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);
			}
		}

		/// <summary>
		/// Writes the log messsage to the log file.
		/// </summary>
		/// <param name="strMessage"></param>
		public void Write(string strMessage)
		{	
			Write(@"N/A", strMessage, LogLevel.Info);
		}


		/// <summary>
		/// Writes the log message to the log file. 
		/// </summary>
		/// <param name="strModule">The name of the application that is writing to the log</param>
		/// <param name="strMessage">The message string that is being written to the log.</param>
		public void Write(string strModule, string strMessage)
		{	
			Write(strModule, strMessage, LogLevel.Info);
		}
		
		/// <summary>
		/// Builds the log message and writes it to the log file.
		/// </summary>
		/// <param name="strModule">The name of the application that is writing to the log</param>
		/// <param name="strMessage">The message string that is being written to the log.</param>
		/// <param name="lt">The type of log. ??</param>
		public void Write(string strModule, string strMessage, LogLevel lt)
		{
			Write(-1, strModule, strMessage, lt);
		}
		
		#region ILogger Members
		public void Write(int strModule, string strObject, string strMessage
						, CommonLib.Utilities.LogLevel lt)
		{
			//
			// Chain loggers together
			//
			if (mNextLogger != null)
			{
				mNextLogger.Write(strModule, strObject, strMessage, lt);
			}
			// Write to the stream, ONLY if log level is accepted
			if(lt >= mltLogLevel)
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					DateTime dt = DateTime.Now;

					// add log info first...
                    sb.Append(getLogType(lt).Substring(0,3) + "|");
                    sb.Append(dt.ToString("yy-MM-dd HH:mm:ss.") + dt.Millisecond.ToString("D03") + "|");
					sb.Append(mstrLogCaller + "|");
					sb.Append(strModule.ToString() + "|");
					sb.Append(strObject + "\t");
					sb.Append(strMessage);

					lock(this)
					{
						if (mLogWriter.BaseStream.Length > DEFAULT_LOG_SIZE)
						{
							Initialise(LogState.Rollover, true);
						}
						mLogWriter.WriteLine(sb.ToString());
					}

					sb = null;
				}
				catch(Exception)
				{
					//EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);
				}
			}
		}

		#endregion

		private string GetFilePath(LogState flag)
		{
			string strReturnValue = ""; // return value

			uint uiSort = 0, uiTempSort = 0; // temp sorting file index
			DateTime dt = DateTime.Now; // get current date time

			string searchPattern = // prepare search pattern
				dt.ToString("yyyy-MM-dd_") +
				mstrLogCaller + "_";

			// create log directory if not available
			if(Directory.Exists(mstrLogPath) == false) { Directory.CreateDirectory(mstrLogPath); }

			// get a file list
			string[] strFileList = Directory.GetFiles(mstrLogPath, searchPattern + "*.LOG");

			strReturnValue = mstrLogPath + searchPattern + "0.LOG";
			FileInfo fi = new FileInfo(strReturnValue);
			// 
			// If there are several files and this one already exists
			// try to fomulate a new file name
			//
			if((strFileList.Length != 0) && fi.Exists)
			{
				// loop through strFileList
				foreach(string strItem in strFileList)
				{
					// split file name to get ?? from 2002-12-08_STL_??.LOG
					string[] tmpArray = strItem.Split('_', '.'); 
					
					try
					{
						if((uiTempSort = System.Convert.ToUInt32(tmpArray[tmpArray.Length - 2])) >= uiSort)
						{
							uiSort = uiTempSort;
							if(flag != LogState.Rollover) { strReturnValue = strItem; } 
							else { strReturnValue = mstrLogPath + searchPattern + ++uiSort + ".LOG"; }
						}
					}
					catch
					{
						//
						// If this fails, assume the file does not exist
						// Can happen when somebody renamed a file and the above expodes
						//
						strReturnValue = mstrLogPath + searchPattern + "0.LOG";
					}
				}
			}

			return strReturnValue;
		}

		public static string getLogType(LogLevel lt)
		{
			string strReturnValue;

			switch(lt)
			{
				case LogLevel.Debug:   { strReturnValue = "DEBUG  "; } break;
				case LogLevel.Info:    { strReturnValue = "INFO   "; } break;
				case LogLevel.Warning: { strReturnValue = "WARNING"; } break;
				case LogLevel.Error:   { strReturnValue = "ERROR  "; } break;
				case LogLevel.Fatal:   { strReturnValue = "FATAL  "; } break;
				default:               { strReturnValue = "UNKNOWN"; }	break;
			}

			return strReturnValue;
		}

		/// <summary>
		/// Reset the logger and start again - good for creating a new log file.
		/// </summary>
		public void Rollover()
		{
			Initialise(LogState.Rollover, mLogWriter.AutoFlush);
		}

		/// <summary>
		/// Clears all the buffers for the current writer and causes any buffered data
		/// to be written to the underlying stream.
		/// </summary>
		public void Flush()
		{
			try
			{
				lock(this)
				{
					mLogWriter.Flush();
				}
			}
			catch(Exception)
			{
				//EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);
			}
		}

		/// <summary>
		/// Closes the Stream Writer and the underlying stream. it also closes the file and 
		/// releases all of the resources associated with the current file stream.
		/// </summary>
		public void Close()
		{	
			try
			{
				lock(this)
				{
					// if already accessing a file, close off the logger
					if(mLogStream != null)
					{
						mLogWriter.Close();
						mLogStream.Close();
						mLogWriter = null;
						mLogStream = null;
					}

				}
			}
			catch(Exception)
			{
				//EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);
			}
		}
	}

	public enum LogState
	{
		Startup, 
		Rollover,
	}
}