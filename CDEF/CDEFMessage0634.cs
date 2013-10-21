using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFMessage0634 : CDEFMessage
    {
		#region Variables and Definitions

        public const byte INVALID_MSG_CODE = 255;
		// message variables
        public byte mFunctionFlags = 0;

        public string mTrackID = "";
        public UInt16 mTermNr = 0;
        public UInt32 mOrigOperatorNr = 0;
        public byte   mOrigOperatorType = 0;

        public UInt32   mDestOperatorNr = 0;
        public byte     mDestOperatorType = 0;
        public byte     mMessageCode = INVALID_MSG_CODE;
        public string   mMessageTxt = "";

		#endregion

		#region Constructors

        public CDEFMessage0634(string track, UInt16 terminal, UInt32 origOp, byte origType,
                               UInt32 destOp, byte destType, int code, string msg)
            : base()
        {
            mFunctionCode = (Int16)FuncCode.CMS_Message;

            mTrackID = track;
            mTermNr = terminal;
            mOrigOperatorNr = origOp;
            mOrigOperatorType = origType;

            mDestOperatorNr = destOp;
            mDestOperatorType = destType;
            mMessageCode = (byte)code;
            mMessageTxt = msg;
        }

		public CDEFMessage0634(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0634(CDEFMessage newCDEF)
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

                    mTrackID = mCDEFReader.ReadString();
                    mTermNr = mCDEFReader.ReadUInt16();
                    mOrigOperatorNr = mCDEFReader.ReadUInt32();
                    mOrigOperatorType = mCDEFReader.ReadByte();

                    mDestOperatorNr = mCDEFReader.ReadUInt32();
                    mDestOperatorType = mCDEFReader.ReadByte();

                    mMessageCode = mCDEFReader.ReadByte();
                    mMessageTxt = mCDEFReader.ReadString();

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

                    mCDEFWriter.Write(mTrackID);
                    mCDEFWriter.Write(mTermNr);
                    mCDEFWriter.Write(mOrigOperatorNr);
                    mCDEFWriter.Write(mOrigOperatorType);

                    mCDEFWriter.Write(mDestOperatorNr);
                    mCDEFWriter.Write(mDestOperatorType);

                    mCDEFWriter.Write(mMessageCode);
                    mCDEFWriter.Write(mMessageTxt);

                    // get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception e)
				{
					mLength = 0;
					mCDEFStatus = CDEFStatus.BuildError;
                    throw e;
				}
			}
		}
		#endregion
	}
}

