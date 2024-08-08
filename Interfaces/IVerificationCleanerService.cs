namespace VerificationProvider.Interfaces;

public interface IVerificationCleanerService
{
    Task RemoveExpiredRecordsAsync();
}