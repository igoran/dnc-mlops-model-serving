using System;
using Predictor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Predictor.Models;
using Microsoft.Extensions.ML;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Predictor
{
    public class Startup : FunctionsStartup
    {
        private string _environment;

        private bool IsDevelopmentEnvironment => "Development".Equals(_environment, StringComparison.OrdinalIgnoreCase);

        public override void Configure(IFunctionsHostBuilder builder)
        {
            _environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");

            if (IsDevelopmentEnvironment) {
                // Load From File
                builder.Services.AddPredictionEnginePool<SentimentIssue, SentimentPrediction>().FromFile(modelName: "SentimentAnalysisModel", filePath: "model.zip", watchForChanges: false);
            }
            else
            {
                // Load From URI
                throw new ApplicationException("Invalid model uri");
            }
        }
    }
}
