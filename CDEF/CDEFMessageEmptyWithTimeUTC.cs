using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    class CDEFMessageEmptyWithTimeUTC: CDEFMessage
    {
		#region Variables and Definitions

		// message variables
        public byte mFunctionFlags = 0;
        public Int64 mHostTime = 0;

		#endregion

		#region Constructors

        public CDEFMessageEmptyWithTimeUTC(FuncCode func, bool response)
            : base()
        {
            mFunctionCode = (Int16)func;
            if (response)
            {
                mSystemFlags = 0x80;
            }
            mHostTime = Conversions.DateTimeToCdefTimeUtc(DateTime.Now);
        }

		public CDEFMessageEmptyWithTimeUTC(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessageEmptyWithTimeUTC(CDEFMessage newCDEF)
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