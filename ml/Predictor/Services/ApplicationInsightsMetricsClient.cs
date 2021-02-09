using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Predictor.Models;

namespace Predictor.Services
{
    public class ApplicationInsightsClient : IMetricsClient
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger _logger;

        public ApplicationInsightsClient(TelemetryConfiguration telemetryConfiguration, ILogger logger)
        {
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
            _telemetryClient.Context.Operation.Name = "AnalyzeModelResult";
            _logger = logger;
        }

        public void Track(SentimentPrediction prediction, SentimentIssue data)
        {
            try
            {
                var props = new Dictionary<string, string>
                {
                    { "model_name", Constants.ModelName },
                    { "text", data.SentimentText },
                };

                _telemetryClient.TrackMetric("Prediction.Probability", prediction.Probability, props);

                _telemetryClient.TrackMetric("Prediction.Score", prediction.Score, props);

                _logger.LogInformation($"Metrics updated. Score: {prediction.Score}");
            }
            catch(Exception ex)
            {
                // avoid fail prediction due to telemetry record saving issues
                _logger.LogError(ex, nameof(Track));
            }
        }
    }
}