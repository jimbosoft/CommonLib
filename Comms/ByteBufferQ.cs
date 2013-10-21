using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Comms
{
    /**
     * Threadsafe q for byte buffers
     */
    public class ByteBufferQ
    {
        private Queue<byte[]> mBufferQ = new Queue<byte[]>();

        //------------------------------------------------------------------------
        public void Enqueue(byte[] buffer)
        {
            lock (mBufferQ)
            {
                mBufferQ.Enqueue(buffer);
            }
        }
        //------------------------------------------------------------------------
        public byte[] GetNextBuffer()
        {
            byte[] msg = null;

            lock (mBufferQ)
            {
                if (mBufferQ.Count > 0)
                {
                    msg = (byte[])mBufferQ.Dequeue();
                }
            }
            return msg;
        }
        //------------------------------------------------------------------------
        public int Size()
        {
            int size = 0;
            lock (mBufferQ)
            {
                size = mBufferQ.Count;
            }
            return size;
        }
        //------------------------------------------------------------------------
        public int ClearMsgQueue()
        {
            int size = 0;
            lock (mBufferQ)
            {
                size = mBufferQ.Count;
                mBufferQ.Clear();
            }
            return size;
        }
    }
}
