using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// RLDB – Request Multiple Previous Days Results : 0x16E1
	/// </summary>
	public class CDEFMessage16E1 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected bool		mAllDays = false;
		protected TimeLord	mDate = new TimeLord(); // NOTE: stored in message as UInt16

		#endregion

		#region Constructors

		public CDEFMessage16E1() : base()
		{
			mFunctionCode = (Int16)FuncCode.RLDB_ReqMultiplePrevDaysResults;
		}

		public CDEFMessage16E1(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage16E1(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public DateTime Date
		{
			get	{ return mDate.dtDateTime.Date; }
			set	{ mDate = new TimeLord(value); }
		}

		public UInt16 U16Date
		{
			get	{ return mDate.ui16Date; }
		}

		public bool AllDaysRequired
		{
			get	{ return mAllDays; }
			set	{ mAllDays = value; }
		}

		#endregion

		#region Method Overrides

		protected override void parseMessage()
		{
			// read the header...
			base.parseMessage();

			if(mCDEFStatus == CDEFStatus.OK)
			{
				try
				{
					// parse the message...
					mFunctionFlags = mCDEFReader.ReadByte();
					mAllDays = mCDEFReader.ReadBoolean();
					mDate.ui16Date = mCDEFReader.ReadUInt16();
				
					// get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception e)
				{
					EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", e.Message, e.StackTrace), EventLogEntryType.Error);

					// set the current length to zero
					mLength = 0;

					// set the status...
					mCDEFStatus = CDEFStatus.ParseError;
				}
			}
		}

		protected override void buildMessage()
		{
			// build the header...
			base.buildMessage();

			if(mCDEFStatus == CDEFStatus.OK)
			{
				try
				{
					// build the message...
					mCDEFWriter.Write(mFunctionFlags);
					mCDEFWriter.Write(mAllDays);
					mCDEFWriter.Write(mDate.ui16Date);

					// get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception e)
				{
					EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", e.Message, e.StackTrace), EventLogEntryType.Error);

					// set the current length to zero
					mLength = 0;

					// set the status...
					mCDEFStatus = CDEFStatus.BuildError;
				}
			}
		}

		public override string ToStringEx()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(base.ToStringEx());

			if(mCDEFStatus == CDEFStatus.OK)
			{
				sb.Append("\r\nFunctionFlags=" + mFunctionFlags + "\r\n\r\n");
				sb.Append("Date=" + mDate + "\r\n\r\n");
				sb.Append("AllDaysRequired=" + mAllDays.ToString() + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
