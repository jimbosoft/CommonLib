using System;

namespace CommonLib.Utilities
{
	public enum LogLevel : int
	{
		Debug   = 0,
		Info    = 1,
		Warning = 2,
		Error   = 3,
		Fatal   = 4
	}
	/// <summary>
	/// Defines the interface all logging class will implement
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// write a log entry
		/// </summary>
		/// <param name="Id">used for sorting and filtering</param>
		/// <param name="strModule">The name of the application that is writing to the log</param>
		/// <param name="strMessage">The message string that is being written to the log.</param>
		/// <param name="lt">The severity of log the message</param>
		void Write(int strModule, string strObject, string strMessage, LogLevel lt);
	}
}
