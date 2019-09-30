using MessageNotifier.Utils;
using MessageNotifierLibrary.Interface;
using MessageNotifierLibrary.Models;
using MessageNotifierLibrary.Models.utils;

namespace MessageNotifier.Models
{
    public class EmailSender : ISender
    {
        /*
        
            PLEASE READ CAREFULLY

            Therefore Google rejects any unauthorized smtp connections, except Telnet (i dunno why, but it works), 
            i wrote my own small telnet smtp client realization, based on TcpClient.
            Seriously, i hate bycicles. But Google prevented me.

             
        */
        public bool Send(User recepient, TextMessage message, Credentials senderCredentials)
        {
            var credentials = senderCredentials.Mail;
            if (credentials == null)
            {
                return false;
            }

            bool success = false;

            if (recepient.Contacts != null)
            {

                foreach (var contact in recepient.Contacts)
                {
                    if (contact.ContactInfo?.Type == ContactType.EMAIL)
                    {
                        //MailMessage mail = new MailMessage(credentials.Address, contact.Value, message.Title, message.Content);

                        //using (var mailClient = new SmtpClient
                        //{
                        //    Host = credentials.Server,
                        //    Port = credentials.Port,
                        //    Credentials = new NetworkCredential(credentials.Address, credentials.Password),
                        //    EnableSsl = true,
                        //    DeliveryMethod = SmtpDeliveryMethod.Network,
                        //    UseDefaultCredentials = false,
                        //    Timeout = 20000
                        //})
                        //{
                        //    try
                        //    {
                        //        mailClient.Send(mail);
                        //        success = true;
                        //    }
                        //    catch
                        //    {
                        //        success = false;
                        //    }
                        //}

                        using (var client = new TelnetSmtpClient(credentials.Server, credentials.Port))
                        {
                            if (client.Connected)
                            {
                                client.Login(credentials.Address, credentials.Password);
                                if (client.LoggedIn)
                                {
                                    success = client.SendMail(credentials.Address, contact.Value, message.Title, message.Content);
                                }
                            }
                        }

                    }
                }
            }

            return success;
        }
    }
}