using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SunyaSuite.Web.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        var otelSection = builder.Configuration.GetSection("OpenTelemetry");
        var serviceName = otelSection["ServiceName"] ?? "SunyaSuite.Api";
        var seqUrl = builder.Configuration["Seq:Url"];
        var otlpEndpoint = !string.IsNullOrEmpty(seqUrl)
            ? $"{seqUrl.TrimEnd('/')}/ingest/otlp"
            : otelSection["Otlp:Endpoint"];

        var otlpProtocol = Enum.TryParse<OtlpExportProtocol>(
            otelSection["Otlp:Protocol"], ignoreCase: true, out var parsed)
            ? parsed
            : OtlpExportProtocol.HttpProtobuf;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: "1.0.0")
                .AddEnvironmentVariableDetector())
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint))
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = otlpProtocol;
                    });
                else if (builder.Environment.IsDevelopment())
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint))
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = otlpProtocol;
                    });
                else if (builder.Environment.IsDevelopment())
                    metrics.AddConsoleExporter();
            });

        return builder;
    }
}
