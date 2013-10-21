using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

using CommonLib.Dispatcher;
using CommonLib.Utilities;
using CommonLib.CDEF;

namespace CommonLib.Comms
{
	/// <summary>
	/// Summary description for CDEF_TCPConnection.
	/// </summary>
	public class CDEF_TCPConnection : TCPConnection 
	{
        public enum EtCDEFConEvents
        {
            eHearbeatFailure = TCPConnection.EtConnectionEvents.eLast,
            eLast
        }
        
        private ILogger mLogger = null;
		//private ManualResetEvent mSendDone = new ManualResetEvent(true);
		//private CDEFSession mConsumer = null;
        private Dictionary<CDEFSession.SessionKey, CDEFSession> mSessions = new Dictionary<CDEFSession.SessionKey, CDEFSession>();
        private IConnection mOwner = null;
		private Timer mHeartbeatTimer = null;
		private Timer mReceiveTimer = null;
		private byte[] mheartMsg = null;
		private int mHeartbeat = 0;
		private int mReceiveTime = 0;
		private bool mHeartbeatON = false;
		private int mUniqueID;
        private bool mRawReceive = false;
		public SessionDescriptor mHeartTo = new SessionDescriptor(0,0,0,0);
		public SessionDescriptor mHeartFrom = new SessionDescriptor(0,0,0,0);
		public ManualResetEvent mDisconnect = new ManualResetEvent(false);

        public CDEF_TCPConnection(Socket newCon, ILogger logger, IConnection owner,
                                int heartbeat, int receiveTimeout, int id)
                                : base(newCon)
        {
            Init(logger, owner, heartbeat, receiveTimeout, id);
        }
        public CDEF_TCPConnection(ILogger logger, IConnection owner,
			int heartbeat, int receiveTimeout, int id)
		{
            Init(logger, owner, heartbeat, receiveTimeout, id);
        }

        public void SetRawReceive() { mRawReceive = true; }

