using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// RLDB - Request Results Sequence Number Range : 0x1621
	/// </summary>
	public class CDEFMessage1621 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected TimeLord	mDate = new TimeLord(); // NOTE: stored in message as UInt16
		protected byte		mVenueCode = 0;
		protected byte		mTypeCode = 0;
		protected byte		mEventNumber = 0;
		protected UInt32	mFirstSequenceNumber = 0;
		protected UInt32	mLastSequenceNumber = 0;

		#endregion

		#region Constructors

		public CDEFMessage1621() : base()
		{
			mFunctionCode = (Int16)FuncCode.RLDB_RequestResultsSNRange;
		}

		public CDEFMessage1621(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage1621(CDEFMessage newCDEF) : base(newCDEF)
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
			get { return mDate.dtDateTime.Date; }
			set { mDate = new TimeLord(value); }
		}

		public UInt16 U16Date
		{
			get { return mDate.ui16Date; }
			set { mDate = new TimeLord(value); }
		}

		public byte VenueCode
		{
			get { return mVenueCode; }
			set	{ mVenueCode = value; }
		}

		public byte TypeCode
		{
			get	{ return mTypeCode; }
			set	{ mTypeCode = value; }
		}

		public byte EventNumber
		{
			get	{ return mEventNumber; }
			set	{ mEventNumber = value;	}
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
					mDate.ui16Date = mCDEFReader.ReadUInt16();
					mVenueCode = mCDEFReader.ReadByte();
					mTypeCode = mCDEFReader.ReadByte();
					mEventNumber = mCDEFReader.ReadByte();
					mFirstSequenceNumber = mCDEFReader.ReadUInt32();
					mLastSequenceNumber = mCDEFReader.ReadUInt32();
				
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
					mCDEFWriter.Write(mDate.ui16Date);
					mCDEFWriter.Write(mVenueCode);
					mCDEFWriter.Write(mTypeCode);
					mCDEFWriter.Write(mEventNumber);
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
				sb.Append("Date=" + mDate + "\r\n\r\n");
				sb.Append("VenueCode=" + mVenueCode + "\r\n");
				sb.Append("TypeCode=" + mTypeCode + "\r\n");
				sb.Append("EventNumber=" + mEventNumber + "\r\n\r\n");
				sb.Append("FirstSequenceNumber=" + mFirstSequenceNumber + "\r\n");
				sb.Append("LastSequenceNumber=" + mLastSequenceNumber + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
