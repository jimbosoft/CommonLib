using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;


namespace CommonLib.CDEF
{
	public delegate CDEFMessage GenMsg(byte[] buf);

	public enum CDEFStatus : int
	{
		OK			= 0,
		ParseError	= 1,
		BuildError	= 2,
		InitError	= 3,
		DataError	= 4
	}

	public class CDEFMessage
	{
		//------------------------------------------------------------
		#region CDEFMessage Factory

		public const int HIGH_FUNC = 15;
		public const int LOW_FUNC = 14;
        public const int HEADER_LENGTH = 14;
		public const int SESS = 12;
		public const int RLDB_T = 0x16;

		static Hashtable cdefMakers = new Hashtable();
		public static void RegisterMsg(uint func, GenMsg maker)
		{
			cdefMakers.Add(func, maker);
		}
		public static CDEFMessage MakeMessage(byte[] buf)
		{
			CDEFMessage msg = null;
			uint func = GetFunc(buf, 0);
			if (cdefMakers.Contains(func))
			{
				msg = ((GenMsg)cdefMakers[func])(buf);
			}
			return msg;
		}
		public static uint GetFunc(byte[] buffer, int idx)
		{
			uint funcCode = 0;

            if (buffer.Length > HIGH_FUNC + idx)
            {
                if (buffer[SESS + idx] == 2)
                {
                    funcCode = buffer[HIGH_FUNC + idx - 1];
                    funcCode <<= 8;
                    funcCode += buffer[LOW_FUNC + idx - 1];
                }
                else
                {
                    funcCode = buffer[HIGH_FUNC + idx];
                    funcCode <<= 8;
                    funcCode += buffer[LOW_FUNC + idx];
                }
            }
			return funcCode;
		}
        public static bool IsReponse(byte[] buffer, int idx)
        {
            byte sysflag = GetSysFlags(buffer, idx);
            return (sysflag & 0x80) != 0;
        }
        public static bool IsError(byte[] buffer, int idx)
        {
            byte sysflag = GetSysFlags(buffer, idx);
            return (sysflag & 0x01) != 0;
        }
        public static byte GetSysFlags(byte[] buffer, int idx)
        {
            if (buffer[SESS + idx] == 2)
            {
                return buffer[HIGH_FUNC + idx + 1];
            }
            else
            {
                return buffer[HIGH_FUNC + idx + 2];
            }
        }
        public static byte GetMSN(byte[] buffer, int idx)
        {
            if (buffer[SESS + idx] == 1)
            {
                return buffer[SESS + idx + 1];
            }
            return 0;
        }
        public static uint Get32bitNr(byte[] msg, int start_idx)
		{
			uint seq;
			seq = msg[start_idx + 3];
			seq <<= 8;
			seq += msg[start_idx + 2];
			seq <<= 8;
			seq += msg[start_idx + 1];
			seq <<= 8;
			seq += msg[start_idx];

			return seq;
		}
		#endregion
		//------------------------------------------------------------
		#region Variables and Definitions

		// offsets for default fields
		public const int CDEF_MAX_SIZE = 0xFFFFF;
		protected const int OS_FUNCCODE = 0;
		protected const int OS_TRANSNUM = 2;
		protected const int OS_SYSFLAGS = 3;

		public const int LENGTHLENGTH = 4;
		public const int APPLICATION = 1;

		// CDEF Message status...
		protected CDEFStatus mCDEFStatus = CDEFStatus.OK;

		// Header
		private SessionDescriptor m_toDescriptor = new SessionDescriptor(0);
		private SessionDescriptor m_fromDescriptor = new SessionDescriptor(0);
		protected byte m_protocol = (byte)ProtocolType.Application;
		// Application messags only - 
		// Not for session msg: 3308, 3309, 0310, 3311 and 3312
		private byte m_sequenceNumber = 0;

		// message variables
		protected Int16		mFunctionCode = 0;
		protected byte		mTransactionNumber = 0;
		protected byte		mSystemFlags = 0;
		private byte[]		mPayLoad = null;
		// message...
		protected byte[] mMessage = null;
	
