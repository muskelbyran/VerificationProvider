using Microsoft.AspNetCore.Http;
using VerificationProvider.Models;

namespace VerificationProvider.Interfaces;

public interface IValidateVerificationCodeService
{
    Task<ValidateRequest> UnpackValidateRequestAsync(HttpRequest reg);
    Task<bool> ValidateCodeAsync(ValidateRequest validateRequest);
}