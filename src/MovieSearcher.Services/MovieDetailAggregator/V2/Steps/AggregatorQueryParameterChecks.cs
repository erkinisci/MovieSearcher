﻿using Microsoft.Extensions.Logging;
using MovieSearcher.Core.Exceptions;
using MovieSearcher.Core.Patterns.CoR;
using MovieSearcher.Core.Query;

namespace MovieSearcher.Services.MovieDetailAggregator.V2.Steps;

public class AggregatorQueryParameterChecks(ILogger<AggregatorQueryParameterChecks> logger) : BaseHandler
{
    public override async Task<object?> Handle(object request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request object is checking {request}", request);

        if (request is not QueryParameters queryParameters || string.IsNullOrWhiteSpace(queryParameters.Query))
            throw new MovieAggregatorException("Query can not be null");

        logger.LogInformation("Request object is checked {request}", request);

        return await base.Handle(request, cancellationToken);
    }
}