		// length of message being parsed / built
		protected long mLength = 0;

		// message processor objects...
		protected MemoryStream mStream = null;
		protected BinaryReader mCDEFReader = null;
		protected BinaryWriter mCDEFWriter = null;

		#endregion
        #region ErrorResponse
            public class ErrorDetail
            {
                public byte mErrorNr = 0;
                public Int32 mErrorCode = 0;
                public string mErrorDesc = "";
                public ErrorDetail(byte nr, Int32 code, string msg)
                {
                    mErrorNr = nr;
                    mErrorCode = code;
                    mErrorDesc = msg;
                }
            }
            protected List<ErrorDetail> mErrorLst = new List<ErrorDetail>();
        #endregion


        #region Constructors

        /// <summary>
		/// Default Constructor
		/// Creates in mMessage a byte array of the maximum CDEF message size.
		/// </summary>
		public CDEFMessage()
		{
			mMessage = new byte[0];
		}
        public CDEFMessage(FuncCode fcode)
        {
            mFunctionCode = (Int16)fcode;
        }

		/// <summary>
		/// Alternate Constructor
		/// Sets mMessage to the passed byte array (CDEF Message).
		/// </summary>
		/// <param name="newMessage"></param>
/*		public CDEFMessage(byte[] newMessage)
		{
			// only allow messages that are of legal size...
			if((newMessage.GetLength(0) > 0) &&
				(newMessage.GetLength(0) <= CDEF_MAX_SIZE))
			{
				mMessage = (byte[]) newMessage.Clone();

				importMessage();
			}
			else
			{
				mMessage = new byte[0];

				// set the status...
				mCDEFStatus = CDEFStatus.InitError;
			}
		}
*/		public CDEFMessage(byte[] newMessage)
		{
			if((newMessage.GetLength(0) > 0) &&
				(newMessage.GetLength(0) <= CDEF_MAX_SIZE))
			{
				mMessage = newMessage;
				importMessage();
			}
			else
			{
				mMessage = new byte[0];

				// set the status...
				mCDEFStatus = CDEFStatus.InitError;
			}			
		}

