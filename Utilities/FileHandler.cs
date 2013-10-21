using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace CommonLib.Utilities
{
    public class FileHandler
    {
        /*public class WriteFileState
        {
            private IRaiseEvent mHandler;
            private int mEventT = 0;

            private Exception mOperationException = null;
            public FileStream mFileStream;
            public FileInfo mFileInfo;

            public WriteFileState(IRaiseEvent bh, int t, FileInfo file)
            {
                mHandler = bh;
                mEventT = t;
                mFileInfo = file;
            }
            public Exception GetException
            {
                get { return mOperationException; }
            }
            internal void RaiseException(Exception ex)
            {
                mOperationException = ex;
                mHandler.RaiseEvent(mEventT, this);
            }
            public void AllWriteComplete()
            {
                mHandler.RaiseEvent(mEventT, this);
            }
        }
        */
        public const int BUFFER_SIZE = 4096;

        //--------------------------------------------------------------------------------------
        static public bool WriteFile(IRaiseEvent bh, int t, FileInfo fileInfo, byte[] buffer, bool rename)
        {
            try
            {
                if (fileInfo.Exists)
                {
                    if (rename)
                    {
                        DateTime td = DateTime.Now;
                        int index = fileInfo.FullName.LastIndexOf('.');
                        string newName = fileInfo.FullName.Remove(index, fileInfo.FullName.Length - index);
                        newName = newName + td.ToString("MMddHHmmss") + ".txt";
                        fileInfo.CopyTo(newName);
                    }
                    else
                    {
                        fileInfo.Delete();
                    }
                }

                FileReadWriteState ws = new FileReadWriteState(bh, t, fileInfo);
                ws.mFileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, true);

                IAsyncResult asyncResult = ws.mFileStream.BeginWrite(
                        buffer, 0, buffer.Length,
                        new AsyncCallback(WriteFileComplete), ws);
            }
            catch (Exception e)
            {
                FileReadWriteState st = new FileReadWriteState(bh, t, fileInfo);
                st.RaiseException(e);
            }
           return true;
        }
        //--------------------------------------------------------------------------------------
        static public void ReadFile(IRaiseEvent bh, int t, FileInfo fileInfo)
        {
            try
            {
                if (fileInfo.Exists)
                {
                    FileReadWriteState ws = new FileReadWriteState(bh, t, fileInfo);
                    ws.mFileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);

                    IAsyncResult asyncResult = ws.mFileStream.BeginRead(
                                ws.mBuffer, 0, ws.mBuffer.Length,
                                new AsyncCallback(ReadFileComplete), ws);
                }
                else
                {
                    throw new Exception("File: " + fileInfo.FullName + " does not exist");
                }
            }
            catch (Exception e)
            {
                FileReadWriteState st = new FileReadWriteState(bh, t, fileInfo);
                st.RaiseException(e);
            }
        }
        //--------------------------------------------------------------------------------------
        static private void WriteFileComplete(IAsyncResult asyncResult)
        {
            try
            {
                FileReadWriteState tempState = (FileReadWriteState)asyncResult.AsyncState;
                FileStream fStream = tempState.mFileStream;
                fStream.EndWrite(asyncResult);
                fStream.Close();
                tempState.AllWriteComplete();
            }
            catch (Exception e)
            {
                FileReadWriteState tempState = (FileReadWriteState)asyncResult.AsyncState;
                tempState.RaiseException(e);
            }
        }
        //--------------------------------------------------------------------------------------
        static private void ReadFileComplete(IAsyncResult asyncResult)
        {
            try
            {
                FileReadWriteState fileDetail = (FileReadWriteState)asyncResult.AsyncState;
                int readCount = fileDetail.mFileStream.EndRead(asyncResult);
                //
                // Read 0 means we have reached the end of the stream
                //
                if (readCount == 0)
                {
                    fileDetail.AllBufferFillComplete();
                    fileDetail.mFileStream.Close();
                }
                else
                {
                    fileDetail.BufferFillComplete(readCount);
                    fileDetail.mFileStream.BeginRead(fileDetail.mBuffer, 0, fileDetail.mBuffer.Length,
                                                  new AsyncCallback(ReadFileComplete), fileDetail);
                }
            }
            catch (Exception e)
            {
                FileReadWriteState fileDetail = (FileReadWriteState)asyncResult.AsyncState;
                fileDetail.RaiseException(e);
            }
        }
    }
}
