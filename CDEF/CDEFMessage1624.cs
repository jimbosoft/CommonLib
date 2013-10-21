using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// RLDB – Sequence Number Notification : 0x1624
	/// </summary>
	public class CDEFMessage1624 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected UInt32	mSequenceNumber = 0;
		protected TimeLord	mRacedayVersion = new TimeLord(); // NOTE: stored in message as Int64
		protected int			mRetryTime = 0; // Default

		#endregion

		#region Constructors

		public CDEFMessage1624() : base()
		{
			mFunctionCode = (Int16)FuncCode.RLDB_SNNotification;
		}

		public CDEFMessage1624(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage1624(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public UInt32 SequenceNumber
		{
			get { return mSequenceNumber; }
			set	{ mSequenceNumber = value; }
		}

		public DateTime RacedayVersion
		{
			get	{ return mRacedayVersion.dtDateTime; }
			set	{ mRacedayVersion = new TimeLord(value); }
		}

		public int RetryTime
		{
			get	{ return mRetryTime; }
			set { mRetryTime = value; }
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
					mSequenceNumber = mCDEFReader.ReadUInt32();
					mRacedayVersion.utcDateTime = mCDEFReader.ReadInt64();
					mRetryTime = mCDEFReader.ReadInt32();

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
					mCDEFWriter.Write(mSequenceNumber);
					mCDEFWriter.Write(mRacedayVersion.utcDateTime);
					mCDEFWriter.Write(mRetryTime);

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
				sb.Append("SequenceNumber=" + mSequenceNumber + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}