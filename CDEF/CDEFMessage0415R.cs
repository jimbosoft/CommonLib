using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// Cancel Transaction response
    /// </summary>
    public class CDEFMessage0415R : CDEFMessage
    {
		#region Variables and Definitions

		// message variables
        public byte mFunctionFlags = 0;

        public byte mTransactionType = 0;
        public Int64 mAmount = 0;
        public Int64 mHostTime = 0;
        public Int64 mAvailableBettingAmount = 0;
        public Int64 mAvailableWithdrawalAmount = 0;

		#endregion

		#region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="amount"> in cents </param>
        public CDEFMessage0415R(Int64 amount)
            : base()
        {
            mSystemFlags = 0x80;
            mFunctionCode = (Int16)FuncCode.Cancel_Transaction;
            mAmount = amount * CDEFAmount.AMOUNT_FACTOR;
            TimeLord t = new TimeLord(DateTime.Now);
            mHostTime = t.utcDateTime;
        }

		public CDEFMessage0415R(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0415R(CDEFMessage newCDEF)
            : base(newCDEF)
		{
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

                    mTransactionType = mCDEFReader.ReadByte();
                    mAmount = mCDEFReader.ReadInt64();
                    mHostTime = mCDEFReader.ReadInt64();
                    mAvailableBettingAmount = mCDEFReader.ReadInt64();
                    mAvailableWithdrawalAmount = mCDEFReader.ReadInt64();

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

			if(mCDEFStatus == CDEFStatus.OK )
			{
				try
				{
					mCDEFWriter.Write(mFunctionFlags);

                    mCDEFWriter.Write(mTransactionType);
                    mCDEFWriter.Write(mAmount);
                    mCDEFWriter.Write(mHostTime);
                    mCDEFWriter.Write(mAvailableBettingAmount);
                    mCDEFWriter.Write(mAvailableWithdrawalAmount);

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

