using System.Net;
using System.Text.Json;
using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Enterprise.Functions.Functions;

public sealed class HttpApiFunctions(
    IOrderWorkflowService workflowService,
    IReadinessService readinessService,
    ICommandEnvelopeFactory envelopeFactory,
    ILogger<HttpApiFunctions> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(SubmitOrderHttp))]
    public async Task<HttpResponseData> SubmitOrderHttp(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var order = await JsonSerializer.DeserializeAsync<OrderSubmitted>(request.Body, JsonOptions, cancellationToken);
        if (order is null || string.IsNullOrWhiteSpace(order.OrderId) || order.Lines.Count == 0)
        {
            return await CreateJsonResponseAsync(request, HttpStatusCode.BadRequest, new
            {
                error = "A valid order with at least one line item is required."
            }, cancellationToken);
        }

        var risk = workflowService.AssessRisk(order);
        var command = workflowService.BuildFulfillmentCommand(order);
        var envelope = envelopeFactory.Create(command, nameof(SubmitOrderHttp), order.OrderId);

        logger.LogInformation(
            "Accepted order {OrderId} for customer {CustomerId}. Risk={RiskLevel} MessageId={MessageId}",
            order.OrderId,
            order.CustomerId,
            risk.RiskLevel,
            envelope.MessageId);

        return await CreateJsonResponseAsync(request, HttpStatusCode.Accepted, new
        {
            order.OrderId,
            risk,
            fulfillmentCommand = envelope,
            statusUrl = $"/api/orders/{Uri.EscapeDataString(order.OrderId)}/status"
        }, cancellationToken);
    }

    [Function(nameof(OrderStatusHttp))]
    public async Task<HttpResponseData> OrderStatusHttp(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderId}/status")] HttpRequestData request,
        string orderId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Order status lookup requested for {OrderId}.", orderId);

        return await CreateJsonResponseAsync(request, HttpStatusCode.OK, new
        {
            orderId,
            status = "Accepted",
            lastUpdatedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    [Function(nameof(HealthLiveHttp))]
    public async Task<HttpResponseData> HealthLiveHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/live")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        return await CreateJsonResponseAsync(request, HttpStatusCode.OK, new
        {
            status = "Healthy",
            checkedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    [Function(nameof(HealthReadyHttp))]
    public async Task<HttpResponseData> HealthReadyHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/ready")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var report = await readinessService.CheckAsync(cancellationToken);
        return await CreateJsonResponseAsync(
            request,
            report.Ready ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable,
            report,
            cancellationToken);
    }

    private static async Task<HttpResponseData> CreateJsonResponseAsync<T>(
        HttpRequestData request,
        HttpStatusCode statusCode,
        T body,
        CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(body, cancellationToken);
        return response;
    }
}
