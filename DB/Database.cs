using System;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Threading;
using System.Diagnostics;

using CommonLib.Utilities;//WEASL.Logging;

namespace CommonLib.DB
{
	public enum DataProvider
	{
		UNDEFINED,
		MSSQL,
		MSSQL_OLEDB,
		CACHE,
	}

	/// <summary>
	/// Class Database encapsulates the functionality to perform the common data access 
	/// operations. This class is used by other applications for connecting to the database 
	/// and reading configuration settings or error information from the database.
	/// </summary>
	public class Database
	{
		private bool			m_waiting			= false;
		private AutoResetEvent	m_waitingSig		= null;
        // TODO:: Add SNMP Alerts back into the Database code
		//private SNMPAlert		m_alert				= null;

		private DataProvider	m_provider			= DataProvider.CACHE;
		private string			m_connectionString	= string.Empty;
		private string			m_cacheNamespace	= String.Empty;

		private static Database m_instance = null;

		/// <summary>
		/// If an instance of Database class does not exist, it initialises a new instance.
		/// </summary>
		/// <returns>Returns the instance of Database class.</returns>
		public static Database GetInstance()
		{
			if(m_instance == null)
			{
				m_instance = new Database();
			}

			return m_instance;
		}

		/// <summary>
		/// Initializes a new instance of Database object containing an instance of 
		/// AutoResetEvent class with a boolean value which indicates that the initial state 
		/// is not signalled.
		/// </summary>
		public Database()
		{
			m_waitingSig = new AutoResetEvent(false);
		}

		/// <summary>
		/// Gets or sets the SNMPAlert object.
		/// </summary>
		//public SNMPAlert SNMP
		//{
		//	get { return m_alert; }
		//	set { m_alert = value; }
		//}

		/// <summary>
		/// Gets or sets the value of dataprovider.
		/// </summary>
		public DataProvider DataProvider
		{
			get { return m_provider; }
			set { m_provider = value; }
		}

		/// <summary>
		/// Gets or sets the value of connection string.
		/// </summary>
		public string ConnectionString
		{
			get { return m_connectionString; }
			set { m_connectionString = value; }
		}

		/// <summary>
		/// Gets or sets the value of CacheNamespace.
		/// </summary>
		public string CacheNamespace
		{
			get { return m_cacheNamespace; }
			set { m_cacheNamespace = value; }
		}

		/// <summary>
		/// Establishes a connection to the datasource. Opens the connection with the settings
		/// specified by the connection string. If connection to datasource opens successfully 
		/// then it returns true.
		/// </summary>
		/// <returns>Returns true or false.</returns>
		public bool IsDatabaseUp()
		{
			bool returnStatus = false;

			IDbConnection cn = null;

			try
			{
				cn = GetConnection();
				cn.Open();
				returnStatus = true;
			}
			catch ( Exception )
			{
				returnStatus = false;
			}
			finally
			{
				cn.Close();
			}

			return returnStatus;
		}

		/// <summary>
		/// If database is not running, the connecting application waits for a certain
		/// time until either database connection is up or it gets a signal to wait for a 
		/// certain time period. It sends an alert message that database server is down. 
		/// 
		/// </summary>
		/// <param name="sleepPeriod">The timeperiod when connecting application waits.</param>
		/// <param name="firstAlertDelay">The first time when alert delay happens.</param>
		/// <param name="alertDelay">The subsequent alert delays.</param>
		/// <param name="application">The name of connecting application</param>
		/// <returns>Returns the status about database connection.</returns>
		public bool StartWaitingForDb(int sleepPeriod, int firstAlertDelay, int alertDelay, string application)
		{
			bool returnStatus = false;

			bool eventLogged = false;
			DateTime lastAlert = DateTime.MinValue;
			DateTime startTime = DateTime.Now;
			m_waiting = true;

			while ( m_waiting == true )
			{
				returnStatus = IsDatabaseUp();

				if ( returnStatus == false && eventLogged == false )
				{
					if ( EventLog.SourceExists(application) == true )
					{
						EventLog.WriteEntry(application, "Database server connection down", EventLogEntryType.Warning);
						eventLogged = true;
					}
				}

				if ( m_waiting == false )
				{
					returnStatus = false;
				}
				else
				{
					if ( returnStatus == false )
					{
						if ( DateTime.Now.Subtract(TimeSpan.FromMilliseconds(firstAlertDelay)) > startTime &&
							DateTime.Now.Subtract(TimeSpan.FromMilliseconds(alertDelay)) > lastAlert )
						{
							//if ( m_alert != null )
							//{
							//	m_alert.Send(application, AlertSeverity.Warning, "Database server connection down");
							//}
                            
							lastAlert = DateTime.Now;
						}

						m_waitingSig.WaitOne(sleepPeriod, false);
					}
					else
					{
						m_waiting = false;
					}
				}
			}

			if ( returnStatus == true && eventLogged == true )
			{
				if ( EventLog.SourceExists(application) == true )
				{
					EventLog.WriteEntry(application, "Database server connection up", EventLogEntryType.Information);
				}
			}

			return returnStatus;
		}

