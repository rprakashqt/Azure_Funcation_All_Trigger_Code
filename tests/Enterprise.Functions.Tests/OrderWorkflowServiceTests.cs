using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Configuration;
using Enterprise.Functions.Models;
using Enterprise.Functions.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Enterprise.Functions.Tests;

public sealed class OrderWorkflowServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 26, 6, 30, 0, TimeSpan.Zero);

    [Fact]
    public void AssessRisk_FlagsHighValueAndMarketplaceOrders()
    {
        var service = CreateService(highValueOrderThreshold: 1_000);
        var order = new OrderSubmitted(
            "ORD-1001",
            "CUST-42",
            5_000,
            "USD",
            "marketplace",
            [new OrderLine("SKU-1", 2, 2_500)],
            FixedNow);

        var result = service.AssessRisk(order);

        Assert.Equal("High", result.RiskLevel);
        Assert.Contains("High order value", result.Reasons);
        Assert.Contains("Marketplace order requires partner verification", result.Reasons);
    }

    [Fact]
    public void BuildFulfillmentCommand_UsesExpeditePriorityForHighValueOrders()
    {
        var service = CreateService(highValueOrderThreshold: 1_000);
        var order = new OrderSubmitted(
            "ORD-1002",
            "CUST-43",
            2_500,
            "USD",
            "web",
            [new OrderLine("SKU-2", 1, 2_500)],
            FixedNow);

        var command = service.BuildFulfillmentCommand(order);

        Assert.Equal("Expedite", command.Priority);
        Assert.Equal("US-EAST-01", command.WarehouseCode);
        Assert.Equal(FixedNow, command.RequestedAtUtc);
    }

    [Fact]
    public void BuildPaymentLedgerEntry_MapsStripeToDedicatedLedgerAccount()
    {
        var service = CreateService();
        var payment = new PaymentCaptured("PAY-1", "ORD-1", 123.45m, "USD", "stripe", FixedNow);

        var ledger = service.BuildPaymentLedgerEntry(payment);

        Assert.Equal("1100-STRIPE-CASH", ledger.LedgerAccount);
        Assert.Equal(FixedNow, ledger.PostedAtUtc);
    }

    private static OrderWorkflowService CreateService(int highValueOrderThreshold = 10_000)
    {
        var options = Options.Create(new EnterpriseFunctionOptions
        {
            EnvironmentName = "test",
            HighValueOrderThreshold = highValueOrderThreshold
        });

        return new OrderWorkflowService(new FixedClock(FixedNow), options);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
