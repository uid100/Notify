using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Linq;
using MailKit.Net.Smtp;

namespace Notify
{
    class EmailNotice
    {
        private static EmailConfiguration _smtp;
        private static List<EmailAddress> _Subscribers;

        static void ReadConfiguration()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<EmailNotice>()
               .AddEnvironmentVariables();
            IConfigurationRoot configuration = builder.Build();

            var SmtpConfig = new EmailConfiguration();
            configuration.GetSection("email").Bind(SmtpConfig);
            _smtp = SmtpConfig;
            _Subscribers = configuration.GetSection("subscribers").Get<List<EmailAddress>>();
        }

        static MimeMessage simpleMessage()
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("No Reply", _smtp.SmtpUsername));
            message.To.AddRange(_Subscribers.Select(to => new MailboxAddress(to.Name, to.Address)));
            message.Subject = "New Timestamp";

            message.Body = new TextPart("plain")
            {
                Text = $"It's {new DateTime().ToShortTimeString()}. Are you awake?"
            };

            return message;
        }

        static void SendNotice()
        {
            ReadConfiguration(); 
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(_smtp.SmtpServer, _smtp.SmtpPort, true);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Authenticate(_smtp.SmtpUsername, _smtp.SmtpPassword);
                    client.Send(simpleMessage());
                    client.Disconnect(true);
                }
            } catch( Exception ex )
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void Main(string[] args)
        {
            SendNotice();
        }
    }
}
