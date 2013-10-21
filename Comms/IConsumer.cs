using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Comms
{
    public interface IConsumer
    {
        bool Interested(byte[] buffer, int idx, int bufferLength);
        void SessionDisconnectedEvt();
        void SessionConnectedEvt();
    }
}
