namespace MovieSearcher.Core.Utility;

public class DelayService : IDelayService
{
    /// <summary>
    /// Exponential backoff delays
    /// </summary>
    /// <param name="failedAttempts">Failed attempts count</param>
    /// <param name="maxDelayInSeconds">Cap the maximum delay to avoid waiting for an unreasonably long time before retrying</param>
    /// <exception cref="ArgumentException"></exception>
    public async Task DelayWithExponentialBackoff(int failedAttempts, int maxDelayInSeconds = 1024)
    {
        if (failedAttempts < 0)
            throw new ArgumentException("failedAttempts should be non-negative.", nameof(failedAttempts));

        var delaySeconds = 0.5 * (Math.Pow(2.0, failedAttempts) - 1.0);
        var finalDelaySeconds = Math.Min(maxDelayInSeconds, delaySeconds);

        await Task.Delay(TimeSpan.FromSeconds(finalDelaySeconds));
    }

    /// <summary>
    /// The method is to calculate the exponential backoff delay.
    /// </summary>
    /// <param name="failedAttempts">Failed attempts count</param>
    /// <param name="maxDelayInSeconds">Cap the maximum delay to avoid waiting for an unreasonably long time before retrying</param>
    /// <returns>Integer</returns>
    /// <exception cref="ArgumentException"></exception>
    public int CalculateExponentialBackoffDelay(int failedAttempts, int maxDelayInSeconds = 1024)
    {
        if (failedAttempts < 0)
            throw new ArgumentException("failedAttempts should be non-negative.", nameof(failedAttempts));

        var delaySeconds = 0.5 * (Math.Pow(2.0, failedAttempts) - 1.0);
        var finalDelaySeconds = Math.Min(maxDelayInSeconds, delaySeconds);

        return Convert.ToInt32(finalDelaySeconds);
    }
}