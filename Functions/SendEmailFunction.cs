using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Models;
using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class SendEmailFunction
{
    private readonly ILogger<SendEmailFunction> _logger;
    private readonly EmailSenderService _emailSenderService;

    public SendEmailFunction(ILogger<SendEmailFunction> logger, EmailSenderService emailSenderService)
    {
        _logger = logger;
        _emailSenderService = emailSenderService;
    }

    [Function("SendEmailFunction")]
    public async Task Run([ServiceBusTrigger("email_request", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        try
        {
            var payload = message.Body.ToString();
            _logger.LogInformation("SendEmailFunction received payload: {Payload}", payload);

            var emailRequest = JsonConvert.DeserializeObject<EmailRequest>(payload);
            if (emailRequest == null)
            {
                _logger.LogError("Invalid email request payload");
                await messageActions.DeadLetterMessageAsync(message, "InvalidPayload", "Could not deserialize EmailRequest");
                return;
            }

            var success = await _emailSenderService.SendEmailAsync(emailRequest);
            if (success)
            {
                await messageActions.CompleteMessageAsync(message);
            }
            else
            {
                _logger.LogError("Failed to send email, abandoning message");
                await messageActions.AbandonMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendEmailFunction");
            try { await messageActions.AbandonMessageAsync(message); } catch { }
        }
    }
}
