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

namespace Predictor
{
    public class Funcs
    {
        private readonly PredictionEnginePool<SentimentIssue, SentimentPrediction> _predictionEnginePool;

        public Funcs(PredictionEnginePool<SentimentIssue, SentimentPrediction> predictionEnginePool)
        {
            _predictionEnginePool = predictionEnginePool;
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

            //Return Prediction
            return new OkObjectResult(sentiment);
        }

        [FunctionName("ping")]
        public IActionResult Ping([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req)
        {
            return new OkObjectResult("ok");
        }
    }
}