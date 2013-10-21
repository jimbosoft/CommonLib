using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0319R: CDEFMessage
    {
        public int ID_LENGTH = 2;

        public class ClubDetails
        {
            public string mTrackID = "";
            public byte mActive = 0;
            public string mTrackName = "";
            
            public ClubDetails(string trackID, byte active, string trackName)
            {
                mTrackID = trackID;
                mActive = active;
                mTrackName = trackName;
            }
            public bool IsActive()
            {
                if (mActive > 0)
                {
                    return true;
                }
                return false;
            }
        }
		#region Variables and Definitions

		// message variables
        public byte mFunctionFlags = 0;
        public Int64 mHostTime = 0;
        public List<ClubDetails> mClubDetails = new List<ClubDetails>();

		#endregion

		#region Constructors

        public CDEFMessage0319R() : base()
        {
            mFunctionCode = (Int16)FuncCode.ListActiveClubs;
            mSystemFlags = 0x80;

            TimeLord t = new TimeLord(DateTime.Now);
            mHostTime = t.utcDateTime;
        }

		public CDEFMessage0319R(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0319R(CDEFMessage newCDEF)
            : base(newCDEF)
		{
		}

		#endregion


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

                    byte nrOf = mCDEFReader.ReadByte();
                    for (int i = 0; i < nrOf; i++)
                    {
                        ClubDetails a = new ClubDetails(
                            mCDEFReader.ReadString(),
                            mCDEFReader.ReadByte(),
                            mCDEFReader.ReadString());
                        mClubDetails.Add(a);
                    }

                    mLength = mStream.Position;
                }
                catch (Exception e)
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
                    if (mClubDetails.Count <= byte.MaxValue)
                    {
                        mCDEFWriter.Write((byte)mClubDetails.Count);
                    }
                    else
                    {
                        mCDEFWriter.Write(byte.MaxValue);
                    }
                    for (int i = 0; i < mClubDetails.Count && i < byte.MaxValue; i++)
                    {
                        mCDEFWriter.Write(mClubDetails[i].mTrackID.Substring(0, ID_LENGTH));
                        mCDEFWriter.Write(mClubDetails[i].mActive);
                        mCDEFWriter.Write(mClubDetails[i].mTrackName);
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
	}
}
