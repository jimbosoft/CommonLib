using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	/// <summary>
	/// Sportsbet – Sequence Number Notification : 0x3106
	/// </summary>
	public class CDEFMessage3106 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte			mFunctionFlags = 0;
		protected byte			mNumOfUpdates = 0;
		protected ArrayList		updateArray = new ArrayList();
		protected int			mRetryTime = 0; // Default

		#endregion

		#region Constructors

		public CDEFMessage3106() : base()
		{
			mFunctionCode = (Int16)FuncCode.SB_SNNotification;
		}

		public CDEFMessage3106(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage3106(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get	{ return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public byte NumOfUpdates
		{
			get	{ return mNumOfUpdates;	}
		}

		public int RetryTime
		{
			get	{ return mRetryTime; }
			set { mRetryTime = value; }
		}

		#endregion

		#region Array Handlers

		public void clearUpdateList()
		{
			updateArray.Clear();
			mNumOfUpdates = 0;
		}

		public bool addUpdateToList(byte newSUT, byte newVersion, Int32 newSN)
		{
			bool bReturnValue = false;

			if(mNumOfUpdates <= 255)
			{
				updateArray.Add(new D3106UpdateItem(newSUT, newVersion, newSN));
				mNumOfUpdates++;

				bReturnValue = true;
			}
			else
			{
				// set the status...
				mCDEFStatus = CDEFStatus.DataError;
			}

			return bReturnValue;
		}

		public D3106UpdateItem[] getUpdateList()
		{
			D3106UpdateItem[] returnArray = new D3106UpdateItem[mNumOfUpdates];

			updateArray.CopyTo(returnArray);

			return returnArray;
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
					mNumOfUpdates = mCDEFReader.ReadByte();
					// loop for updates...
					if(mNumOfUpdates > 0)
					{
						byte	tempSUT;	byte tempVER;	Int32	tempSN;
						for(int i = 0; i < mNumOfUpdates; i++)
						{
							tempSUT = mCDEFReader.ReadByte();
							tempVER = mCDEFReader.ReadByte();
							tempSN = mCDEFReader.ReadInt32();

							updateArray.Add(new D3106UpdateItem(tempSUT, tempVER, tempSN));
						}
					}

					mRetryTime = mCDEFReader.ReadInt32();

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
					mCDEFWriter.Write(mNumOfUpdates);
					// loop for updates...
					if(mNumOfUpdates > 0)
					{
						for(int i = 0; i < mNumOfUpdates; i++)
						{
							D3106UpdateItem tempItem = (D3106UpdateItem)updateArray[i];
							mCDEFWriter.Write(tempItem.SportsbetUpdateType);
							mCDEFWriter.Write(tempItem.VersionNumber);
							mCDEFWriter.Write(tempItem.SequenceNumber);
						}
					}

					mCDEFWriter.Write(mRetryTime);

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
				sb.Append("NumOfUpdates=" + mNumOfUpdates + "\r\n");
				foreach(D3106UpdateItem update in updateArray)
				{
					sb.Append("\tUpdateType=" + update.SportsbetUpdateType + "\r\n");
					sb.Append("\tVersionNumber=" + update.VersionNumber + "\r\n");
					sb.Append("\tSequenceNumber=" + update.SequenceNumber + "\r\n");
				}
			}

			return sb.ToString();
		}

		#endregion
	}

	public struct D3106UpdateItem
	{
		private byte	mSportsbetUpdateType;
		private byte	mVersion;
		private Int32	mSequenceNumber;

		public D3106UpdateItem(byte newSUT, byte newVersion, Int32 newSN)
		{
			mSportsbetUpdateType = newSUT;
			mSequenceNumber = newSN;
			mVersion = newVersion;
		}

		public byte SportsbetUpdateType
		{
			get { return mSportsbetUpdateType; }
			set { mSportsbetUpdateType = value; }
		}

		public byte VersionNumber
		{
			get { return mVersion; }
			set { mVersion = value; }
		}

		public Int32 SequenceNumber
		{
			get { return mSequenceNumber; }
			set { mSequenceNumber = value; }
		}
	}
}
