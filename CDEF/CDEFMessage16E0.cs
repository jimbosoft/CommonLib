using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// Racing Results - Previous Days Results Sequence Number Range : 0x16E0
	/// </summary>
	public class CDEFMessage16E0 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected UInt32	mNumberOfDays = 0;
		protected ArrayList mResultsRanges = new ArrayList(); // NOTE: array of D3220ResultsRange
		protected int			mRetryTime = 0; // Default

		#endregion

		#region Constructors

		public CDEFMessage16E0() : base()
		{
			mFunctionCode = (Int16)FuncCode.RLDB_PreviousDaysResultsEx;
		}

		public CDEFMessage16E0(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage16E0(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public UInt32 NumberOfDays
		{
			get { return mNumberOfDays; }
			set	{ mNumberOfDays = value; }
		}

		public ArrayList ResultsRanges
		{
			get { return mResultsRanges; }
			set	{ mResultsRanges = value; }
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
					mNumberOfDays = mCDEFReader.ReadUInt32();
					for(int i = 0; i < mNumberOfDays; i++)
					{
						UInt16	uiDate = mCDEFReader.ReadUInt16();
						UInt32	uiLSN = mCDEFReader.ReadUInt32();
						UInt32	uiCRC = mCDEFReader.ReadUInt32();

						mResultsRanges.Add(new DPrevDaysResultsRange(uiDate, uiLSN, uiCRC));
					}

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

					mNumberOfDays = (UInt32) mResultsRanges.Count;

					mCDEFWriter.Write(mNumberOfDays);

					foreach(DPrevDaysResultsRange rr in mResultsRanges)
					{
						mCDEFWriter.Write(rr.Date);
						mCDEFWriter.Write(rr.LastSequenceNumber);
						mCDEFWriter.Write(rr.CRC);
					}

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
				sb.Append("NumberOfDays=" + mNumberOfDays + "\r\n");
				foreach(DPrevDaysResultsRange rr in mResultsRanges)
				{
					TimeLord tlDate = new TimeLord(rr.Date);

					sb.Append("\r\nDate=" + tlDate + "\r\n");
					sb.Append("LastSequenceNumber=" + rr.LastSequenceNumber + "\r\n");
					sb.Append("CRC=" + rr.CRC + "\r\n");
				}
			}

			return sb.ToString();
		}

		#endregion
	}
}