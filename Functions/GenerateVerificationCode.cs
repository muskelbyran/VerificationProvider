using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VerificationProvider.Interfaces;

namespace VerificationProvider.Functions;

public class GenerateVerificationCode(ILogger<GenerateVerificationCode> logger, IVerificationService verificationService)
{
    private readonly ILogger<GenerateVerificationCode> _logger = logger;
    private readonly IVerificationService _verificationService = verificationService;

    [Function(nameof(GenerateVerificationCode))]
    [ServiceBusOutput("email_request", Connection = "ServiceBusConnection")]
    public async Task<string> Run([ServiceBusTrigger("verification_request", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        try
        {
            _logger.LogError($"SUCCESS : GenerateVerificationCode :: {message}");

            var verificationRequest = _verificationService.UnpackVerificationRequest(message);

            if (verificationRequest != null)
            {
                _logger.LogError($"SUCCESS : VerificationRequest :: {verificationRequest}");

                var code = _verificationService.GenerateCode();
                if (!string.IsNullOrEmpty(code))
                {
                    _logger.LogError($"SUCCESS : GenerateCode :: {code}");

                    var result = await _verificationService.SaveVerificationRequest(verificationRequest, code);
                    if (result)
                    {
                        _logger.LogError($"SUCCESS : SaveVerificationRequest :: {result}");

                        var emailRequest = _verificationService.GenerateEmailRequest(verificationRequest, code);
                        if (emailRequest != null)
                        {
                            _logger.LogError($"SUCCESS : EmailRequest :: {emailRequest}");

                            var payload = _verificationService.GenerateServiceBusEmailRequest(emailRequest);
                            if (!string.IsNullOrEmpty(payload))
                            {
                                _logger.LogError($"SUCCESS : GenerateServiceBusEmailRequest :: {message}");
                                _logger.LogError($"SUCCESS : GenerateServiceBusEmailRequest :: {payload}");

                                await messageActions.CompleteMessageAsync(message);
                                return payload;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.Run :: {ex.Message}");
        }

        return null!;
    }
}