		/// <summary>
		/// The return value indicates that whether application is still waiting for 
		/// database connection or not.
		/// </summary>
		/// <returns>Returns a true or false value.</returns>
		public bool IsWaitingForDb()
		{
			return m_waiting;
		}

		/// <summary>
		/// The waiting application is signalled and stops waiting for database connection. ??
		/// </summary>
		public void StopWaitingForDb()
		{
			m_waiting = false;

			if ( m_waitingSig != null )
			{
				m_waitingSig.Set();
			}
		}

		private void PreparCommand(IDbCommand cmd, SqlParameter[] commandParameters)
		{
			if ( m_provider == DataProvider.CACHE )
			{
				string commandText = string.Format("CALL {0}.{1}", m_cacheNamespace, cmd.CommandText);

				if ( commandParameters != null )
				{
					int paramIndex = 0;
					StringBuilder strBuilder = new StringBuilder(commandText);

					strBuilder.Append(" (");

					foreach(SqlParameter p in commandParameters)
					{
						OdbcParameter param = new OdbcParameter();
						param.ParameterName = p.ParameterName;
						param.OdbcType = ConvertType(p.SqlDbType);
						param.Value = p.Value;

						cmd.Parameters.Add(param);
						paramIndex++;


						if ( paramIndex == 1 )
						{
							strBuilder.Append("?");
						}
						else
						{
							strBuilder.Append(",?");
						}
					}

					strBuilder.Append(")");
					commandText = strBuilder.ToString();
				}

				cmd.CommandText = commandText;
			}
			else if ( m_provider == DataProvider.MSSQL )
			{
				if ( commandParameters != null )
				{
					foreach(SqlParameter p in commandParameters)
					{
						cmd.Parameters.Add(p);
					}
				}
			}
		}

		private OdbcType ConvertType(SqlDbType sqlType)
		{
			OdbcType returnType;

			switch ( sqlType )
			{
				case SqlDbType.Image:
				{
					returnType = OdbcType.Image;
					break;
				}

				case SqlDbType.Int:
				{
					returnType = OdbcType.Int;
					break;
				}

				case SqlDbType.BigInt:
				{
					returnType = OdbcType.BigInt;
					break;
				}

				case SqlDbType.Bit:
				{
					returnType = OdbcType.Bit;
					break;
				}

				case SqlDbType.DateTime:
				{
					returnType = OdbcType.DateTime;
					break;
				}

				case SqlDbType.VarChar:
				{
					returnType = OdbcType.VarChar;
					break;
				}

				case SqlDbType.TinyInt:
				{
					returnType = OdbcType.TinyInt;
					break;
				}

				case SqlDbType.Char:
				{
					returnType = OdbcType.Char;
					break;
				}

				case SqlDbType.SmallInt:
				{
					returnType = OdbcType.SmallInt;
					break;
				}

                case SqlDbType.Decimal:
                {
                    returnType = OdbcType.Decimal;
                    break;
                }

				default:
				{
					returnType = (OdbcType) sqlType;
					break;
				}
			}

			return returnType;
		}

		private IDbConnection GetConnection()
		{	
			IDbConnection cn = null;
			
			switch ( m_provider )
			{
				case DataProvider.MSSQL: 
				{
					cn = new SqlConnection(m_connectionString);
					break;
				}

				case DataProvider.MSSQL_OLEDB:
				{
					cn = new OleDbConnection(m_connectionString);
					break;
				}

				case DataProvider.CACHE:
				{
					cn = new OdbcConnection(m_connectionString);
					break;
				}
			}

			return cn;
		}

		/// <summary>
		/// This method establishes a connection with the datasource. Creates a command and 
		/// a dataAdapter object. Initialises a Dataset and populates it from the datasource
		/// by executing the command of dataAdapter object.
		/// </summary>
		/// <param name="commandType">Specifies how a command string is interpreted.</param>
		/// <param name="commandText">The command in string format. It could be the 
		/// Stored Proc name.</param>
		/// <param name="commandParameters">??</param>
		/// <returns>Returns the dataset object to the client application.</returns>
		public DataSet ExecuteDataSet(CommandType commandType, string commandText, params SqlParameter[] commandParameters)
		{
			IDbConnection cn = null;
			IDataAdapter da = null;
			IDbCommand cmd = null;
			DataSet ds = null;

			using ( cn = GetConnection() )
			{
				try
				{
					cn.Open();

					cmd = cn.CreateCommand();
					cmd.CommandType = commandType;
					cmd.CommandText = commandText;

					if ( commandType == CommandType.StoredProcedure )
					{
						PreparCommand(cmd, commandParameters);
					}

					ds = new DataSet();

					switch ( m_provider )
					{
						case DataProvider.MSSQL:
						{
							da = new SqlDataAdapter((SqlCommand) cmd);
							break;
						}

						case DataProvider.MSSQL_OLEDB:
						{
							da = new OleDbDataAdapter((OleDbCommand) cmd);
							break;
						}

						case DataProvider.CACHE:
						{
							da = new OdbcDataAdapter((OdbcCommand) cmd);
							break;
						}
					}

					da.Fill(ds);
				}
				catch ( Exception ex )
				{
					if(m_provider == DataProvider.CACHE)
					{
						OdbcConnection.ReleaseObjectPool();
					}
					throw ex;
				}
				finally
				{
					if(m_provider == DataProvider.MSSQL)
					{
						cmd.Parameters.Clear();
					}

					if ( cn != null )
						cn.Close();
				}
			}

			return ds;
		}

