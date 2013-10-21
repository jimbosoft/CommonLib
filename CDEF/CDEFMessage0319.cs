using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// OCM - List Venues
    /// </summary>
    public class CDEFMessage0319: CDEFMessage
    {

		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;

        public Int64    mHostTime = 0;
        public UInt16   mRaceDate = 0;
        public byte     mAustState = 1; // Victoria

		#endregion

		#region Constructors

        public CDEFMessage0319(DateTime raceday)
            : base()
		{
			mFunctionCode = (Int16)FuncCode.ListActiveClubs;

            TimeLord t = new TimeLord(DateTime.Now);
            mHostTime = t.utcDateTime;

            t = new TimeLord(raceday);
            mRaceDate = t.ui16Date;
        }

		public CDEFMessage0319(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0319(CDEFMessage newCDEF)
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
                    mHostTime = mCDEFReader.ReadInt64();
                    mRaceDate = mCDEFReader.ReadUInt16();
                    mAustState = mCDEFReader.ReadByte();
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
                    mCDEFWriter.Write(mRaceDate);
                    mCDEFWriter.Write(mAustState);
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
