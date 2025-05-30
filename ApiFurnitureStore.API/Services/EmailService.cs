using ApiFurnitureStore.API.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace ApiFurnitureStore.API.Services
{
    public class EmailService : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings;
        public EmailService(IOptions<SmtpSettings> smtpSettings) //IOptions<T> es una caja que contiene la configuración, por eso usare Value para acceder a sus prop
        {
            _smtpSettings = smtpSettings.Value; //smptpSettings es del tipo IOptions<SmtpSettings>, no es directamente el objeto con las prop cargadas por eso para acceder a su prop real pongo .Value
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                //abro smtpclient con using para abrir y cerrar conexiones
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpSettings.Server);
                    await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
