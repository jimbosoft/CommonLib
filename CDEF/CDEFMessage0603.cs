using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace CommonLib.CDEF
{
    public class CDEFMessage0603 : CDEFMessage
    {

		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected UInt32	mOperatorNr = 0;
        protected string mPassword = "";

		#endregion

		#region Constructors

		public CDEFMessage0603(UInt32 op, string pass) : base()
		{
			mFunctionCode = (Int16)FuncCode.Sign_On;
            mOperatorNr = op;
            mPassword = pass;
		}

		public CDEFMessage0603(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage0603(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public UInt32 OperatorNr
		{
            get { return mOperatorNr; }
            set { mOperatorNr = value; }
		}

		public string Password
		{
            get { return mPassword; }
            set { mPassword = value; }
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
                    mOperatorNr = mCDEFReader.ReadUInt32();
                    StringBuilder passwd = new StringBuilder();
                    passwd.Append(mCDEFReader.ReadChars(4));
                    mPassword = passwd.ToString();
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
					mCDEFWriter.Write(mOperatorNr);
                    if (mPassword.Length > 4)
                    {
                        mPassword.Remove(4);
                    }
                    else if (mPassword.Length < 4)
                    {
                        mPassword = String.Format("{0,4}",mPassword);
                    }
					mCDEFWriter.Write(mPassword.ToCharArray());

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

		public override string ToStringEx()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(base.ToStringEx());

			if(mCDEFStatus == CDEFStatus.OK)
			{
                sb.Append("\r\nFunctionFlags=" + mFunctionFlags);
                sb.Append("OperatorNumber=" + mOperatorNr.ToString());
                sb.Append("Password=" + String.Format("{4:s}", mPassword));
			}

			return sb.ToString();
		}

		#endregion
	}
}
