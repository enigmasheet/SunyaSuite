using Serilog;
using SunyaSuite.Infrastructure;
using SunyaSuite.Web.Api.Extensions;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureSerilog();
    builder.ConfigureOpenTelemetry();
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddIdentityServices();
    builder.Services.AddAppAuthorization();
    builder.Services.AddAppServices(builder.Configuration);

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    }

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.ConfigurePipeline();
    await app.RunDatabaseStartupAsync();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
