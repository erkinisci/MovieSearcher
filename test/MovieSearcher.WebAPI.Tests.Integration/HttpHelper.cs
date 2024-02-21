using Microsoft.AspNetCore.Http.Extensions;
using MovieSearcher.Core.Query;

namespace MovieSearcher.WebAPI.Tests.Integration;

internal static class HttpHelper
{
    public const string SearchV1 = "/api/v1/movie/search";
    public const string SearchV2 = "/api/v2/movie/search";
}

public static class QueryParametersExtensions
{
    public static string CreateQueryString(this QueryParameters queryParameters)
    {
        var queryBuilder = new QueryBuilder { { "query", queryParameters.Query } };

        if (queryParameters.Page.HasValue)
            queryBuilder.Add("perPage", queryParameters.Page.Value.ToString());

        if (queryParameters.PerPage.HasValue)
            queryBuilder.Add("perPage", queryParameters.PerPage.Value.ToString());

        return queryBuilder.ToString();
    }
}