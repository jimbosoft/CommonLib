using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.DataAccess
{
    /// <summary>
    /// A query result is a list of rows containing one 
    /// or more columns.
    /// </summary>
    public class QueryResult<T> : DataResult // where T : BaseObject
    {
        // should I use a map/vector/array? indexed by row number?
        private List<T> m_Results;

        // provide a read only result
        // data changes should be saved using an UpdateData object
        //public ReadOnlyCollection<T> Rows
        //{
        //    get { return m_Results.AsReadOnly(); }
        //}
        // PROBLEM: issue with read only!
        // need write access from within the factory object
        // used to create the results.
        public List<T> Rows
        {
            get { return m_Results; }
        }

        public QueryResult()
            : base()
        {
            m_Results = new List<T>();
        }

        public bool Refresh()
        {
            return true;
        }
    }

}
