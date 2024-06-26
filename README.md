# MarginTrading.Backend, MarginTrading.AccountMarginEventsBroker #

Margin trading core API. Broker to pass margin and liquidation events from message queue to storage.
Below is the API description.

## How to use in prod env? ##

1. Pull "mt-trading-core" docker image with a corresponding tag.
2. Configure environment variables according to "Environment variables" section.
3. Put secrets.json with endpoint data including the certificate:
```json
"Kestrel": {
  "EndPoints": {
    "HttpsInlineCertFile": {
      "Url": "https://*:5130",
      "Certificate": {
        "Path": "<path to .pfx file>",
        "Password": "<certificate password>"
      }
    }
}
```
4. Initialize all dependencies. 
5. Run.

## How to run for debug? ##

1. Clone repo to some directory.
2. In MarginTrading.Backend root create a appsettings.dev.json with settings.
3. Add environment variable "SettingsUrl": "appsettings.dev.json".
4. VPN to a corresponding env must be connected and all dependencies must be initialized. 
5. Optionally, external dependencies can be replaced with docker images 
6. Run.

## Running Rabbit MQ in docker ##
Example:
```bash
docker run -d --hostname nova.lykke --name rabbit-nova -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=margintrading -e RABBITMQ_DEFAULT_PASS=margintrading rabbitmq:3-management
```
Example for running docker on macOS with Apple Silicon processor:
```bash
docker run -d --hostname nova.lykke --name rabbit-nova -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=margintrading -e RABBITMQ_DEFAULT_PASS=margintrading arm64v8/rabbitmq:3-management
```
Stop rabbit mq:
```bash
docker stop rabbit-nova
docker rm rabbit-nova
```

## Startup process ##

1. Standard ASP.NET middlewares are initialised.
2. Settings are loaded.
3. Health checks are ran:
- StartupDeduplicationService checks a "TradingEngine:DeduplicationTimestamp" in Redis and 
if DeduplicationTimestamp > (now - DeduplicationCheckPeriod) exception is thrown saying:
"Trading Engine failed to start due to deduplication validation failure".
- StartupQueuesCheckerService checks that OrderHistory and PositionHistory broker related queues are empty.
- Additionally, corresponding poison queues are checked for emptyness if parameter `DisablePoisonQueueCheck` is disabled.  
Queue names are set in settings StartupQueuesChecker section with OrderHistoryQueueName and PositionHistoryQueueName. 
4. IoC container is built, all caches are warmed up.
5. Scheduled jobs are initialised.

### Dependencies ###

TBD

### Configuration ###

Kestrel configuration may be passed through appsettings.json, secrets or environment.
All variables and value constraints are default. For instance, to set host URL the following env variable may be set:
```json
{
    "Kestrel__EndPoints__Http__Url": "http://*:5030"
}
```

### Environment variables ###

* *RESTART_ATTEMPTS_NUMBER* - number of restart attempts. If not set int.MaxValue is used.
* *RESTART_ATTEMPTS_INTERVAL_MS* - interval between restarts in milliseconds. If not set 10000 is used.
* *SettingsUrl* - defines URL of remote settings or path for local settings.

### Queues checker

The service checks `OrdersHistory` and `PositionsHistory` queues on startup. If those queues contain any unprocessed events, the service startup fails. The `StartupQueuesChecker.DisablePoisonQueueCheck` parameter controls this behaviour: by default, poison queues (with postfix `-poison`) are checked too. By setting `StartupQueuesChecker.DisablePoisonQueueCheck` to true you can skip checking the poison queues (only normal queues will be checked) and start the service.

### Settings ###

Settings schema is:

```json
{
  "AccountsManagementServiceClient": {
    "ServiceUrl": "http://mt-account-management.mt.svc.cluster.local"
  },
  "Jobs": {
    "NotificationsHubName": "",
    "NotificationsHubConnectionString": ""
  },
  "MtBackend": {
    "ApiKey": "MT Core backend api key",
    "MtRabbitMqConnString": "amqp://login:password@rabbit-mt.mt.svc.cluster.local:5672",
    "Db": {
      "StorageMode": "SqlServer",
      "LogsConnString": "logs connection string",
      "MarginTradingConnString": "date connection string",
      "StateConnString": "state connection string",
      "SqlConnectionString": "sql connection string",
      "OrdersHistorySqlConnectionString": "sql connection string",
      "OrdersHistoryTableName": "OrdersHistory",
      "PositionsHistorySqlConnectionString": "sql connection string",
      "PositionsHistoryTableName": "PositionsHistory",
	  "QueryTimeouts": 
      {
        "GetLastSnapshotTimeoutS": 120
      }
    },
    "RabbitMqQueues": {
      "OrderHistory": {
        "ExchangeName": "lykke.mt.orderhistory"
      },
      "OrderRejected": {
        "ExchangeName": "lykke.mt.orderrejected"
      },
      "OrderbookPrices": {
        "ExchangeName": "lykke.mt.pricefeed"
      },
      "AccountStopout": {
        "ExchangeName": "lykke.mt.account.stopout"
      },
      "AccountMarginEvents": {
        "ExchangeName": "lykke.mt.account.marginevents"
      },
      "AccountStats": {
        "ExchangeName": "lykke.mt.account.stats"
      },
      "Trades": {
        "ExchangeName": "lykke.mt.trades"
      },
      "PositionHistory": {
        "ExchangeName": "lykke.mt.position.history"
      },
      "ExternalOrder": {
        "ExchangeName": "lykke.stpexchangeconnector.trades"
      },
      "MarginTradingEnabledChanged": {
        "ExchangeName": "lykke.mt.enabled.changed"
      },
      "SettingsChanged": {
        "ExchangeName": "MtCoreSettingsChanged"
      }
    },
    "FxRateRabbitMqSettings": {
      "ConnectionString": "amqp://login:pwd@rabbit-mt.mt.svc.cluster.local:5672",
      "ExchangeName": "lykke.stpexchangeconnector.fxRates"
    },
    "StpAggregatorRabbitMqSettings": {
      "ConnectionString": "amqp://login:pwd@rabbit-mt.mt.svc.cluster.local:5672",
      "ExchangeName": "lykke.exchangeconnector.orderbooks",
      "ConsumerCount": 10
    },
    "BlobPersistence": {
      "QuotesDumpPeriodMilliseconds": 3400000,
      "FxRatesDumpPeriodMilliseconds": 3500000,
      "OrderbooksDumpPeriodMilliseconds": 3600000,
      "OrdersDumpPeriodMilliseconds": 600000
    },
    "RequestLoggerSettings": {
      "Enabled": false,
      "MaxPartSize": 2048
    },
    "Telemetry": {
      "LockMetricThreshold": 10
    },
    "ReportingEquivalentPricesSettings": [
      {
        "LegalEntity": "Default",
        "EquivalentAsset": "EUR"
      },
      {
        "LegalEntity": "UNKNOWN",
        "EquivalentAsset": "USD"
      }
    ],
    "UseAzureIdentityGenerator": false,
    "WriteOperationLog": true,
    "UseSerilog": false,
    "ExchangeConnector": "FakeExchangeConnector",
    "MaxMarketMakerLimitOrderAge": 3000000,
    "Cqrs": {
      "ConnectionString": "amqp://login:pwd@rabbit-mt.mt.svc.cluster.local:5672",
      "RetryDelay": "00:00:02",
      "EnvironmentName": "env name"
    },
    "SpecialLiquidation": {
      "Enabled": true,
      "FakePrice": 5,
      "PriceRequestTimeoutSec": 600,
      "RetryTimeout": "00:01:00",
      "VolumeThreshold": 1000,
      "VolumeThresholdCurrency": "EUR",
      "FakePriceRequestAutoApproval": true
    },
    "ChaosKitty": {
      "StateOfChaos": 0
    },
    "Throttling": {
      "MarginCallThrottlingPeriodMin": 30,
      "StopOutThrottlingPeriodMin": 1
    },
    "OvernightMargin": {
      "ScheduleMarketId": "PlatformScheduleMarketId",
      "OvernightMarginParameter": 100,
      "WarnPeriodMinutes": 10,
      "ActivationPeriodMinutes": 10
    },
     "PendingOrderRetriesThreshold": 100,
     "RedisSettings": {
       "Configuration": "redis conn str"
     },
     "DeduplicationTimestampPeriod": "00:00:01",
     "DeduplicationCheckPeriod": "00:00:02",
     "StartupQueuesChecker": {
       "ConnectionString": "amqp://login:pwd@rabbit-mt.mt.svc.cluster.local:5672",
       "OrderHistoryQueueName": "lykke.mt.orderhistory.MarginTrading.TradingHistory.OrderHistoryBroker.DefaultEnv",
       "PositionHistoryQueueName": "lykke.mt.position.history.MarginTrading.TradingHistory.PositionHistoryBroker.DefaultEnv.PositionsHistory",
       "DisablePoisonQueueCheck": true | false
     }
  },
  "MtStpExchangeConnectorClient": {
    "ServiceUrl": "http://gavel.mt.svc.cluster.local:5019",
    "ApiKey": "key"
  },
  "OrderBookServiceClient": {
    "ServiceUrl": "http://mt-orderbook-service.mt.svc.cluster.local"
  },
  "SettingsServiceClient": {
    "ServiceUrl": "http://mt-settings-service.mt.svc.cluster.local"
  },
  "MdmServiceClient": {
    "ServiceUrl": "http://mdm.mt.svc.cluster.local",
    "ApiKey": "key"
  }
}
```

#### Optional sections of MtBackend ####
(with hardcoded default values):

```json
"OrderbookValidation": {
    "ValidateInstrumentStatusForTradingQuotes": false,
    "ValidateInstrumentStatusForTradingFx": false,
    "ValidateInstrumentStatusForEodQuotes": true,
    "ValidateInstrumentStatusForEodFx": true
}
```
