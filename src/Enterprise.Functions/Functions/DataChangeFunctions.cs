using Enterprise.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;

namespace Enterprise.Functions.Functions;

public sealed class DataChangeFunctions(ILogger<DataChangeFunctions> logger)
{
    [Function(nameof(CustomerProfileCosmosChangeFeed))]
    public void CustomerProfileCosmosChangeFeed(
        [CosmosDBTrigger(
            databaseName: "%CosmosDatabaseName%",
            containerName: "%CosmosContainerName%",
            Connection = "CosmosDbConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<CustomerProfile> profiles)
    {
        if (profiles.Count == 0)
        {
            return;
        }

        foreach (var profile in profiles)
        {
            logger.LogInformation(
                "Customer profile changed. CustomerId={CustomerId} Tier={Tier} UpdatedAt={UpdatedAtUtc}",
                profile.CustomerId,
                profile.Tier,
                profile.UpdatedAtUtc);
        }
    }

    [Function(nameof(ShipmentSqlChangeTracking))]
    public void ShipmentSqlChangeTracking(
        [SqlTrigger("[dbo].[Shipments]", "SqlConnectionString")] IReadOnlyList<SqlChange<ShipmentRow>> changes)
    {
        foreach (var change in changes)
        {
            logger.LogInformation(
                "Shipment SQL change received. Operation={Operation} ShipmentId={ShipmentId} OrderId={OrderId} Status={Status}",
                change.Operation,
                change.Item.ShipmentId,
                change.Item.OrderId,
                change.Item.Status);
        }
    }
}
