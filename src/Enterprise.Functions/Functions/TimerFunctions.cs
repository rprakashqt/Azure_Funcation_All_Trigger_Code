using Enterprise.Functions.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Enterprise.Functions.Functions;

public sealed class TimerFunctions(IOrderWorkflowService workflowService, ILogger<TimerFunctions> logger)
{
    [Function(nameof(NightlyOrderReconciliationTimer))]
    public void NightlyOrderReconciliationTimer(
        [TimerTrigger("0 0 2 * * *", RunOnStartup = false)] TimerInfo timerInfo)
    {
        var businessDate = DateTimeOffset.UtcNow.AddDays(-1);
        var summary = workflowService.BuildReconciliationSummary(businessDate);

        logger.LogInformation(
            "Nightly reconciliation completed for {BusinessDate}. OrdersReviewed={OrdersReviewed} ExceptionsRaised={ExceptionsRaised} NextRun={NextRun}",
            summary.BusinessDate,
            summary.OrdersReviewed,
            summary.ExceptionsRaised,
            timerInfo.ScheduleStatus?.Next);
    }

    [Function(nameof(CacheWarmupTimer))]
    public void CacheWarmupTimer([TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo)
    {
        logger.LogInformation("Operational cache warmup completed. LastRun={LastRun} NextRun={NextRun}",
            timerInfo.ScheduleStatus?.Last,
            timerInfo.ScheduleStatus?.Next);
    }
}
