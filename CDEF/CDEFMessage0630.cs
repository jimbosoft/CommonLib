using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace CommonLib.CDEF
{
    public class CDEFMessage0630 : CDEFMessage
    {
		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;
        public UInt32   mOperatorNr = 0;
        public string   mPassword = "";
        public bool     mAuthenticate = true;

		#endregion

		#region Constructors

		public CDEFMessage0630(UInt32 op, string pass) : base()
		{
            mFunctionCode = (Int16)FuncCode.Ad_Sign_On;
            mOperatorNr = op;
            mPassword = pass;
		}

		public CDEFMessage0630(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0630(CDEFMessage newCDEF)
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
                    mOperatorNr = mCDEFReader.ReadUInt32();
                    mPassword = mCDEFReader.ReadString();
                    mAuthenticate = mCDEFReader.ReadBoolean();
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
					mCDEFWriter.Write(mPassword);
                    mCDEFWriter.Write(mAuthenticate);

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
				sb.Append("FunctionFlags=" + mFunctionFlags);
                sb.Append(" OperatorNumber=" + mOperatorNr.ToString());
                sb.Append(" Password=" + mPassword);
                sb.Append(" Authenticate=" + mAuthenticate.ToString());
			}
			return sb.ToString();
		}

		#endregion
    }
}
