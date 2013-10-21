using System;
using System.Collections.Generic;
using System.Text;

using CommonLib.CDEF;
using CommonLib.Utilities;

namespace CommonLib.Comms
{
    public delegate void SessionConnectedEvt(CDEFSession source);
    public delegate void SessionConnectFailedEvt(CDEFSession source, int timeout);
    public delegate void SessionDisconnectedEvt(CDEFSession source);
    public delegate bool InterestedCb(CDEFSession source, byte[] buffer, int idx, int bufferLength);
    public delegate void MsgReceivedEvt(CDEFSession source);
    public delegate void LogError(CDEFSession source, string error, LogLevel level);

    public class CDEFConsumer : ICDEFSession
    {
        private InterestedCb mInterested;
        private SessionConnectedEvt mConnected;
        private SessionConnectFailedEvt mConFail;
        private SessionDisconnectedEvt mDiconnected;
        private MsgReceivedEvt mMsgRec;
        private LogError mLogger;


        public CDEFConsumer(InterestedCb i, SessionConnectedEvt sc, SessionConnectFailedEvt cf,
                        SessionDisconnectedEvt sd , MsgReceivedEvt mr)
        {
            mInterested = i;
            mConnected = sc;
            mConFail = cf;
            mDiconnected = sd;
            mMsgRec = mr;
        }
        public bool InterestedCb(CDEFSession source, byte[] buffer, int idx, int bufferLength)
        {
            return mInterested(source, buffer, idx, bufferLength);
        }
        public void SessionConnectedEvt(CDEFSession source)
        {
            mConnected(source);
        }
        public void SessionConnectFailedEvt(CDEFSession source, int timeout)
        {
            mConFail(source, timeout);
        }
        public void SessionDisconnectedEvt(CDEFSession source)
        {
            mDiconnected(source);
        }
        public void MessageReadyEvt(CDEFSession source)
        {
            mMsgRec(source);
        }
    }
}
