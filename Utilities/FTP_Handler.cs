using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace CommonLib.Utilities
{
    public class FTP_Handler
    {
        public class FtpState
        {
            public FtpWebRequest mRequest;
            public string mStatus;
            public MemoryStream mData = new MemoryStream();

            private Exception mOperationException = null;
            private FileInfo mFileName;
            private IRaiseEvent mHandler;
            private int eventId;

            public FtpState(IRaiseEvent h, int e, FileInfo fname)
            {
                mHandler = h;
                eventId = e;
                mFileName = fname;
            }
            public FileInfo FileName
            {
                get { return mFileName; }
            }
            public Exception GetException()
            {
                return mOperationException; 
            }
            internal void RaiseException(Exception ex)
            {
                mOperationException = ex;
                mHandler.RaiseEvent(eventId, this);
            }
            internal void OperationComplete()
            {
                mOperationException = null;
                mHandler.RaiseEvent(eventId, this);
            }
        }
        //----------------------------------------------------------------------------------------------
        static private void SetupRequest(Uri target, FtpState state, string fileName, string user, string passwrd)
        {
            if (target.Scheme != Uri.UriSchemeFtp)
            {
                throw new Exception("UploadRequest: Target uri was not ftp");
            }
            state.mRequest = (FtpWebRequest)WebRequest.Create(target + @"/" + fileName);
            state.mRequest.Proxy = null;
            //
            // This example uses anonymous logon.
            // The request is anonymous by default; the credential does not have to be specified. 
            // The example specifies the credential only to
            // control how actions are logged on the server.
            //
            if (user != null && user.Length != 0)
            {
                state.mRequest.Credentials = new NetworkCredential(user, passwrd); //"anonymous", "janeDoe@contoso.com");
            }
        }
        //----------------------------------------------------------------------------------------------
        /// <summary>
        /// Make a FTP request to get a file from the server.
        /// It will raise an event on the BaseHandler, when done 
        /// </summary>
        /// <param name="target"> url that is the name of the file being uploaded to the server</param>
        /// <param name="state">holds all the required data as supplied by the caller</param>
        /// <param name="user">username used to connect to the server</param>
        /// <param name="passwrd">username used to connect to the server</param>
        static public void DownloadRequest(Uri target, FtpState state, string user, string passwrd)
        {
            try
            {
                SetupRequest(target, state, state.FileName.Name, user, passwrd);
                state.mRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                //
                // Store the request in the object that we pass into the
                // asynchronous operations.
                // Asynchronously get the stream for the file contents.
                //
                state.mRequest.BeginGetResponse(
                    new AsyncCallback(EndDownloadResponseCallback),
                    state);
                
                
                //.BeginGetRequestStream(
                //    new AsyncCallback(EndDownloadGetStreamCallback),
                //    state
                //);
            }
            catch (Exception e)
            {
                state.RaiseException(e);
            }
        }
        //----------------------------------------------------------------------------------------------
        /// <summary>
        /// Make a FTP request to put a file onto the server.
        /// It will raise an event on the BaseHandler, when done 
        /// </summary>
        /// <param name="target"> url that is the name of the file being uploaded to the server</param>
        /// <param name="state">holds all the required data as supplied by the caller</param>
        /// <param name="user">username used to connect to the server</param>
        /// <param name="passwrd">username used to connect to the server</param>
        static public void UploadRequest(Uri target, FtpState state, string user, string passwrd)
        {
            try
            {
                if (!state.FileName.Exists)
                {
                    throw new Exception("FTP_Handler::UploadRequest file: " + state.FileName.FullName
                        + " does not exist");
                }
                SetupRequest(target, state, state.FileName.Name, user, passwrd);
                state.mRequest.Method = WebRequestMethods.Ftp.UploadFile;
                //
                // Store the request in the object that we pass into the
                // asynchronous operations.
                // Asynchronously get the stream for the file contents.
                //
                state.mRequest.BeginGetRequestStream(
                    new AsyncCallback(EndUploadGetStreamCallback),
                    state
                );
            }
            catch (Exception e)
            {
                state.RaiseException(e);
            }
        }
        //----------------------------------------------------------------------------------------------
        static private void EndUploadGetStreamCallback(IAsyncResult ar)
        {
            FtpState state = (FtpState)ar.AsyncState;

            Stream requestStream = null;
            // End the asynchronous call to get the request stream.
            try
            {
                requestStream = state.mRequest.EndGetRequestStream(ar);
                // Copy the file contents to the request stream.
                const int bufferLength = 2048;
                byte[] buffer = new byte[bufferLength];
                //int count = 0;
                int readBytes = 0;
                FileStream stream = state.FileName.OpenRead();
                do
                {
                    readBytes = stream.Read(buffer, 0, bufferLength);
                    requestStream.Write(buffer, 0, readBytes);
                    //count += readBytes;
                }
                while (readBytes != 0);
                // IMPORTANT: Close the request stream before sending the request.
                requestStream.Close();
                // Asynchronously get the response to the upload request.
                state.mRequest.BeginGetResponse(
                    new AsyncCallback(EndUploadResponseCallback),
                    state
                );
            }
            // Return exceptions to the main application thread.
            catch (Exception e)
            {
                state.RaiseException(e);
            }
        }
        //----------------------------------------------------------------------------------------------
        // The EndGetResponseCallback method  
        // completes a call to BeginGetResponse.
        static private void EndUploadResponseCallback(IAsyncResult ar)
        {
            FtpState state = (FtpState)ar.AsyncState;
            FtpWebResponse response = null;
            try
            {
                response = (FtpWebResponse)state.mRequest.EndGetResponse(ar);
                response.Close();
                state.mStatus = response.StatusDescription;
                state.OperationComplete();
            }
            // Return exceptions to the main application thread.
            catch (Exception e)
            {
                state.RaiseException(e);
            }
        }
        //----------------------------------------------------------------------------------------------
        // The EndGetResponseCallback method  
        // completes a call to BeginGetResponse.
        //
        static private void EndDownloadResponseCallback(IAsyncResult ar)
        {
            FtpState state = (FtpState)ar.AsyncState;
            FtpWebResponse response = null;
            try
            {
                response = (FtpWebResponse)state.mRequest.EndGetResponse(ar);
                state.mStatus = response.StatusDescription;

                const int bufferLength = 2048;
                byte[] buffer = new byte[bufferLength];
                int readBytes = 0;
                do
                {
                    readBytes = response.GetResponseStream().Read(buffer, 0, bufferLength);
                    state.mData.Write(buffer, 0, readBytes);
                }
                while (readBytes > 0);
                response.Close();

                state.OperationComplete();
            }
            // Return exceptions to the main application thread.
            catch (Exception e)
            {
                state.RaiseException(e);
            }
        }
    }
}

