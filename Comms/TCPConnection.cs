using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

using CommonLib.Dispatcher;

namespace CommonLib.Comms
{
	// State object for receiving data from remote device.
	public class SocketState
	{
		public enum StateType : int
		{
			connecting,
			connected,
			disconnecting,
			disconnected
		}
		/// <summary>
		/// This has to be protected because the callbacks 
		/// will change it, hence multi-threading
		/// </summary>
		public StateType StateVal 
		{ 
			get { lock(this) { return conState; }}
			set { lock(this) { conState = value; }}
		}
		// Client socket.
		//public Socket workSocket = null;
		// Size of receive buffer.
        public const int BufferSize = CommonLib.CDEF.CDEFMessage.CDEF_MAX_SIZE;
		// Receive buffer.
		public byte[] buffer = new byte[BufferSize];
		// Index into the buffer
		public int readOffset = 0;
		public int dataRemaining = 0;
		private StateType conState = StateType.disconnected;
		public string GetState () 
		{
			switch (StateVal)
			{
				case StateType.connecting: return "connecting"; 
				case StateType.connected: return "connected"; 
				case StateType.disconnecting: return "disconnecting"; 
				case StateType.disconnected: return "disconnected"; 
				default: return "unknown";
			}
		}
	}
	/// <summary>
	/// Summary description for TCPConnection.
	/// </summary>
    public abstract class TCPConnection : BaseHandler
	{
        public enum EtConnectionEvents
        {
            eConnect,
            eConnectFailure,
            eDisconnect,
            eSendComplete,
            eSendError,
            eLast
        }
        // Create a TCP/IP socket.
		private Socket mClient;
		private SocketState state = new SocketState();
		private int mPort;
		private int mLocalPort;
		private string mIPaddr;
        //
        // Incomming connection, used by listener when a connection is established
        //
        public TCPConnection(Socket connectedSocket)
        {
            CleanUp();
            mClient = connectedSocket;
            mIPaddr = ((IPEndPoint)mClient.RemoteEndPoint).Address.ToString();
            mPort = ((IPEndPoint)mClient.RemoteEndPoint).Port;
            //StartReceive();
            state.StateVal = SocketState.StateType.connected;
        }
        //
        // Outgoing connection
        //
        public TCPConnection()
		{
			CleanUp();
		}
		private void CleanUp()
		{
            if (mClient != null)
            {
                mClient.Close();
            }
            mClient = null;
			state.readOffset = 0;
			state.dataRemaining = 0;
			state.StateVal = SocketState.StateType.disconnected;
		}
		//--------------------------------------------------------------------
		public SocketState State
		{
			get { return state; }
		}
		public int Port
		{
			get { return mPort; }
		}
		public int LocalPort
		{
			get { return mLocalPort; }
		}
		public string IP
		{
			get { return mIPaddr; }
		}
        public override string ToString()
        {
            return IP + " " + Port.ToString();
        }
		//--------------------------------------------------------------------
		/// <summary>
		/// Make a connection to the give ip and port address
		/// THROWS EXCEPTIONS: to make sure the called does not miss the failure
		/// </summary>
		/// <param name="ip"> ip address, if incorrect will throw exeption</param>
		/// <param name="port"> port nr</param>
		public void Connect(string ip, int port)
		{
			lock(this)
			{
				if (state.StateVal == SocketState.StateType.disconnected)
				{
					mPort = port;
					mIPaddr = ip;
					mClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
					// Create the state object.
					//SocketState state = new SocketState();
					//state.workSocket = mClient;
					state.StateVal = SocketState.StateType.connecting;
					// Connect to the remote endpoint.
					mClient.BeginConnect( remoteEP, new AsyncCallback(ConnectCb), state);
				}
				else if ((state.StateVal != SocketState.StateType.connected) 
						|| ((IP != ip) && (Port != port)))
				{
					throw (new InvalidOperationException("Connection state: " + state.GetState()));
				}
			}
		}
		//--------------------------------------------------------------------
		public bool Connected()
		{
			return ((mClient != null) && (mClient.Connected));
		}
		//--------------------------------------------------------------------
		private void ConnectCb(IAsyncResult ar) 
		{
			lock (this)
			{
				try 
				{
					// Retrieve the socket from the state object.
					//SocketState state = (SocketState) ar.AsyncState;

					// Complete the connection.
					mClient.EndConnect(ar);
					//
					// Complete the connection process if we are in the same state
					//
					if (state.StateVal == SocketState.StateType.connecting)
					{
						//
						// Open the connection for incoming data
						//
						state.StateVal = SocketState.StateType.connected;
						StartReceive();
						mLocalPort = ((IPEndPoint)mClient.LocalEndPoint).Port;
						//ConnectedEvt();
                        RaiseEvent((int)EtConnectionEvents.eConnect);
					}
					else // If somebody has called disconnect in the mean time - abandon
					{
						//
						// Only call disconnect if we have a connection 
						// don't call it if the connection failed or
						// disconnect will blow up
						//
						if (mClient.Connected)
						{
							state.StateVal = SocketState.StateType.connected;
						}
						HandleDisconnectionCb(null);
					}
				} 
				catch (Exception e) 
				{
                    RaiseEvent((int)EtConnectionEvents.eConnectFailure, e);
					CleanUp();
				}
			}
		}
		//--------------------------------------------------------------------
		/// <summary>
		/// Start the receiving process
		/// </summary>
		public void StartReceive() 
		{
			// Create the state object.
			//SocketState state = new SocketState();
			// Begin receiving the data from the remote device.
			mClient.BeginReceive( state.buffer, 0, SocketState.BufferSize, 0,
				new AsyncCallback(ReceiveCb), state);
		}
		//--------------------------------------------------------------------

