namespace VerificationProvider.Models;

public class VerificationRequest
{
    public string Email { get; set; } = null!;
    public string AppName { get; set; } = "Muskelbyrån";
}