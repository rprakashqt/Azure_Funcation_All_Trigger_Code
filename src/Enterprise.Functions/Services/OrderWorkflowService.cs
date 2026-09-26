using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Configuration;
using Enterprise.Functions.Models;
using Microsoft.Extensions.Options;

namespace Enterprise.Functions.Services;

public sealed class OrderWorkflowService(IClock clock, IOptions<EnterpriseFunctionOptions> options) : IOrderWorkflowService
{
    private readonly EnterpriseFunctionOptions _options = options.Value;

    public OrderRiskAssessment AssessRisk(OrderSubmitted order)
    {
        var reasons = new List<string>();

        if (order.Amount >= _options.HighValueOrderThreshold)
        {
            reasons.Add("High order value");
        }

        if (order.Lines.Any(line => line.Quantity > 25))
        {
            reasons.Add("Bulk quantity line item");
        }

        if (string.Equals(order.Channel, "marketplace", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Marketplace order requires partner verification");
        }

        var riskLevel = reasons.Count switch
        {
            0 => "Low",
            1 => "Medium",
            _ => "High"
        };

        return new OrderRiskAssessment(order.OrderId, riskLevel, reasons);
    }

    public FulfillmentCommand BuildFulfillmentCommand(OrderSubmitted order)
    {
        var priority = order.Amount >= _options.HighValueOrderThreshold ? "Expedite" : "Standard";
        var warehouseCode = string.Equals(order.Currency, "USD", StringComparison.OrdinalIgnoreCase) ? "US-EAST-01" : "GLOBAL-01";

        return new FulfillmentCommand(order.OrderId, order.CustomerId, warehouseCode, priority, clock.UtcNow);
    }

    public PaymentLedgerEntry BuildPaymentLedgerEntry(PaymentCaptured payment)
    {
        var ledgerAccount = payment.Provider.Equals("stripe", StringComparison.OrdinalIgnoreCase)
            ? "1100-STRIPE-CASH"
            : "1100-PAYMENTS-CLEARING";

        return new PaymentLedgerEntry(payment.PaymentId, payment.OrderId, payment.Amount, payment.Currency, ledgerAccount, clock.UtcNow);
    }

    public CustomerLoyaltyUpdate BuildLoyaltyUpdate(OrderSubmitted order)
    {
        var points = Math.Max(1, decimal.ToInt32(Math.Floor(order.Amount)));
        return new CustomerLoyaltyUpdate(order.CustomerId, order.OrderId, points, clock.UtcNow);
    }

    public ReconciliationSummary BuildReconciliationSummary(DateTimeOffset businessDate)
    {
        var deterministicSample = Math.Abs(HashCode.Combine(businessDate.Date, _options.EnvironmentName));
        return new ReconciliationSummary(
            DateOnly.FromDateTime(businessDate.UtcDateTime),
            OrdersReviewed: 1_000 + deterministicSample % 500,
            ExceptionsRaised: deterministicSample % 11,
            CompletedAtUtc: clock.UtcNow);
    }
}