		private void ReceiveCb( IAsyncResult ar ) 
        {
			try 
			{
				// Retrieve the state object and the client socket 
				// from the asynchronous state object.
				SocketState state = (SocketState) ar.AsyncState;
				// Read data from the remote device.
				int bytesRead = mClient.EndReceive(ar);

				if (bytesRead > 0) 
				{
					state.dataRemaining += bytesRead;
					int bytesUsed = 0;
					do
					{
						//
						// Keep passing it to the upper layer till they return 0
						//
						bytesUsed = ReadAndStoreCb(state.buffer, state.readOffset, state.dataRemaining);
						state.dataRemaining -= bytesUsed;
						state.readOffset += bytesUsed;

					} while ((bytesUsed != 0) && (state.dataRemaining > 0));
					//
					// If there is data left and we used more then half the buffer
					// Shuffel the remaining bytes to the start of the buffer
					// Else keep filling the buffer from the offset
					//
					int fillOffset = 0;
					if (state.dataRemaining > 0)
					{
						if (state.readOffset + state.dataRemaining > SocketState.BufferSize / 2)
						{
							//PrintError("Doing the shuffle ...");
							Buffer.BlockCopy(state.buffer, state.readOffset, state.buffer, 0, state.dataRemaining);
							state.readOffset = 0;
							fillOffset = state.dataRemaining;
						}
						else
						{
							fillOffset = state.readOffset + state.dataRemaining;
						}
					}
					else	// no data left, fill from the start again
					{
						state.readOffset = 0;
					}
					mClient.BeginReceive(state.buffer, fillOffset,
						SocketState.BufferSize - fillOffset, 0,
						new AsyncCallback(ReceiveCb), state);
				} 
				else // 0 byets received == disconnect
				{
					throw(new Exception("0 byte read"));
				}
			}
			catch (Exception e) 
			{
				HandleDisconnectionCb(e);
			}
		}
		//--------------------------------------------------------------------
		private void HandleDisconnectionCb(Exception e)
		{
			lock(this)
			{
				//
				// if this was not caused by a disconnect call, do it now
				// eg the other side disconnected
				//
				if (state.StateVal != SocketState.StateType.disconnecting)
				{
					Disconnect();
				}
				else // if we were disconnecting a excpetion is normal - ignore
				{
					e = null;
				}
                RaiseEvent((int)TCPConnection.EtConnectionEvents.eDisconnect, e);
				CleanUp();
			}
		}
		//--------------------------------------------------------------------
		public bool Disconnect()
		{
			bool ret = true;

			lock(this)
			{
				try
				{
					//
					// Only try to disconnect if we are connected, else set the next state
					// Nothing can be assume in a async environment, we could be connecting ...
					//
					if (state.StateVal == SocketState.StateType.connected)
					{
						state.StateVal = SocketState.StateType.disconnecting;
						mClient.Shutdown(SocketShutdown.Both);
						mClient.Close();
						if (mClient.Connected) 
						{
							ret = false;
						}
					}
					else if (state.StateVal == SocketState.StateType.connecting)
					{
						state.StateVal = SocketState.StateType.disconnecting;
					}
					else
					{
						 ret = false;
					}
				}
				catch
				{
					state.StateVal = SocketState.StateType.disconnecting;
					HandleDisconnectionCb(null);
				}
				return ret;
			}
		}
		//--------------------------------------------------------------------

