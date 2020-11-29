using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Predictor.Models;
using Microsoft.Extensions.ML;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System.Collections.Generic;
using System.Globalization;

namespace Predictor
{
    public class Funcs
    {
        private readonly PredictionEnginePool<SentimentIssue, SentimentPrediction> _predictionEnginePool;
        private readonly TelemetryClient _telemetryClient;

        public Funcs(PredictionEnginePool<SentimentIssue, SentimentPrediction> predictionEnginePool, TelemetryConfiguration telemetryConfiguration)
        {
            _predictionEnginePool = predictionEnginePool;
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName("predictor")]
        public async Task<IActionResult> Predict(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "predict")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //Parse HTTP Request Body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            SentimentIssue data = JsonConvert.DeserializeObject<SentimentIssue>(requestBody);

            //Make Prediction
            SentimentPrediction prediction = _predictionEnginePool.Predict(modelName: "SentimentAnalysisModel", example: data);

            //Convert prediction to string
            string sentiment = Convert.ToBoolean(prediction.Prediction) ? "Positive" : "Negative";

            EmitTelemetry("SentimentAnalysisModel", prediction, data);

            //Return Prediction
            return new OkObjectResult(sentiment);
        }

        private void EmitTelemetry(string modelName, SentimentPrediction prediction, SentimentIssue data)
        {
            try
            {
                _telemetryClient.Context.Operation.Name = "AnalyzeModelResult";

                var props = new Dictionary<string, string>
                {
                    { "model", modelName },
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

        [FunctionName("ping")]
        public IActionResult Ping([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req)
        {
            return new OkObjectResult("ok");
        }
    }
}