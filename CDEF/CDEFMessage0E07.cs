using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0E07 : CDEFMessage
    {
        #region Variables and Definitions

        // message variables
        public byte mFunctionFlags = 0;
        public string mTrackID = "";
        public string mMnemonic = "";
        public Int64 mAmount = 0;

        #endregion

		public CDEFMessage0E07(string track, string mnemonic, Int64 am) : base()
		{
            mFunctionCode = (Int16)FuncCode.OCMMeetingTotal;
            mTrackID = track;
            mMnemonic = mnemonic;
            mAmount = am * CDEFAmount.AMOUNT_FACTOR;
		}

		public CDEFMessage0E07(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0E07(CDEFMessage newCDEF)
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
                    mMnemonic = mCDEFReader.ReadString();
                    mAmount = mCDEFReader.ReadInt64();
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
                    mCDEFWriter.Write(mMnemonic);
                    mCDEFWriter.Write(mAmount);

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