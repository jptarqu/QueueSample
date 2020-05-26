using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace FunctionAppQueue2
{
    public static class DbOrchestratorFunc
    {
        //[FunctionName("DbOrchestratorFunc")]
        //public static async Task<List<string>> RunOrchestrator(
        //    [OrchestrationTrigger] DurableOrchestrationContext context)
        //{
        //    var outputs = new List<string>();

        //    // Replace "hello" with the name of your Durable Activity Function.
        //    outputs.Add(await context.CallActivityAsync<string>("DbOrchestratorFunc_Hello", "Tokyo"));
        //    outputs.Add(await context.CallActivityAsync<string>("DbOrchestratorFunc_Hello", "Seattle"));
        //    outputs.Add(await context.CallActivityAsync<string>("DbOrchestratorFunc_Hello", "London"));

        //    // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
        //    return outputs;
        //}



        private const string queueName = "outqueue";
        private const string queueNameIn = "inqueue";
        private static double duration(DateTime b) =>
            (DateTime.Now - b).TotalSeconds;
        [FunctionName("DbCallOnQueue")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            ILogger log)
        {
            var startTime = DateTime.Now;
            var name = await req.Content.ReadAsAsync<string>();

            var connStr = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            log.LogInformation(connStr);
            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(connStr);
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queueName);
            await queue.AddMessageAsync(new CloudQueueMessage($"Hello, {name} {startTime}"));
            var queueIn = queueClient.GetQueueReference(queueNameIn);
            var peekedMessage = await queueIn.PeekMessageAsync();
            if (peekedMessage != null)
            {
                log.LogInformation($"The peeked message is: {peekedMessage.AsString}");
            }

            log.LogInformation("DbCallOnQueue took " + duration(startTime));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}