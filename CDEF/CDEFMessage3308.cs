using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// Network Establishment – Connect : 0x3308
	/// </summary>
	/// <remarks>
	/// For response message, use CDEFMessage3308R.
	/// </remarks>
	public class CDEFMessage3308 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;
		protected byte		mDeviceType = 0;
		protected UInt16	mTerminalNumber = 0;
		//protected UInt16	mSalesLocationCode = 0;

		#endregion

		#region Constructors

		public CDEFMessage3308() : base()
		{
			mFunctionCode = (Int16)FuncCode.Session_Establishment_Connect;
			m_protocol = (byte)ProtocolType.Session;
		}

		public CDEFMessage3308(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage3308(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get { return mFunctionFlags; }
			set { mFunctionFlags = value; }
		}

		public byte DeviceType
		{
			get	{ return mDeviceType; }
			set	{ mDeviceType = value; }
		}

		public UInt16 TerminalNumber
		{
			get { return mTerminalNumber; }
			set	{ mTerminalNumber = value; }
		}

//		public UInt16 SalesLocationCode
//		{
//			get	{ return mSalesLocationCode; }
//			set	{ mSalesLocationCode = value; }
//		}

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
					mDeviceType = mCDEFReader.ReadByte();
					mTerminalNumber = mCDEFReader.ReadUInt16();
					//mSalesLocationCode = mCDEFReader.ReadUInt16();

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
					//mCDEFWriter.Write(mSalesLocationCode);

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
				//sb.Append("SalesLocationCode=" + mSalesLocationCode + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
