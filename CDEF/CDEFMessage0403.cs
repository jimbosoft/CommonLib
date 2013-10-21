using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace CommonLib.CDEF
{
    public class CDEFMessage0403 : CDEFMessage
    {
        public class Selection
        {
            public Int16 mContestant = 0;
            public char mSelectionSeperator = ',';
            public Selection(short contestant, char sep)
            {
                mContestant = contestant;
                mSelectionSeperator = sep;
            }
        }
        public class Leg
        {
            public byte mEvent = 0;
            public eWPC mWageringProduct = eWPC.WPC_WIN;
            public List<Selection> mSelectionLst = new List<Selection>();
            public Leg(byte ev, eWPC prod, short contestant, char sep)
            {
                mEvent = ev; mWageringProduct = prod; 
                mSelectionLst.Add(new Selection(contestant, sep));
            }
        }
		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;
        public byte mNrOfBets = 1;
		public TimeLord	mDate = new TimeLord();
        public byte mVenue = 0;
        public byte mType = 0;
        public UInt16 mAllupForm = 0; 
        public List<Leg> mAllUpLegs = new List<Leg>();
        public bool mBetParameter = false;
        public List<Int64> mAmountLst = new List<long>();

		#endregion

		#region Constructors

        public CDEFMessage0403(DateTime date, byte venue, byte type, byte ev, 
                                eWPC prod, byte contestant, long amount, char sep)
            : base()
		{
			mFunctionCode = (Int16)FuncCode.SellTicket;
            mDate = new TimeLord(date);
            mVenue = venue;
            mType = type;
            mAllUpLegs.Add(new Leg(ev, prod, contestant, sep));
            mAmountLst.Add(amount);
		}

		public CDEFMessage0403(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage0403(CDEFMessage newCDEF) : base(newCDEF)
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
                    mNrOfBets = mCDEFReader.ReadByte();
                    int nrOfBets = 1;
                    if (mNrOfBets > 0)
                    {
                        nrOfBets = mNrOfBets;
                    }
                    for (int i = 0; i < nrOfBets; i++)
                    {
                        mDate = new TimeLord(mCDEFReader.ReadUInt16());
                        mVenue = mCDEFReader.ReadByte();
                        mType = mCDEFReader.ReadByte();
                        mAllupForm = mCDEFReader.ReadUInt16();
                        byte legs = mCDEFReader.ReadByte(); //Legs in an all up bet
                        for (int x = 0; x < legs; x++)
                        {
                            mAllUpLegs.Add(new Leg(mCDEFReader.ReadByte(), (eWPC)mCDEFReader.ReadByte(),
                                mCDEFReader.ReadInt16(), mCDEFReader.ReadChar()));
                        }
                        if (mCDEFReader.ReadByte() > 0)
                        {
                            mBetParameter = true;
                        }
                        else
                        {
                            mBetParameter = false;
                        }
                        byte amounts = mCDEFReader.ReadByte();
                        for (int y = 0; y < amounts; i++)
                        {
                            mAmountLst.Add(mCDEFReader.ReadInt64());
                        }
                    }
					// get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception e)
				{
					EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", e.Message, e.StackTrace), EventLogEntryType.Error);

					// set the current length to zero
					mLength = 0;

					// set the status...
					mCDEFStatus = CDEFStatus.ParseError;
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
                    mCDEFWriter.Write((byte)1);
                    //int nrOfBets = 1;
                    if (mNrOfBets > 1)
                    {
                        EventLog.WriteEntry("WEASL", "0403 Do not handle ALL UP bets", EventLogEntryType.Error);
                        //nrOfBets = mNrOfBets;
                    }
                    //for (int i = 0; i < nrOfBets; i++)
                    //{
                        mCDEFWriter.Write(mDate.ui16Date);
                        mCDEFWriter.Write(mVenue);
                        mCDEFWriter.Write(mType);
                        mCDEFWriter.Write(mAllupForm);
                        mCDEFWriter.Write((byte)mAllUpLegs.Count);

                        for (int x = 0; x < mAllUpLegs.Count; x++)
                        {
                            mCDEFWriter.Write(mAllUpLegs[x].mEvent);
                            mCDEFWriter.Write((byte)mAllUpLegs[x].mWageringProduct);
                            mCDEFWriter.Write((byte)mAllUpLegs[x].mSelectionLst.Count);
                            foreach (Selection s in mAllUpLegs[x].mSelectionLst)
                            {
                                mCDEFWriter.Write(s.mContestant);
                                mCDEFWriter.Write(s.mSelectionSeperator);
                            }
                        }
                        if (mBetParameter)
                        {
                            mCDEFWriter.Write((byte)1);
                        }
                        else
                        {
                            mCDEFWriter.Write((byte)0);
                        }
                        mCDEFWriter.Write((byte)mAmountLst.Count);
                        for (int y = 0; y < mAmountLst.Count; y++)
                        {
                            mCDEFWriter.Write(mAmountLst[y]);
                        }
                    //}
					// get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception e)
				{
					EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", e.Message, e.StackTrace), EventLogEntryType.Error);

					// set the current length to zero
					mLength = 0;

					// set the status...
					mCDEFStatus = CDEFStatus.BuildError;
				}
			}
		}

		public override string ToStringEx()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(base.ToStringEx());

			if(mCDEFStatus == CDEFStatus.OK)
			{
				sb.Append("\r\nFunctionFlags=" + mFunctionFlags + "\r\n\r\n");
                //sb.Append("OperatorNumber=" + mOperatorNr.ToString() + "\r\n");
                //sb.Append("Password=" + String.Format("{4:s}", mPassword) + "\r\n\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
