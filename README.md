# C# Dispatcher Library

This CommonLib is a collection of useful classes to make life easier when programming in C#.   
The centre piece is **Dispatcher** library.

## The problem to solve

When Microsoft introduced their .NET platform, they replaced the event handling model it used previously with _callback functions_ model. C# refers to them as delegate. This means that  whenever an asynchronous operation is performed, a user function is called. The catch is that this function will be executed in a separate thread. Additionally, the ease at which threads can be created in .Net tempt programmers to use them liberally. The result is a massive multi threading mess that is difficult to control. Once locks are used, the net result is highly unstable and unreliable software.
This Dispatcher removes this problem by reintroducing the event handling model that Microsoft and others used to use. 

## How it works
If you want to create a thread, simply instantiate a **Dispatcher**. All classes that want to process events need to inherit the **BaseHandler** and implement the three virtual and abstract functions. In the asynchronous callback functions  call **RaiseEvent** and pass an _enum_ that identifies what has occurred and the associated _data_. **RaiseEvent** will post the event to the class it is raised in, but one could also raise an event for another class, or even another Dispatcher (thread).

Example TCPConnection registers _ConnectCb_ with a Socket

		public void Connect(string ip, int port)
		{
			lock(this)
			{
				if (state.StateVal == SocketState.StateType.disconnected)
				{
					...
					mClient.BeginConnect( remoteEP, new AsyncCallback(ConnectCb), state);
				}
				else if ...
			}
		}

The _ConnectCb_ raises an event to indicate that the connection is established:

		private void ConnectCb(IAsyncResult ar) 
		{
			lock (this)
			{
				try 
					…
					if (state.StateVal == SocketState.StateType.connecting)
					{
                        …
                        RaiseEvent((int)EtConnectionEvents.eConnect);
					}
					else // If somebody has called disconnect in the mean time - abandon
					{
						…
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

and the HandleEvent function can now process in its own thread:

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
                    ...

This has the effect of serialising all events and eliminate multithreading and locking issues. I have tested it on applications with hundreds of TCP connections processing large amounts of data.

## Comms Library
The Comms library code, as presented, does not compile as I have not included the message library, which is propriety software. Simply modify it to incorporate you own message library format.
 
