using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace CommonLib.CDEF
{
    public class CDEFMessage0603R : CDEFMessage
    {
        public class Amounts
        {
            public byte mType = 0;
            public Int64 mAmount;
            public Amounts(byte typ, Int64 amount)
            {
                mType = typ;
                mAmount = amount;
            }
        }
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected List<Amounts> mAmountDetail= new List<Amounts>();
        protected TimeLord mTime = new TimeLord();

		#endregion

		#region Constructors

		public CDEFMessage0603R() : base()
		{
			mFunctionCode = (Int16)FuncCode.Sign_On;
            mSystemFlags = 0x80;
        }

		public CDEFMessage0603R(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage0603R(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

        public List<Amounts> AmountDetail
		{
            get { return mAmountDetail; }
            set { mAmountDetail = value; }
		}

		public TimeLord Time
		{
            get { return mTime; }
            set { mTime = value; }
		}

		#endregion

		#region Method Overrides

		protected override void parseMessage()
		{
			// read the header...
			base.parseMessage();

            if (mCDEFStatus == CDEFStatus.OK && !IsError())
            {
				try
				{
                        // parse the message...
                        mFunctionFlags = mCDEFReader.ReadByte();
                        byte nrOf = mCDEFReader.ReadByte();
                        for (int i = 0; i < nrOf; i++)
                        {
                            Amounts a = new Amounts(mCDEFReader.ReadByte(), mCDEFReader.ReadInt64());
                            mAmountDetail.Add(a);
                        }
                        mTime = new TimeLord(mCDEFReader.ReadInt64());
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
                    if (mAmountDetail.Count <= byte.MaxValue)
                    {
                        mCDEFWriter.Write((byte)mAmountDetail.Count);
                    }
                    else
                    {
                        mCDEFWriter.Write(byte.MaxValue);
                    }
                    for (int i = 0; i < mAmountDetail.Count && i < byte.MaxValue; i++)
                    {
                        Amounts a = mAmountDetail[i];
                        mCDEFWriter.Write(a.mType);
                        mCDEFWriter.Write(a.mAmount);
                    }
                    mCDEFWriter.Write(mTime.utcDateTime);
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
                for (int i = 0; i < mAmountDetail.Count; i++)
                {
                    Amounts a = mAmountDetail[i];
                    sb.Append("AmountType=" + a.mType.ToString() + "\r\n");
                    sb.Append("Amount=" + a.mAmount.ToString() + "\r\n");
                }
                sb.Append("Time=" + mTime.ToString() + "\r\n");
			}
			return sb.ToString();
		}
		#endregion
	}
}
