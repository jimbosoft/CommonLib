using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO; // Stream
using System.Runtime.Serialization; // ISerializationSurrogate
using System.Runtime.Serialization.Formatters.Binary; // BinaryFormatter
using System.Xml;
using System.Xml.Serialization;
using CommonLib.Utilities;

namespace CommonLib.DataAccess
{
    
    class LogSerialisationSurrogate<T> : ISerializationSurrogate
        where T : DataOperation
    {
        // serialise
        public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context)
        {
            T o = (T)obj;
            if (o != null)
            {
                o.GetObjectData(info, context);
            }
        }

        // deserialise
        public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector ss)
        {
            T o = (T)obj;
            if (o != null)
            {
                // call private constructor
                Type t = o.GetType();
                Type[] types = { info.GetType(), context.GetType() };
                Object[] objs = { info, context };
                o = (T)t.GetConstructor(types).Invoke(objs);
            }
            return o;
        }
    }

    public enum LoggingState
    {
        LogNothing,
        LogEverything,
        LogUpdates,
        LogQueries
    }

    /// <summary>
    /// Log containing all operations executed.
    /// Only capable of appending new operations
    /// and retrieving a readonly collection of
    /// operations that have been executed.
    /// </summary>
    public class AccessLog : IDisposable
    {
        private ILogger m_Logger = null;
        private DataAccess m_DataAccess = null;
        private DateTime m_Created = DateTime.Now;
        private List<DataOperation> m_Log = null;
        private String m_BaseFileName = String.Empty; // base name for building log filename
        private String m_PostExecLogFileName = String.Empty; // actual txt log filename
        // Serialising data operations to an output file
        private Stream m_Output = null;
        private TextFormatter m_TxtFormatter = new TextFormatter();
        //private SurrogateSelector m_SurrogateSelector = new SurrogateSelector();
        private StreamingContext m_StreamingContext = new StreamingContext(StreamingContextStates.All);

        // only output a debug file - contains all data operations
        // and is updated immediately prior to execution
        private Boolean m_PreExecuteLog = true;
        private Boolean m_PostExecuteLog = true;
        private String m_PreExecLogFileName = String.Empty;
        private Stream m_PreExecOutput = null;

        private UInt64 m_LastId = 0;
        private Boolean m_AutoLoadLastTransactionFile = true;

        /// <summary>
        /// Is the given Stream open?
        /// Can we either read or write to the stream?
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private Boolean IsOpen(Stream s)
        {
            return (
                s != null
                && (s.CanWrite || s.CanRead)
                );
        }

        /// <summary>
        /// Used to open a specific file and return a valid Stream 
        /// for accessing the file.
        /// Will open or create the file if it doesn't exist.
        /// Allows the file to be shared for read access.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Stream OpenFile(String filename)
        {
            Stream s = null;
            try
            {
                if (filename == null)
                {// use current filename if not provided
                    filename = m_PostExecLogFileName;
                }

                if (filename != null && filename.Length > 0)
                {
                    FileInfo fi = new FileInfo(filename);
                    if (!fi.Directory.Exists)
                    {
                        Log(0, "AccessLog"
                            , "File Open: creating directory " + fi.DirectoryName
                            , LogLevel.Info);
                        fi.Directory.Create();
                    }
                    if (!fi.Exists)
                    {
                        Log(0, "AccessLog"
                            , "File Open: creating file " + fi.FullName
                            , LogLevel.Info);
                        s = fi.Create();
                        if (s != null)
                        {
                            s.Close();
                        }
                    }
                    // open with read/write access
                    // and allow others to read the file
                    s = fi.Open(FileMode.OpenOrCreate
                        , FileAccess.ReadWrite, FileShare.Read);
                    //s = File.Open(filename, FileMode.OpenOrCreate
                    //    , FileAccess.ReadWrite, FileShare.Read);
                }
            }
            catch (Exception e)
            {
                // file could not be opened!
                Log(0, "AccessLog"
                    , "File Open Exception: " + e.Message
                    , LogLevel.Error);
            }
            return s;
        }

        /// <summary>
        /// Automatically load the last log file
        /// when a new base log file name is assigned.
        /// </summary>
        public Boolean AutoLoadLastLog
        {
            get { return m_AutoLoadLastTransactionFile; }
            set { m_AutoLoadLastTransactionFile = value; }
        }

        public String PreExecuteLogTag = "_PreExecute";
        public String PostExecuteLogTag = "_PostExecute";

        /// <summary>
        /// Configure the main logger for the application.
        /// </summary>
        public ILogger Logger
        {
            get { return m_Logger; }
            set { m_Logger = value; }
        }

        /// <summary>
        /// Turn transaction logging on and off
        /// </summary>
        public LoggingState LoggingState = LoggingState.LogEverything;

        /// <summary>
        /// Set the text formatter class for formatting the
        /// transaction log output.
        /// </summary>
        public TextFormatter TextFormatter
        {
            set { m_TxtFormatter = value; }
            get { return m_TxtFormatter; }
        }

        /// <summary>
        /// The last data operation to be successfully executed.
        /// ie Completed(op) was called.
        /// </summary>
        public UInt64 LastOperationId
        {
            get { return m_LastId; }
        }

        /// <summary>
        /// Read only access to the sequential list of log
        /// entries.
        /// </summary>
        public ReadOnlyCollection<DataOperation> LogOperations
        {
            get { return m_Log.AsReadOnly(); }
        }

        /// <summary>
        /// Set the base filename for the transaction logs.
        /// The base filename is used as a base for creating
        /// the actual filename used for the logs.
        /// A Pre/Post transaction log name is generated
        /// by appending the Pre/PostExecuteLogTag and the 
        /// current date and time.
        /// Eg. BaseFile_<yyyyMMdd_HHmmss><PreExecTag>.log
        /// 
        /// If AutoLoadLastLog is true, the last PreExec
        /// transaction log will be automatically loaded.
        /// The most recent is determined based on the file
        /// modified date time.
        /// </summary>
        public String BaseLogName
        {
            get { return m_BaseFileName; }
            set
            {
                // update the log creation date
                m_Created = DateTime.Now;
                Boolean ChangedName = (m_BaseFileName != value);
                Boolean FirstName = (m_BaseFileName.Length == 0);
                m_BaseFileName = value;
                if (m_BaseFileName.Length > 0)
                {
                    m_PostExecLogFileName = m_BaseFileName
                        + m_Created.ToString("_yyyyMMdd_HHmmss")
                        + PostExecuteLogTag + ".log";
                    m_PreExecLogFileName = m_BaseFileName
                        + m_Created.ToString("_yyyyMMdd_HHmmss")
                        + PreExecuteLogTag + ".log";
                }
                else
                {
                    m_PostExecLogFileName = String.Empty;
                    m_PreExecLogFileName = String.Empty;
                }

                if (m_AutoLoadLastTransactionFile && ChangedName && FirstName)
                {
                    // Only load if the base log name has changed and it is the 
                    // first name assigned, as this will only happen on startup.
                    // Setting the base file name is used to rotate the log, which 
                    // should never reload the previous log.
                    // Log rotate may happen at the start of a new race day?
                    LoadLastLogByName(m_BaseFileName);

                }
            }
        }

        /// <summary>
        /// Full log name for completed transactions.
        /// </summary>
        public String LogName
        {
            get { return m_PostExecLogFileName; }
        }

        /// <summary>
        /// Full log name for transactions that have been executed.
        /// These transactions have been executed but may not have 
        /// completed.
        /// </summary>
        public String PreExecuteLogName
        {
            get { return m_PreExecLogFileName; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AccessLog(DataAccess da)
        {
            this.m_DataAccess = da;
            this.m_Log = new List<DataOperation>();

            // Associate the SurrogateSelector with the BinaryFormatter.
            //this.m_TxtFormatter.SurrogateSelector = m_SurrogateSelector;  

            // set the serialization format
            this.TextFormatter.Storage = TextFormatter.StorageType.NameValue;
        }

        /// <summary>
        /// On destruction ensure the object resources are 
        /// released.
        /// </summary>
        ~AccessLog()
        {
            Dispose(false);
        }

        /// <summary>
        /// Ensure the file resources are closed.
        /// </summary>
        public void Dispose() 
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Ensure the log files are closed.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(Boolean disposing)
        {
            Close();
        }

        /// <summary>
        /// Logging to main logger for the application.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="msg"></param>
        /// <param name="level"></param>
        public virtual void Log(int moduleId, String module, String msg, LogLevel level)
        {
            if (m_Logger != null)
            {
                m_Logger.Write(moduleId, module, msg, level);
            }
        }

        /// <summary>
        /// Ask the question: Do I log this data operation?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="op"></param>
        /// <returns></returns>
        public Boolean LogOperation<T>(T op) where T : DataOperation
        {
            return (LoggingState == LoggingState.LogEverything
                || (LoggingState == LoggingState.LogUpdates && op.Type == OperationType.UPDATE)
                || (LoggingState == LoggingState.LogQueries && op.Type == OperationType.QUERY));
        }

        /// <summary>
        /// Add an operation to the transaction log before 
        /// executing the operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="op"></param>
        public void Add<T>(T op) where T : DataOperation
        {
            if (LogOperation(op))
            {
                m_Log.Add(op);
                
                if (m_PreExecuteLog)
                {
                    // logs before executing
                    if (IsOpen(m_PreExecOutput))
                    {
                        SerializeOperation(m_PreExecOutput, op);
                    }
                }
            }
        }

        /// <summary>
        /// Search backwards until the data operation is found
        /// or a threshold has been reached for the number of
        /// items that can be checked.  Default threshold is 
        /// infinite, ie search all items.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        private Boolean LogEntryExists(DataOperation op)
        {
            Boolean ret = false;
            if (m_Log.Count > 0)
            {
                UInt64 LogSearchThreshold = UInt64.MaxValue; // search all
                UInt64 notThisOneCount = 0;
                for (Int32 i = m_Log.Count - 1
                    ; i > 0 && notThisOneCount < LogSearchThreshold; --i)
                {
                    if (m_Log[i].InstanceId == op.InstanceId
                        && m_Log[i].ClassId == op.ClassId
                        && m_Log[i].ExecuteId == op.ExecuteId
                        && m_Log[i].InstanceTimeStamp == op.InstanceTimeStamp)
                    {
                        ret = true;
                        break;
                    }
                    notThisOneCount++;
                }
            }
            return ret;
        }

        /// <summary>
        /// Add an entry to the transaction log after completing
        /// the operation.  This should be called after calling 
        /// "Add<T>(T op)" for the same data operation.
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="op"></param>
        public void Completed<T>(T op) where T : DataOperation
        {
            if (LogOperation(op))
            {
                // add the entry if it does not exist
                // call Add<T>(T op) before calling this method.
                // current implementation should only need
                // to check the last item in the log.
                //if (!LogEntryExists(op))
                //{
                //    m_Log.Add(op);
                //}
                // log when a transaction completed
                if (IsOpen(m_Output))
                {
                    SerializeOperation(m_Output, op);
                }
            }
            m_LastId = op.InstanceId;
        }

        /// <summary>
        /// Close the open files.
        /// </summary>
        public void Close()
        {
            if (IsOpen(m_Output))
            {
                m_Output.Flush();
                m_Output.Close();
            }

            if (IsOpen(m_PreExecOutput))
            {
                m_PreExecOutput.Flush();
                m_PreExecOutput.Close();
            }
        }

        /// <summary>
        /// Is the log file open?
        /// </summary>
        /// <returns></returns>
        public Boolean IsOpen()
        {
            return (
                IsOpen(m_Output)
                || IsOpen(m_PreExecOutput)
                );
        }

        /// <summary>
        /// Open the current log file(s)
        /// The base filename is used as a base for creating
        /// the actual filename used for the log.
        /// </summary>
        public void Open()
        {
            // open new log
            if (m_PostExecuteLog)
            {
                if (m_PostExecLogFileName != null
                    && m_PostExecLogFileName.Length > 0)
                {
                    if (IsOpen(m_Output))
                    {
                        m_Output.Flush();
                        m_Output.Close();
                    }
                    m_Output = OpenFile(m_PostExecLogFileName);
                    if (m_Output != null && m_Output.CanSeek)
                    {
                        m_Output.Seek(0, SeekOrigin.End);
                    }
                }
            }
            if (m_PreExecuteLog)
            {
                if (m_PreExecLogFileName != null 
                    && m_PreExecLogFileName.Length > 0)
                {
                    if (IsOpen(m_PreExecOutput))
                    {
                        m_PreExecOutput.Flush();
                        m_PreExecOutput.Close();
                    }
                    m_PreExecOutput = OpenFile(m_PreExecLogFileName);
                    if (m_PreExecOutput != null && m_PreExecOutput.CanSeek)
                    {
                        m_PreExecOutput.Seek(0, SeekOrigin.End);
                    }
                }
            }
        }

        /// <summary>
        /// Rotate the log
        /// </summary>
        public void RotateLog()
        {
            // re-assign existing base filename
            // this should update to a newer actual name
            // then close and re-open the new log file
            Close();
            BaseLogName = m_BaseFileName;
            Open();
        }

        public void RegisterOperation<T>(T op)  where T : DataOperation
        {
            // Tell the SurrogateSelector that Employee objects are serialized and deserialized 
            // using the GenericSerializationSurrogate object.
            // BinaryFormatter used to output binary form of object
            //m_SurrogateSelector.AddSurrogate(typeof(T)
            //    , m_StreamingContext
            //    , new LogSerialisationSurrogate<T>());

            // register type for the text formatter
            // this is used to output a comma separated list of name-value pairs
            m_TxtFormatter.RegisterType(typeof(T));
        }

        /// <summary>
        /// Serialize DataOperation to file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="op"></param>
        public void SerializeOperation<T>(Stream strm, T op) where T : DataOperation
        {
            if (strm != null)
            {
                if (m_TxtFormatter != null)
                {
                    m_TxtFormatter.Serialize(strm, op);
                    strm.Flush();
                }
            }
        }

        public void Clear()
        {
            m_Log.Clear();
        }

        public virtual Boolean AcceptLogFile(FileInfo logFile)
        {
            Boolean ret = true;
            m_DataAccess.AcceptAutoLoadFile(logFile);
            return ret;
        }

        /// <summary>
        /// Locate the most recently changed existing transaction log 
        /// of the same base name as the current log and auto-load the 
        /// transaction file.
        /// 
        /// We can only restore from a PreExecute transaction file
        /// as we populate some values in the transaction during 
        /// execution which causes problems when replaying the log.
        /// Also note that replaying a log will not give the exact
        /// results of the original since the create/modify/delete
        /// time stamps are at the time of execution.
        /// </summary>
        /// <param name="BaseFileName"></param>
        private void LoadLastLogByName(String baseFileName)
        {
            if (baseFileName != null && baseFileName.Length > 0)
            {
                // restoring from a previous log file
                // requires full logging so we log everything 
                // that was in the previous log file
                LoggingState ls = LoggingState;
                LoggingState = LoggingState.LogEverything;
                Boolean logsOpen = IsOpen();
                try
                {
                    FileInfo bfi = new FileInfo(baseFileName);
                    String path = bfi.DirectoryName;
                    String name = bfi.Name;
                    Int32 pos = baseFileName.LastIndexOf('\\');
                    if (pos != -1)
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        FileInfo loadFile = null;
                        if (di.Exists)
                        {
                            FileInfo[] logfiles = di.GetFiles(
                                name + "*" + PreExecuteLogTag + ".log"
                                , SearchOption.TopDirectoryOnly);
                            foreach (FileInfo fi in logfiles)
                            {
                                // do not load the current log file
                                // only looking for a previous file
                                if (fi.FullName != m_PreExecLogFileName)
                                {
                                    if (loadFile == null)
                                    {
                                        loadFile = fi;
                                    }
                                    else if (fi.LastWriteTime > loadFile.LastWriteTime)
                                    {
                                        loadFile = fi;
                                    }
                                }
                            }
                        }
                        if (loadFile != null && loadFile.Exists)
                        {
                            if (AcceptLogFile(loadFile))
                            {
                                if (!logsOpen)
                                {
                                    Open();
                                }
                                Stream s = loadFile.OpenRead();
                                try
                                {
                                    ReplayFromStream(s);
                                }
                                finally
                                {
                                    s.Close();
                                }
                            }
                        }
                    }
                }
                finally
                { // restore existing loggin state
                    LoggingState = ls;
                    if (!logsOpen && IsOpen())
                    { // restore logs to previous state of closed
                        Close();
                    }
                }
            }
        }

        /// <summary>
        /// Load DataOperations from an existing file.
        /// IMPORTANT: Clears existing transactions and starts new log!
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Boolean ReplayFromFile(String filename)
        {
            Boolean ret = false;
            // open the file
            Stream s = OpenFile(filename);
            if (s != null)
            {
                try
                {
                    // replay existing and get ready to append
                    ret = ReplayFromStream(s);
                }
                finally
                {
                    s.Close();
                }
            }
            return ret;
        }

        private Boolean m_ReplayLog = false;

        public Boolean ReplayingLog
        {
            get { return m_ReplayLog; }
        }

        /// <summary>
        /// Load DataOperations from a stream.
        /// IMPORTANT: Clears existing transactions and starts new log!
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Boolean ReplayFromStream(Stream s)
        {
            Boolean ret = false;
            // replay existing and get ready to append
            if (IsOpen(s) && s.Length > 0)
            {
                // clear current transaction log
                Clear();
                // start a new transaction log
                RotateLog();
                // read transactions from file
                Int64 pos = 0;
                pos = s.Seek(pos, SeekOrigin.Begin);
                if (s.Position == pos)
                {
                    try
                    {
                        m_ReplayLog = true;
                        DataOperation op = (DataOperation)m_TxtFormatter.Deserialize(s);
                        while (op != null)
                        {
                            DataResult dr = null;
                            m_DataAccess.Execute(op, ref dr);
                            op = (DataOperation)m_TxtFormatter.Deserialize(s);
                        }
                        ret = true;
                    }
                    catch (Exception ex)
                    {
                        if (m_Log.Count > 0)
                        {
                            Log(0, "AccessLog", "Error replaying file: " + ex.Message
                                + " [Last ExecuteID:"
                                + m_Log[m_Log.Count - 1].ExecuteId.ToString()
                                + "] [LastRead:" + m_TxtFormatter.LastLineRead + "]"
                                , LogLevel.Error);
                        }
                        else
                        {
                            Log(0, "AccessLog", "Error replaying file: " + ex.Message
                                + " [LastRead:" + m_TxtFormatter.LastLineRead + "]"
                                , LogLevel.Error);
                        }
                        ret = false;
                    }
                    m_ReplayLog = false;
                }
                else
                {
                    ret = true;
                }
            }

            return ret;
        }

    }
}
