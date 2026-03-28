using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using VerificationProvider.Models;

namespace VerificationProvider.Services;

public class EmailSenderService
{
    private readonly ILogger<EmailSenderService> _logger;
    private readonly IConfiguration _configuration;

    public EmailSenderService(ILogger<EmailSenderService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SendEmailAsync(EmailRequest request)
    {
        try
        {
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var smtpUser = _configuration["Smtp:Username"];
            var smtpPass = _configuration["Smtp:Password"];
            var fromName = _configuration["Smtp:FromName"] ?? "Muskelbyrån";
            var fromAddress = _configuration["Smtp:FromAddress"] ?? smtpUser;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(request.To));
            message.Subject = request.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = request.HtmlBody,
                TextBody = request.PlainText
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(smtpUser))
                await client.AuthenticateAsync(smtpUser, smtpPass);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}", request.To);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", request?.To);
            return false;
        }
    }
}
