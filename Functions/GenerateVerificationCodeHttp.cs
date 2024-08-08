using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Models;
using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class GenerateVerificationCodeHttp(ILoggerFactory loggerFactory, VerificationService verificationService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GenerateVerificationCodeHttp>();
    private readonly VerificationService _verificationService = verificationService;

    [Function("GenerateVerificationCodeHttp")]
    public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(requestBody);

        if (verificationRequest == null)
        {
            return new BadRequestObjectResult("Invalid verification request.");
        }

        try
        {
            _logger.LogInformation($"Received verification request: {verificationRequest}");

            var code = _verificationService.GenerateCode();
            if (string.IsNullOrEmpty(code))
            {
                return new BadRequestObjectResult("Failed to generate verification code.");
            }

            _logger.LogInformation($"Generated code: {code}");

            var result = await _verificationService.SaveVerificationRequest(verificationRequest, code);
            if (!result)
            {
                return new BadRequestObjectResult("Failed to save verification request.");
            }

            _logger.LogInformation("Successfully saved verification request.");

            var emailRequest = _verificationService.GenerateEmailRequest(verificationRequest, code);
            if (emailRequest == null)
            {
                return new BadRequestObjectResult("Failed to generate email request.");
            }

            _logger.LogInformation($"Generated email request: {emailRequest}");

            var payload = _verificationService.GenerateServiceBusEmailRequest(emailRequest);
            if (string.IsNullOrEmpty(payload))
            {
                return new BadRequestObjectResult("Failed to generate email request payload.");
            }

            _logger.LogInformation($"Generated email request payload: {payload}");


            return new OkObjectResult(new { code = code });
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : Run :: {ex.Message}");
            return new StatusCodeResult(500);
        }
    }
}