This demo demonstrate on how to use custom telemetry to show Function Invocation ID and Durable Instance ID.

Make sure to have your APPINSIGHTS_INSTRUMENTATIONKEY set.

{
    "IsEncrypted": false,
    "Values": {
        "APPINSIGHTS_INSTRUMENTATIONKEY": "xxx",
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
    }
}
