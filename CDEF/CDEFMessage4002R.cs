using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage4002R: CDEFMessage
    {
		#region Variables and Definitions

		// message variables
        public byte mFunctionFlags = 0;
        public UInt64 mSerialNr = 0;
        public Int64 mHostTime = 0;

		#endregion

		#region Constructors

		public CDEFMessage4002R(UInt64 serialnr) : base()
		{
            mFunctionCode = (Int16)FuncCode.Notify;
            mSystemFlags = 0x80;
            mSerialNr = serialnr;
            TimeLord t = new TimeLord(DateTime.Now);
            mHostTime = t.utcDateTime;
		}

		public CDEFMessage4002R(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage4002R(CDEFMessage newCDEF)
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

                    mSerialNr = mCDEFReader.ReadUInt64();
                    mHostTime = mCDEFReader.ReadInt64();

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

