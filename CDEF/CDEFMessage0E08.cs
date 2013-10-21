using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0E08: CDEFMessage
    {
        #region Variables and Definitions

        public const byte OPEN = 0x4F;
        public const byte CLOSE = 0x43;

        // message variables
        public byte mFunctionFlags = 0;
        public string mTrackID = "";
        public byte mAmountType = 0;
        public Int64 mCloseAmount = 0;

        #endregion

        public CDEFMessage0E08(string track, byte type, Int64 close)
            : base()
		{
            mFunctionCode = (Int16)FuncCode.OCMOpenCloseCash;
            mTrackID = track;
            mAmountType = type;
            mCloseAmount = close * CDEFAmount.AMOUNT_FACTOR;
		}

		public CDEFMessage0E08(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0E08(CDEFMessage newCDEF)
            : base(newCDEF)
		{
		}

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
                    mTrackID = mCDEFReader.ReadString();
                    mAmountType = mCDEFReader.ReadByte();
                    mCloseAmount = mCDEFReader.ReadInt64();
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
                    mCDEFWriter.Write(mTrackID);
                    mCDEFWriter.Write(mAmountType);
                    mCDEFWriter.Write(mCloseAmount);

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