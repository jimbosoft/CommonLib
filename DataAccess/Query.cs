using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace CommonLib.DataAccess
{
    /// <summary>
    /// Retrieve data from the data store using the
    /// properties of this class.
    /// </summary>
    [Serializable]
    public class Query : DataOperation
    {
        public enum StringSearchType
        {
            EXACT,
            STARTSWITH,
            ENDSWITH,
            CONTAINS
        }

        public Query(UInt32 cid, String classname)
            : base(OperationType.QUERY, cid, classname)
        {

        }

        public Query(SerializationInfo info, StreamingContext con)
            : base(info, con)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext con)
        {
            base.GetObjectData(info, con);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
