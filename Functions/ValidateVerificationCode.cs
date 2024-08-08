using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VerificationProvider.Interfaces;

namespace VerificationProvider.Functions;

public class ValidateVerirficationCode
{
    private readonly ILogger<ValidateVerirficationCode> _logger;
    private readonly IValidateVerificationCodeService _validateVerificationCodeService;

    public ValidateVerirficationCode(ILogger<ValidateVerirficationCode> logger, IValidateVerificationCodeService validateVerificationCodeService)
    {
        _logger = logger;
        _validateVerificationCodeService = validateVerificationCodeService;
    }

    [Function("ValidateVerirficationCode")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "validate")] HttpRequest req)
    {
        try
        {
            var validateRequest = await _validateVerificationCodeService.UnpackValidateRequestAsync(req);
            if (validateRequest != null)
            {
                var validateResult = await _validateVerificationCodeService.ValidateCodeAsync(validateRequest);
                if (validateResult)
                {
                    return new OkResult();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : ValidateVerirficationCode.Run :: {ex.Message}");
        }

        return new UnauthorizedResult();
    }
}