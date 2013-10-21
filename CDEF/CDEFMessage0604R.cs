using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// SignOff Response
    /// </summary>
    public class CDEFMessage0604R : CDEFMessage
    {
        /*public class CDEFAmount
        {
            public byte mType = 0;
            public Int64 mAmount;
            public CDEFAmount(byte typ, Int64 amount)
            {
                mType = typ;
                mAmount = amount;
            }
        }*/
		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;
        private List<CDEFAmount> mAmounts = new List<CDEFAmount>();
        public Int64 mTime = 0;

		#endregion

		#region Constructors

        public CDEFMessage0604R()
            : base()
		{
            mFunctionCode = (Int16)FuncCode.Sign_Off;
            mSystemFlags = 0x80;
            TimeLord t = new TimeLord(DateTime.Now);
            mTime = t.utcDateTime;
		}

		public CDEFMessage0604R(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0604R(CDEFMessage newCDEF)
            : base(newCDEF)
		{
		}

		#endregion

        public void AddAmountInCents(CDEFAmount am)
        {
            am.AmountVal = am.AmountVal << CDEFAmount.SHIFT_FACTOR; // *CDEFAmount.FACTOR;
            mAmounts.Add(am);
        }
        public List<CDEFAmount> GetRawAmounts()
        {
            return mAmounts;
        }

		#region Method Overrides

		protected override void parseMessage()
		{
			// read the header...
			base.parseMessage();

            if (mCDEFStatus == CDEFStatus.OK && !IsError())
            {
				try
				{
                    if (!IsError())
                    {
                        // parse the message...
                        mFunctionFlags = mCDEFReader.ReadByte();
                        byte nrOf = mCDEFReader.ReadByte();
                        for (int i = 0; i < nrOf; i++)
                        {
                            CDEFAmount a = new CDEFAmount(mCDEFReader.ReadByte(), mCDEFReader.ReadInt64());
                            mAmounts.Add(a);
                        }
                        mTime = mCDEFReader.ReadInt64();
                        // get the current length of the message
                        mLength = mStream.Position;
                    }
				}
				catch(Exception e)
				{
					// set the current length to zero
					mLength = 0;

					// set the status...
					mCDEFStatus = CDEFStatus.ParseError;
                    throw e;
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
                    mCDEFWriter.Write((byte)mAmounts.Count);
                    for (int i = 0; i < mAmounts.Count; i++)
                    {
                        CDEFAmount a = mAmounts[i];
                        mCDEFWriter.Write(a.AmountID);
                        mCDEFWriter.Write(a.AmountVal);
                    }
                    mCDEFWriter.Write(mTime);
					// get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception e)
				{
					// set the current length to zero
					mLength = 0;

					// set the status...
					mCDEFStatus = CDEFStatus.BuildError;
                    throw e;
				}
			}
		}
		#endregion
	}
}
