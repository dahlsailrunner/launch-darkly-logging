using System.Reflection;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("Application", Assembly.GetExecutingAssembly().GetName().Name ?? "API")
        .Enrich.FromLogContext()
        .WriteTo.Seq("http://localhost:5341")
        .WriteTo.Console(new CompactJsonFormatter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
    o.SwaggerDoc("v1", new OpenApiInfo {Title = "LaunchDarkly Sample"}));

// intermediate build step to enable retrieval of ILoggerFactory from DI
// NOTE!! Avoid adding Singleton services before calling BuildServiceProvider if at all possible
var sp = builder.Services.BuildServiceProvider(); 
var loggerFactory = sp.GetService<ILoggerFactory>();

// the loggerFactory is passed in to the LaunchDarkly Logging method below
var sdkKey = builder.Configuration.GetValue<string>("LaunchDarklySdkKey"); 

var ldConfig = LaunchDarkly.Sdk.Server.Configuration.Builder(sdkKey)
    .Logging(LdMicrosoftLogging.Adapter(loggerFactory)).Build();

builder.Services.AddSingleton<ILdClient>(_ => new LdClient(ldConfig));

var app = builder.Build();  // This ALSO builds the ServiceProvider -- and Singletons before BuildServiceProvider will be duplicated
app.UseSerilogRequestLogging();

app.MapGet("/get-toggle", (string toggleName, ILdClient ld) =>
{
    var toggleValue = ld.BoolVariation(toggleName, Context.New("minimal"));
    return toggleValue;
});

app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Swagger Demo Minimal API v1"));

app.Run();
