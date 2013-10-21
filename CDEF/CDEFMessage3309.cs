using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// Network Establishment – Disconnect : 0x3309
	/// </summary>
	public class CDEFMessage3309 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected UInt32	mConnectCode = 0;
		protected Int32		mTimeDifference = 0;
		protected string	mDisconnectionText = ""; // Note: ASCIIL

		#endregion

		#region Constructors

		public CDEFMessage3309() : base()
		{
			mFunctionCode = (Int16)FuncCode.Session_Establishment_Disconnect;
			m_protocol = (byte)ProtocolType.Session;
		}

		public CDEFMessage3309(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage3309(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get { return mFunctionFlags; }
			set { mFunctionFlags = value; }
		}

		public UInt32 ConnectCode
		{
			get { return mConnectCode; }
			set { mConnectCode = value; }
		}

		public Int32 TimeDifference
		{
			get { return mTimeDifference; }
			set { mTimeDifference = value; }
		}

		public string DisconnectionText
		{
			get { return mDisconnectionText; }
			set { mDisconnectionText = value; }
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
					mConnectCode = mCDEFReader.ReadUInt32();
					mTimeDifference = mCDEFReader.ReadInt32();
					// For ASCIIL, data is preceded by 8 bit length... get that first
					mDisconnectionText = Encoding.ASCII.GetString(mCDEFReader.ReadBytes(mCDEFReader.ReadByte()));

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
					mCDEFWriter.Write(mConnectCode);
					mCDEFWriter.Write(mTimeDifference);
					mCDEFWriter.Write(toASCIIL(mDisconnectionText));

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
				sb.Append("ConnectCode=" + mConnectCode + "\r\n");
				sb.Append("TimeDifference=" + mTimeDifference + "\r\n");
				sb.Append("DisconnectionText=" + mDisconnectionText + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
