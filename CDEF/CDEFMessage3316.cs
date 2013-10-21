using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// Start of Raceday Notify
    /// </summary>
    public class CDEFMessage3316 : CDEFMessage
    {
		#region Variables and Definitions

		// message variables
        public byte mFunctionFlags = 0;
        public Int64 mHostTime = 0;
        public UInt16 mDate = 0;

		#endregion

		#region Constructors

        public CDEFMessage3316() : base()
        {
            mFunctionCode = (Int16)FuncCode.StartOfRacedayNotify;
            mHostTime = Conversions.DateTimeToCdefTimeUtc(DateTime.Now);
            mDate = Conversions.DateTimeToCdefDate(DateTime.Now);
        }

		public CDEFMessage3316(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage3316(CDEFMessage newCDEF) : base(newCDEF)
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