using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution_Core
{
   

    public interface IEmailService
    {
        Task SendEmailAsync(EmailMessage emailMessage);
    }

    public class EmailService : IEmailService
    {
        // These could be injected from config in constructor instead of hardcoded
        private readonly string _fromName = "Innovative Technology South Africa";
        private readonly string _fromAddress = "support@iotsa.tech";
        private readonly string _smtpServer = "live.smtp.mailtrap.io";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser = "api";
        private readonly string _smtpPassword = "0611fbb0dbcb707a2da163bf1bb46d11";
        private readonly bool _enableSsl = true;

        public async Task SendEmailAsync1(EmailMessage emailMessage)
        {
            if (emailMessage == null)
                throw new ArgumentNullException(nameof(emailMessage));

            var smtpClient = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                EnableSsl = _enableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = emailMessage.IsBodyHtml,
            };

            mailMessage.To.Add(emailMessage.ToEmail);

            if (!string.IsNullOrEmpty(emailMessage.Cc))
                mailMessage.CC.Add(emailMessage.Cc);

            if (!string.IsNullOrEmpty(emailMessage.Bcc))
                mailMessage.Bcc.Add(emailMessage.Bcc);

            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendEmailAsync(EmailMessage emailMessage)
        {
            if (emailMessage == null)
                throw new ArgumentNullException(nameof(emailMessage));

            using var smtpClient = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(
                    _smtpUser,
                    _smtpPassword
                ),
                EnableSsl = _enableSsl
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(
                    _fromAddress,
                    _fromName
                ),

                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = emailMessage.IsBodyHtml
            };

            // TO
            if (!string.IsNullOrWhiteSpace(emailMessage.ToEmail))
            {
                mailMessage.To.Add(emailMessage.ToEmail);
            }

            // CC
            if (!string.IsNullOrWhiteSpace(emailMessage.Cc))
            {
                mailMessage.CC.Add(emailMessage.Cc);
            }

            // BCC
            if (!string.IsNullOrWhiteSpace(emailMessage.Bcc))
            {
                mailMessage.Bcc.Add(emailMessage.Bcc);
            }

            // ---------------------------------------------------------
            // ATTACHMENTS
            // ---------------------------------------------------------

            if (emailMessage.Attachments != null &&
                emailMessage.Attachments.Any())
            {
                foreach (var attachment in emailMessage.Attachments)
                {
                    if (string.IsNullOrWhiteSpace(attachment.Filepath))
                        continue;

                    if (!System.IO.File.Exists(attachment.Filepath))
                        continue;

                    string mimeType = "application/octet-stream";

                    string extension = Path.GetExtension(attachment.Filepath)
                                         .ToLowerInvariant();

                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            mimeType = "image/jpeg";
                            break;

                        case ".png":
                            mimeType = "image/png";
                            break;

                        case ".gif":
                            mimeType = "image/gif";
                            break;

                        case ".pdf":
                            mimeType = "application/pdf";
                            break;

                        case ".doc":
                            mimeType = "application/msword";
                            break;

                        case ".docx":
                            mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                            break;

                        case ".xls":
                            mimeType = "application/vnd.ms-excel";
                            break;

                        case ".xlsx":
                            mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            break;
                    }

                    var mailAttachment = new Attachment(
                        attachment.Filepath,
                        mimeType
                    );

                    mailAttachment.Name = attachment.Filename;

                    mailMessage.Attachments.Add(mailAttachment);
                }
            }

            await smtpClient.SendMailAsync(mailMessage);
        }
    }



    public class EmailMessage
    {
        public string ToEmail { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public bool IsBodyHtml { get; set; } = true;

        public string Cc { get; set; }

        public string Bcc { get; set; }

        // NEW
        public List<EmailAttachment> Attachments { get; set; }
            = new List<EmailAttachment>();
    }

    public class EmailAttachment
    {
        public string Filename { get; set; }

        public string Filepath { get; set; }
    }
}


