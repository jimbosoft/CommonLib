using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0415 : CDEFMessage
    {
		#region Variables and Definitions

        public const int MNEMONIC_LENGTH = 3;
		// message variables
        public byte mFunctionFlags = 0;
        public UInt64 mSerialNr = 0;

        public byte mTansactionFlag = 0;
        public string mMnemonic = "";
        public string mDescription = "";

		#endregion

		#region Constructors

        public CDEFMessage0415(UInt64 serialNr)
            : base()
        {
            mFunctionCode = (Int16)FuncCode.Cancel_Transaction;
            mSerialNr = serialNr;
        }

		public CDEFMessage0415(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0415(CDEFMessage newCDEF)
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

                    mSerialNr = mCDEFReader.ReadUInt64();
                    if (IsInternal())
                    {
                        mTansactionFlag = mCDEFReader.ReadByte();
                        mMnemonic = new string(mCDEFReader.ReadChars(3));
                        mDescription = mCDEFReader.ReadString();
                    }
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
                    mCDEFWriter.Write(mTansactionFlag);

                    if (IsInternal())
                    {
                        if (mMnemonic.Length > MNEMONIC_LENGTH)
                        {
                            mMnemonic.Remove(MNEMONIC_LENGTH);
                        }
                        else if (mMnemonic.Length < MNEMONIC_LENGTH)
                        {
                            mMnemonic = String.Format("{0, 3}", mMnemonic);
                        }
                        mCDEFWriter.Write(mMnemonic.ToCharArray());
                        mCDEFWriter.Write(mDescription);
                    }
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

