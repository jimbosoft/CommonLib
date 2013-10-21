using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace CommonLib.Comms
{
    public interface IListenerHandler
    {
        void NewConnection(Socket newConnection);
        void ListenError(Exception ex);
        void PrintError(String errorMsg);
    }
}
