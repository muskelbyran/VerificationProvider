using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Interfaces;
using VerificationProvider.Models;

namespace VerificationProvider.Services;

public class VerificationService(ILogger<VerificationService> logger, IServiceProvider serviceProvider) : IVerificationService
{
    private readonly ILogger<VerificationService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<bool> SaveVerificationRequest(VerificationRequest verificationRequest, string code)
    {
        try
        {
            using var context = _serviceProvider.GetRequiredService<DataContext>();

            var existingRequest = await context.VerificationRequests.FirstOrDefaultAsync(x => x.Email == verificationRequest.Email);
            if (existingRequest != null)
            {
                existingRequest.Code = code;
                existingRequest.ExpiryDate = DateTime.Now.AddMinutes(5);
                context.Entry(existingRequest).State = EntityState.Modified;
            }
            else
            {
                context.VerificationRequests.Add(new Data.Entities.VerificationRequestEntity() { Email = verificationRequest.Email, Code = code });
            }

            await context.SaveChangesAsync();
            return true;

        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.SaveVerificationRequest :: {ex.Message}");
        }

        return false;
    }

    public VerificationRequest UnpackVerificationRequest(ServiceBusReceivedMessage message)
    {
        try
        {
            var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(message.Body.ToString());
            if (verificationRequest != null && !string.IsNullOrEmpty(verificationRequest.Email))
                return verificationRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.UnpackVerificationRequest :: {ex.Message}");
        }

        return null!;
    }

    public string GenerateCode()
    {
        try
        {
            var rnd = new Random();
            var code = rnd.Next(100000, 999999);
            Console.WriteLine(code);
            _logger.LogError($"SUCCESS : VerificationService.GenerateCode :: {code}");

            return code.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.GenerateCode :: {ex.Message}");
        }

        return null!;
    }

    public EmailRequest GenerateEmailRequest(VerificationRequest verificationRequest, string code)
    {
        try
        {
            if (!string.IsNullOrEmpty(verificationRequest.Email) && !string.IsNullOrEmpty(code))
            {
                var emailRequest = new EmailRequest()
                {
                    To = verificationRequest.Email,
                    Subject = $"Verifikation Kod {code}",
                    HtmlBody = $@"			
					<html lang='sv'>
						<head>
							<meta charset=""UTF-8"">
							<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
							<title>Verifikation Kod</title> 
						</head>
						<body>
							<div style='color: #191919; max-width: 500px'>
								<div style='background-color: #4F85F6; color: white; text-align: center; padding: 20px 0;'>
									<h1 style='font-weight: 400;'>Verifikation Kod</h1>
								</div>
								<div style='background-color: #f4f4f4; padding: 1rem 2rem;'>
									<p>Välkommen som kund hos Muskelbyrån!</p>
									<p>Du behöver bekräfta ditt konto och din e-post {verificationRequest.Email}. Verifiera ditt konto med denna kod:</p>
									<p class='code' style='font-weight: 700; text-align: center; font-size: 48px; letter-spacing: 8px;'>
										{code}
									</p>
									<div style='color: #191919; font-size: 11px;'>
										<p>Om du inte bett om en kod eller registrerat ett konto hos Muskelbyrån så är det möjligt att någon försöker använda din e-post <span style='color: #0041cd;'>{verificationRequest.Email}.</span> Du kan inte svara på det här mailet. För mer information kontakta Muskelbyrån.</p> 
									</div>
								</div>
								<div style='color: #191919; text-align: center; font-size: 11px;'>
									<p>© Muskelbyrån, Borlänge</p>
								</div>
							</div>
						</body>
					</html>
					",
                    PlainText = $"Please verify your account using this verification code: {code}. If you did not request this code, it is possible that someone else is trying to access the Muskelbyrån account. This email can not receive replies. FOr more information, contact Muskelbyrån."
                };

                return emailRequest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.GenerateEmailRequest :: {ex.Message}");
        }

        return null!;
    }

    public string GenerateServiceBusEmailRequest(EmailRequest emailRequest)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(emailRequest);
            if (!string.IsNullOrEmpty(payload))
            {
                _logger.LogError($"SUCCESS : GenerateVerificationCode.GenerateServiceBusEmailRequest :: {payload}");
                return payload;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.GenerateServiceBusEmailRequest :: {ex.Message}");
        }

        return null!;
    }
}