using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// SportsBet Sequence Number Query : 0x3107
	/// </summary>
	public class CDEFMessage3107 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected byte		mSportsbetUpdateType = 0;
		protected Int32		mFirstSequenceNumber = 0;
		protected Int32		mLastSequenceNumber = 0;

		#endregion

		#region Constructors

		public CDEFMessage3107() : base()
		{
			mFunctionCode = (Int16)FuncCode.SB_SNQuery;
		}

		public CDEFMessage3107(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage3107(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get { return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public byte SportsbetUpdateType
		{
			get	{ return mSportsbetUpdateType; }
			set { mSportsbetUpdateType = value;	}
		}

		public Int32 FirstSequenceNumber
		{
			get { return mFirstSequenceNumber; }
			set	{ mFirstSequenceNumber = value;	}
		}

		public Int32 LastSequenceNumber
		{
			get	{ return mLastSequenceNumber; }
			set	{ mLastSequenceNumber = value; }
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
					mSportsbetUpdateType = mCDEFReader.ReadByte();
					mFirstSequenceNumber = mCDEFReader.ReadInt32();
					mLastSequenceNumber = mCDEFReader.ReadInt32();

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
					mCDEFWriter.Write(mSportsbetUpdateType);
					mCDEFWriter.Write(mFirstSequenceNumber);
					mCDEFWriter.Write(mLastSequenceNumber);

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
				sb.Append("SportsbetUpdateType=" + mSportsbetUpdateType + "\r\n\r\n");
				sb.Append("FirstSequenceNumber=" + mFirstSequenceNumber + "\r\n");
				sb.Append("LastSequenceNumber=" + mLastSequenceNumber + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
