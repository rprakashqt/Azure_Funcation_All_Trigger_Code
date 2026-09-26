@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Base name for the Function App and related resources.')
param appName string

@description('Environment name such as dev, test, or prod.')
param environmentName string = 'dev'

var normalizedName = toLower(replace(appName, '-', ''))
var storageName = take('${normalizedName}${uniqueString(resourceGroup().id)}', 24)
var hostingPlanName = '${appName}-plan'
var appInsightsName = '${appName}-appi'
var serviceBusName = '${appName}-sb'
var eventHubNamespaceName = '${appName}-eh'
var cosmosName = '${appName}-cosmos'
var signalRName = '${appName}-signalr'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

resource paymentsQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: 'payment-captured'
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
  }
}

resource orderEventsTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  parent: serviceBus
  name: 'order-events'
}

resource loyaltySubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: orderEventsTopic
  name: 'loyalty'
  properties: {
    maxDeliveryCount: 10
  }
}

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: eventHubNamespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
}

resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  parent: eventHubNamespace
  name: 'device-telemetry'
  properties: {
    partitionCount: 4
    messageRetentionInDays: 1
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
  }
}

resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: signalRName
  location: location
  sku: {
    name: 'Free_F1'
    capacity: 1
  }
  kind: 'SignalR'
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Serverless'
      }
    ]
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storage.id, storage.apiVersion).keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'DurableHubName'
          value: 'enterpriseorchestration'
        }
        {
          name: 'Enterprise__EnvironmentName'
          value: environmentName
        }
        {
          name: 'ServiceBusConnection'
          value: listKeys(serviceBus.id, serviceBus.apiVersion).primaryConnectionString
        }
        {
          name: 'EventHubConnection'
          value: listKeys(eventHubNamespace.id, eventHubNamespace.apiVersion).primaryConnectionString
        }
        {
          name: 'CosmosDbConnection'
          value: listConnectionStrings(cosmos.id, cosmos.apiVersion).connectionStrings[0].connectionString
        }
        {
          name: 'BlobContainerName'
          value: 'inbound-documents'
        }
        {
          name: 'OrdersQueueName'
          value: 'orders-to-fulfill'
        }
        {
          name: 'OrderEventsTopicName'
          value: 'order-events'
        }
        {
          name: 'LoyaltyEventsSubscriptionName'
          value: 'loyalty'
        }
        {
          name: 'TelemetryEventHubName'
          value: 'device-telemetry'
        }
        {
          name: 'CosmosDatabaseName'
          value: 'enterprise'
        }
        {
          name: 'CosmosContainerName'
          value: 'customers'
        }
        {
          name: 'AzureSignalRConnectionString'
          value: listKeys(signalR.id, signalR.apiVersion).primaryConnectionString
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionPrincipalId string = functionApp.identity.principalId
