using System;
using Predictor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Predictor.Models;
using Microsoft.Extensions.ML;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Predictor
{
    public class Startup : FunctionsStartup
    {
        private string _environment;
        private string _aiKey;

        private bool IsDevelopmentEnvironment => "Development".Equals(_environment, StringComparison.OrdinalIgnoreCase);

        public override void Configure(IFunctionsHostBuilder builder)
        {
            _environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            _aiKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

            if (IsDevelopmentEnvironment) {
                // Load From File
                builder.Services.AddPredictionEnginePool<SentimentIssue, SentimentPrediction>().FromFile(modelName: "SentimentAnalysisModel", filePath: Path.Combine(Environment.CurrentDirectory, "model.zip"), watchForChanges: false);
            }

            builder.Services.AddSingleton(sp =>
            {
                var telemetryConfiguration = new TelemetryConfiguration
                {
                    InstrumentationKey = _aiKey
                };
                telemetryConfiguration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
                return telemetryConfiguration;
            });
        }
    }
}
