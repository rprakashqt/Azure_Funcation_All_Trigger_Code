using Azure.Messaging.EventGrid;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Enterprise.Functions.Functions;

public sealed class EventingFunctions(ILogger<EventingFunctions> logger)
{
    [Function(nameof(DeviceTelemetryEventHub))]
    public void DeviceTelemetryEventHub(
        [EventHubTrigger("%TelemetryEventHubName%", Connection = "EventHubConnection", ConsumerGroup = "$Default")] EventData[] events)
    {
        foreach (var eventData in events)
        {
            logger.LogInformation(
                "Telemetry event received. EventId={EventId} PartitionKey={PartitionKey} SequenceNumber={SequenceNumber} EnqueuedTime={EnqueuedTime}",
                eventData.MessageId,
                eventData.PartitionKey,
                eventData.SequenceNumber,
                eventData.EnqueuedTime);
        }
    }

    [Function(nameof(StorageLifecycleEventGrid))]
    public void StorageLifecycleEventGrid([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        logger.LogInformation(
            "Event Grid notification received. Id={EventId} EventType={EventType} Subject={Subject}",
            eventGridEvent.Id,
            eventGridEvent.EventType,
            eventGridEvent.Subject);
    }
}
