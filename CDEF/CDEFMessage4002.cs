using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace CommonLib.CDEF
{
    public class CDEFMessage4002 : CDEFMessage
    {
		#region Variables and Definitions

		// message variables
        public byte mFunctionFlags = 0;
        public string mTrackID = "";
        public UInt16 mTermNr = 0;
        public UInt32 mOperatorNr = 0;
        public UInt64 mSerialNr = 0;
        public Int64 mHostTime = 0;
        
        private byte[] mRequestBuffer = new byte[0];
        private byte[] mResponseBuffer = new byte[0];

		#endregion

		#region Constructors

        public CDEFMessage4002(string track, UInt16 terminal, UInt32 op, UInt64 serialNr,
                byte[] request, byte[] response)
            : base()
        {
            mFunctionCode = (Int16)FuncCode.Notify;
            mTrackID = track;
            mTermNr = terminal;
            mOperatorNr = op;
            mSerialNr = serialNr;
            TimeLord t = new TimeLord(DateTime.Now);
            mHostTime = t.utcDateTime;
            if (request != null)
            {
                mRequestBuffer = request;
            }
            if (response != null)
            {
                mResponseBuffer = response;
            }
        }

		public CDEFMessage4002(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage4002(CDEFMessage newCDEF)
            : base(newCDEF)
		{
		}

		#endregion

        public byte[] GetNewRequestBuffer()
        {
            byte[] msgBuffer = new byte[mRequestBuffer.Length];
            Buffer.BlockCopy(mRequestBuffer, 0, msgBuffer, 0, mRequestBuffer.Length);
            return msgBuffer;
        }
        public byte[] GetNewResponseBuffer()
        {
            byte[] msgBuffer = new byte[mResponseBuffer.Length];
            Buffer.BlockCopy(mResponseBuffer, 0, msgBuffer, 0, mResponseBuffer.Length);
            return msgBuffer;
        }
        public void SetRequestBufferRef( byte[] msgBuffer )
        {
            mRequestBuffer = msgBuffer;
        }
        public void SetResponseBufferRef(byte[] msgBuffer)
        {
            mResponseBuffer = msgBuffer;
        }
        //
       // Attach a valid header
       //
       private void PopulateHeader(byte[] msgBuffer)
        {
            MemoryStream stream = new MemoryStream(msgBuffer);
            BinaryWriter writer = new BinaryWriter(stream);
            UInt32 msgLength = (uint)msgBuffer.Length - LENGTHLENGTH;
            UInt32 addess = 0;
            byte protocol = APPLICATION;
            byte msn = 0;
            writer.Write(msgLength);
            writer.Write(addess);
            writer.Write(addess);
            writer.Write(protocol);
            writer.Write(msn);
        }
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
                    mOperatorNr = mCDEFReader.ReadUInt32();
                    mSerialNr = mCDEFReader.ReadUInt64();
                    mHostTime = mCDEFReader.ReadInt64();

                    UInt32 mBuffLengh = mCDEFReader.ReadUInt32();
                    if (mBuffLengh > 0)
                    {
                        mRequestBuffer = new byte[mBuffLengh + HEADER_LENGTH];
                        mCDEFReader.Read(mRequestBuffer, HEADER_LENGTH, (int)mBuffLengh);
                        PopulateHeader(mRequestBuffer);
                    }
                    mBuffLengh = mCDEFReader.ReadUInt32();
                    if (mBuffLengh > 0)
                    {
                        mResponseBuffer = new byte[mBuffLengh + HEADER_LENGTH];
                        mCDEFReader.Read(mResponseBuffer, HEADER_LENGTH, (int)mBuffLengh);
                        PopulateHeader(mResponseBuffer);
                    }

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
                    mCDEFWriter.Write(mOperatorNr);
                    mCDEFWriter.Write(mSerialNr);
                    mCDEFWriter.Write(mHostTime);

                    
                    int mBuffLengh;
                    if (mRequestBuffer.Length > 0)
                    {
                        mBuffLengh = mRequestBuffer.Length - HEADER_LENGTH;
                        mCDEFWriter.Write((UInt32)mBuffLengh);
                        mCDEFWriter.Write(mRequestBuffer, HEADER_LENGTH, mBuffLengh);
                    }
                    else
                    {
                        mCDEFWriter.Write((UInt32)0);
                    }
                    if (mResponseBuffer.Length > 0)
                    {
                        mBuffLengh = mResponseBuffer.Length - HEADER_LENGTH;
                        mCDEFWriter.Write((UInt32)mBuffLengh);
                        mCDEFWriter.Write(mResponseBuffer, HEADER_LENGTH, mBuffLengh);
                    }
                    else
                    {
                        mCDEFWriter.Write((UInt32)0);
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
