using System;
using System.Threading;
using System.Collections;

using CommonLib.CDEF;

namespace CommonLib.Comms
{
	/// <summary>
	/// Summary description for Event interface for a (TCP) connection
	/// </summary>
	public interface IConnection
	{
		void DisconnectedEvt(int id);
        void ConnectedEvt(int id);
        void ConnectFailedEvt(int id);
        void ConnectionErrorEvt(string reason, int id);
        //
        // Connections without a session can hand the buffer directly to the owner
        // ... if he is interested ...
        //
        bool InterestedConnectionMsgCb(byte[] buffer, int idx, int bufferLength, int id);
        void ReceivedCb(byte[] buffer, int id);
	}
}
