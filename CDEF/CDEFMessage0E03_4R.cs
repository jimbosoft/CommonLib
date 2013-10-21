using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0E03_4R: CDEFMessage
    {
		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;
        public Int64    mHostTime = 0;
        public UInt64   mSerialNr = 0;

		#endregion

		#region Constructors

        public CDEFMessage0E03_4R(UInt64 snr, bool cashin)
            : base()
		{
            if (cashin)
            {
                mFunctionCode = (Int16)FuncCode.CashIn;
            }
            else
            {
                mFunctionCode = (Int16)FuncCode.CashOut;
            }
            mSystemFlags = 0x80;
            mSerialNr = snr;
		}

		public CDEFMessage0E03_4R(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0E03_4R(CDEFMessage newCDEF)
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
                    mHostTime = mCDEFReader.ReadInt64();
                    mSerialNr = mCDEFReader.ReadUInt64();
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
                    mCDEFWriter.Write(mHostTime);
                    mCDEFWriter.Write(mSerialNr);

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