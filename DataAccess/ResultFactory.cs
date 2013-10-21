using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.DataAccess
{
    public class NullDataStoreException : ApplicationException
    {
    }

    /// <summary>
    /// Create a type specific results factory.
    /// 
    /// The DataAccess component is used to Execute
    /// DataOperation instances to generate a Result.
    /// 
    /// The Default constructor is used to create a Result.
    /// </summary>
    /// <typeparam name="T">Data operation type</typeparam>
    /// <typeparam name="R">Result type</typeparam>
    /// <typeparam name="D">DataAccess child class type</typeparam>
    public class ResultFactory<T, R, D> : ResultFactoryBase
        where T : DataOperation
        where R : DataResult, new()
        where D : DataAccess
    {
        public ResultFactory()
        {
        }

        public ResultFactory(D da)
            : base(da)
        {
        }

        public D DataStore
        {
            get 
            {
                D ret = Data as D;
                if (ret == null)
                {
                    throw new NullDataStoreException();
                }
                return ret; 
            }
        }

        public virtual R CreateR()
        {
            R ret = new R();
            return ret;
        }

        public override DataResult CreateResult()
        {
            R rslt = CreateR();
            return rslt as DataResult;
        }

        public override void PreExecute(DataOperation op, ref DataResult dr)
        {
            T top = op as T;
            if (top != null)
            {
                base.PreExecute(op, ref dr);
            }
            else
            {
                // invalid operation for this factory
                dr = new DataResult();
                dr.Error = new DataError(
                    DBOperationErrorCode.EInvalidOperation);
            }
        }

        public override void ExecuteOperation(DataOperation op, ref DataResult dr)
        {
            base.ExecuteOperation(op, ref dr);
            // PreExecute guarantees operation is correct type
            T top = op as T; 
            R rslt = dr as R;
            if (rslt != null)
            {
                PopulateResult(ref rslt, top);
                dr = rslt;
            }
            else
            {
                dr = new DataResult();
                dr.Error = new DataError(
                    DBOperationErrorCode.ECreateResultFailed);
            }
        }

        public override void PostExecute(DataOperation op, ref DataResult rslt)
        {
            base.PostExecute(op, ref rslt);
        }

        public override DataResult Execute(DataOperation op)
        {
            DataResult ret = null;
            PreExecute(op, ref ret);
            if (op is T && ret is R)
            {
                ExecuteOperation(op, ref ret);
            }
            PostExecute(op, ref ret);
            return ret;
        }

        /// <summary>
        /// Override this method to populate the result object that 
        /// has been created.
        /// 
        /// It is guaranteed that the result has been created prior
        /// to executing this method.
        /// </summary>
        /// <param name="?"></param>
        /// <param name="dataOperation"></param>
        public virtual void PopulateResult(ref R rslt, T op)
        {
            // populate the result from the data access object
        }
    }
}
