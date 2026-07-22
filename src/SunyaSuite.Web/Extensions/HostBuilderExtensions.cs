using Serilog;

namespace SunyaSuite.Web.Api.Extensions;

public static class HostBuilderExtensions
{
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var seqUrl = builder.Configuration["Seq:Url"] ?? "http://localhost:5341";
        var seqApiKey = builder.Configuration["Seq:ApiKey"];

        var logConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SunyaSuite.Api")
            .WriteTo.Console();

        if (!string.IsNullOrEmpty(seqApiKey))
            logConfig.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
        else
            logConfig.WriteTo.Seq(seqUrl);

        Log.Logger = logConfig.CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }
}