		public virtual CDEFMessage CreateMsg(byte[] newMessage)
		{
			if((newMessage.GetLength(0) > 0) &&
				(newMessage.GetLength(0) <= CDEF_MAX_SIZE))
			{
				mMessage = newMessage;
				importMessage();
			}
			else
			{
				mMessage = new byte[0];

				// set the status...
				mCDEFStatus = CDEFStatus.InitError;
			}
			return this;
		}
		/// <summary>
		/// Alternate Constructor
		/// Sets mMessage to the message from the passed CDEF Message.
		/// </summary>
		/// <param name="newCDEF"></param>
		public CDEFMessage(CDEFMessage newCDEF)
		{
			// message within class may not have been built before
			// being passed...
			if(newCDEF.mMessage.GetLength(0) == 0)
			{
				mMessage = newCDEF.Message;

				importMessage();
			}
			else if(newCDEF.mMessage.GetLength(0) <= CDEF_MAX_SIZE)
			{ // only allow messages that are of legal size...
				mMessage = (byte[]) newCDEF.mMessage.Clone();

				importMessage();
			}
			else
			{
				mMessage = new byte[0];

				// set the status...
				mCDEFStatus = CDEFStatus.InitError;
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// A constructed CDEF Message that is either sent or received through
		/// this property.
		/// </summary>
		public byte[] Message
		{
			get	{ return exportMessage(); }
			set
			{
				mMessage = value;
				importMessage();
			}
		}
		
		/// <summary>
		/// The raw byte[] without parsing or building
		/// </summary>
		public byte[] RawMessage
		{
			get { return mMessage; }
		}

		/// <summary>
		/// The current status of the CDEF Message.
		/// </summary>
		public CDEFStatus Status
		{
			get { return mCDEFStatus; }
		}


		public SessionDescriptor ToDesc
		{
			get { return m_toDescriptor; } 
			set { m_toDescriptor = value; }
		}
		public SessionDescriptor FromDesc
		{
			get { return m_fromDescriptor; } 
			set { m_fromDescriptor = value; }
		}
		public byte Protocol
		{
			get { return m_protocol; }
		}
		/// <summary>
		/// The identifier used for determining the type of CDEF Message
		/// </summary>
		public Int16 FunctionCode
		{
			get	{ return mFunctionCode;	}
			set	{ mFunctionCode = value; }
		}
        public byte MSN
        {
            get { return m_sequenceNumber; }
            set { m_sequenceNumber = value; }
        }
		/// <summary>
		/// Used (optionally) for synchronizing request / responses.
		/// </summary>
		public byte TransactionNumber
		{
			get { return mTransactionNumber; }
			set	{ mTransactionNumber = value; }
		}

		/// <summary>
		/// Used (optionally) for message status.
		/// e.g. 0 = request, 128 = response.
		/// </summary>
		public byte SystemFlags
		{
			get	{ return mSystemFlags; }
			set	{ mSystemFlags = value;	}
		}
        public bool IsReponse()
        {
            return ((mSystemFlags & 0x80) > 0);
        }
        public bool IsError()
        {
            return ((mSystemFlags & 0x01) > 0);
        }
        public bool IsTraining()
        {
            return ((SystemFlags & 0x02) > 0);
        }
        public bool IsInternal()
        {
            return ((SystemFlags & 0x04) > 0);
        }
        public List<ErrorDetail> GetErrors()
        {
            return mErrorLst;
        }
        public void SetErrors(List<ErrorDetail> errors)
        {
            byte mask = 0x01;
            mSystemFlags = (byte)(mSystemFlags | mask); 
            mErrorLst = errors;
        }
        #endregion

		#region CDEF Stream Handlers

		/// <summary>
		/// Open the streams used for parsing a CDEF message.
		/// </summary>
		/// <returns>success or failure</returns>
		protected bool openReadStreams()
		{
			bool bReturnValue = false;

			try
			{
				// initialise the stream and reader
				mStream = new MemoryStream(mMessage);
				mCDEFReader = new BinaryReader(mStream);
				bReturnValue = true;
			}
			catch(Exception)
			{
				// set the status...
				mCDEFStatus = CDEFStatus.ParseError;
			}

			return bReturnValue;
		}

		/// <summary>
		/// Close the streams used for parsing a CDEF message.
		/// </summary>
		/// <returns>success or failure</returns>
		protected bool closeReadStreams()
		{
			bool bReturnValue = false;

			try
			{
				// finished with the streams, so close them
				mCDEFReader.Close(); mCDEFReader = null;
				mStream.Close(); mStream = null;
				bReturnValue = true;
			}
			catch(Exception)
			{
				// set the status...
				mCDEFStatus = CDEFStatus.ParseError;
			}

			return bReturnValue;
		}

		/// <summary>
		/// Open the streams used for building a CDEF message.
		/// </summary>
		/// <returns>success or failure</returns>
		protected bool openWriteStreams(bool modBuffer)
		{
			bool bReturnValue = false;

			try
			{
				// initialise the stream and writer
                if (modBuffer)
                {
                    mStream = new MemoryStream(mMessage);
                }
                else
                {
                    mMessage = null;
                    mStream = new MemoryStream();//mMessage);
                }

				mCDEFWriter = new BinaryWriter(mStream);
				bReturnValue = true;
			}
			catch(Exception)
			{
				// set the status...
				mCDEFStatus = CDEFStatus.BuildError;
			}

			return bReturnValue;
		}

		/// <summary>
		/// Close the streams used for building a CDEF message.
		/// </summary>
		/// <returns>success or failure</returns>
		protected bool closeWriteStreams()
		{
			bool bReturnValue = false;

			try
			{
				// finished with the streams, so close them
				mCDEFWriter.Close(); 
                mCDEFWriter = null;
				mStream.Close();
                if (mMessage == null || mMessage.Length == 0)
                {
                    mMessage = mStream.GetBuffer();
                }
                mStream = null;
				bReturnValue = true;
			}
			catch(Exception ex)
			{
				// set the status...
				mCDEFStatus = CDEFStatus.BuildError;
			}

			return bReturnValue;
		}

		#endregion

		#region CDEF Parse Handlers

		/// <summary>
		/// Breakdown the byte array (CDEF Message) and extract the data.
		/// CDEF classes that inherit from the base class will need to call this
		/// first before parsing their own specific data.
		/// </summary>
		protected virtual void parseMessage()
		{
			try
			{
				// move to the origin...
				mCDEFReader.Read(mMessage, 0, 0);
				//
				// new header information
				//
				mLength = mCDEFReader.ReadUInt32() + LENGTHLENGTH;
				m_toDescriptor.Descriptor = mCDEFReader.ReadUInt32();
				m_fromDescriptor.Descriptor = mCDEFReader.ReadUInt32();
				m_protocol = mCDEFReader.ReadByte();

				if (m_protocol == APPLICATION)
					m_sequenceNumber = mCDEFReader.ReadByte();
				// parse the message...
				mFunctionCode = mCDEFReader.ReadInt16();
				mTransactionNumber = mCDEFReader.ReadByte();
				mSystemFlags = mCDEFReader.ReadByte();

                if (IsError())
                {
                    mCDEFReader.ReadByte(); // get the function flags out
                    byte errlen = mCDEFReader.ReadByte();
                    for (int i = 0; i < errlen; i++)
                    {
                        ErrorDetail err = new ErrorDetail(mCDEFReader.ReadByte()
                                                        , mCDEFReader.ReadInt32()
                                                        , mCDEFReader.ReadString());
                        mErrorLst.Add(err);
                    }
                    mLength = mStream.Position;
                }
				// get the remainder of the message
				//long payloadLength = mLength - mStream.Position;
				//if (payloadLength > 0)
					//mPayLoad = mCDEFReader.ReadBytes((int)payloadLength);
				// set the status...
				mCDEFStatus = CDEFStatus.OK;
			}
			catch(Exception)
			{
				// set the current length to zero
				mLength = 0;

				// set the status...
				mCDEFStatus = CDEFStatus.ParseError;
			}
		}

		/// <summary>
		/// Breaks down a CDEF Message and populates variables by calling the
		/// overriding parseMessage() method.
		/// </summary>
		protected void importMessage()
		{
			// parse the message
			if(openReadStreams() == true)
			{
				parseMessage();
				closeReadStreams();
			}
		}

		#endregion

		#region CDEF Build Handlers

		/// <summary>
		/// Build the byte array (CDEF Message) from the class data.
		/// CDEF classes that inherit from the base class will need to call this
		/// first before building from their own specific data.
		/// </summary>
		protected virtual void buildMessage()
		{
            try
            {
                // move to the origin...
                mCDEFWriter.Seek(OS_FUNCCODE, SeekOrigin.Begin);
                //
                // Add new header info
                //

                mCDEFWriter.Write((uint)mLength);
                mCDEFWriter.Write(m_toDescriptor.Descriptor);
                mCDEFWriter.Write(m_fromDescriptor.Descriptor);
                mCDEFWriter.Write(m_protocol);

                if (m_protocol == APPLICATION)
                    mCDEFWriter.Write(m_sequenceNumber);

                // build the message...
                mCDEFWriter.Write(mFunctionCode);
                mCDEFWriter.Write(mTransactionNumber);
                mCDEFWriter.Write(mSystemFlags);

                if (IsError())
                {
                    byte funcflag = 0;
                    mCDEFWriter.Write(funcflag); // get the function flags out
                    byte errlen = (byte)mErrorLst.Count; // mCDEFReader.ReadByte();
                    mCDEFWriter.Write(errlen);
                    for (int i = 0; i < errlen; i++)
                    {
                        mCDEFWriter.Write(mErrorLst[i].mErrorNr);
                        mCDEFWriter.Write(mErrorLst[i].mErrorCode);
                        mCDEFWriter.Write(mErrorLst[i].mErrorDesc);
                    }
                }
                else if (mPayLoad != null)
                {
                    mCDEFWriter.Write(mPayLoad);
                }
                // get the current length of the message
                mLength = mStream.Position;

                // set the status...
                mCDEFStatus = CDEFStatus.OK;
            }
            catch (Exception)
            {
                // set the current length to zero
                mLength = 0;

                // set the status...
                mCDEFStatus = CDEFStatus.BuildError;
            }
		}

		/// <summary>
		/// Builds a CDEF Message by calling the overriding buildMessage() method
		/// and returns it as a byte array.
		/// </summary>
		/// <returns>CDEF Message</returns>
		protected byte[] exportMessage()
		{
			// reset the message buffer...
            //mMessage = new byte[CDEF_MAX_SIZE];

			// put the message together
			if(openWriteStreams(false))
			{
				buildMessage();
				closeWriteStreams();
			}


			// if there was a failure to build the message, we
			// don't want to further process it...
			if(mCDEFStatus == CDEFStatus.OK)
			{
				// create a temp buffer, with correct message length
				byte[] tempMessage = new byte[mLength];

				Buffer.BlockCopy(mMessage,0,tempMessage,0,(int)mLength);
				mMessage = tempMessage;

				uint msgLength = (uint)mMessage.Length - LENGTHLENGTH;
				if(openWriteStreams(true))
				{
					mCDEFWriter.Seek(OS_FUNCCODE, SeekOrigin.Begin);
					//
					// Add new header info
					//
					mCDEFWriter.Write(msgLength);
					closeWriteStreams();
				}

				// copy message into temp buffer
				/*for(int i = 0; i < mLength; i++)
				{
					tempMessage[i] = mMessage[i];
				}

				// copy temp buffer into message
				mMessage = (byte[]) tempMessage.Clone();
				*/
			}
			if (mCDEFStatus != CDEFStatus.OK)
			{
				// ... we instead return a zero length byte array!
				mMessage = new byte[0];
			}

			return mMessage;
		}

		/// <summary>
		/// Force a build of the CDEF Message using whatever data is currently
		/// populated in the class.
		/// </summary>
		public CDEFStatus forceBuildMessage()
		{
			exportMessage();
			return mCDEFStatus;
		}

		#endregion

		#region Miscellaneous

		/// <summary>
		/// Overrided method used to produce text output of class.
		/// </summary>
		/// <returns>basic message info</returns>
		public override string ToString()
		{
			string strReturnValue;

			strReturnValue = "FC=0x" + mFunctionCode.ToString("x") +
				", TN=" + mTransactionNumber +
				", SF=" + mSystemFlags +
				", Status=" + mCDEFStatus;

			return strReturnValue;
		}

		/// <summary>
		/// Produce an extended breakdown of the class contents.
		/// </summary>
		/// <returns>Formatted block of text.</returns>
		public virtual string ToStringEx()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(ToString() + "\r\n");

			if(mCDEFStatus == CDEFStatus.OK)
			{
				sb.Append("Length=" + mLength + "\r\n");
			}
			else
			{
				sb.Append("Unable to break down message!");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Used internally to convert strings into the ASCIIL format -
		/// 8 bit length followed by ASCII of that length.
		/// </summary>
		/// <param name="strInput">String to be converted.</param>
		/// <returns>ASCIIL format byte array.</returns>
		protected byte[] toASCIIL(string strInput)
		{
			byte[] baReturnValue = new byte[0];
			string strTemp = strInput;

			try
			{
				// if string is longer than 255, throw an error!
				if(strTemp.Length > 255)
				{
					throw new Exception("String length was greater than ASCIIL limit.");
				}

				// create a correctly sized byte array...
				baReturnValue = new byte[strTemp.Length + 1];

				// ... and populate it
				baReturnValue[0] = (byte)strTemp.Length;
				Encoding.ASCII.GetBytes(strTemp, 0, strTemp.Length, baReturnValue, 1);
			}
			catch(Exception)
			{
				// set the status...
				mCDEFStatus = CDEFStatus.BuildError;
			}

			return baReturnValue;
		}

		/// <summary>
		/// Used internally to convert strings into the ASCII16 format -
		/// 16 bit length followed by ASCII of that length.
		/// </summary>
		/// <param name="strInput">String to be converted.</param>
		/// <returns>ASCII16 format byte array.</returns>
		protected byte[] toASCII16(string strInput)
		{
			UInt16 MAX_SIZE = 0xFFFF;

			byte[] baReturnValue = new byte[0];
			string strTemp = strInput;

			try
			{
				// if string is longer than MAX_SIZE, throw an error!
				if(strTemp.Length > MAX_SIZE)
				{
					throw new Exception("String length was greater than ASCII16 limit.");
				}

				// create a correctly sized byte array...
				baReturnValue = new byte[strTemp.Length + 2];

				// ... and populate it
				byte[] byteLength = BitConverter.GetBytes((UInt16)strTemp.Length);
				byteLength.CopyTo(baReturnValue, 0);
				Encoding.ASCII.GetBytes(strTemp, 0, strTemp.Length, baReturnValue, 2);
			}
			catch(Exception)
			{
				// set the status...
				mCDEFStatus = CDEFStatus.BuildError;
			}

			return baReturnValue;
		}

		/// <summary>
		/// Used internally to convert strings into the ASCII32 format -
		/// 32 bit length followed by ASCII of that length.
		/// </summary>
		/// <param name="strInput">String to be converted.</param>
		/// <returns>ASCII32 format byte array.</returns>
		protected byte[] toASCII32(string strInput)
		{
			UInt32 MAX_SIZE = 0xFFFFFFFF;

			byte[] baReturnValue = new byte[0];
			string strTemp = strInput;

			try
			{
				byte[] byteString = new byte[strTemp.Length];
				Encoding.ASCII.GetBytes(strTemp, 0, strTemp.Length, byteString, 0);

				// if string is longer than MAX_SIZE, throw an error!
				if(byteString.LongLength > MAX_SIZE)
				{
					throw new Exception("String length was greater than ASCII32 limit.");
				}

				// create a correctly sized byte array...
				baReturnValue = new byte[byteString.LongLength + 4];

				// ... and populate it
				byte[] byteLength = BitConverter.GetBytes((UInt32)byteString.LongLength);
				byteLength.CopyTo(baReturnValue, 0);
				byteString.CopyTo(baReturnValue, 4);
			}
			catch(Exception)
			{
				// set the status...
				mCDEFStatus = CDEFStatus.BuildError;
			}

			return baReturnValue;
		}

		/// <summary>
		/// Generate a hex string version of the CDEF Message.
		/// </summary>
		/// <param name="UseRawMessage">Flag to indicate whether to use currently stored
		/// message or to rebuild it prior to break down.</param>
		/// <returns>CDEF Message converted to a hex string.</returns>
		public string ToHexString(bool UseRawMessage)
		{
			string strReturnValue = string.Empty;

			// using the raw message means less processing, so if it
			// is selected...
			if(UseRawMessage == true) { strReturnValue = bytesToHexString(mMessage); }
			else { strReturnValue = bytesToHexString(Message); }

			return strReturnValue;
		}

		/// <summary>
		/// Overloaded version of ToHexString - defaults to generating message
		/// prior to breaking it down.
		/// </summary>
		/// <returns>CDEF Message converted to a hex string.</returns>
		public string ToHexString()
		{
			return ToHexString(false);
		}

		/// <summary>
		/// Convert a byte array into a hex string.
		/// </summary>
		/// <param name="bytes">Data for conversion.</param>
		/// <returns>Hex string version of byte array.</returns>
		private string bytesToHexString(byte[] bytes)
		{
			byte[] output = new byte[bytes.Length * 2];

			for(int index=0; index < bytes.Length; index++)
			{
				int ln = bytes[index]>>4;
				int rn = bytes[index]&0x0f;

				output[index*2] = (byte)(ln<10?ln+'0':ln+'A'-10);
				output[index*2+1] = (byte)(rn<10?rn+'0':rn+'A'-10);
			}

			return "0x" + Encoding.ASCII.GetString(output);
		}

		#endregion
	}
}
