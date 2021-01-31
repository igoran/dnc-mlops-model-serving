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
        private string _modelUri;

        private bool IsDevelopmentEnvironment => "Development".Equals(_environment, StringComparison.OrdinalIgnoreCase);

        public override void Configure(IFunctionsHostBuilder builder)
        {
            _environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            _aiKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
            _modelUri = Environment.GetEnvironmentVariable("ML_MODEL_URI");

            var predictionEngine = builder.Services.AddPredictionEnginePool<SentimentIssue, SentimentPrediction>();

            if (IsDevelopmentEnvironment)
            {
                // Load From File
                predictionEngine.FromFile(modelName: "SentimentAnalysisModel", filePath: Path.Combine(Environment.CurrentDirectory, "model.zip"), watchForChanges: true);
            }
            else if (Uri.TryCreate(_modelUri, UriKind.RelativeOrAbsolute, out var _))
            {
                predictionEngine.FromUri(_modelUri);
            }
            else
            {
                Console.WriteLine($"{_environment} {_modelUri}");
                throw new ApplicationException($"Invalid Model Uri. Environment={_environment} Uri={_modelUri}");
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
