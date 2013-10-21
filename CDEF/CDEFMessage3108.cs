using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Text;

namespace CommonLib.CDEF
{
	public class CDEFMessage3108 : CDEFMessage
	{
		#region Variables and Definitions

		// message variables
		protected byte			mFunctionFlags = 0;
		protected byte			mNumOfUpdates = 0;
		protected ArrayList		updateArray = new ArrayList();

		#endregion

		#region Constructors

		public CDEFMessage3108() : base()
		{
			mFunctionCode = (Int16)FuncCode.SB_SNRequest;
		}

		public CDEFMessage3108(byte[] newMessage) : base(newMessage)
		{
		}

		public CDEFMessage3108(CDEFMessage newCDEF) : base(newCDEF)
		{
		}

		#endregion

		#region Properties

		public byte FunctionFlags
		{
			get { return mFunctionFlags; }
			set	{ mFunctionFlags = value; }
		}

		public byte NumOfUpdates
		{
			get { return mNumOfUpdates;	}
		}

		#endregion

		#region Array Handlers

		public void clearUpdateList()
		{
			updateArray.Clear();
			mNumOfUpdates = 0;
		}

		public bool addUpdateToList(SportsbetUpdateType newSUT)
		{
			bool bReturnValue = false;

			if(mNumOfUpdates <= 255)
			{
				updateArray.Add(new D3108UpdateItem((byte)newSUT));
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

		public D3108UpdateItem[] getUpdateList()
		{
			D3108UpdateItem[] returnArray = new D3108UpdateItem[mNumOfUpdates];

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
						byte	tempSUT;
						for(int i = 0; i < mNumOfUpdates; i++)
						{
							tempSUT = mCDEFReader.ReadByte();

							updateArray.Add(new D3108UpdateItem(tempSUT));
						}
					}

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
							D3108UpdateItem tempItem = (D3108UpdateItem)updateArray[i];
							mCDEFWriter.Write(tempItem.SportsbetUpdateType);
						}
					}

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
				foreach(D3108UpdateItem update in updateArray)
				{
					sb.Append("\tUpdateType=" + update.SportsbetUpdateType + "\r\n");
				}
			}

			return sb.ToString();
		}

		#endregion
	}

	public struct D3108UpdateItem
	{
		private byte	mSportsbetUpdateType;

		public D3108UpdateItem(byte newSUT)
		{
			mSportsbetUpdateType = newSUT;
		}

		public byte SportsbetUpdateType
		{
			get { return mSportsbetUpdateType; }
			set { mSportsbetUpdateType = value; }
		}
	}
}
