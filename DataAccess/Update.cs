using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace CommonLib.DataAccess
{
    /// <summary>
    /// Updates/Deletes data from the data store.
    /// </summary>
    public class Update : DataOperation
    {
        public Update(UInt32 cid, String classname)
            : base(OperationType.UPDATE, cid, classname)
        {
        }

        public Update(SerializationInfo info, StreamingContext con)
            : base(info,con)
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
