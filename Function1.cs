using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace F_DOTNET_DURABLE
{
    public class GreetingInput
    {
        public string Name { get; set; }
        public string InstanceId { get; set; }
    }

    public static class Function1
    {
        private static TelemetryClient _telemetryClient;

        static Function1()
        {
            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [Function(nameof(Function1))]
        public static async Task<List<string>> RunOrchestrator(
      [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Function1));
            string instanceId = context.InstanceId; // Get Durable Instance ID
            logger.LogInformation("Saying hello with Instance ID: {instanceId}", instanceId);

            var outputs = new List<string>();
            var greetingInput1 = new GreetingInput
            {
                Name = "Tokyo",
                InstanceId = instanceId
            };
            var greetingInput2 = new GreetingInput
            {
                Name = "Seattle",
                InstanceId = instanceId
            };
            var greetingInput3 = new GreetingInput
            {
                Name = "London",
                InstanceId = instanceId
            };
            // Pass instanceId
            outputs.Add(await context.CallActivityAsync<string>("SayHello", greetingInput1));
            outputs.Add(await context.CallActivityAsync<string>("SayHello", greetingInput2));
            outputs.Add(await context.CallActivityAsync<string>("SayHello", greetingInput3));

            await Task.Delay(600000);
            return outputs;
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] GreetingInput input, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            string functionInvocationId = executionContext.InvocationId; // Get Function Invocation ID

            logger.LogInformation($"Saying hello to {input.Name} with Invocation ID: {functionInvocationId} and Instance ID: {input.InstanceId}");


            var traceTelemetry = new TraceTelemetry($"Said hello to {input.Name}");
            traceTelemetry.Properties["FunctionInvocationId"] = functionInvocationId; // Add Function Invocation ID
            traceTelemetry.Properties["DurableInstanceId"] = input.InstanceId; // Add Durable Instance ID
            _telemetryClient.TrackTrace(traceTelemetry);

            return $"Hello {input.Name}!";
        }


        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");
            string functionInvocationId = executionContext.InvocationId; // Get the Function Invocation ID

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(Function1));

            logger.LogInformation("Started orchestration with ID = '{instanceId}' and Invocation ID = '{functionInvocationId}'.", instanceId, functionInvocationId);

            var evt = new EventTelemetry("Orchestration started");
            evt.Properties["DurableInstanceId"] = instanceId; // Add the Durable Instance ID to the event properties
            evt.Properties["FunctionInvocationId"] = functionInvocationId; // Add the Function Invocation ID to the event properties
            _telemetryClient.TrackEvent(evt);

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
