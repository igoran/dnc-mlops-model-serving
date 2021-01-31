using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Predictor.Models;

namespace Predictor
{
    public class ApplicationInsightsMetricsClient : IMetricsClient
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsMetricsClient(TelemetryConfiguration telemetryConfiguration)
        {
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
            _telemetryClient.Context.Operation.Name = "AnalyzeModelResult";
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
            }
            catch
            {
                // avoid fail prediction due to telemetry record saving issues
            }
        }
    }
}