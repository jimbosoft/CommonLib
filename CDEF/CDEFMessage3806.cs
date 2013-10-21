using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// End of Oncource Meeting Notify
    /// </summary>
    public class CDEFMessage3806: CDEFMessage
    {
		#region Variables and Definitions

        public const int MNEMONIC_LENGTH = 3;
        // message variables
        public byte mFunctionFlags = 0;
        public Int64 mHostTime = 0;
        public UInt16 mDate = 0;
        public string mVenueMnc = "";

		#endregion

		#region Constructors

        public CDEFMessage3806(string venue) : base()
        {
            mFunctionCode = (Int16)FuncCode.EndOfOncourceMeetingNotify;
            mHostTime = Conversions.DateTimeToCdefTimeUtc(DateTime.Now);
            mDate = Conversions.DateTimeToCdefDate(DateTime.Now);
            mVenueMnc = venue;
        }

		public CDEFMessage3806(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage3806(CDEFMessage newCDEF)
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
                    mDate = mCDEFReader.ReadUInt16();
                    mVenueMnc = mCDEFReader.ReadString();// new string(mCDEFReader.ReadChars(MNEMONIC_LENGTH));

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
					mCDEFWriter.Write(mFunctionFlags);
                    mCDEFWriter.Write(mHostTime);
                    mCDEFWriter.Write(mDate);
                    /*
                    if (mVenueMnc.Length > MNEMONIC_LENGTH)
                    {
                        mVenueMnc.Remove(MNEMONIC_LENGTH);
                    }
                    else if (mVenueMnc.Length < MNEMONIC_LENGTH)
                    {
                        string add = new string(' ', MNEMONIC_LENGTH - mVenueMnc.Length);
                        mVenueMnc = mVenueMnc + add;
                    }
                    mCDEFWriter.Write(mVenueMnc.ToCharArray());
                    */
                    mCDEFWriter.Write(mVenueMnc);
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