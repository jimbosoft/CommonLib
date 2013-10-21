using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// RLDB – Sequence Number Query : 0x1608
	/// </summary>
	public class CDEFMessage1608 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected UInt32	mFirstSequenceNumber = 0;
		protected UInt32	mLastSequenceNumber = 0;
		protected TimeLord	mMessageTime = new TimeLord(); // NOTE: stored in message as Int64

		#endregion

		#region Constructors

		public CDEFMessage1608() : base()
		{
			mFunctionCode = (Int16)FuncCode.RLDB_SNQuery;
		}

		public CDEFMessage1608(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage1608(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public UInt32 FirstSequenceNumber
		{
			get	{ return mFirstSequenceNumber; }
			set	{ mFirstSequenceNumber = value;	}
		}

		public UInt32 LastSequenceNumber
		{
			get	{ return mLastSequenceNumber; }
			set	{ mLastSequenceNumber = value; }
		}

		public DateTime MessageTime
		{
			get	{ return mMessageTime.dtDateTime; }
			set	{ mMessageTime.dtDateTime = value; }
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
					mFirstSequenceNumber = mCDEFReader.ReadUInt32();
					mLastSequenceNumber = mCDEFReader.ReadUInt32();
					mMessageTime.utcDateTime = mCDEFReader.ReadInt64();

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
					mCDEFWriter.Write(mFirstSequenceNumber);
					mCDEFWriter.Write(mLastSequenceNumber);
					mCDEFWriter.Write(mMessageTime.utcDateTime);

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
				sb.Append("FirstSequenceNumber=" + mFirstSequenceNumber + "\r\n");
				sb.Append("LasySequenceNumber=" + mLastSequenceNumber + "\r\n\r\n");
				sb.Append("MessageTime=" + mMessageTime + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