		protected void RawSend(byte[]sendBuffer) 
		{
			lock(this)
			{
				if (Connected())
				{
					// Create the state object.
					//SocketState state = new SocketState();
					// Begin sending the data to the remote device.
					mClient.BeginSend(sendBuffer, 0, sendBuffer.Length, 0,
						new AsyncCallback(SendCompleteCb), state);
				}
				else
				{
					throw (new InvalidOperationException("Connection not open, send invalid"));
				}
			}
		}
		//--------------------------------------------------------------------

		private void SendCompleteCb(IAsyncResult ar) 
		{
			try 
			{
				// Complete sending the data to the remote device.
                Int32 i = 0;
                if (mClient != null)
                {
                    i = mClient.EndSend(ar);
                }
                RaiseEvent((int)EtConnectionEvents.eSendComplete, i);
			} 
			catch (ObjectDisposedException o) 
			{
                RaiseEvent((int)EtConnectionEvents.eSendError, new Exception("Send failed due to disconnection"));
            }
            catch (Exception e)
            {
                RaiseEvent((int)EtConnectionEvents.eSendError, e);
			}
		}
        //------------------BaseHandler Implementations ----------------------
        //
        // Handle Dispatcher Events
        //
        public override bool HandleEvent(int evt, object argum)
        {
            bool ret = true;

            if (evt < 0 || evt >= (int)EtConnectionEvents.eLast)
            {
                PrintError("TCPConnection received invalid event nr: " + evt.ToString());
                return false;
            }
            EtConnectionEvents ev = (EtConnectionEvents)evt;
            switch (ev)
            {
                case EtConnectionEvents.eConnect:
                    ConnectedEvt();
                    break;
                case EtConnectionEvents.eConnectFailure:
                    ConnectFailureEvt((Exception)argum);
                    break;
                case EtConnectionEvents.eDisconnect:
                    if (argum == null)
                    {
                        DisconnectedEvt(null);
                    }
                    else
                    {
                        DisconnectedEvt((Exception)argum);
                    }
                    break;
                case EtConnectionEvents.eSendComplete:
                    int i = Convert.ToInt32(argum);
                    SendCompleteEvt(i);
                    break;
                case EtConnectionEvents.eSendError:
                    SendErrorEvt((Exception)argum);
                    break;
                default:
                    PrintError("TCPConnection received invalid event: " + evt.ToString());
                    ret = false;
                    break;
            }
            return ret;
        }
        public override void DispatcherStop()
        {
            this.Disconnect();
        }
		//--------------------------------------------------------------------
		/// <summary>
		/// It is ok to igmore this event an rely on the 
		/// operatings system ability to buffer ougoing data
		/// </summary>
		/// <param name="bytesSent"></param>
		protected virtual void SendCompleteEvt(int bytesSent)
		{
		}
		//--------------------------------------------------------------------
		protected virtual void PrintError(string msg) {}
		/// <summary>
		/// Uses as much of the buffer as it desires. 
		/// Maybe it should be abstract as the children are expected to override it.
		/// Return the number of bytes used
		/// </summary>
		/// <param name="buffer"> array of bytes available </param>
		/// <returns> the number of bytes that were consumed </returns>
		protected abstract int ReadAndStoreCb(byte[] buffer, int idx, int length);
		/// <summary>
		/// The connection has been established
		/// </summary>
		/// <param name="e"></param>
		protected abstract void ConnectedEvt();
		/// <summary>
		/// Inform the upper layers of event that have occured
		/// </summary>
		protected abstract void DisconnectedEvt(Exception e);
		/// <summary>
		/// Something has gone wrong, somebody deal with it
		/// </summary>
		/// <param name="e"> An Exception </param>
		protected abstract void ConnectFailureEvt( Exception e);
		protected abstract void SendErrorEvt( Exception e);
		//--------------------------------------------------------------------
	}
}
