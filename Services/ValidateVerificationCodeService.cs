﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Interfaces;
using VerificationProvider.Models;

namespace VerificationProvider.Services;

public class ValidateVerificationCodeService(ILogger<ValidateVerificationCodeService> logger, DataContext context) : IValidateVerificationCodeService
{
    private readonly ILogger<ValidateVerificationCodeService> _logger = logger;
    private readonly DataContext _context = context;

    public async Task<bool> ValidateCodeAsync(ValidateRequest validateRequest)
    {
        try
        {
            var entity = await _context.VerificationRequests.FirstOrDefaultAsync(x => x.Email == validateRequest.Email && x.Code == validateRequest.Code);
            if (entity != null)
            {
                _context.VerificationRequests.Remove(entity);
                await _context.SaveChangesAsync();

                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : ValidateVerirficationCode.ValidateCodeAsync :: {ex.Message}");
        }

        return false;
    }

    public async Task<ValidateRequest> UnpackValidateRequestAsync(HttpRequest reg)
    {
        try
        {
            var body = await new StreamReader(reg.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                var validateRequest = JsonConvert.DeserializeObject<ValidateRequest>(body);
                if (validateRequest != null)
                    return validateRequest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : ValidateVerirficationCode.UnpackValidateRequestAsync :: {ex.Message}");
        }

        return null!;
    }
}