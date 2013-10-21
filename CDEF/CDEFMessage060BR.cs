using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// Operator Balance Response
    /// </summary>
    public class CDEFMessage060BR : CDEFMessage
    {

		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;
        public UInt16   mTerminalNr = 0;
        public UInt32   mOperatorNr = 0;
        private List<CDEFAmount> mAmounts = new List<CDEFAmount>();
        public Int64    mHostTime = 0;

		#endregion

		#region Constructors

        public CDEFMessage060BR(UInt16 term, UInt32 op)
            : base()
		{
			mFunctionCode = (Int16)FuncCode.OperatorBalance;
            mSystemFlags = 0x80;

            mTerminalNr = term;
            mOperatorNr = op;

            TimeLord t = new TimeLord(DateTime.Now);
            mHostTime = t.utcDateTime;
        }

		public CDEFMessage060BR(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage060BR(CDEFMessage newCDEF)
            : base(newCDEF)
		{
		}
        /// <summary>
        /// Add an amount, in cents, to reponse
        /// </summary>
        /// <param name="am"></param>
        public void AddAmountInCents(CDEFAmount am)
        {
            am.AmountVal = am.AmountVal << CDEFAmount.SHIFT_FACTOR; // *CDEFAmount.FACTOR;
            mAmounts.Add(am);
        }
        public List<CDEFAmount> GetRawAmounts()
        {
            return mAmounts;
        }
		#endregion

		#region Method Overrides

		protected override void parseMessage()
		{
			// read the header...
			base.parseMessage();

			if(mCDEFStatus == CDEFStatus.OK && !IsError()) 
			{
				try
				{
					// parse the message...
					mFunctionFlags = mCDEFReader.ReadByte();
                    mTerminalNr = mCDEFReader.ReadUInt16();
                    mOperatorNr = mCDEFReader.ReadUInt32();
                    byte amountCnt = mCDEFReader.ReadByte();
                    mAmounts.Clear();
                    for (int i = 0; i < amountCnt; i++)
                    {
                        CDEFAmount a = new CDEFAmount(mCDEFReader.ReadByte(), mCDEFReader.ReadInt64());
                        mAmounts.Add(a);
                    }
                    mHostTime = mCDEFReader.ReadInt64();
                    // get the current length of the message
					mLength = mStream.Position;
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
                    mCDEFWriter.Write(mTerminalNr);
                    mCDEFWriter.Write(mOperatorNr);
                    if (mAmounts.Count <= byte.MaxValue)
                    {
                        mCDEFWriter.Write((byte)mAmounts.Count);
                    }
                    else
                    {
                        mCDEFWriter.Write(byte.MaxValue);
                    }
                    for (int i = 0; i < mAmounts.Count && i < byte.MaxValue; i++)
                    {
                        mCDEFWriter.Write(mAmounts[i].AmountID);
                        mCDEFWriter.Write(mAmounts[i].AmountVal);
                    }
                    mCDEFWriter.Write(mHostTime);
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
