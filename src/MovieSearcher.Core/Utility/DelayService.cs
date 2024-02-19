namespace MovieSearcher.Core.Utility;

public class DelayService : IDelayService
{
    public Task ExponentialDelaySecondsAsync(int failedAttempts, int maxDelayInSeconds = 1024)
    {
        var num = 0.5 * (Math.Pow(2.0, failedAttempts) - 1.0);

        return Task.Delay(TimeSpan.FromSeconds(maxDelayInSeconds < num
            ? Convert.ToInt32(maxDelayInSeconds)
            : Convert.ToInt32(num)));
    }
}