using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.DataAccess
{

    /// <summary>
    /// Interface for data object populated by the result factory.
    /// </summary>
    public interface IDataObject
    {
        // nothing here yet jut a place holder to ensure all
        // a objects returned have a common denominator
        // possibly for future use?
    }

    /// <summary>
    /// Populate an instance of this class with the results 
    /// from accessing the data store, either to retrieve data
    /// or update data in the data store.
    /// </summary>
    public class DataResult
    {
        // Result data is implemented in child classes where data is returned.  
        // Updates/Inserts/Deletions do not normally return any data other 
        // than a success/failure indicator (see ErrorResult).

        // Error indicator
        private DataError m_Error;

        public DataError Error
        {
            get { return m_Error; }
            set { m_Error = value; }
        }

        // create a new result object
        public DataResult()
        {
            m_Error = null;
        }

    }


}
