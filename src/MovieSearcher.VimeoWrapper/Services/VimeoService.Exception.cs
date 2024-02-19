namespace MovieSearcher.VimeoWrapper.Services;

public partial class VimeoService
{
    /// <summary>
    /// https://restfulapi.net/http-status-codes/
    /// A 500 error is never the client’s fault, and therefore, it is reasonable for the client to retry the same request that triggered this response and hope to get a different response.
    /// </summary>
    /// <param name="exception">Exception</param>
    /// <returns>Boolean</returns>
    private static bool ExceptionChecker(Exception exception)
    {
        return exception.Message.Contains("Internal Server Error", StringComparison.OrdinalIgnoreCase);
    }
}