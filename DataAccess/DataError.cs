using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.DataAccess
{
    public enum DBOperationErrorCode
    {
        ESuccess = 0,
        EDataStoreInvalid, // data store is not correctly set in a factory
        ECreateResultFailed, // error creating the data result
        EInvalidOperation, // operation class id registered with wrong factory
        EUnregisteredOperation, // operation class id not registered with DataAccess instance
        EOperationRegoFailed, // registration failed for operation
        EUknownError,
        ELast
    }

    /// <summary>
    /// Error results are set for success and failure
    /// The default is success, code = 0 and string = null
    /// </summary>
    public class DataError
    {
        private int m_ErrorCode = 0;
        private String m_ErrorString = null;

        public int Code
        {
            get { return m_ErrorCode; }
        }

        public String Message
        {
            get { return m_ErrorString; }
        }

        public DataError(int code, String desc)
        {
            m_ErrorCode = code;
            m_ErrorString = desc;
        }

        public override string ToString()
        {
            return "[" + m_ErrorCode.ToString() + "] " + m_ErrorString;
        }

        public static String ErrorString(DBOperationErrorCode code)
        {
            switch (code)
            {
                case DBOperationErrorCode.ESuccess:
                    { return "Success."; }
                case DBOperationErrorCode.EDataStoreInvalid:
                    { return "Unknown database type."; }
                case DBOperationErrorCode.ECreateResultFailed:
                    { return "Factory unable to create result for data operation."; }
                case DBOperationErrorCode.EInvalidOperation: // wrong factory registered for operation
                    { return "Database operation associated with wrong transformation class."; }
                case DBOperationErrorCode.EUnregisteredOperation:
                    { return "Database operation has not been implemented for this database type."; }
                case DBOperationErrorCode.EOperationRegoFailed:
                    { return "Database operation registration failed."; }
                case DBOperationErrorCode.EUknownError:
                    { return "Unknown database error."; }
                default:
                    { return "Unknown error."; }
            }
        }

        public DataError(DBOperationErrorCode code)
        {
            m_ErrorCode = (int)code;
            m_ErrorString = ErrorString(code);
        }

        public DataError(DBOperationErrorCode code, String msg)
        {
            m_ErrorCode = (int)code;
            m_ErrorString = msg;
        }

    }

}
