using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using CommonLib.Dispatcher;

namespace CommonLib.Comms
{

    public class TCPListener : BaseHandler
    {
        public enum EtListenerEvents
        {
            eNewConnection,
            eListenError,
            eLast
        }
        private IListenerHandler mOwner = null;
        private Socket mListenSocket;
        private SocketState state = new SocketState();
        private int mListenPort = 0;
        private string mListenIPaddr = "";
        private bool mListening = false;

        public TCPListener(int port, IListenerHandler owner)
        {
            mOwner = owner;
            mListenIPaddr = "0.0.0.0";
            //IPAddress hostIP = (Dns.Resolve(IPAddress.Any.ToString())).AddressList[0];
            mListenPort = port;
        }
        public TCPListener(string listenIp, int listenPort, IListenerHandler owner)
        {
            mOwner = owner;
            mListenIPaddr = listenIp;
            mListenPort = listenPort;
        }
        public void StartListening()
        {
            if (!mListening)
            {
                try
                {
                    mListenSocket = new Socket(AddressFamily.InterNetwork,
                                               SocketType.Stream,
                                               ProtocolType.Tcp);

                    IPEndPoint listenEP = new IPEndPoint(IPAddress.Parse(mListenIPaddr), mListenPort);
                    mListenSocket.Bind(listenEP);

                    // start listening
                    mListenSocket.Listen(5);
                    IAsyncResult res = mListenSocket.BeginAccept(new AsyncCallback(ListenConnectCb), mListenSocket);
                }
                catch (Exception ex)
                {
                    RaiseEvent((int)EtListenerEvents.eListenError, ex);
                }
            }
            else 
            {
                RaiseEvent((int)EtListenerEvents.eListenError, 
                           new InvalidOperationException("Listen failed: Already listening on ip:" 
                           + mListenIPaddr + " port: " + mListenPort));
            }
        }
        //-----------------------------------------------------------------------------------------
        private void ListenConnectCb(IAsyncResult ar)
        {
			lock (this)
			{
                try
                {
                    //Retrieve the socket from the state object. - same mListenSocket 
                    //Socket s  = (Socket) ar.AsyncState;

                    // Complete the connection.
                    Socket newSock = mListenSocket.EndAccept(ar);
                    RaiseEvent((int)EtListenerEvents.eNewConnection, newSock);
                }
                catch (Exception ex)
                {
                    RaiseEvent((int)EtListenerEvents.eListenError, ex);
                }
            }
        }
        //------------------BaseHandler Implementations ----------------------
        //
        // Handle Dispatcher Events
        //
        public override bool HandleEvent(int evt, object argum)
        {
            bool ret = true;

            if (evt < 0 || evt >= (int)EtListenerEvents.eLast)
            {
                mOwner.PrintError(this.ToString() + " received invalid event nr: " + evt.ToString());
                return false;
            }
            EtListenerEvents ev = (EtListenerEvents)evt;
            switch (ev)
            {
                case EtListenerEvents.eNewConnection:
                    Socket newCon = (Socket)argum;
                    mOwner.NewConnection(newCon);
                    IAsyncResult res = mListenSocket.BeginAccept(new AsyncCallback(ListenConnectCb), mListenSocket);
                    break;
                case EtListenerEvents.eListenError:
                    mOwner.ListenError((Exception)argum);
                    break;
                default:
                    mOwner.PrintError(this.ToString() + " received invalid event: " + evt.ToString());
                    ret = false;
                    break;
            }
            return ret;
        }
        public override string ToString()
        {
            return "TCPListener on port: " + mListenPort.ToString();
        }
    }
}
