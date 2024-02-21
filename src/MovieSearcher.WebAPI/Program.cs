using Asp.Versioning;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Options;
using MovieSearcher.WebAPI.Binder;
using MovieSearcher.WebAPI.Extensions;
using MovieSearcher.WebAPI.MiddleWare;
using MovieSearcher.WebAPI.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {environment}");

// Add API versioning
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(
    options =>
    {
        // add a custom operation filter which sets default values
        options.OperationFilter<SwaggerDefaultValues>();
    });

builder.Services.AddApiVersioning(
    options =>
    {
        options.ReportApiVersions = true;
        options.DefaultApiVersion = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
    }).AddApiExplorer(options =>
{
    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
    // note: the specified format code will format the version as "'v'major[.minor][-status]"
    options.GroupNameFormat = "'v'VVV";

    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
    // can also be used to control the format of the API version in route templates
    options.SubstituteApiVersionInUrl = true;
});

// Azure app configuration
var azureAppConfig = builder.Configuration.GetConnectionString("AzureAppConfig");
if (!string.IsNullOrEmpty(azureAppConfig))
{
    var labelFilter = builder.Configuration.GetValue<string>("AzureAppConfigLabel");
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(azureAppConfig).Select(KeyFilter.Any, labelFilter);
    });
}

// logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();

// Application extensions
builder.AddApplicationServices();
builder.AddApplicationOptions();

// Default output caching policy default 1 hour
// Learn more about configuring OutputCache https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?view=aspnetcore-8.0#add-the-middleware-to-the-app
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policyBuilder => policyBuilder.Expire(TimeSpan.FromHours(1)));
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// QueryParametersModelBinderProvider is added as the first model binder provider, ensuring that it takes precedence when binding query parameters in incoming requests
builder.Services.AddMvc(options =>
{
    options.ModelBinderProviders.Insert(0, new QueryParametersModelBinderProvider());
});

// Distributed caching with redis for backend services
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnectionString");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "MovieSearch:";
});

// OutputCache with redis for endpoints
builder.Services.AddStackExchangeRedisOutputCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "MovieSearch:OutputCache:";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options =>
        {
            var descriptions = app.DescribeApiVersions();

            // build a swagger endpoint for each discovered API version
            foreach ( var description in descriptions )
            {
                var url = $"/swagger/{description.GroupName}/swagger.json";
                var name = description.GroupName.ToUpperInvariant();
                options.SwaggerEndpoint( url, name );
            }
        } );
}

// middleware for api
app.UseMiddleware(typeof(ErrorHandlingMiddleware));

// Activate OutputCache
//app.UseOutputCache();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();