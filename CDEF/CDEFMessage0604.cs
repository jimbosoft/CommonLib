using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0604: CDEFMessage
    {
		#region Variables and Definitions

		// message variables
		public byte		mFunctionFlags = 0;
        public UInt32   mOperatorNr = 0;

		#endregion

		#region Constructors

		public CDEFMessage0604(UInt32 op) : base()
		{
            mFunctionCode = (Int16)FuncCode.Sign_Off;
            mOperatorNr = op;
		}

		public CDEFMessage0604(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0604(CDEFMessage newCDEF)
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