﻿using System;
using System.IO;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ML;
using Predictor;
using Predictor.Models;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Predictor
{
    public class Startup : FunctionsStartup
    {
        public virtual bool IsDevelopmentEnvironment => "Development".Equals(Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT"), StringComparison.OrdinalIgnoreCase);

        public virtual Uri GetModelUri() {

            var uri = Environment.GetEnvironmentVariable("ML_MODEL_URI") ?? string.Empty;

            if (Uri.TryCreate(uri, UriKind.Absolute, out var _)) {
                return new Uri(uri);
            }

            return new Uri("http://localhost/model.zip");
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var modelUri = GetModelUri();

            builder.Services.AddSingleton(sp => modelUri);

            builder.Services.AddSingleton<IMetricsClient>(sp =>
            {
                var telemetryConfiguration = new TelemetryConfiguration
                {
                    InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
                };

                telemetryConfiguration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

                return new ApplicationInsightsMetricsClient(telemetryConfiguration);
            });

            if (modelUri.IsLoopback)
            {
                builder.Services.AddPredictionEnginePool<SentimentIssue, SentimentPrediction>().FromFile(modelName: Constants.ModelName, filePath: Path.Combine(Environment.CurrentDirectory, "model.zip"), watchForChanges: false);
            }
            else
            {
                builder.Services.AddPredictionEnginePool<SentimentIssue, SentimentPrediction>().FromUri(modelUri.ToString());
            }
        }
    }
}
