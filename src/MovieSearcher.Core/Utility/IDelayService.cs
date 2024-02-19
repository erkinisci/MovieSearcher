namespace MovieSearcher.Core.Utility;

public interface IDelayService
{
    Task ExponentialDelaySecondsAsync(int failedAttempts, int maxDelayInSeconds = 1024);
}