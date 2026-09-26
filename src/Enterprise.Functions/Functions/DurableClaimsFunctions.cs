using System.Net;
using System.Text.Json;
using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Enterprise.Functions.Functions;

public sealed class DurableClaimsFunctions(IClock clock, ILogger<DurableClaimsFunctions> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(StartClaimsWorkflowHttp))]
    public async Task<HttpResponseData> StartClaimsWorkflowHttp(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "claims")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        CancellationToken cancellationToken)
    {
        var claim = await JsonSerializer.DeserializeAsync<ClaimsCase>(request.Body, JsonOptions, cancellationToken);
        if (claim is null || string.IsNullOrWhiteSpace(claim.ClaimId))
        {
            var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "A valid claim is required." }, cancellationToken);
            return badRequest;
        }

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(ClaimsWorkflowOrchestrator), claim, cancellationToken);
        logger.LogInformation("Started claims workflow. InstanceId={InstanceId} ClaimId={ClaimId}", instanceId, claim.ClaimId);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            instanceId,
            claim.ClaimId,
            statusQueryGetUri = $"/runtime/webhooks/durabletask/instances/{instanceId}"
        }, cancellationToken);
        return response;
    }

    [Function(nameof(ClaimsWorkflowOrchestrator))]
    public static async Task<ClaimsDecision> ClaimsWorkflowOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var claim = context.GetInput<ClaimsCase>() ?? throw new InvalidOperationException("Claim input is required.");

        await context.CallActivityAsync(nameof(RegisterClaimActivity), claim);
        var fraudScore = await context.CallActivityAsync<int>(nameof(ScoreClaimActivity), claim);

        if (fraudScore >= 80)
        {
            await context.CallActivityAsync(nameof(RouteClaimForInvestigationActivity), claim);
            return new ClaimsDecision(claim.ClaimId, "ManualReview", "Special investigations unit", context.CurrentUtcDateTime);
        }

        await context.CallActivityAsync(nameof(ApproveClaimPaymentActivity), claim);
        return new ClaimsDecision(claim.ClaimId, "Approved", "Payment scheduled", context.CurrentUtcDateTime);
    }

    [Function(nameof(RegisterClaimActivity))]
    public void RegisterClaimActivity([ActivityTrigger] ClaimsCase claim)
    {
        logger.LogInformation("Claim registered. ClaimId={ClaimId} Policy={PolicyNumber}", claim.ClaimId, claim.PolicyNumber);
    }

    [Function(nameof(ScoreClaimActivity))]
    public int ScoreClaimActivity([ActivityTrigger] ClaimsCase claim)
    {
        var baseScore = claim.EstimatedLoss > 25_000 ? 60 : 20;
        var incidentScore = claim.IncidentType.Equals("theft", StringComparison.OrdinalIgnoreCase) ? 25 : 10;
        var score = Math.Min(100, baseScore + incidentScore);

        logger.LogInformation("Claim scored. ClaimId={ClaimId} Score={FraudScore}", claim.ClaimId, score);
        return score;
    }

    [Function(nameof(RouteClaimForInvestigationActivity))]
    public void RouteClaimForInvestigationActivity([ActivityTrigger] ClaimsCase claim)
    {
        logger.LogWarning("Claim routed to investigation. ClaimId={ClaimId} EstimatedLoss={EstimatedLoss}", claim.ClaimId, claim.EstimatedLoss);
    }

    [Function(nameof(ApproveClaimPaymentActivity))]
    public void ApproveClaimPaymentActivity([ActivityTrigger] ClaimsCase claim)
    {
        logger.LogInformation("Claim approved for payment. ClaimId={ClaimId} ApprovedAtUtc={ApprovedAtUtc}", claim.ClaimId, clock.UtcNow);
    }
}
