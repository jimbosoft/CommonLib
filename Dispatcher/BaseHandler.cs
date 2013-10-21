using System;
using System.Collections.Generic;
using System.Text;

using CommonLib.Utilities;

namespace CommonLib.Dispatcher
{
    public abstract class BaseHandler : IRaiseEvent
    {
        private class EventDetail
        {
            public EventDetail(int e, object o)
            {
                evt = e; args = o;
            }
            public int evt;
            public object args;
        }
        private int mHandlerID = 0;
        private Dispatcher mEventPump = null;
        private Queue<EventDetail> mEventQ = new Queue<EventDetail>();

        public BaseHandler()
        {
        }

        public void RegisterHandler(Dispatcher boss)
        {
            if (boss == null)
            {
                throw new Exception("Dispatcher is null");
            }

            mEventPump = boss;
            mHandlerID = boss.RegisterHandler(this);
        }
        public bool DeRegisterHandler()
        {
            EventQClear();

            if (mEventPump != null)
            {
                return mEventPump.DeRegisterHandler(this);
            }
            return false;
        }
        public int GetID()
        {
            return mHandlerID;
        }
        //
        // Queue up an event fro processing
        //
        public void RaiseEvent(int evt)
        {
            RaiseEvent(evt, null);
        }
        public void RaiseEvent(int evt, object parameter)
        {
            EventDetail ed = new EventDetail(evt, parameter);

            lock (mEventQ)
            {
                mEventQ.Enqueue(ed);
            }
            mEventPump.AddHandlerQ(this);
        }
        //
        // We are in the main thread again, time to process the event
        //
        public bool ProcessEvent()
        {
            EventDetail ed = null;
            lock (mEventQ)
            {
                if (mEventQ.Count > 0)
                {
                    ed = mEventQ.Dequeue();
                }
            }
            if (ed != null)
            {
                return HandleEvent(ed.evt, ed.args);
            }
            return false;
        }
        public int EventQSize()
        {
            lock (mEventQ)
            {
                return mEventQ.Count;
            }
        }
        public void EventQClear()
        {
            lock (mEventQ)
            {
                mEventQ.Clear();
            }
        }

        //
        //-------- To be implemented by child -----------
        //
        // If been told it is all over, my child may be interested
        //
        public virtual void DispatcherStop()
        {
        }
        //
        // Child needs to do what needs to be done
        //
        public abstract bool HandleEvent(int evt, object arguments);
        public virtual void ReleaseResources(){}
    }
}
