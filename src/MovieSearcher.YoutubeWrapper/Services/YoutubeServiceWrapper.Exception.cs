using System.Net;
using MovieSearcher.Core.Models;
using Google;

namespace MovieSearcher.YoutubeWrapper.Services;

public partial class YoutubeServiceWrapper
{
    private static ServiceResult<string[]> ReturnClearServiceResult(Exception exception)
    {
        return exception switch
        {
            GoogleApiException { HttpStatusCode: HttpStatusCode.Forbidden } => new ServiceResult<string[]>(
                Array.Empty<string>(), "Youtube Service has an error. Request exceeded!"),
            GoogleApiException { HttpStatusCode: HttpStatusCode.Unauthorized } => new ServiceResult<string[]>(
                Array.Empty<string>(), "Youtube Service has an error. Client Unauthorized!"),
            _ => new ServiceResult<string[]>(Array.Empty<string>(), "Youtube Service has an unexpected error!")
        };
    }

    private static bool ExceptionChecker(Exception exception)
    {
        return exception switch
        {
            GoogleApiException { HttpStatusCode: HttpStatusCode.BadGateway } => true,
            GoogleApiException { HttpStatusCode: HttpStatusCode.InternalServerError } => true,
            GoogleApiException { HttpStatusCode: HttpStatusCode.Forbidden } => false,
            GoogleApiException { HttpStatusCode: HttpStatusCode.Unauthorized } => false,
            _ => false
        };
    }
}