using Microsoft.Extensions.Options;
using MovieSearcher.WebAPI.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MovieSearcher.WebAPI.Extensions;

public static class OpenApiExtensions
{
    public static void AddOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        
        builder.Services.AddSwaggerGen(
            options =>
            {
                // add a custom operation filter which sets default values
                options.OperationFilter<SwaggerDefaultValues>();
            });
    }
}