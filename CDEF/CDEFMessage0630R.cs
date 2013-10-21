using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0630R : CDEFMessage
    {
        /*public class CDEFAmount
        {
            public byte AmountID;
            public Int64 AmountVal;
            public CDEFAmount(byte id, Int64 amount)
            {
                AmountID = id;
                AmountVal = amount;
            }
        }
        public class CDEFRole
        {
            public string Group;
            public string RoleType;
            public CDEFRole(string g, string t)
            {
                Group = g;
                RoleType = t;
            }
        }*/
        #region Variables and Definitions

		// message variables
        public byte mFunctionFlags = 0;
        public byte mPasswordChangeIndicator = 0;
        private List<CDEFAmount> mAmounts = new List<CDEFAmount>();
        public string mName;
        public string mTxtMessage;
        public List<CDEFRole> mRoles = new List<CDEFRole>();

		#endregion

		#region Constructors

		public CDEFMessage0630R(byte indicator, string name, string message) : base()
		{
            mFunctionCode = (Int16)FuncCode.Ad_Sign_On;
            mSystemFlags = 0x80;
            mPasswordChangeIndicator = indicator;
            mName = name;
            mTxtMessage = message;
		}

		public CDEFMessage0630R(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0630R(CDEFMessage newCDEF)
            : base(newCDEF)
		{
		}

		#endregion

        public void AddAmountInCents(CDEFAmount am)
        {
            am.AmountVal = am.AmountVal << CDEFAmount.SHIFT_FACTOR; // *CDEFAmount.FACTOR;
            mAmounts.Add(am);
        }
        public List<CDEFAmount> GetRawAmounts()
        {
            return mAmounts;
        }

		#region Method Overrides

		protected override void parseMessage()
		{
			// read the header...
			base.parseMessage();

			if(mCDEFStatus == CDEFStatus.OK && !IsError())
			{
				try
				{
					// parse the message...
					mFunctionFlags = mCDEFReader.ReadByte();

                    mPasswordChangeIndicator = mCDEFReader.ReadByte();
                    byte amountCnt = mCDEFReader.ReadByte();
                    mAmounts.Clear();
                    for (int i = 0; i < amountCnt; i++)
                    {
                        CDEFAmount a = new CDEFAmount(mCDEFReader.ReadByte(), mCDEFReader.ReadInt64());
                        mAmounts.Add(a);
                    }
                    mName = mCDEFReader.ReadString();
                    mTxtMessage = mCDEFReader.ReadString();
                    mRoles.Clear();
                    byte roleCnt = mCDEFReader.ReadByte();
                    for (int i = 0; i < roleCnt; i++)
                    {
                        CDEFRole r = new CDEFRole(mCDEFReader.ReadString(), mCDEFReader.ReadString());
                        mRoles.Add(r);
                    }
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
					mCDEFWriter.Write(mFunctionFlags);
					mCDEFWriter.Write(mPasswordChangeIndicator);
                    if (mAmounts.Count <= byte.MaxValue)
                    {
                        mCDEFWriter.Write((byte)mAmounts.Count);
                    }
                    else
                    {
                        mCDEFWriter.Write(byte.MaxValue);
                    }
                    for (int i = 0; i < mAmounts.Count && i < byte.MaxValue; i++)
                    {
                        mCDEFWriter.Write(mAmounts[i].AmountID);
                        mCDEFWriter.Write(mAmounts[i].AmountVal);
                    }
                    mCDEFWriter.Write(mName);
                    mCDEFWriter.Write(mTxtMessage);
                    if (mRoles.Count <= byte.MaxValue)
                    {
                        mCDEFWriter.Write((byte)mRoles.Count);
                    }
                    else
                    {
                        mCDEFWriter.Write(byte.MaxValue);
                    }
                    for (int i = 0; i < mRoles.Count && i < byte.MaxValue; i++)
                    {
                        mCDEFWriter.Write(mRoles[i].Group);
                        mCDEFWriter.Write(mRoles[i].RoleType);
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
