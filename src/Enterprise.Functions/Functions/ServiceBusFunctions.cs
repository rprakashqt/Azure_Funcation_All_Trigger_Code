using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Enterprise.Functions.Functions;

public sealed class ServiceBusFunctions(IOrderWorkflowService workflowService, ILogger<ServiceBusFunctions> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(PaymentCapturedServiceBusQueue))]
    public void PaymentCapturedServiceBusQueue(
        [ServiceBusTrigger("payment-captured", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        var payment = JsonSerializer.Deserialize<PaymentCaptured>(message.Body, JsonOptions)
            ?? throw new InvalidOperationException("Payment message could not be deserialized.");

        var ledgerEntry = workflowService.BuildPaymentLedgerEntry(payment);

        logger.LogInformation(
            "Payment ledger entry prepared. MessageId={MessageId} PaymentId={PaymentId} OrderId={OrderId} LedgerAccount={LedgerAccount}",
            message.MessageId,
            ledgerEntry.PaymentId,
            ledgerEntry.OrderId,
            ledgerEntry.LedgerAccount);
    }

    [Function(nameof(OrderEventTopicForLoyalty))]
    public void OrderEventTopicForLoyalty(
        [ServiceBusTrigger("%OrderEventsTopicName%", "%LoyaltyEventsSubscriptionName%", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        var order = JsonSerializer.Deserialize<OrderSubmitted>(message.Body, JsonOptions)
            ?? throw new InvalidOperationException("Order event could not be deserialized.");

        var loyalty = workflowService.BuildLoyaltyUpdate(order);

        logger.LogInformation(
            "Customer loyalty update prepared. MessageId={MessageId} CustomerId={CustomerId} Points={PointsEarned}",
            message.MessageId,
            loyalty.CustomerId,
            loyalty.PointsEarned);
    }
}
