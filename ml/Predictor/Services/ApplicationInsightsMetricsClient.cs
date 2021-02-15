using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Predictor.Models;

namespace Predictor.Services
{
    public class ApplicationInsightsClient : IMetricsClient
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly Uri _modelUri;

        public ApplicationInsightsClient(TelemetryConfiguration telemetryConfiguration, Uri modelUri)
        {
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
            _telemetryClient.Context.Operation.Name = modelUri?.Segments?.LastOrDefault()?.ToString();
            _modelUri = modelUri;
        }

        public void Track(SentimentPrediction prediction, SentimentIssue data, ILogger logger)
        {
            try
            {
                string sentimentText = data.SentimentText;

                logger?.LogInformation($"{_telemetryClient.Context.Operation.Name} {sentimentText} is {prediction.Sentiment}", nameof(Track));

                var props = new Dictionary<string, string>
                {
                    { "model_name", Constants.ModelName },
                    { "model_version", _modelUri?.Segments?.LastOrDefault()?.ToString() },
                    { "model_uri", _modelUri?.ToString() },
                    { "text", sentimentText },
                };

                _telemetryClient.TrackMetric("Prediction.Response", prediction.Sentiment ? 1 : 0, props);

                _telemetryClient.TrackMetric("Prediction.Probability", prediction.Probability, props);

                _telemetryClient.TrackMetric("Prediction.Score", prediction.Score, props);
            }
            catch(Exception ex)
            {
                // avoid fail prediction due to telemetry record saving issues
                logger?.LogError(ex, nameof(Track));
            }
        }
    }
}