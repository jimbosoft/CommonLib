using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	public class Helper3308R
	{
		public Helper3308R()
		{
			CDEFMessage3308R msg = new CDEFMessage3308R(); 
			GenMsg f = new GenMsg(msg.CreateMsg);
			CDEFMessage.RegisterMsg(3308, f);
		}
	}
	/// <summary>
	/// Network Establishment – Connect : 0x3308 (Response)
	/// </summary>
	public class CDEFMessage3308R : CDEFMessage
	{
		static Helper3308R nada = new Helper3308R();
		#region Variables and Definitions

        public const UInt32 CONNECT_SUCESS = 0;
        public const UInt32 CONNECT_FAILURE = 1;

		// message variables
		protected byte		mFunctionFlags = 0;
		protected byte		mDeviceType = 0;
		protected UInt16	mTerminalNumber = 0;
        protected UInt32    mConnectCode = CONNECT_SUCESS;
		protected Int32		mTimeDifference = 0;
		protected string	mConnectionText = ""; // Note: ASCIIL

		#endregion

		#region Constructors

		public CDEFMessage3308R() : base()
		{
			
			mFunctionCode = (Int16)FuncCode.Session_Establishment_Connect;
			mSystemFlags = 0x80;
			m_protocol = (byte)ProtocolType.Session;
		}

		public CDEFMessage3308R(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage3308R(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public byte DeviceType
		{
			get { return mDeviceType; }
			set	{ mDeviceType = value; }
		}

		public UInt16 TerminalNumber
		{
			get	{ return mTerminalNumber; }
			set	{ mTerminalNumber = value; }
		}

		public UInt32 ConnectCode
		{
			get { return mConnectCode; }
			set { mConnectCode = value; }
		}

		public Int32 TimeDifference
		{
			get	{ return mTimeDifference; }
			set	{ mTimeDifference = value; }
		}

		public string ConnectionText
		{
			get { return mConnectionText; }
			set { mConnectionText = value; }
		}

		#endregion

		#region Method Overrides

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
					mDeviceType = mCDEFReader.ReadByte();
					mTerminalNumber = mCDEFReader.ReadUInt16();
					mConnectCode = mCDEFReader.ReadUInt32();
					mTimeDifference = mCDEFReader.ReadInt32();
					// For ASCIIL, data is preceded by 8 bit length... get that first
					mConnectionText = Encoding.ASCII.GetString(mCDEFReader.ReadBytes(mCDEFReader.ReadByte()));

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
					mCDEFWriter.Write(mDeviceType);
					mCDEFWriter.Write(mTerminalNumber);
					mCDEFWriter.Write(mConnectCode);
					mCDEFWriter.Write(mTimeDifference);
					mCDEFWriter.Write(toASCIIL(mConnectionText));

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
				sb.Append("DeviceType=" + mDeviceType + "\r\n");
				sb.Append("TerminalNumber=" + mTerminalNumber + "\r\n");
				sb.Append("ConnectCode=" + mConnectCode + "\r\n");
				sb.Append("TimeDifference=" + mTimeDifference + "\r\n");
				sb.Append("ConnectionText=" + mConnectionText + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
