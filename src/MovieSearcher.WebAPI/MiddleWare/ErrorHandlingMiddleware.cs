using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace MovieSearcher.WebAPI.MiddleWare;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var code = HttpStatusCode.InternalServerError; // 500 if unexpected

        switch (ex)
        {
            case UnauthorizedAccessException:
                code = HttpStatusCode.Forbidden;
                logger.LogWarning(ex, "Forbidden activity");
                break;

            case ApplicationException:
                code = HttpStatusCode.BadRequest;
                logger.LogError(ex, "Application error");
                break;

            default:
            {
                switch (ex)
                {
                    case ArgumentNullException:
                        code = HttpStatusCode.BadRequest;
                        logger.LogError(ex, "Argument null error");
                        break;

                    case OutOfMemoryException:
                        code = HttpStatusCode.BadRequest;
                        logger.LogError(ex, "Out of memory error");
                        break;

                    case IndexOutOfRangeException:
                        code = HttpStatusCode.BadRequest;
                        logger.LogError(ex, "Index out of range error");
                        break;

                    case InvalidOperationException:
                        code = HttpStatusCode.BadRequest;
                        logger.LogError(ex, "Invalid operation error");
                        break;

                    default:
                        logger.LogError(ex, "Global error");
                        break;
                }

                break;
            }
        }

        var result = JsonConvert.SerializeObject(new { error = GetAllFootprints(ex) });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }

    private static string GetAllFootprints(Exception x)
    {
        const int indent = 0;
        const int indentWidth = 4;

        var st = new StackTrace(x, true);
        var frames = st.GetFrames();
        var traceString = new StringBuilder();
        var makeIndent = new Func<int, string>((depth) => new string(' ', indentWidth * (depth + indent)));

        foreach (var frame in frames)
        {
            if (frame.GetFileLineNumber() < 1)
                continue;

            traceString.Append("Message: " + $"{makeIndent(1)}{x.GetType().Name}: \"{x.Message}\"");
            traceString.Append(", File: " + frame.GetFileName());
            traceString.Append(", Method:" + frame.GetMethod()?.Name);
            traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
            traceString.Append("  -->  ");
        }

        return traceString.ToString();
    }
}