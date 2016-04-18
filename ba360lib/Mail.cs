using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Web;
  
namespace ba360lib.Mail
{
    public class Mail       
    {
        /// <summary>
        /// use for send mail to single recipientsor or  send the bulk email's,with the attachment.
        /// <para>add using System.Net; using System.Net.Mail,using ba360lib.Mail this 3 namespaces </para>
        /// <para>recipients email address value for To,CC,Bcc,replyTo.</para>
        /// <para>this parameter accept value like below</para>
        /// <para>for single email address eg. me@me.com</para>
        /// <para>for multiple email address pass the comma seperated emailid string eg.  me@mycompany.com,him@hiscompany.com,her@hercompany.com</para> 
        /// <para>For attachemet  
        /// List&lt;Attachment&gt; attachment = new List&lt;Attachment&gt;();
        ///attachment.Add(new Attachment(uploadfile.InputStream, uploadfile.FileName));
        /// Or use other overloaded method.
        /// </para>
        /// <para>returns ok if mail successfilly dilivered.otherwise return exception or status code.</para>
        /// </summary>
         /// <param name="IsBodyHtml">if you want to html format email body pass the boolean true value. for text format pass booleav false value</param>
        /// <param name="bodyencoding">accepts value like Encoding.UTF8 </param>
        /// <param name="smtpServer">smtp server name.eg.smtp.com or ip address.if smtp contains port pass value like eg. smtp.com:80</param>
        /// <param name="smtpUsername">smtp user name </param>
        /// <param name="smtpPassword">smtp password </param>
        /// <param name="smtpEnableSSL">smtp SSL passs true or false boolean value </param>
        /// <returns>returns ok if mail successfilly dilivered.otherwise return exception</returns> 
        public string Sendmail(string mailFrom, string FromName, string mailTo, string cc, string bcc, string replyTo, string mailbody, string mailsubject, List<Attachment> mailAttachment, MailPriority priority, bool IsBodyHtml, Encoding bodyencoding, string smtpServer, string smtpUsername, string smtpPassword, bool smtpEnableSSL)
        {
            string result = string.Empty;

            MailMessage mailMessage = new MailMessage();

            try
            {
                mailMessage.Sender = new MailAddress(smtpUsername);

                //from address
                if (FromName == string.Empty)
                {
                    mailMessage.From = new MailAddress(mailFrom);

                }
                else
                {
                    mailMessage.From = new MailAddress(mailFrom, FromName);
                }

                //To address
                if (mailTo == string.Empty)
                {

                }
                else
                {
                    //mailMessage.To.Add(new MailAddress(mailTo));
                    string[] SplitedEmailAdress = mailTo.Split(',');

                    foreach (string EMailID in SplitedEmailAdress)
                    {
                        if (EMailID == string.Empty)
                        { }
                        else
                        {
                            mailMessage.To.Add(new MailAddress(EMailID));
                        }
                    }
                }

                //cc address
                if (cc == string.Empty)
                {
                }
                else
                {
                    //mailMessage.CC.Add(cc);
                    string[] SplitedEmailAdress = cc.Split(',');

                    foreach (string EMailID in SplitedEmailAdress)
                    {
                        if (EMailID == string.Empty)
                        { }
                        else
                        {
                            mailMessage.CC.Add(new MailAddress(EMailID));
                        }
                    }
                }

                //bcc address
                if (bcc == string.Empty)
                {
                }
                else
                {
                    //mailMessage.Bcc.Add(bcc);
                    string[] SplitedEmailAdress = bcc.Split(',');

                    foreach (string EMailID in SplitedEmailAdress)
                    {
                        if (EMailID == string.Empty)
                        { }
                        else
                        {
                            mailMessage.Bcc.Add(new MailAddress(EMailID));
                        }
                    }

                }

                //replyTo address
                if (replyTo == string.Empty)
                {

                }
                else
                {

                    string[] SplitedEmailAdress = replyTo.Split(',');

                    foreach (string EMailID in SplitedEmailAdress)
                    {
                        if (EMailID == string.Empty)
                        { }
                        else
                        {
                            mailMessage.ReplyToList.Add(new MailAddress(EMailID));
                        }
                    }
                }

                //attachment
                if (mailAttachment.Count > 0)
                {
                    foreach (Attachment attachment in mailAttachment)
                    {
                        // Attach the newly created email attachment
                        mailMessage.Attachments.Add(attachment);
                    }
                }

                mailMessage.Subject = mailsubject;

                mailMessage.Body = mailbody;

                mailMessage.IsBodyHtml = IsBodyHtml;

                mailMessage.Priority = priority;

                mailMessage.BodyEncoding = bodyencoding;

                result = SmtpClient(mailMessage, smtpServer, smtpUsername, smtpPassword, smtpEnableSSL);

            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }
            finally
            {
                mailMessage.Dispose();
            }

            return result;
        }

