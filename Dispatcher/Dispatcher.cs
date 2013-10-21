using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CommonLib.Dispatcher
{
    public class Dispatcher
    {
        private Queue<int> mEventQ = new Queue<int>();

        private WaitHandle[] mEventList = new WaitHandle[2];
        private AutoResetEvent mShutdown = new AutoResetEvent(false);
        private AutoResetEvent mEventAlert = new AutoResetEvent(false);
        private AutoResetEvent mStarted = new AutoResetEvent(false);

        private Dictionary<int, BaseHandler> mHandlerMap= new Dictionary<int, BaseHandler>();
        private int mLastID = 0;

        public Dispatcher()
        {
            mEventList[0] = mShutdown;
            mEventList[1] = mEventAlert;
        }
        public bool Started()
        {
            return mStarted.WaitOne(0, false);
        }
        public void Stop()
        {
            mShutdown.Set();
        }
        //
        // Starts the main processing loop, doesn not return till Stop() is called
        //
        public void Start()
        {
			int index = 1;
			int handlerNr = 0;
            mStarted.Set();

			while (index != 0)
			{
				index = WaitHandle.WaitAny(mEventList);
                if (index > 0)
                {
                    do
                    {
                        lock (mEventQ)
                        {
                            if (mEventQ.Count > 0)
                            {
                                handlerNr = (int)mEventQ.Dequeue();
                            }
                            else
                            {
                                handlerNr = -1;
                            }
                        }
                        if (handlerNr > -1)
                        {
                            if (mHandlerMap.ContainsKey(handlerNr))
                            {
                                mHandlerMap[handlerNr].ProcessEvent();
                            }
                        }
                    } while (handlerNr >= 0);
                }
                else //let everybody know we a going to die
                {
                    foreach(KeyValuePair<int, BaseHandler> i in mHandlerMap)
                    {
                        i.Value.DispatcherStop();
                    }
                }
			}
            mStarted.Reset();
        }
        //
        // Queue up an eventhandler, which has an event that needs processing
        //
        public void AddHandlerQ(BaseHandler handler)
        {
            lock (mEventQ)
            {
                mEventQ.Enqueue(handler.GetID());
                mEventAlert.Set();
            }
        }
        internal int RegisterHandler(BaseHandler handler)
        {
            lock (mHandlerMap)
            {
                if (handler == null)
                {
                    return -1;
                }

                mLastID++;
                while (mHandlerMap.ContainsKey(mLastID))
                {
                    mLastID++;
                }
                mHandlerMap.Add(mLastID, handler);

                return mLastID;
            }
        }
        internal bool DeRegisterHandler(BaseHandler handler)
        {
            handler.ReleaseResources();

            lock (mHandlerMap)
            {
                if (handler == null)
                {
                    return false;
                }
                return mHandlerMap.Remove(handler.GetID());
            }
        }
    }
}
