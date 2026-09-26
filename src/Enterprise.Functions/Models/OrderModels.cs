using System.Text.Json.Serialization;

namespace Enterprise.Functions.Models;

public sealed record OrderSubmitted(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Channel,
    IReadOnlyCollection<OrderLine> Lines,
    DateTimeOffset SubmittedAtUtc);

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

public sealed record FulfillmentCommand(
    string OrderId,
    string CustomerId,
    string WarehouseCode,
    string Priority,
    DateTimeOffset RequestedAtUtc);

public sealed record PaymentCaptured(
    string PaymentId,
    string OrderId,
    decimal Amount,
    string Currency,
    string Provider,
    DateTimeOffset CapturedAtUtc);

public sealed record PaymentLedgerEntry(
    string PaymentId,
    string OrderId,
    decimal Amount,
    string Currency,
    string LedgerAccount,
    DateTimeOffset PostedAtUtc);

public sealed record CustomerLoyaltyUpdate(
    string CustomerId,
    string OrderId,
    int PointsEarned,
    DateTimeOffset EarnedAtUtc);

public sealed record OrderRiskAssessment(
    string OrderId,
    string RiskLevel,
    IReadOnlyCollection<string> Reasons);

public sealed record ReconciliationSummary(
    DateOnly BusinessDate,
    int OrdersReviewed,
    int ExceptionsRaised,
    DateTimeOffset CompletedAtUtc);

public sealed record CommandEnvelope<T>(
    string MessageId,
    string CorrelationId,
    string Source,
    T Payload,
    DateTimeOffset CreatedAtUtc);

public sealed record ReadinessReport(
    bool Ready,
    IReadOnlyDictionary<string, string> Dependencies,
    DateTimeOffset CheckedAtUtc);

public sealed record CustomerProfile(
    [property: JsonPropertyName("id")] string Id,
    string CustomerId,
    string Email,
    string Tier,
    DateTimeOffset UpdatedAtUtc);

public sealed record ShipmentRow(
    string ShipmentId,
    string OrderId,
    string Carrier,
    string TrackingNumber,
    string Status,
    DateTimeOffset UpdatedAtUtc);

public sealed record ClaimsCase(
    string ClaimId,
    string CustomerId,
    string PolicyNumber,
    decimal EstimatedLoss,
    string IncidentType,
    DateTimeOffset ReportedAtUtc);

public sealed record ClaimsDecision(
    string ClaimId,
    string Decision,
    string NextAction,
    DateTimeOffset CompletedAtUtc);