        /// <summary>
        /// use for send mail to single recipients or  send the bulk email's,with the attachment and recipients display name.
        /// <para>add using System.Net; using System.Net.Mail,using ba360lib.Mail this 3 namespaces. </para>
        /// <para>recipients email address value for To,CC,Bcc,replyTo.</para>
        /// <para>this parameter accept value like below</para>
        /// <para>List&lt;To&gt; to = new List&lt;To&gt;();
        ///   to.Add(new To { address = "me@yahoo.com", displayName = "dispaly name" });
        ///</para>
        /// <para>
        /// Or create recipients classess object
        /// List&lt;To&gt; toList = new List&lt;To&gt;();
        /// To to=new To();
        /// to.address="me@gmail.com;
        /// to.displayName="display name";
        /// toList.Add(to);
        /// </para>  
        /// <para>For attachemet  
        /// List&lt;Attachment&gt; attachment = new List&lt;Attachment&gt;();
        ///attachment.Add(new Attachment(uploadfile.InputStream, uploadfile.FileName));
        /// Or use other overloaded method.
        /// </para>
        /// <para>returns ok if mail successfilly dilivered.otherwise return exception or status code.</para>
        /// </summary>
        /// <param name="IsBodyHtml">if you want to html format email body pass the boolean true value. for text format pass booleav false value</param>
        /// <param name="bodyencoding">accepts value like Encoding.UTF8 </param>
        /// <param name="smtpServer">smtp server name.eg.smtp.com or ip address.if smtp contains port pass value like eg. smtp.com:80</param>
        /// <param name="smtpUsername">smtp user name </param>
        /// <param name="smtpPassword">smtp password </param>
        /// <param name="smtpEnableSSL">smtp SSL passs true or false boolean value </param>
        /// <returns>returns ok if mail successfilly dilivered.otherwise return exception</returns> 
        public string Sendmail(string mailFrom, string FromName, List<To> mailTo, List<CC> cc, List<BCC> bcc, List<replyTo> replyTo, string mailbody, string mailsubject, List<Attachment> mailAttachment, MailPriority priority, bool IsBodyHtml, Encoding bodyencoding, string smtpServer, string smtpUsername, string smtpPassword, bool smtpEnableSSL)
        {
            string result = string.Empty;

            MailMessage mailMessage = new MailMessage();    

            try
            {
                mailMessage.Sender = new MailAddress(smtpUsername);

                //from address
                if (FromName == string.Empty)
                {
                    mailMessage.From = new MailAddress(mailFrom);
                }
                else
                {
                    mailMessage.From = new MailAddress(mailFrom, FromName);
                }

                //To address
                if (mailTo.Count > 0)
                {
                    foreach (To to in mailTo)
                    {
                        if (to.address == string.Empty)
                        {
                        }
                        else
                        {
                            mailMessage.To.Add(new MailAddress(to.address, to.displayName));
                        }
                    }
                }

                //cc address
                if (cc.Count > 0)
                {
                    foreach (CC ccadd in cc)
                    {
                        if (ccadd.address == string.Empty)
                        {
                        }
                        else
                        {
                            mailMessage.CC.Add(new MailAddress(ccadd.address, ccadd.displayName));
                        }
                    }
                }

                //bcc address
                if (bcc.Count > 0)
                {
                    foreach (BCC bccadd in bcc)
                    {
                        if (bccadd.address == string.Empty)
                        {
                        }
                        else
                        {
                            mailMessage.Bcc.Add(new MailAddress(bccadd.address, bccadd.displayName));
                        }
                    }
                }

                //replyTo address
                if (replyTo.Count > 0)
                {
                    foreach (replyTo replyToadd in replyTo)
                    {
                        if (replyToadd.address == string.Empty)
                        {
                        }
                        else
                        {
                            mailMessage.ReplyToList.Add(new MailAddress(replyToadd.address, replyToadd.displayName));
                        }
                    }
                }

                //attachment
                if (mailAttachment.Count > 0)
                {
                    foreach (Attachment attachment in mailAttachment)
                    {
                        // Attach the newly created email attachment
                        mailMessage.Attachments.Add(attachment);
                    }
                }

                mailMessage.Subject = mailsubject;

                mailMessage.Body = mailbody;

                mailMessage.IsBodyHtml = IsBodyHtml;

                mailMessage.Priority = priority;

                mailMessage.BodyEncoding = bodyencoding;

                result = SmtpClient(mailMessage, smtpServer, smtpUsername, smtpPassword, smtpEnableSSL);


            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }
            finally
            {
                mailMessage.Dispose();
            }

            return result;
        }

        private string SmtpClient(MailMessage mailMessage, string smtpServer, string smtpUsername, string smtpPassword, bool smtpEnableSSL)
        {
            string result = string.Empty;

            string[] splitport = new string[0];

            SmtpClient smtpClient = new SmtpClient();
            

            if (smtpServer != string.Empty)
            {
                splitport = smtpServer.Split(':');

                if (splitport.Length == 2)
                {
                    int port = Convert.ToInt32(splitport[1]);
                    smtpClient = new SmtpClient(splitport[0], port);
                }
                else
                {
                    smtpClient = new SmtpClient(smtpServer);
                }
            }
            else
            {
                throw new Exception("smtp server name is null.");
            }

            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            smtpClient.UseDefaultCredentials = false;

            if (smtpUsername == string.Empty || smtpPassword == string.Empty)
            {
                throw new Exception("smtp user name is null or smtp password is null.");
            }
            else
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
            }

            smtpClient.EnableSsl = smtpEnableSSL;

            try
            {
                //send mail
                smtpClient.Send(mailMessage);
                result = SmtpStatusCode.Ok.ToString();
            }
            catch (SmtpException e)
            {
                result = e.StatusCode.ToString();
            }
            finally
            {
                //dispose  both object
                smtpClient.Dispose();
                mailMessage.Dispose();
            }

            return result;
        }

    }
    public class To  
    {

        public string address { get; set; }
        public string displayName { get; set; }
    }
    public class CC
    {
        public string address { get; set; }
        public string displayName { get; set; }
    }
    public class BCC
    {
        public string address { get; set; }
        public string displayName { get; set; }  
    }
    public class replyTo
    {
        public string address { get; set; }
        public string displayName { get; set; }
    }
}
