using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//using System.Linq;
using System.Text;
using System.IO;
using CommonLib.Utilities;

namespace CommonLib.DataAccess
{
    /// <summary>
    /// A database event
    /// </summary>
    public class DBEvent
    {
    }

    public enum DataEventType
    {
        COLCHANGED, // a spacific column value has changed
        ROWDELETED, // a specific row has been "logically" deleted - physically still on disk
        ROWCHANGED, // part or all of a specific row has changed
        ROWADDED, // a new row has been added to a specific table
        STOREDPROC, // a specific stored procedure was called
        ACCESS, // executed a query of specific data
        FILTER // a filter object/expression has different data output
    }

    public enum DataEventTiming
    {
        BEFORE, // Notify of event before it happens
        AFTER // Notify of event after it happens
    }

    public enum EntityType
    {
        ROW, // a specific row
        COLUMN, // a specific column value
        TABLE,
        QUERY,
        STOREDPROC
    }

    /// <summary>
    /// Data object containing the information for a specific event
    /// </summary>
    public class EventData
    {
        public EntityType m_Type; // type of data in the event
        public string EntityName; // table name, stored proc, query...
    }

    /// <summary>
    /// Delegate for when a specific database event occurs.
    /// </summary>
    /// <param name="et"></param>
    /// <param name="timing"></param>
    public delegate void DataEventCb( DataEventType et, DataEventTiming timing );

    /// <summary>
    /// A data event interface object used to register for 
    /// notifications relating to a specific data event.
    /// Inherit your own data event objects from this interface.
    /// </summary>
    public class DataEvent
    {
        public static string Name( DataEventType et )
        {
            switch (et)
            {
                case DataEventType.COLCHANGED: return "COLCHANGED";
                case DataEventType.ROWCHANGED: return "ROWCHANGED";
                case DataEventType.ROWDELETED: return "ROWDELETED";
                case DataEventType.ROWADDED: return "ROWADDED";
                    
                default: return "UNKNOWN";
            }
        }

        public string TypeName()
        {
            return Name(m_Type);
        }

        public DataEventType m_Type;
        public DataEventCb m_EventCb;
        public EventData m_Data;
    }

    public class ColumnChangedData : EventData
    {
    }

    public class ColumnChanged : DataEvent
    {
        public ColumnChanged()
        {
            m_Type = DataEventType.COLCHANGED;
        }
    }




    /// <summary>
    /// A data item that knows when its value has changed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataItem<T>
    {
        private bool m_changed = false;
        private T m_value;
        private String m_FIELDNAME; // name used in database queries

        public DataItem(String fieldName, T defValue)
        {
            m_changed = false;
            m_value = defValue;
            m_FIELDNAME = fieldName;
        }

        public T Data
        {
            get
            {
                return m_value;
            }
            set
            {
                m_changed = true;
                m_value = value;
            }
        }

        public bool IsSet
        {
            get { return (m_value != null); }
        }

        public bool HasChanged
        {
            get { return m_changed; }
        }

        public void ResetChanged()
        {
            m_changed = false;
        }
    }


    /// <summary>
    ///  Data storage access layer
    /// </summary>
    public class DataAccess
    {
        private static UInt64 s_ExecUID = 0;
        private static object myMutex = new object();
        private ILogger m_Log = null;
        private AccessLog m_AccessLog = null;
        private Dictionary<UInt32, ResultFactoryBase> m_ResultFactories;
        
        public Dictionary<UInt32, ResultFactoryBase> Factories
        {
            get { return m_ResultFactories; }
        }

        /// <summary>
        /// Log containing all operations executed.
        /// </summary>
        public ReadOnlyCollection<DataOperation> AccessLog
        {
            get { return m_AccessLog.LogOperations; }
        }

        public Boolean AutoLoadLastLog
        {
            get { return m_AccessLog.AutoLoadLastLog; }
            set { m_AccessLog.AutoLoadLastLog = value; }
        }

        /// <summary>
        /// Base filename for the access log.
        /// This should contain a fully qualified pathname.
        /// </summary>
        public String AccessBaseLogName
        {
            get { return m_AccessLog.BaseLogName; }
            set { m_AccessLog.BaseLogName = value; }
        }

        /// <summary>
        /// Actual filename for the access log.
        /// This is generated from BaseLogName.
        /// </summary>
        public String AccessLogName
        {
            get { return m_AccessLog.LogName; }
        }

        /// <summary>
        /// Rotate the access log by closing the current
        /// log and generating a new log from the original
        /// base log name.
        /// </summary>
        public void AccessLogRotate()
        {
            m_AccessLog.RotateLog();
        }

