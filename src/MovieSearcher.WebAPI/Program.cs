using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using MovieSearcher.WebAPI.Binder;
using MovieSearcher.WebAPI.Extensions;
using MovieSearcher.WebAPI.MiddleWare;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {environment}");

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
    app.UseSwaggerUI();
}

// middleware for api
app.UseMiddleware(typeof(ErrorHandlingMiddleware));

// Activate OutputCache
//app.UseOutputCache();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();