using System;
using System.Diagnostics;
using System.Text;

namespace CommonLib.CDEF

{
	public class Helper0310
	{
		public Helper0310()
		{
			CDEFMessage0310 msg = new CDEFMessage0310(); 
			GenMsg f = new GenMsg(msg.CreateMsg);
			CDEFMessage.RegisterMsg(310, f);
		}
	}
	/// <summary>
	/// Heartbeat : 0x0310
	/// </summary>
	public class CDEFMessage0310 : CDEFMessage
	{
		public static Helper0310 nada = new Helper0310();

		#region Variables and Definitions

		// message variables
		protected byte		mFunctionFlags = 0;

		#endregion

		#region Constructors

		public CDEFMessage0310() : base()
		{
			mFunctionCode = (Int16)FuncCode.Heartbeat;
			m_protocol = (byte)ProtocolType.Session;
		}

		public CDEFMessage0310(byte[] newMessage) : base(newMessage)
		{
		}

		public override CDEFMessage CreateMsg(byte[] newMessage)
		{
			return base.CreateMsg(newMessage);
		}
		public CDEFMessage0310(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
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

					// get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception ex)
				{
					EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);

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

					// get the current length of the message
					mLength = mStream.Position;
				}
				catch(Exception ex)
				{
					EventLog.WriteEntry("WEASL", string.Format("Exception: {0} Stack: {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);

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
				sb.Append("\r\nFunctionFlags=" + mFunctionFlags + "\r\n");
			}

			return sb.ToString();
		}

		#endregion
	}
}
