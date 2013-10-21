using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Threading;

namespace CommonLib.DataAccess
{
    /// <summary>
    /// Categorise data operations into operations that
    /// inspect/read (QUERY) the data and operations that 
    /// change/write (UPDATE) the data.
    /// </summary>
    public enum OperationType
    {
        QUERY, // query the data store for specific data (query)
        UPDATE // change the data store contents (update/delete/insert)
    }

    /// <summary>
    /// A base class for accessing data in the data store.
    /// </summary>
    [Serializable]
    public class DataOperation : ISerializable
    {
        // the type of access operation
        private OperationType m_OperationType;

        // maintain a unique id for all data accesses
        // TODO:: Load last known value on startup
        // This can be used as a unique id for each
        // entry in a transaction log.
        private static UInt64 s_InstanceId = 0;
        private static object myMutex = new object();
        private UInt64 m_InstanceId = 0;
        private DateTime m_InstanceCreated = DateTime.Now;
        private UInt64 m_ExecuteId = 0;
        private DateTime m_OperationExecuted = DateTime.MinValue;
        private DateTime m_OperationCompleted = DateTime.MinValue;

        // uniquely identifies the operation class
        private UInt32 m_ClassId;
        private String m_ClassName;

        /// <summary>
        /// Reset the Instance counter to a given value.
        /// The next value will be one greater than this value.
        /// </summary>
        /// <param name="startId"></param>
        public static void ResetInstanceCount(UInt64 startId)
        {
            lock (myMutex)
            {
                s_InstanceId = startId;
            }
        }

        /// <summary>
        /// Unique identifier for when this operation
        /// was executed.
        /// </summary>
        public UInt64 ExecuteId
        {
            get { return m_ExecuteId; }
            set { m_ExecuteId = value; }
        }

        public DateTime ExecutionTimeStamp
        {
            get { return m_OperationExecuted; }
            set { m_OperationExecuted = value; }
        }

        /// <summary>
        /// Unique instance of this data operation object
        /// </summary>
        public UInt64 InstanceId
        {
            get { return m_InstanceId; }
        }

        public DateTime InstanceTimeStamp
        {
            get { return m_InstanceCreated; }
        }

        public DateTime Completed
        {
            get { return m_OperationCompleted; }
            set 
            {
                if (value >= m_InstanceCreated)
                {
                    m_OperationCompleted = value;
                }
                else
                {
                    throw new InvalidOperationException(
                        "DataOperation (ID" + m_InstanceId.ToString() + ") " 
                        + m_ClassName + "[" + m_ClassId.ToString()
                        + "] completed date is prior to creation date."
                        );
                }
            }
        }

        public OperationType Type
        {
            get { return m_OperationType; }
        }

        public UInt32 ClassId
        {
            get { return m_ClassId; }
        }

        public String ClassName
        {
            get { return m_ClassName; }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext con)
        {
            info.AddValue("_ClassId", m_ClassId);
            info.AddValue("_ClassName", m_ClassName);
            info.AddValue("_OperationType", m_OperationType);
            info.AddValue("_OperationUID", m_InstanceId);
            info.AddValue("_ExecuteUID", m_ExecuteId);
            info.AddValue("_Created", m_InstanceCreated);
            info.AddValue("_Executed", m_OperationExecuted);
            info.AddValue("_Completed", m_OperationCompleted);
        }

        /// <summary>
        /// Construct from a stream of serialized data
        /// </summary>
        /// <param name="info"></param>
        /// <param name="con"></param>
        public DataOperation(SerializationInfo info, StreamingContext con)
        {
            m_ClassId = info.GetUInt32("_ClassId");
            m_ClassName = info.GetString("_ClassName");
            // WARNING:: enum deserialize causes an exception
            // serialization of an enum prints the string 
            // representation of the enum value, so we need to 
            // deserialize to a string and convert to the relvant
            // enum value.
            // ENUM Deserialization - read as string and convert string to ENUM value
            String s = info.GetString("_OperationType");
            m_OperationType = (OperationType)Enum.Parse(typeof(OperationType), s, true);
            m_InstanceId = info.GetUInt64("_OperationUID");
            m_ExecuteId = info.GetUInt64("_ExecuteUID");

            String dt = info.GetString("_Created");
            DateTime.TryParse(dt, null
                , System.Globalization.DateTimeStyles.AssumeLocal
                , out m_InstanceCreated);
            dt = info.GetString("_Executed");
            DateTime.TryParse(dt, null
                , System.Globalization.DateTimeStyles.AssumeLocal
                , out m_OperationExecuted);
            dt = info.GetString("_Completed");
            DateTime.TryParse(dt, null
                , System.Globalization.DateTimeStyles.AssumeLocal
                , out m_OperationCompleted);
            // deserialization resets the static instance count
            // from the input stream, all previous data is overwritten
            lock (myMutex)
            {
                if (s_InstanceId < m_InstanceId)
                {
                    s_InstanceId = m_InstanceId;
                }
            }
        }

        /// <summary>
        /// Child provides the type of access object.
        /// </summary>
        /// <param name="ot">Type of access object</param>
        /// <param name="cid">Unique class identifier</param>
        public DataOperation(OperationType ot, UInt32 cid, String classname)
        {
            this.m_ClassId = cid;
            this.m_ClassName = classname;
            this.m_OperationType = ot;
            lock (myMutex)
            {
                this.m_InstanceId = ++s_InstanceId;
            }
            this.m_InstanceCreated = DateTime.Now;
            this.m_OperationCompleted = DateTime.MinValue;
        }

        // default constructor was added for XmlSerializer using typeof()
        public DataOperation()
        {
        }

    }

  
}