        private void Init(ILogger logger, IConnection owner,
			            int heartbeat, int receiveTimeout, int id)
        {
			mLogger = logger;
			mOwner = owner;
			mHeartbeat = heartbeat;
			mReceiveTime = receiveTimeout;
			mUniqueID = id;

			TimerCallback heartDelegate = new TimerCallback(HeartbeatCb);
			mHeartbeatTimer = new Timer(heartDelegate, null, 
				Timeout.Infinite, Timeout.Infinite);

			TimerCallback recDelegate = new TimerCallback(HeartbeatFailureCb);
			mReceiveTimer = new Timer(recDelegate, null, 
				Timeout.Infinite, Timeout.Infinite);
		}
        public int GetUniqueId() { return mUniqueID; }
        //--------------------------------------------------------------------
        public void RegisterSession(CDEFSession sess)
        {
            if (mSessions.ContainsKey(sess.GetKey))
            {
                throw new Exception("Session: " + sess.ToString()
                 + " already exists on connection " + this.ToString());
            }
            else
            {
                mSessions.Add(sess.GetKey, sess);
            }
        }
		//--------------------------------------------------------------------
        public void DeRegisterSession(CDEFSession sess)
        {
            if (mSessions.ContainsKey(sess.GetKey))
            {
                mSessions.Remove(sess.GetKey);
            }
            //
            // If it is the last session and we are not waiting for 
            // a disconnect as a result of sending a 3309
            //
            if ((mSessions.Count == 0) && !mDisconnect.WaitOne(0, false))
            {
                Disconnect();
            }
        }
        public int GetSessionCount()
        {
            return mSessions.Count;
        }
        //--------------------------------------------------------------------
        public void EnableHeartbeats()
		{
			if (mHeartbeat > 0)
			{
				CDEFMessage0310 heart = new CDEFMessage0310();
				heart.FromDesc = mHeartFrom;
				heart.ToDesc = mHeartTo;
				mheartMsg = heart.Message;
			
				mHeartbeatON = true;
				ResetHeartbeat();
			}
            if (mReceiveTime > 0)
            {
				ResetReceiveTimer();
            }
		}
		public void DisableHeartbeats()
		{
			mHeartbeatON = false;
			mHeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
			mReceiveTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}
		public void ResetHeartbeat()
		{
			mHeartbeatTimer.Change(mHeartbeat, Timeout.Infinite);
		}
		public void ResetReceiveTimer()
		{
			mReceiveTimer.Change(mReceiveTime, Timeout.Infinite);
		}
		public void HeartbeatCb(Object stateInfo)
		{
			try
			{
				if (this.Connected())
				{
					Send(mheartMsg);
				}
			}
			catch (Exception ex)
			{
				mLogger.Write(mUniqueID, "CDEF_TCPCon", 
					"Failed to send hearbeat msg: " + ex.Message, LogLevel.Error);
			}
		}
        //-----------------------------------------------------------------------
		public void HeartbeatFailureCb(Object stateInfo)
		{
			mLogger.Write(mUniqueID, "CDEF_TCPCon", 
				"ReceiveTimeout - disconnecting" + " current state: " + State.GetState(), LogLevel.Warning);
            RaiseEvent((int)EtCDEFConEvents.eHearbeatFailure);
			//mOwner.ConnectionErrorEvt("ReceiveTimeout, remote connection failed to send heartbeats");
		}
		//--------------------------------------------------------------------
		/// <summary>
		/// Thread safe sending of data
		/// </summary>
		/// <param name="msg"></param>
		public void Send(byte[] msg)
		{
			try
			{
				RawSend(msg);
			}
			catch (Exception ex)
			{
				mLogger.Write(mUniqueID, "CDEF_TCPCon", 
					"Send failed for : " + String .Format("{0:X} ",CDEFMessage.GetFunc(msg,0)) 
					+ ex.Message, LogLevel.Warning);
				Disconnect();
			}

			if (mHeartbeatON)
				ResetHeartbeat();
		}
		//--------------------------------------------------------------------
		/// <summary>
		/// Uses as much of the buffer as it desires. 
		/// Maybe it should be abstract as the children are expected to override it.
		/// Return the number of bytes used
		/// </summary>
		/// <param name="buffer"> array of bytes available </param>
		/// <returns> the number of bytes that were consumed </returns>
		protected override int ReadAndStoreCb(byte[] buffer, int idx, int bufferLength)
		{
			int takeLength = 0;

            if ((mReceiveTime > 0))
				ResetReceiveTimer();
			//
			// If we are disconnecting, through away all incoming messages
			//
			if (mDisconnect.WaitOne(0,false))
			{
				return bufferLength;
			}
            if (mRawReceive)
            {
                byte[] msgBuffer = new byte[bufferLength];
                Buffer.BlockCopy(buffer, idx, msgBuffer, 0, bufferLength);
                mOwner.ReceivedCb(msgBuffer, mUniqueID);
                return bufferLength;
            }
            if (bufferLength > CDEFMessage.LENGTHLENGTH)
			{
				int msgLength = 0;
				for ( int i = CDEFMessage.LENGTHLENGTH - 1; i >= 0; i-- )
				{
					msgLength <<= 8;
					msgLength |= buffer[idx + i];
				}
				if (msgLength > CDEFMessage.CDEF_MAX_SIZE)
				{
                    mLogger.Write(mUniqueID, "CDEF_TCPConnection", "Received CDEF length: " + msgLength.ToString()
						+ " exceeded limit: " + CDEFMessage.CDEF_MAX_SIZE.ToString() 
                        + " current state: " + State.GetState(), LogLevel.Error);

                    Disconnect();
				}
				msgLength += CDEFMessage.LENGTHLENGTH;
				if (bufferLength >= msgLength)
				{
                    //
                    // Handle heartbeat
                    //
					if ((buffer[CDEFMessage.SESS + idx] == 2) 
						&& (buffer[CDEFMessage.HIGH_FUNC + idx - 1] == 0x3)
						&& (buffer[CDEFMessage.LOW_FUNC + idx - 1] == 0x10))
					{
					}
					else //if (mConsumer.Interested(buffer, idx, bufferLength))
					{
                        if (mSessions.Count == 0)
                        {
                            if (mOwner.InterestedConnectionMsgCb(buffer, idx, bufferLength, mUniqueID))
                            {
                                byte[] msgBuffer = new byte[msgLength];
                                Buffer.BlockCopy(buffer, idx, msgBuffer, 0, msgLength);
                                mOwner.ReceivedCb(msgBuffer, mUniqueID);
                            }
                        }
                        foreach (KeyValuePair<CDEFSession.SessionKey, CDEFSession> kp in mSessions)
                        {
                            if (kp.Value.InterestedCb(buffer, idx, bufferLength))
                            {
                                byte[] msgBuffer = new byte[msgLength];
                                Buffer.BlockCopy(buffer, idx, msgBuffer, 0, msgLength);
                                kp.Value.EnqueueCb(msgBuffer);
                            }
                        }
					}
					takeLength = msgLength;
				}
			}
			return takeLength;
		}
		//--------------------------------------------------------------------
        //
        // Send a disconnect message and close the connection
        // if that is wanted
        //
        public bool Disconnect(CDEFMessage msg, bool disconnectAfterSend)
		{
			bool ret = true;

			if (this.Connected())
			{
                //
                // Disconnect if that is what they want 
                // may not be what they want if the connection has multiple sessions
                //
                if (disconnectAfterSend)
                {
                    mDisconnect.Set();
                }
				Send(msg.Message);
			}
			else  // we may be in "connecting" state
			{
				ret = Disconnect();
			}
			return ret;
		}
        //-------------------------------------------------------------------
        protected override void SendCompleteEvt(int bytesSent)
        {
            if (mDisconnect.WaitOne(0, false))
            {
                Disconnect();
            }
        }
        //------------------BaseHandler Implementations ----------------------
        //
        // Handle Dispatcher Events
        //
        public override bool HandleEvent(int evt, object argum)
        {
            bool ret = true;

            if (evt < (int)TCPConnection.EtConnectionEvents.eLast)
            {
                return base.HandleEvent(evt, argum);
            }
            if (evt > (int)TCPConnection.EtConnectionEvents.eLast + Enum.GetValues(typeof(EtCDEFConEvents)).Length - 1)
            {
                PrintError("CDEF_TCPConnection received invalid event nr: " + evt.ToString());
                return false;
            }
            EtCDEFConEvents e = (EtCDEFConEvents)evt;
            switch (e)
            {
                case EtCDEFConEvents.eHearbeatFailure:
                    mOwner.ConnectionErrorEvt("ReceiveTimeout, not data received for (ms)" + mReceiveTime.ToString(), mUniqueID);
                    Disconnect();
                    break;
                default:
                    ret = false;
                    break;
            }
            return ret;
        }
 		//--------------------------------------------------------------------
		/// <summary>
		/// Inform the upper layers of event that have occured
		/// </summary>
		protected override void DisconnectedEvt(Exception e)
		{
			string msg;
			DisableHeartbeats();

			if (e != null)
			{
				msg = "Remote Disconnect for " + IP + " " + Port 
					+ " Msg: " + e.Message;
			}
			else
			{
				msg = "Disconnect for " + IP + " " + Port + " current state: " + State.GetState();
			}
			mLogger.Write(mUniqueID, "CDEF_TCPCon", msg, LogLevel.Warning);
            mOwner.DisconnectedEvt(mUniqueID);

            CDEFSession[] sessArr = new CDEFSession[mSessions.Count];
            mSessions.Values.CopyTo(sessArr, 0);

            foreach (CDEFSession s in sessArr)
            {
                s.ConnectionLostEvt();
            }
		}
		//--------------------------------------------------------------------
		protected override void ConnectedEvt( )
		{
			mDisconnect.Reset();
			//EnableHeartbeats();
			string msg = "Connect Successful to: " + IP + " " + Port 
				+ " current state: " + State.GetState();
			mLogger.Write(mUniqueID, "CDEF_TCPCon",msg, LogLevel.Info);
            mOwner.ConnectedEvt(mUniqueID);
		}
		//--------------------------------------------------------------------
		/// <summary>
		/// Something has gone wrong, somebody deal with it
		/// </summary>
		/// <param name="e"> An Exception </param>
		protected override void ConnectFailureEvt( Exception e)
		{
			string msg = "Connect Failed to: " + IP + " " + Port + " :: " + e.Message;
			mLogger.Write(mUniqueID, "CDEF_TCPCon", msg, LogLevel.Warning);
            mOwner.ConnectFailedEvt(mUniqueID);
		}
		//--------------------------------------------------------------------
		protected override void SendErrorEvt( Exception e)
		{
			string msg = "Send Error: " + e.Message;
			mLogger.Write(mUniqueID, "CDEF_TCPCon", msg, LogLevel.Warning);
            mOwner.ConnectionErrorEvt("Send error occured on the local connection", mUniqueID);
		}
		//--------------------------------------------------------------------
		protected override void PrintError(string msg) 
		{
			mLogger.Write(mUniqueID, "TCPCon", msg, LogLevel.Error);
		}
	}
}
