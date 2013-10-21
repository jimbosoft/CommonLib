using System;
using System.Threading;
using System.Collections.Generic;

using CommonLib.CDEF;
using CommonLib.Dispatcher;
using CommonLib.Utilities;

namespace CommonLib.Comms
{
	/// <summary>
	/// Summary description for CDEFConsumer.
	/// </summary>
	public class CDEFSession : BaseHandler
	{
        public class SessionKey : IComparable
        {
            private SessionDescriptor mToAddr = new SessionDescriptor(0,0,0);
            private SessionDescriptor mFromAddr = new SessionDescriptor(0, 0, 0);

            public SessionKey(SessionDescriptor to, SessionDescriptor from)
            {
                mToAddr = to;
                mFromAddr = from;
            }
            public int CompareTo(object obj)
            {
                if (obj is SessionKey)
                {
                    SessionKey rhs = (SessionKey)obj;

                    uint to = mToAddr.Descriptor;
                    uint from = mFromAddr.Descriptor;

                    if ( to == rhs.mToAddr.Descriptor 
                        && from == rhs.mFromAddr.Descriptor)
                    {
                        return 0;
                    }
                    else if (to == rhs.mToAddr.Descriptor)
                    {
                        return from.CompareTo(rhs.mFromAddr.Descriptor);
                    }
                    else
                    {
                        return to.CompareTo(rhs.mToAddr.Descriptor);
                    }
                }
                else
                {
                    throw new ArgumentException("Object is not a SessionKey");
                }
            }
            public override String ToString()
            {
                return "To: " + mToAddr.SalesLocation.ToString() + "," 
                        + mToAddr.AddressType.ToString() + "," 
                        + mToAddr.UniqueAddress.ToString()
                    + " From: " + mFromAddr.SalesLocation.ToString() + ","
                        + mFromAddr.AddressType.ToString() + ","
                        + mFromAddr.UniqueAddress.ToString();
            }
        }

        public enum EtSessionEvents
        {
            eMsgReady,
            eSessionConnected,
            eSessionReconnect,
            eSessionConnectFailed,
            eSessionDisconnect,
            eLogError,
            eLogMessage,
            eLast
        }

        private ByteBufferQ mReceiveQ = new ByteBufferQ();

		protected ManualResetEvent mSessionConnected = new ManualResetEvent(false);
        private Timer mReconSessTimer = null;
        private int mReconSessTime = 0;
        private const int DEFAULT_RECON_SESS_TIME = 20; 

        private CDEFConsumer mOwner = null;
        private ILogger mLogger = null;
		private SessionDescriptor mTo = null;
		private SessionDescriptor mFrom = null;
        private SessionKey mkey = null;
		private CDEF_TCPConnection mConn = null;
		private ManualResetEvent mSessionEstablished = new ManualResetEvent(false);
		private bool mAutoConnect = false;
        private bool mSessionPermConnected = false; 

