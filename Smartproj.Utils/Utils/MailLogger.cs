using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace Smartproj.Utils
{
    public class MailLogger
    {
        private MailAddress mMail;
        private SmtpClient mSmtpClient;
        public void Open(string _host, int _timeout, string _mail, string _display, string _login, string _passw)
        {
            mMail = new MailAddress(_mail, _display);

            mSmtpClient = new SmtpClient(_host);
            mSmtpClient.EnableSsl = true;
            mSmtpClient.UseDefaultCredentials = false;
            mSmtpClient.Credentials = new System.Net.NetworkCredential(_login, _passw);
            mSmtpClient.Timeout = _timeout;
            mSmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
        }
        public void Close()
        {
            if (IsRunning)
            {
                mSmtpClient.Dispose();
                mSmtpClient = null;
            }
        }
        public void Send(string _message, string _subject, IEnumerable<string> _clients)
        {
            if (IsRunning && _clients != null && _clients.Count() > 0)
            {
                using (MailMessage message = new MailMessage())
                {
                    message.From = mMail;

                    foreach (string client in _clients)
                    {
                        message.To.Add(client);
                    }

                    message.Body = _message;
                    message.BodyEncoding = System.Text.Encoding.UTF8;
                    message.Subject = _subject;
                    message.SubjectEncoding = System.Text.Encoding.UTF8;

                    mSmtpClient.Send(message);
                }
            }
        }
        public bool IsRunning => mSmtpClient != null;
    }
}
