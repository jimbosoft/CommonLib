//using System;
//using System.Threading;
//using System.Collections;


namespace CommonLib.Comms
{
	/// <summary>
	/// Summary description for Event interface CDEF session
	/// </summary>
	public interface ICDEFSession
	{
		void SessionConnectedEvt(CDEFSession source);
        void SessionConnectFailedEvt(CDEFSession source, int timeout);
        void SessionDisconnectedEvt(CDEFSession source);
        bool InterestedCb(CDEFSession source, byte[] buffer, int idx, int bufferLength);
        void MessageReadyEvt(CDEFSession source);
	}
}