        /// <summary>
        /// Enable the transaction logger.
        /// All future executed data operations will
        /// be logged.
        /// </summary>
        public void EnableLogging()
        {
            m_AccessLog.LoggingState = LoggingState.LogEverything;
            m_AccessLog.Open();
        }

        /// <summary>
        /// Disable the transaction logger.
        /// All future executed data operations will NOT
        /// be logged.
        /// </summary>
        public void DisableLogging()
        {
            m_AccessLog.LoggingState = LoggingState.LogNothing;
            m_AccessLog.Close();
        }

        /// <summary>
        /// The last data operation to be successfully executed.
        /// </summary>
        public UInt64 LastOperationId
        {
            get { return m_AccessLog.LastOperationId; }
        }

        public static UInt64 LastExecuteId
        {
            get { return s_ExecUID; }
            set { s_ExecUID = value; }
        }

        public Boolean ReplayingLog
        {
            get { return m_AccessLog.ReplayingLog; }
        }

        /// <summary>
        /// Reload the data from an existing log file.
        /// WARNING: THIS WILL CLEAR THE CURRENT DATA!!
        /// </summary>
        /// <param name="filename">Full path and filename</param>
        public void ReplayLog(String filename)
        {
            // Load the log into a temporary AccessLog
            m_AccessLog.ReplayFromFile(filename);
        }

        /// <summary>
        /// Allow for a custom method for accepting a file
        /// from a triggered AccessLog.AutoLoadLastLog On Startup.
        /// </summary>
        /// <param name="logFile">Most recent log file found</param>
        /// <returns>Return false if you do not want to load the file.</returns>
        public virtual Boolean AcceptAutoLoadFile(FileInfo logFile)
        {
            Boolean ret = true;
            return ret;
        }

        /// <summary>
        /// Logging to main logger.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="msg"></param>
        /// <param name="level"></param>
        public virtual void Log(int moduleId, String module, String msg, LogLevel level)
        {
            if (m_Log != null)
            {
                m_Log.Write(moduleId, module, msg, level);
            }
        }

        /// <summary>
        /// Prepare for the application to shutdown.
        /// </summary>
        public virtual void Shutdown()
        {
        }

        /// <summary>
        /// Allows a reset to base for the database
        /// being managed though this interface.
        /// </summary>
        public virtual void Reset()
        {
            // reset the instance id counter
            // for data operations to its initial value
            DataOperation.ResetInstanceCount(0);
            lock (myMutex)
            {
                s_ExecUID = 0;
            }
        }

        /// <summary>
        /// Register a result factory with the data access instance.
        /// Result factories generate a result from a data operation.
        /// A result factory is registered with a given data operation
        /// as a one-to-one mapping.
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="rf"></param>
        public void RegisterResultFactory(UInt32 fid, ResultFactoryBase rf)
        {
            m_ResultFactories.Add(fid, rf);
            Log(0, "DataAccess"
                , "RegisterResultFactory: " + fid.ToString() + "=" + rf.GetType().Name
                , LogLevel.Debug);
        }

        /// <summary>
        /// Generic method for adding data operations and their related 
        /// result factory.
        /// </summary>
        /// <typeparam name="O"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="operation"></param>
        /// <param name="factory"></param>
        public void RegisterResultFactory<O, F>(O operation, F factory)
            where O : DataOperation
            where F : ResultFactoryBase
        {
            if (m_AccessLog != null)
            {
                m_AccessLog.RegisterOperation(operation);
            }
            m_ResultFactories.Add(operation.ClassId, factory);
            Log(0, "DataAccess"
                , "RegisterResultFactory: " + operation.ClassName + ":" + operation.ClassId.ToString()
                + "=" + factory.GetType().Name
                , LogLevel.Debug);
        }

        /// <summary>
        /// Register to receive callbacks when specific data access events
        /// are triggered, such as changes to a specific database value 
        /// </summary>
        /// <returns></returns>
        public bool RegisterEvent( DataEvent de )
        {
            // internally we can keep a map of event lists
            // IMPLEMENT LATER
            return true;
        }

        /// <summary>
        /// Generate a new unique execute identifier
        /// </summary>
        /// <returns></returns>
        public virtual UInt64 NewExecuteId()
        {
            lock (myMutex)
            {
                return ++s_ExecUID;
            }
        }

        /// <summary>
        /// Add an entry to the Access Log without
        /// executing the operation.
        /// </summary>
        /// <param name="obj"></param>
        public void LogDataOperation(DataOperation obj)
        {
            m_AccessLog.Add(obj);
        }

