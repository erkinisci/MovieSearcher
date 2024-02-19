using System.Text;
using MovieSearcher.Core.Query;

namespace MovieSearcher.Core.Extensions;

public static class QueryParametersExtensions
{
    public static string GenerateKey(this QueryParameters queryParameters)
    {
        var sb = new StringBuilder(queryParameters.Query);

        if (queryParameters.Page.HasValue)
            sb.Append($":Page:{queryParameters.Page.Value}");

        if (queryParameters.PerPage.HasValue)
            sb.Append($":PerPage:{queryParameters.PerPage.Value}");

        return sb.ToString();
    }
}