using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.DataAccess
{
    /// <summary>
    /// Generate a data access result from the data access object
    /// Inherit from this class to generate a result for a specific data access
    /// </summary>
    public class ResultFactoryBase
    {
        private DataAccess m_Data = null;

        // read only
        public DataAccess Data
        {
            get { return m_Data; }
        }

        public ResultFactoryBase()
        {
        }

        public ResultFactoryBase(DataAccess da)
        {
            this.m_Data = da;
        }

        public virtual DataResult CreateResult()
        {
            DataResult rslt = new DataResult();
            return rslt;
        }

        public virtual void PreExecute(DataOperation dataOperation, ref DataResult rslt)
        {
            rslt = CreateResult();
        }

        public virtual void PostExecute(DataOperation dataOperation, ref DataResult rslt)
        {
        }

        public virtual void ExecuteOperation(DataOperation dataOperation, ref DataResult rslt)
        {
        }

        public virtual DataResult Execute(DataOperation dataOperation)
        {
            DataResult rslt = null;
            PreExecute(dataOperation, ref rslt);
            ExecuteOperation(dataOperation, ref rslt);
            PostExecute(dataOperation, ref rslt);
            return rslt;
        }
    }
}