        public CDEFSession(SessionDescriptor to, SessionDescriptor from, 
                           CDEFConsumer parent, ILogger errorlLog, bool isConnected, bool autoConnect)
		{
            mOwner = parent;
            mLogger = errorlLog;
            mAutoConnect = autoConnect;
            mTo = to;
            mFrom = from;
            mkey = new SessionKey(to, from);
            mSessionPermConnected = isConnected;

            if (mSessionPermConnected)
            {
                mSessionEstablished.Set();
            }

            TimerCallback timerSessDelegate = new TimerCallback(ReconnectSessionCb);
            mReconSessTimer = new Timer(timerSessDelegate, null, Timeout.Infinite, Timeout.Infinite);
        }
        public void RegisterConnection(CDEF_TCPConnection conn)
        {
            mConn = conn;
        }
        public CDEF_TCPConnection GetConnection()
        {
            return mConn;
        }
        private int GetConnectionID()
        {
            if (mConn != null)
            {
                return mConn.GetUniqueId();
            }
            return 0;
        }
        private void Reset()
        {
            mReceiveQ.ClearMsgQueue();
            mSessionConnected.Reset();
        }
		public static uint GetFuncType(byte[] buffer, int idx)
		{
			return buffer[CDEFMessage.HIGH_FUNC + idx];
		}
		/// <summary>
		/// We are only interested in some function codes
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="idx"></param>
		/// <param name="bufferLength"></param>
		/// <returns></returns>
		public virtual bool InterestedCb(byte[] buffer, int idx, int bufferLength)
		{
			SessionDescriptor toDesc = new SessionDescriptor(CDEFMessage.Get32bitNr(buffer, idx + 4));

			//
			// Handle session msgs
			//
			if (buffer[CDEFMessage.SESS + idx] == 2)
			{
				return true;
			}
            if (mFrom == null || (toDesc.AddressType != mFrom.AddressType)
                              || (toDesc.SalesLocation != mFrom.SalesLocation)
                              || (toDesc.UniqueAddress != mFrom.UniqueAddress))
            {
                return false;
            }
            else
            {
                return mOwner.InterestedCb(this, buffer, idx, bufferLength);
            }
		}
		public void EnqueueCb(byte[] buffer)
		{
			uint func = CDEFMessage.GetFunc(buffer,0);

			if ((FuncCode)func == FuncCode.Session_Establishment_Connect)
			{
				if (!CheckSessionState())
				{
                    if (CDEFMessage.IsReponse(buffer, 0))
                    {
                        CDEFMessage3308R resp = new CDEFMessage3308R(buffer);

                        if (resp.ConnectCode != 0)
                        {
                            if (resp.TimeDifference > 0)
                            {
                                Int32 t = resp.TimeDifference;
                                RaiseEvent((int)EtSessionEvents.eSessionConnectFailed, t);
                                //mOwner.SessionConnectFailedEvt(resp.TimeDifference);
                            }
                            else
                            {
                                Int32 t = 0;
                                RaiseEvent((int)EtSessionEvents.eSessionConnectFailed, 0);
                                //mOwner.SessionConnectFailedEvt(0);
                            }
                            LogError("CDEFSession::Connect failed with message: "
                                + resp.ConnectionText + " Reconnecting in: " + resp.TimeDifference.ToString());

                            if (mAutoConnect)
                            {
                                mReconSessTimer.Change(resp.TimeDifference, Timeout.Infinite);
                            }
                        }
                        else
                        {
                            mConn.EnableHeartbeats();
                            mFrom.SalesLocation = resp.ToDesc.SalesLocation;
                            mSessionEstablished.Set();
                            LogMessage("CDEFSession::Connect Msg Resp ok. Sales Location: "
                                + resp.ToDesc.SalesLocation.ToString() + " Local Port: " + mConn.LocalPort.ToString());
                            RaiseEvent((int)EtSessionEvents.eSessionConnected);
                            //mOwner.SessionConnectedEvt();
                        }
                    }
				}
				else
				{
					LogError("CDEFSession::Enqueue Received another Connect msg but session already active");
				}
			}
			else if (CheckSessionState())
			{
				if ((FuncCode)CDEFMessage.GetFunc(buffer,0) == FuncCode.Session_Establishment_Disconnect)
				{
					CDEFMessage3309 discon = new CDEFMessage3309(buffer);
					mSessionEstablished.Reset();
					//LogError("CDEFSession::ConnectSession Session disconnected. Reason: " + discon.DisconnectionText);
                    RaiseEvent((int)EtSessionEvents.eSessionDisconnect);

                    if (mAutoConnect)
					{
                        mReconSessTimer.Change(DEFAULT_RECON_SESS_TIME, Timeout.Infinite);
					}
				}
				else
				{
                    mReceiveQ.Enqueue(buffer);
                    RaiseEvent((int)EtSessionEvents.eMsgReady);
				}
			}
			else
			{
				LogError("CDEFSession::Enqueue NO active session but received msg: " + String.Format("{0:X}",func));
			}
		}
        public byte[] GetNextMsg()
        {
            return mReceiveQ.GetNextBuffer();
        }
        public uint GetSaleLocation()
		{
			if (mFrom == null)
			{
				return 0;
			}
			return mFrom.SalesLocation;
		}
        public SessionDescriptor GetToAddress
        {
            get { return mTo; }
        }
        public SessionDescriptor GetFromAddress
        {
            get { return mFrom; }
        }
        public SessionKey GetKey
        {
            get { return mkey; }
        }
		public bool CheckSessionState()
		{
			bool ret = false;

			if ((mFrom == null) || (mTo == null))
			{
				throw new Exception("DisconnectSession failed, to-from address not set");
			}
			if (mConn == null)
			{
				throw new Exception("DisconnectSession failed, no connection registered");
			}
			if (mSessionEstablished.WaitOne(0,false))
			{
				ret = true;
			}
			return ret;
		}
        public bool SessionConnected()
        {
            return mSessionEstablished.WaitOne(0, false);
        }
        public bool DisconnectSession(string reason, bool disconnectAfterSend)
		{
			bool ret = true;

            if (CheckSessionState())
            {
                CDEFMessage3309 disconnectMsg = new CDEFMessage3309();
                disconnectMsg.FromDesc = mFrom;
                disconnectMsg.ToDesc = mTo;
                disconnectMsg.ConnectCode = 0x42;
                disconnectMsg.DisconnectionText = reason;
                mConn.Disconnect(disconnectMsg, disconnectAfterSend);
                mSessionEstablished.Reset();
            }
            else
            {
                mConn.Disconnect();
            }
			return ret; 
		}
		public void ConnectSession()
		{
            if (CheckSessionState())
            {
                LogError("CDEFSession::ConnectSession Sending connect message, but already got a established session");
            }
            else
            {
                CDEFMessage3308 conmsg = new CDEFMessage3308();
                conmsg.FromDesc = mFrom;
                conmsg.ToDesc = mTo;
                conmsg.DeviceType = (byte)DeviceType.RWT;
                mConn.Send(conmsg.Message);
            }
		}
        //------------------------------------------------------------------------
        public void Send(CDEFMessage msg)
        {
            msg.ToDesc = mTo;
            msg.FromDesc = mFrom;
            mConn.Send(msg.Message);
        }
        //------------------------------------------------------------------------
        public void BroadcastSend(CDEFMessage msg)
        {
            msg.ToDesc.Descriptor = SessionDescriptor.BroadcastDescriptor;
            msg.FromDesc = mFrom;
            mConn.Send(msg.Message);
        }
		//--------------------------------------------------------------------		
        public void ReconnectSessionCb(Object stateInfo)
        {
            RaiseEvent((int)EtSessionEvents.eSessionReconnect);
            //if (!CheckSessionState())
            //{
            //    ConnectSession();
            //}
        }
		//--------------------------------------------------------------------
        //
        // Handle locally generated events
        //
        public override bool HandleEvent(int evt, object arguments)
        {
            bool ret = true;
        
            if (evt < 0 || evt >= (int)EtSessionEvents.eLast)
            {
                mLogger.Write(GetConnectionID(), "CDEFSession", ToString()
                                + "CDEFSession received invalid event nr: " + evt.ToString(), LogLevel.Error);
                return false;
            }
            EtSessionEvents e = (EtSessionEvents)evt;
            switch (e)
            {
                case EtSessionEvents.eMsgReady:
                    mOwner.MessageReadyEvt(this);
                    break;
                case EtSessionEvents.eSessionConnected:
                    mOwner.SessionConnectedEvt(this);
                    break;
                case EtSessionEvents.eSessionReconnect:
                    if (!CheckSessionState())
                    {
                        ConnectSession();
                    }
                    break;
                case EtSessionEvents.eSessionConnectFailed:
                    int t = Convert.ToInt32(arguments);
                    mOwner.SessionConnectFailedEvt(this, t);
                    break;
                case EtSessionEvents.eSessionDisconnect:
                    ConnectionLostEvt();
                    break;
                case EtSessionEvents.eLogError:
                    mLogger.Write(GetConnectionID(), "CDEFSession", ToString()
                                    + " " +(string)arguments, LogLevel.Error);
                    //mOwner.LogError(this, (string)arguments);
                    break;
                case EtSessionEvents.eLogMessage:
                    mLogger.Write(GetConnectionID(), "CDEFSession", ToString()
                                    + (string)arguments, LogLevel.Info);
                    //mOwner.LogMessage(this, (string)arguments);
                    break;
                default:
                    ret = false;
                    break;
            }
            return ret;
       }
        //--------------------------------------------------------------------
        public void ConnectionLostEvt()
        {
            int msgs = mReceiveQ.ClearMsgQueue();
            if (!mSessionPermConnected)
            {
                mSessionEstablished.Reset();
            }
            mOwner.SessionDisconnectedEvt(this);
        }
        private void LogMessage(string s)
        {
            RaiseEvent((int)EtSessionEvents.eLogMessage, s + " " + mkey.ToString());
        }
        private void LogError(string s)
        {
            RaiseEvent((int)EtSessionEvents.eLogError, s + " " + mkey.ToString());
        }
        public override string ToString()
        {
            return mkey.ToString();
        }
    }
}
