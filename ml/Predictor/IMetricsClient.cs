using Predictor.Models;

namespace Predictor
{
    public interface IMetricsClient
    {
        void Track(SentimentPrediction prediction, SentimentIssue data);
    }
}