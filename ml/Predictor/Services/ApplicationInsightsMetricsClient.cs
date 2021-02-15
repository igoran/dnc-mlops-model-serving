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

        public ApplicationInsightsClient(TelemetryConfiguration telemetryConfiguration, Uri modelUri)
        {
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
            _telemetryClient.Context.Operation.Name = modelUri.ToString();
        }

        public void Track(SentimentPrediction prediction, SentimentIssue data, ILogger logger)
        {
            try
            {
                string sentimentText = data.SentimentText;

                var props = new Dictionary<string, string>
                {
                    { "model_name", Constants.ModelName },
                    { "text", sentimentText },
                };

                _telemetryClient.TrackMetric("Prediction.Probability", prediction.Probability, props);

                _telemetryClient.TrackMetric("Prediction.Score", prediction.Score, props);

                logger?.LogInformation($"Metrics updated. Score: {prediction.Score}");
            }
            catch(Exception ex)
            {
                // avoid fail prediction due to telemetry record saving issues
                logger?.LogError(ex, nameof(Track));
            }
        }
    }
}