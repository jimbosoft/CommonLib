using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

using CommonLib.Utilities;

namespace CommonLib.Utilities
{
    public class FileReadWriteState
    {
        private IRaiseEvent mHandler;
        private int mEventT;
        /// <summary>
        /// Filled in the class from the mBuffer
        /// </summary>
        private MemoryStream mStreamBuffer = new MemoryStream();
        private Exception mOperationException = null;
        public FileStream mFileStream;
        public FileInfo mFileInfo;

        /// <summary>
        /// The buffer is filled by the operating system
        /// during a read operation, but may also be filled
        /// manually, by ManuallyAssignBuffer
        /// unfortunatly it has to be public so the os can access it
        /// </summary>
        public byte[] mBuffer = new byte[FileHandler.BUFFER_SIZE];


        public FileReadWriteState(IRaiseEvent bh, int t, FileInfo file)
        {
            mHandler = bh;
            mEventT = t;
            mFileInfo = file;
        }
        internal void RaiseException(Exception ex)
        {
            mOperationException = ex;
            mHandler.RaiseEvent(mEventT, this);
        }
        public Exception GetException()
        {
            return mOperationException;
        }
        public MemoryStream GetStreamBuffer()
        {
            return mStreamBuffer;
        }
        /// <summary>
        /// Manually assigning the buffer,
        /// as opposed to a read operation
        /// </summary>
        /// <param name="buf"></param>
        public void ManuallyAssignBuffer(byte[] buf)
        {
            mBuffer = buf;
            BufferFillComplete(buf.Length);
            AllBufferFillComplete();
        }
        /// <summary>
        /// The next two functions are used 
        /// when reading a file and the mBuffer
        /// is filler by the operating system
        /// </summary>
        /// <param name="bytesRead"></param>
        public void BufferFillComplete(int bytesRead)
        {
            mStreamBuffer.Write(mBuffer, 0, bytesRead);
        }
        public void AllBufferFillComplete()
        {
            mStreamBuffer.Seek(0, SeekOrigin.Begin);
            mHandler.RaiseEvent(mEventT, this);
        }
        /// <summary>
        /// Called when all data has been written to disk
        /// </summary>
        public void AllWriteComplete()
        {
            mHandler.RaiseEvent(mEventT, this);
        }
    }
}
