using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace CommonLib.CDEF
{
    public class CDEFMessage1319 : CDEFMessage
    {

		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;

        public      Int64   mTime = 0;
        public      UInt64  mTransactionNr = 0;

		#endregion

		#region Constructors

		public CDEFMessage1319(UInt64 tx) : base()
		{
            mFunctionCode = (Int16)FuncCode.TransactionLocation;
            TimeLord t = new TimeLord(DateTime.Now);
            mTime = t.utcDateTime;
            mTransactionNr = tx;
		}

		public CDEFMessage1319(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage1319(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public UInt64 TxNr
		{
            get { return mTransactionNr; }
            set { mTransactionNr = value; }
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
                    mTime = mCDEFReader.ReadInt64();
                    mTransactionNr = mCDEFReader.ReadUInt64();
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
                    mCDEFWriter.Write(mTime);
                    mCDEFWriter.Write(mTransactionNr);

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
                sb.Append("TransactionNumber=" + mTransactionNr.ToString() + "\r\n\r\n");
			}
			return sb.ToString();
		}

		#endregion
	}
}
