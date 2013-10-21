using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace CommonLib.CDEF
{
    /// <summary>
    /// Trackside Sell Ticket
    /// </summary>
    public class CDEFMessage0416 : CDEFMessage
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
        public class Bet
        {
            public UInt16 mGamesCount = 0;
            public eWPC mWageringProduct = eWPC.WPC_WIN;
            public List<Selection> mSelectionLst = new List<Selection>();
            public bool mBetParameter = false;
            public List<Int64> mAmountLst = new List<long>();
            public Bet() { }
            public Bet(UInt16 games, eWPC prod, short contestant, char sep, Int64 am)
            {
                mGamesCount = games; mWageringProduct = prod;
                mSelectionLst.Add(new Selection(contestant, sep));
                mAmountLst.Add(am);
            }
        }
        #region Variables and Definitions

        // message variables
        public byte mFunctionFlags = 0;

        public List<Bet> mBetLst = new List<Bet>();
        public Int64 mSessionBalance = 0;
        #endregion

        #region Constructors

        public CDEFMessage0416(Bet abet, long amount)
            : base()
        {
            mFunctionCode = (Int16)FuncCode.TracksideSellTicket;
            mBetLst.Add(abet);
            mSessionBalance = amount;
        }

        public CDEFMessage0416(byte[] newMessage)
            : base(newMessage)
        {
        }

        public CDEFMessage0416(CDEFMessage newCDEF)
            : base(newCDEF)
        {
        }

        #endregion

        #region Method Overrides

        protected override void parseMessage()
        {
            // read the header...
            base.parseMessage();

            if (mCDEFStatus == CDEFStatus.OK)
            {
                try
                {
                    // parse the message...
                    mFunctionFlags = mCDEFReader.ReadByte();
                    byte nrOfBets = mCDEFReader.ReadByte();
                    for (int i = 0; i < nrOfBets; i++)
                    {
                        Bet bet = new Bet();
                        bet.mGamesCount = mCDEFReader.ReadUInt16();
                        bet.mWageringProduct = (eWPC)mCDEFReader.ReadByte();
                        uint contestantNr = mCDEFReader.ReadByte();
                        for (int x = 0; x < contestantNr; x++)
                        {
                            bet.mSelectionLst.Add(new Selection(mCDEFReader.ReadInt16()
                                                                ,(char)mCDEFReader.ReadByte()));
                        }
                        if (mCDEFReader.ReadByte() > 0)
                        {
                            bet.mBetParameter = true;
                        }
                        else
                        {
                            bet.mBetParameter = false;
                        }
                        byte amounts = mCDEFReader.ReadByte();
                        for (int y = 0; y < amounts; i++)
                        {
                            bet.mAmountLst.Add(mCDEFReader.ReadInt64());
                        }
                        mBetLst.Add(bet);
                    }
                    mSessionBalance = mCDEFReader.ReadInt64();
                    // get the current length of the message
                    mLength = mStream.Position;
                }
                catch (Exception e)
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

            if (mCDEFStatus == CDEFStatus.OK)
            {
                try
                {
                    // build the message...
                    mCDEFWriter.Write(mFunctionFlags);
                    byte bets = 0;
                    if (mBetLst.Count <= byte.MaxValue)
                    {
                        bets = (byte)mBetLst.Count;
                    }
                    else
                    {
                        bets = byte.MaxValue;
                    }
                    mCDEFWriter.Write(bets);
                    for (int i = 0; i < bets; i++)
                    {
                        Bet b = mBetLst[i];
                        mCDEFWriter.Write(b.mGamesCount);
                        mCDEFWriter.Write((byte)b.mWageringProduct);

                        byte selections;
                        if(b.mSelectionLst.Count <= byte.MaxValue)
                        {
                            selections = (byte)b.mSelectionLst.Count;
                        }
                        else
                        {
                            selections = byte.MaxValue;
                        }
                        mCDEFWriter.Write(selections);
                        for (int x = 0; x < selections; x++)
                        {
                            mCDEFWriter.Write(b.mSelectionLst[x].mContestant);
                            mCDEFWriter.Write((byte)b.mSelectionLst[x].mSelectionSeperator);
                        }
                        if (b.mBetParameter)
                        {
                            mCDEFWriter.Write((byte)1);
                        }
                        else
                        {
                            mCDEFWriter.Write((byte)0);
                        }
                        byte amounts = 0;
                        if(b.mAmountLst.Count <= byte.MaxValue)
                        {
                            amounts = (byte)b.mAmountLst.Count;
                        }
                        else
                        {
                            amounts = byte.MaxValue;
                        }
                        mCDEFWriter.Write(amounts);
                        for (int a = 0; a < amounts; a++)
                        {
                            mCDEFWriter.Write(b.mAmountLst[a]);
                        }
                        mCDEFWriter.Write(mSessionBalance);
                    }
                    //}
                    // get the current length of the message
                    mLength = mStream.Position;
                }
                catch (Exception e)
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

            if (mCDEFStatus == CDEFStatus.OK)
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
