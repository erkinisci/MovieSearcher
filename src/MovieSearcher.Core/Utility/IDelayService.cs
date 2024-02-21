namespace MovieSearcher.Core.Utility;

public interface IDelayService
{
    Task DelayWithExponentialBackoff(int failedAttempts, int maxDelayInSeconds = 1024);
    int CalculateExponentialBackoffDelay(int failedAttempts, int maxDelayInSeconds = 1024);
}