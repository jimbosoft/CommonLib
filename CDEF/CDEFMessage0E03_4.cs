using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// Cash-In, Cash-out message
    /// </summary>
    public class CDEFMessage0E03_4 : CDEFMessage
    {
		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;
        public UInt32   mOperatorNr = 0;
        public byte     mOperatorType = 0;
        public Int64    mAmount = 0;

		#endregion

		#region Constructors
        /// <summary>
        /// Construct a Cash-In or Cash-out message
        /// with the amount given in cents
        /// </summary>
        /// <param name="op"></param>
        /// <param name="type"></param>
        /// <param name="am"></param>
        /// <param name="cashin"></param>
		public CDEFMessage0E03_4(UInt32 op, byte type, Int64 am, bool cashin) : base()
		{
            if (cashin)
            {
                mFunctionCode = (Int16)FuncCode.CashIn;
            }
            else
            {
                mFunctionCode = (Int16)FuncCode.CashOut;
            }
            mOperatorNr = op;
            mOperatorType = type;
            mAmount = am * CDEFAmount.AMOUNT_FACTOR;
		}

		public CDEFMessage0E03_4(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0E03_4(CDEFMessage newCDEF)
            : base(newCDEF)
		{
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
                    mAmount = mCDEFReader.ReadInt64();
                    mOperatorNr = mCDEFReader.ReadUInt32();
                    mOperatorType = mCDEFReader.ReadByte();
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
                    mCDEFWriter.Write(mAmount);
					mCDEFWriter.Write(mOperatorNr);
                    mCDEFWriter.Write(mOperatorType);

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