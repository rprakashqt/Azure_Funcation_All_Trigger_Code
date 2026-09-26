using Enterprise.Functions.Models;

namespace Enterprise.Functions.Abstractions;

public interface IOrderWorkflowService
{
    OrderRiskAssessment AssessRisk(OrderSubmitted order);

    FulfillmentCommand BuildFulfillmentCommand(OrderSubmitted order);

    PaymentLedgerEntry BuildPaymentLedgerEntry(PaymentCaptured payment);

    CustomerLoyaltyUpdate BuildLoyaltyUpdate(OrderSubmitted order);

    ReconciliationSummary BuildReconciliationSummary(DateTimeOffset businessDate);
}
