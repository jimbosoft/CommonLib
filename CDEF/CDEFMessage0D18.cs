using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// Combine Pay Redeem
    /// </summary>
    public class CDEFMessage0D18 : CDEFMessage
    {
		#region Variables and Definitions

        public const int PASSWD_LENGTH = 4;
		// message variables
        public byte mFunctionFlags = 0;
        public UInt64 mSerialNr = 0;
        public string mPassword = "";
        public Int64 mAmount = 0;

		#endregion

		#region Constructors

        public CDEFMessage0D18(UInt64 serialNr, string pass, Int64 am)
            : base()
        {
            mFunctionCode = (Int16)FuncCode.CombPayRedeem;
            mSerialNr = serialNr;
            mPassword = pass;
            mAmount = am;
        }

		public CDEFMessage0D18(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0D18(CDEFMessage newCDEF)
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

                    mSerialNr = mCDEFReader.ReadUInt32();
                    StringBuilder passwd = new StringBuilder();
                    passwd.Append(mCDEFReader.ReadChars(4));
                    mPassword = passwd.ToString();
                    mAmount = mCDEFReader.ReadInt64();

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

                    mCDEFWriter.Write(mSerialNr);
                    if (mPassword.Length > PASSWD_LENGTH)
                    {
                        mPassword.Remove(PASSWD_LENGTH);
                    }
                    else if (mPassword.Length < PASSWD_LENGTH)
                    {
                        string add = new string(' ', PASSWD_LENGTH - mPassword.Length);
                        mPassword = mPassword + add;
                    }
                    mCDEFWriter.Write(mPassword.ToCharArray());
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