        /// <summary>
        /// Called immediately before calling:
        /// ResultFactory.Execute(DataOperation, DataResult)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="rf"></param>
        /// <param name="dr"></param>
        public virtual void PreExecute(
            DataOperation obj, ResultFactoryBase rf, ref DataResult dr)
        {
            obj.ExecuteId = NewExecuteId();
            obj.ExecutionTimeStamp = DateTime.Now;
            m_AccessLog.Add(obj); // add to log
        }

        /// <summary>
        /// Called immediately after calling:
        /// ResultFactory.Execute(DataOperation, DataResult)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="rf"></param>
        /// <param name="dr"></param>
        public virtual void PostExecute(
            DataOperation obj, ResultFactoryBase rf, ref DataResult dr)
        {
            obj.Completed = DateTime.Now;
            m_AccessLog.Completed(obj); // completed = write to log
        }

        /// <summary>
        /// Accepts a query object tht specifies what data is required.
        /// The query object will be returned with a result data set
        /// containing the query results.
        /// </summary>
        /// <param name="obj">Operation to perform</param>
        /// <param name="dr">Result of the operation</param>
        /// <returns></returns>
        public virtual bool Execute(DataOperation obj, ref DataResult dr)
        {
            bool rslt = false;
            if (Factories.ContainsKey(obj.ClassId))
            {
                ResultFactoryBase rf = Factories[obj.ClassId];
                if (rf != null)
                {
                    try
                    {
                        PreExecute(obj, rf, ref dr);
                        dr = rf.Execute(obj);
                        PostExecute(obj, rf, ref dr);
                        // execute created a result = true
                        rslt = (dr != null);
                    }
                    catch (NullDataStoreException)
                    {// DataStore is not of the correct type
                        dr = new DataResult();
                        dr.Error = new DataError(
                            DBOperationErrorCode.EDataStoreInvalid);
                    }
                    catch (Exception ex)
                    { // log any exceptions and return an error result
                        dr = new DataResult();
                        dr.Error = new DataError(
                            DBOperationErrorCode.EUknownError
                            , ex.Message);
                    }
                }
                else
                {
                    dr = new DataResult();
                    dr.Error = new DataError(
                        DBOperationErrorCode.EUnregisteredOperation);
                }
            }
            else
            {
                dr = new DataResult();
                dr.Error = new DataError(
                    DBOperationErrorCode.EUnregisteredOperation);
            }
            LogResult(obj, dr);
            return rslt;
        }

        /// <summary>
        /// Logs a message for each data operation that completes.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dr"></param>
        private void LogResult(DataOperation obj, DataResult dr)
        {
            if (dr != null)
            {
                if (dr.Error == null)
                {
                    Log(0, "DataAccess", obj.ClassName
                        + "[" + obj.ClassId.ToString() + "]"
                        + ", EID:" + obj.ExecuteId.ToString() 
                        + ": No error status returned."
                        , LogLevel.Error);
                }
                else if (dr.Error.Code != (Int32)DBOperationErrorCode.ESuccess)
                {
                    Log(0, "DataAccess", obj.ClassName 
                        + "["+obj.ClassId.ToString()+"]"
                        + ", EID:" + obj.ExecuteId.ToString() 
                        + ": Failure: "
                        + dr.Error.ToString()
                        , LogLevel.Error);
                }
                else
                {
                    //Log(0, "DataAccess", obj.ClassName
                    //    + "["+obj.ClassId.ToString()+"]"
                    //    + ", ExecID:" + obj.ExecuteId.ToString() 
                    //    + ": Success: "
                    //    + dr.Error.Message
                    //    , LogLevel.Debug);
                }
            }
            else
            {
                Log(0, "DataAccess", obj.ClassName
                    + "[" + obj.ClassId.ToString() + "]"
                    + ", EID:" + obj.ExecuteId.ToString() 
                    + ": No result returned."
                    , LogLevel.Error);
            }
        }

        /// <summary>
        /// Communication Layer will use this constructor.
        /// Session information is not required on creation
        /// but is loaded per transaction depending on the source
        /// of the transaction, ie which operator initiated the 
        /// message received via Bravo.
        /// eg SignOn and SignOff are initiated by the terminal operator
        /// CashBalance is related to a specific operator
        /// </summary>
        public DataAccess()
            : this(null)
        {
        }

        public DataAccess(ILogger log)
        {
            this.m_Log = log;
            this.m_AccessLog = new AccessLog(this);
            this.m_AccessLog.Logger = log;
            this.m_ResultFactories = new Dictionary<uint, ResultFactoryBase>();
        }

    }
}
