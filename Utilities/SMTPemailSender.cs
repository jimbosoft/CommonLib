using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.ComponentModel;

namespace CommonLib.Utilities
{
    public class SMTPemailSender
    {
        public class EmailDetails
        {
            private Exception mOperationException = null;
            private IRaiseEvent mHandler;
            private int mEventId;
            public MailMessage mMessage;
            public object mArguments;

            public EmailDetails(IRaiseEvent h, int eventId)
            {
                mHandler = h;
                mEventId = eventId;
            }
            public Exception GetException()
            {
                return mOperationException;
            }
            internal void RaiseException(Exception ex)
            {
                mOperationException = ex;
                mHandler.RaiseEvent(mEventId, this);
            }
            internal void OperationComplete()
            {
                mOperationException = null;
                mHandler.RaiseEvent(mEventId, this);
            }
        }

        private SmtpClient mailClient;

        public SMTPemailSender(string server, int port)
        {
            if (port > 0)
            {
                mailClient = new SmtpClient(server, port);
            }
            else
            {
                mailClient = new SmtpClient(server);
            }
            mailClient.SendCompleted += new SendCompletedEventHandler(SendComplete);
        }
        //-----------------------------------------------------------------------------
        public void SendMessage(MailAddress toAddress, MailAddress fromAddress, 
                                string subject, string msg, List<Attachment> at,
                                EmailDetails state)
        {
            try
            {
                state.mMessage = new MailMessage(fromAddress, toAddress);
                if (at != null && at.Count > 0)
                {
                    foreach(Attachment a in at)
                    {
                        state.mMessage.Attachments.Add(a);
                    }
                }
                state.mMessage.Body = msg;
                state.mMessage.BodyEncoding = System.Text.Encoding.UTF8;
                state.mMessage.Subject = subject;
                state.mMessage.SubjectEncoding = System.Text.Encoding.UTF8;

                mailClient.SendAsync(state.mMessage, state);
            }
            catch (Exception e)
            {
                state.RaiseException(e);
            }
        }
        //-----------------------------------------------------------------------------
        public void CancelMailSend()
        {
                mailClient.SendAsyncCancel();
        }
        //-----------------------------------------------------------------------------
        private void SendComplete(object sender, AsyncCompletedEventArgs e)
        {
            EmailDetails state = (EmailDetails)e.UserState;

            if (e.Cancelled)
            {
                state.RaiseException(new Exception("Send cancelled."));
            }
            if (e.Error != null)
            {
                state.RaiseException(new Exception("Sending mail failed: " + e.Error.ToString()));
            }
            else
            {
                state.OperationComplete();
            }
        }
    }
}