		/// <summary>
		/// Establishes the connection with the datasource in order to have a read only 
		/// access to database. Sends the commandText to the connection and builds a 
		/// DataReader object using one of the commandBehaviour values. 
		/// </summary>
		/// <param name="commandType">Specifies how a command string is interpreted. The 
		/// commandType property is set to Stored proc</param>
		/// <param name="commandText">The command in string format. It is the Stored Proc name.</param>
		/// <param name="commandParameters">Represents a parameter to a SqlCommand, and 
		/// optionally, its mapping to DataSet columns.</param>
		/// <returns>Returns the result of the query from database.</returns>
		public IDataReader ExecuteDataReader(CommandType commandType, string commandText, params SqlParameter[] commandParameters)
		{
			IDataReader returnValue = null;
			IDbConnection cn = null;
			IDbCommand cmd = null;
			
			// Do not use 'using' on connection as the dataReader needs the connection
			// open to read through the data records. The connection will be closed
			// when the dataReader is closed.
			try
			{
				cn  = GetConnection();
				cn.Open();

				cmd = cn.CreateCommand();
				cmd.CommandType = commandType;
				cmd.CommandText = commandText;

				if ( commandType == CommandType.StoredProcedure )
				{
					PreparCommand(cmd, commandParameters);
				}

				returnValue = cmd.ExecuteReader(CommandBehavior.CloseConnection);
			}
			catch ( Exception ex )
			{
				if(m_provider == DataProvider.CACHE)
				{
					OdbcConnection.ReleaseObjectPool();
				}
				throw ex;
			}
			finally
			{
				if(m_provider == DataProvider.MSSQL)
				{
					cmd.Parameters.Clear();
				}
			}

			return returnValue;
		}

		/// <summary>
		/// Establishes the connection with the datasource. Executes the query, and returns
		/// the first column of the first row in the resultset returned by the query. Extra 
		/// columns or rows are ignored.
		/// </summary>
		/// <param name="commandType">Specifies how a command string is interpreted. The 
		/// commandType property is set to Stored proc</param>
		/// <param name="commandText">The command in string format. It is the Stored Proc name.</param>
		/// <param name="commandParameters">Represents a parameter to a SqlCommand, and 
		/// optionally, its mapping to DataSet columns.</param>
		/// <returns>Returns the resultset from the query ??</returns>
		public object ExecuteScalar(CommandType commandType, string commandText, params SqlParameter[] commandParameters)
		{
			object retVal = null;
			IDbConnection cn = null;
			IDbCommand cmd = null;

			using ( cn  = GetConnection() )
			{
				try
				{
					cn.Open();

					cmd = cn.CreateCommand();
					cmd.CommandType = commandType;
					cmd.CommandText = commandText;

					if ( commandType == CommandType.StoredProcedure )
					{
						PreparCommand(cmd, commandParameters);
					}

					retVal = cmd.ExecuteScalar();
				}
				catch ( Exception ex )
				{
					if(m_provider == DataProvider.CACHE)
					{
						OdbcConnection.ReleaseObjectPool();
					}
					throw ex;
				}
				finally
				{
					if ( cmd != null )
						cmd.Parameters.Clear();

					if ( cn != null )
						cn.Close();
				}
			}

			return retVal;
		}

		/// <summary>
		/// Establishes the connection with the datasource. Executes a Transact-SQL statement
		/// against the connection and returns the number of rows affected.
		/// </summary>
		/// <param name="commandType">Specifies how a command string is interpreted. The 
		/// commandType property is set to Stored proc</param>
		/// <param name="commandText">The command in string format. It is the Stored Proc 
		/// name.</param>
		/// <param name="commandParameters">Represents a parameter to a SqlCommand, and 
		/// optionally, its mapping to DataSet columns.</param>
		/// <returns>The number of rows affected.</returns>
		public int ExecuteNonQuery(CommandType commandType, string commandText, params SqlParameter[] commandParameters)
		{
			int retVal = -1;
			IDbConnection cn = null;
			IDbCommand cmd = null;
		
			using( cn = GetConnection() )
			{
				try
				{
					cn.Open();

					cmd = cn.CreateCommand();
					cmd.CommandType = commandType;
					cmd.CommandText = commandText;

					if ( commandType == CommandType.StoredProcedure )
					{
						PreparCommand(cmd, commandParameters);
					}

					retVal = cmd.ExecuteNonQuery();
				}
				catch ( Exception ex )
				{
					if(m_provider == DataProvider.CACHE)
					{
						OdbcConnection.ReleaseObjectPool();
					}
					throw ex;
				}
				finally
				{
					if ( cmd != null )
						cmd.Parameters.Clear();

					if ( cn != null )
						cn.Close();
				}
			}

			return retVal;
		}
	}
}
