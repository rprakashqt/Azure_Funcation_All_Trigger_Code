using System.Text.Json;
using Azure.Storage.Blobs;
using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Enterprise.Functions.Functions;

public sealed class StorageFunctions(ILogger<StorageFunctions> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(FulfillOrderQueue))]
    public void FulfillOrderQueue(
        [QueueTrigger("%OrdersQueueName%", Connection = "AzureWebJobsStorage")] string message,
        FunctionContext context)
    {
        var envelope = JsonSerializer.Deserialize<CommandEnvelope<FulfillmentCommand>>(message, JsonOptions);
        if (envelope is null)
        {
            throw new InvalidOperationException("Queue message could not be deserialized as a fulfillment command.");
        }

        logger.LogInformation(
            "Fulfillment command received. MessageId={MessageId} OrderId={OrderId} Priority={Priority} DequeueCount={DequeueCount}",
            envelope.MessageId,
            envelope.Payload.OrderId,
            envelope.Payload.Priority,
            context.BindingContext.BindingData.TryGetValue("DequeueCount", out var dequeueCount) ? dequeueCount : "unknown");
    }

    [Function(nameof(InboundDocumentBlob))]
    public async Task InboundDocumentBlob(
        [BlobTrigger("%BlobContainerName%/{name}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        string name,
        CancellationToken cancellationToken)
    {
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        logger.LogInformation(
            "Inbound document discovered. BlobName={BlobName} Size={Size} ContentType={ContentType}",
            name,
            properties.Value.ContentLength,
            properties.Value.ContentType);
    }

    [Function(nameof(DeadLetterAuditQueue))]
    public void DeadLetterAuditQueue(
        [QueueTrigger("poison-operations", Connection = "AzureWebJobsStorage")] string message,
        FunctionContext context)
    {
        logger.LogWarning("Poison operation message captured for audit. InvocationId={InvocationId} Body={Body}",
            context.InvocationId,
            message);
    }
}
