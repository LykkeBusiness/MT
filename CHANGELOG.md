## 2.38.0 - Nova 2. Delivery 49 (February 07, 2025)
### What's changed
* LT-6021: Add a new report accountliquidations.
* LT-6011: Update rabbitmqbroker in trading core.
* LT-5955: "Collection was modified" error.
* LT-5953: Loop on tradingsnapshot in database follow-up.


## 2.36.1 - Nova 2. Delivery 47. Hotfix 2 (January 16, 2025)
### What's changed
* LT-5991: Bump LykkeBiz.RabbitMqBroker to 8.11.1


## 2.37.0 - Nova 2. Delivery 48 (December 20, 2024)
### What's changed
* LT-5921: Update refit to 8.x version.
* LT-5886: Keep schema for appsettings.json up to date.
* LT-5884: Keep schema for appsettings.json up to date.


## 2.36.0 - Nova 2. Delivery 47 (November 18, 2024)
### What's changed
* LT-5817: Update messagepack to 2.x version.
* LT-5791: Add assembly load logger.
* LT-5764: Migrate to quorum queues.

### Deployment
In this release, all previously specified queues have been converted to quorum queues to enhance system reliability. The affected queues are:
- `MtCoreSettingsChanged.MarginTrading.Backend.DefaultEnv`
- `dev.MdmService.events.exchange.MarginTrading.Backend.DefaultEnv`
- `lykke.mt.account.marginevents.MarginTrading.AccountMarginEventsBroker.DefaultEnv`
- if `MarketMakerRabbitMqSettings` section is configured on your side then queue name should be `{MarketMakerRabbitMqSettings.ExchangeName}.MarginTrading.Backend.{Configured Environment Name or DefaultEnv}.{Configured Instance Id}`
- if `RisksRabbitMqSettings` section is configured on your side then queue name should be `{RisksRabbitMqSettings.ExchangeName}.MarginTrading.Backend.{Configured Environment Name or DefaultEnv}.{Configured Instance Id}`

#### Automatic Conversion to Quorum Queues
The conversion to quorum queues will occur automatically upon service startup **if**:
* There are **no messages** in the existing queues.
* There are **no active** subscribers to the queues.

**Warning**: If messages or subscribers are present, the automatic conversion will fail. In such cases, please perform the following steps:
1. Run the previous version of the component associated with the queue.
1. Make sure all the messages are processed and the queue is empty.
1. Shut down the component associated with the queue.
1. Manually delete the existing classic queue from the RabbitMQ server.
1. Restart the component to allow it to create the quorum queue automatically.

#### Poison Queues
All the above is also applicable to the poison queues associated with the affected queues. Please ensure that the poison queues are also converted to quorum queues.

#### Disabling Mirroring Policies
Since quorum queues inherently provide data replication and reliability, server-side mirroring policies are no longer necessary for these queues. Please disable any existing mirroring policies applied to them to prevent redundant configurations and potential conflicts.

#### Environment and Instance Identifiers
Please note that the queue names may include environment-specific identifiers (e.g., dev, test, prod). Ensure you replace these placeholders with the actual environment names relevant to your deployment. The same applies to instance names embedded within the queue names (e.g., DefaultEnv, etc.).


## 2.35.0 - Nova 2. Delivery 46 (September 27, 2024)
### What's changed
* LT-5595: Migrate to net 8.


## 2.34.0 - Nova 2. Delivery 45 (September 02, 2024)
### What's changed
* LT-5495: Liquidation stops and finishes after the next platform opening.


## 2.33.0 - Nova 2. Delivery 44 (August 19, 2024)
### What's changed
* LT-5665: Add an api for deleting quotes from fx cache.
* LT-5524: Update rabbitmq broker library with new rabbitmq.client and templates.
* LT-5515: Failed if no open orders on the platform.
* LT-5364: Execution rejected by the bank matching engine should not appear as an error in mtcore.
* LT-5190: List nuget packages used by host (part 2).

### Deployment
Please ensure that the mirroring policy is configured on the RabbitMQ server side for the following queues:
- `MtCoreSettingsChanged.MarginTrading.Backend.DefaultEnv`
- `dev.MdmService.events.exchange.MarginTrading.Backend.DefaultEnv`
- `lykke.mt.account.marginevents.MarginTrading.AccountMarginEventsBroker.DefaultEnv`
- if `MarketMakerRabbitMqSettings` section is configured on your side then queue name should be `{MarketMakerRabbitMqSettings.ExchangeName}.MarginTrading.Backend.{Configured Environment Name or DefaultEnv}.{Configured Instance Id}`
- if `RisksRabbitMqSettings` section is configured on your side then queue name should be `{RisksRabbitMqSettings.ExchangeName}.MarginTrading.Backend.{Configured Environment Name or DefaultEnv}.{Configured Instance Id}`

These queues require the mirroring policy to be enabled as part of our ongoing initiative to enhance system reliability. They are now classified as "no loss" queues, which necessitates proper configuration. The mirroring feature must be enabled on the RabbitMQ server side.

In some cases, you may encounter an error indicating that the server-side configuration of a queue differs from the client’s expected configuration. If this occurs, please delete the queue, allowing it to be automatically recreated by the client.

**Warning**: The "no loss" configuration is only valid if the mirroring policy is enabled on the server side.

Please be aware that the provided queue names may include environment-specific identifiers (e.g., dev, test, prod). Be sure to replace these with the actual environment name in use. The same applies to instance names embedded within the queue names (e.g., DefaultEnv, etc.).


## 2.32.1 - Nova 2. Delivery 42. Hotfix 9 (July 19, 2024)
### What's changed
* LT-5621: Fix an issue with closing positions after platform's closure


## 2.32.0 - Nova 2. Delivery 41 (April 01, 2024)
### What's changed
* LT-5298: Looping error in mtcore requesting to stop the trading.
* LT-5279: Adjust the snapshot flow.
* LT-5061: Order does not execute automatically when contribution is started again.

### Deployment

* Add a new settings section: `BookKeeperServiceClient` (on the same level as `SettingsServiceClient`, `AccountsManagementServiceClient` etc)
Then fill the `ServiceUrl` parameter with Bookkeeper's api url

example:

```json
{
   "BookKeeperServiceClient": {
      "ServiceUrl": "//url"
   }
}
```

* (Optional) Add a new settings section: `SnapshotMonitorSettings`. Then fill the parameters 
`MonitoringDelay` and `DelayBeforeFallbackSnapshot`.
`MonitoringDelay` determines how often the service will check if snapshot was created.
`DelayBeforeFallbackSnapshot` determines maximum timeout before a fallback creation of snapshot will occur.

Default values: 
- `MonitoringDelay`: 30 sec
- `DelayBeforeFallbackSnapshot`: 5 min

Both parameters must be configured as timespans (e.g. `"00:03:00"`)

example: 
```json
{
   "SnapshotMonitorSettings": {
      "MonitoringDelay": "//timespan",
      "DelayBeforeFallbackSnapshot": "//timespan"
   }
}
```



## 2.31.0 - Nova 2. Delivery 40 (February 28, 2024)
### What's changed
* LT-5276: Step: deprecated packages validation is failed.
* LT-5256: Update lykke.httpclientgenerator to 5.6.2.
* LT-5250: Add IsTradingDisabled api.

### Deployment
* Added a new endpoint: `/api/testing/isTradingDisabled/{productId}`. It's used for EoD test tooling only.




## 2.30.3  - Nova 2. Delivery 39. Hotfix 1  (January 31, 2024)
### What's Changed
* LT-5192: Brackets are missing in assembly logger


## 2.30.0 - Nova 2. Delivery 39 (January 30, 2024)
### What's changed
* LT-5174: Include unconfirmedmargin in account stats and capital figures apis.
* LT-5144: Changelog.md for trading core.
* LT-5124: Add new api that returns filtered items.
* LT-5005: List nuget packages used by host (part 1).
* LT-4805: Return 500 answer code for get /api/positions/{positionid}.

### Deployment

* Added a new endpoint: `/api/sentiments/filtered`
* Added a new feature that logs used assembly versions. Full description is available [here](https://lykke-snow.atlassian.net/wiki/spaces/LEGO/pages/3554967553/Show+the+list+of+assemblies+loaded+by+host)




## 2.29.0 - Nova 2. Delivery 38 (December 13, 2023)
### What's changed
* LT-5062: Rfqevent rabbit mq re-configuration.
* LT-5034: Open orders api - sort chronologically.
* LT-5010: Change the logic for trading core snapshots.

### Deployment
* In `RfqChanged` configuration section (`/MtBackend/RabbitMqPublishers/RfqChanged`) just add field with connection string. If there is already one, ensure connection string is pointing to **multi-broker Rabbit MQ** instance. Also ensure that main Rabbit MQ configuration (`/MtBackend/MtRabbitMqConnString`) is pointing to **broker-level instance of Rabbit MQ**.
* Existing feature `Create trading snapshot` has been extended to include possibility to create snapshot in `Draft` status. Here no actions required.

## 2.28.1 - Nova 2. Delivery 37. Hotfix 2 (2023-11-24)
### What's Changed
* fix(LT-5079): Rollback major RabbitMQ updates

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.28.0...v2.28.1


## 2.27.3 - Nova 2. Delivery 36. Hotfix 4 (2023-10-23)
### What's Changed
* LT-4983: Improve performance statistics
* LT-5004: Demo Incident - Delay in candles

### Deployment
* Disable RabbitMQ messages logging by setting up environment variable `NOVA_DISABLE_OUTGOING_MESSAGE_PERSISTENCE` = true.

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.27.2...v2.27.3


## 2.28.0 - Nova 2. Delivery 37 (2023-10-18)
### What's Changed
* LT-5043: Bump `MarginTrading.BrokerBase` NuGet package
* LT-5004: Demo incident - Delay in candles
* LT-4984: Redis connection leak (potential fix)
* LT-4982: Resolving `IAccountUpdateService` leads to an exception on service shutdown
* LT-4818: Remove inconsistent data within migration

### Deployment
* The list of curl requests provided as a part of `LT-5015` to remove inconsistent data
* Set environment variable `NOVA_DISABLE_OUTGOING_MESSAGE_PERSISTENCE = true`, this will effectively disable any RabbitMQ logging to prevent side effects when testing new version of `Lykke.RabbitMqBroker` which should fix the issue with candles delay. This task will also be delivered as a hotfix for delivery 36. 


**Full change log**: https://github.com/lykkebusiness/mt/compare/v2.27.2...v2.28.0


## 2.27.2 - Nova 2. Delivery 36. Hotfix 3 (2023-10-10)
### What's Changed
* LT-5000: Simultaneous withdraw overcome withdraw limit

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.27.1...v2.27.2


## 2.26.31 - Nova 2. Delivery 35. Hotfix 5 (2023-09-20)
### What's Changed
- LT-4995: Upgrade Lykke.Snow.Common to 2.7.3

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.26.14...v2.26.31


## 2.27.1 - Nova 2. Delivery 36. Hotfix 2 (2023-09-12)
### What's Changed
* LT-4917: Rollback major rabbit mq update (same as [Nova 2. Delivery 34. Hotfix 1](https://github.com/LykkeBusiness/MT/tree/v2.25.6))


**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.27.0...v2.27.1


## 2.26.14 - Nova 2. Delivery 35. Hotfix 4 (2023-09-12)
### What's Changed
* LT-4917: Rollback major rabbit mq update (same as [Nova 2. Delivery 34. Hotfix 1](https://github.com/LykkeBusiness/MT/tree/v2.25.6))

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.26.0...v2.26.14


## 2.27.0 - Nova 2. Delivery 36 (2023-08-31)
### What's Changed
* LT-4940: Add validation for max position notional feature.
* LT-4893: Update nugets.


**Full change log**: https://github.com/lykkebusiness/mt/compare/v2.26.0...v2.27.0


## 2.25.17 - Nova 2. Delivery 34. Hotfix 7 (2023-07-27)
### What's Changed
* LT-4925: Upgrade Lykke.Snow.Common to 2.7.3

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.25.15...v2.25.17


## 2.25.15 - Nova 2. Delivery 34. Hotfix 6 (2023-07-24)
### What's Changed
* LT-4778:  Force cache update on Accounts during snapshot creation

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.25.6...v2.25.15


## 2.26.0 - Nova 2. Delivery 35 (2023-07-12)
### What's Changed
* LT-4803: Add performance tracker.
* LT-4783: Update rabbitmqbroker nuget package.
* LT-4778: Force cache update on accounts during snapshot creation.

### Deployment
* New configuration key `PerformanceTrackerEnabled` was added. Default value is `false`.

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.25.2...v2.26.0


## 2.25.7 - Nova 2. Delivery 33. Hotfix 6 (2023-07-03)
### What's Changed
* LT-4783: Update Lykke.RabbiMqBroker nuget package
* LT-4803: Add performance tracker

### Deployment
* New configuration key `PerformanceTrackerEnabled` was added. Default value is `false`. To enable performance tracking reporting (logs report every 1 minute) `PerformanceTrackerEnabled` should be set to `true`.

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.25.5...v2.25.7


## 2.25.6 - Nova 2. Release 34. Hotfix 1 (2023-06-14)
### What's Changed
* LT-4767: rolling back major Rabbit MQ client library update (6.4.0 -> 5.2.0)

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.25.2...v2.25.6


## 2.25.5 - Nova 2 . Delivery 33. Hotfix 5 (2023-06-14)
### What's Changed
* LT-4767: rolling back major Rabbit MQ client library update (6.4.0 -> 5.2.0)

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.25.0...v2.25.5


## 2.25.2 - Nova 2. Delivery 34 (2023-06-05)
### What's Changed
* LT-4745: Add logs for BUGS-2954
* LT-4742: Fix configuration issue
* LT-4726: Upgrade Lykke.MarginTrading.AssetService.Contracts 
* LT-4696: Upgrade Mdm contracts package
* LT-4585: RFQs get multiplied when timeout

### Deployment

1. Please rename setting: MtBackend.RabbitMqPublishers.RfqChangedRabbitMqSettings -> MtBackend.RabbitMqPublishers.RfqChanged


2. We'll add log "[BUGS-2954](https://lykke-snow.atlassian.net/browse/BUGS-2954): Changing order price" with order, price and forceOpen details before price validation.
We'll also add "[BUGS-2954](https://lykke-snow.atlassian.net/browse/BUGS-2954): Order price change accepted" with same details after successful price validation.


**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.25.0...v2.25.2


## 2.25.0 - Nova 2. Delivery 33 (2023-04-11)
### What's Changed
* LT-4659: Update `lykke.snow.common` package with fixed formula.
* LT-4654: Contract size to affect order validation.
* LT-4651: Honoring code review upon `LT-4280`.
* LT-4644: Fix order volume.
* LT-4553: Make compiledschedulechangedevent and tradecontract publication optional.
* LT-4280: Update with new version of rabbit mq libraries with fixed logging.
* LT-4271*: To add logging for broker publishers.

### Deployment
* Rename configuration section `RabbitMqQueues` to `RabbitMqPublishers`
* Rename `RabbitMqPublishers__SettingsChanged` to `SettingsChangedRabbitMqSettings` and move it next to other RabbitMqSettings (so one level higher in json). 
* Add `ConnectionString` to `SettingsChangedRabbitMqSettings`.

The result should look like this:
```json
{
    "RabbitMqPublishers": {
      "OrderHistory": {
        "ExchangeName": "lykke.mt.orderhistory",
...
      },
// ... other publishers
}, // publishers sections closes here

    "FxRateRabbitMqSettings": {
      "ConnectionString": "${MtRabbitMqConnectionString}",
      "ExchangeName": "lykke.stpexchangeconnector.fxRates"
    },
// .., other rabbit mq settings
    "SettingsChangedRabbitMqSettings": {
      "ConnectionString": "${MtRabbitMqConnectionString}",
      "ExchangeName": "MtCoreSettingsChanged"
    },
}
```

* Move `RfqChangedRabbitMqSettings` configuration section to `RabbitMqPublishers` section

#### RabbitMQ Connection Retry Policies
The new feature `RabbitMQ Connection Retry Policies` was introduced in this release.
We've added two new retry policies to improve the connection stability with the RabbitMQ broker. These policies define the number of retries and the time intervals between each retry attempt. 
* `InitialConnectionSleepIntervals`: This policy is used during the service startup when establishing the initial connections to the RabbitMQ broker. The number of items in the array represents the number of retry attempts, while the interval between items indicates the amount of time to wait before the next retry. 
* `RegularSleepIntervals`: This policy is used during the service's normal operation if the connection to the RabbitMQ broker is detected to be broken. Similar to the InitialConnectionSleepIntervals policy, the number of items in the array defines the number of retries, and the interval between items indicates the amount of time to wait before the next retry.

If all retry attempts are exhausted and the connection to the RabbitMQ broker is still unsuccessful, an exception will be raised.
To configure the RabbitMQ connection retry policies, you need to update the appsettings.json file by adding the `RabbitMqRetryPolicy` section as follows (the one should add new sub-section into `MtBackend`):
```json
{
   "MtBackend":{
      "RabbitMqRetryPolicy":{
         "InitialConnectionSleepIntervals":[
            "00:00:01",
            "00:00:02",
            "00:00:04",
            "00:00:08",
            "00:00:16",
            "00:00:32",
            "00:01:04",
            "00:02:08"
         ],
         "RegularSleepIntervals":[
            "00:00:01",
            "00:00:02",
            "00:00:02",
            "00:00:02",
            "00:00:02",
            "00:00:03",
            "00:00:03",
            "00:00:05"
         ]
      }
   }
}
```

The one can customize the number of retries and the sleep intervals by modifying the arrays for `InitialConnectionSleepIntervals` and `RegularSleepIntervals`, respectively. Make sure to follow the time format `HH:mm:ss` when specifying the intervals.

**Full change log**: https://github.com/lykkebusiness/mt/compare/v2.24.4...v2.25.0

## 2.23.9 - Nova 2 Delivery 31 Hotfix 6 (2023-03-03)
### What's Changed
- LT-4523: Add margin calculation logs to MT Core

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.23.8...v2.23.9


## 2.24.4 - Nova 2. Delivery 32 (2023-03-01)
### What's Changed
* LT-4523: Add margin calculation logs to mt core.
* LT-4502: Maxpositionsize validation works incorrectly.
* LT-4476: Do not let the host keep running if startupmanager failed to start.
* LT-4364: Update contracts to include error codes (additional cases).


**Full change log**: https://github.com/lykkebusiness/mt/compare/v2.23.8...v2.24.4


## 2.23.8 - Nova 2. Delivery 31. Hotfix 2 (2023-01-24)
### What's Changed
* LT-4396: Change cache refresh logic to update ClientTradingCondition

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.23.1...v2.23.8


## 2.23.1 - Nova 2. Delivery 31. Hotfix 1 (2023-01-19)
### What's Changed
* LT-4392: fix missing logs

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.23.0...v2.23.1


## 2.21.10 - Nova 2. Delivery 29. Hotfix 5 (2023-01-19)
### What's Changed
* LT-4392: fix missing logs

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.21.7...v2.21.10


## 2.23.0 - Nova 2. Delivery 31 (2023-01-16)
### What's Changed\
* LT-4370: Update with new version of lykke.messaging libraries to fix verify endpoints.
* LT-4368: Add withdrawal logs.
* LT-4343: One client withdrawed several times all his money.
* LT-4289: Update contracts to include error codes.
* LT-4071: Executing rfq with changed quantity - remaining issues.


**Full change log**: https://github.com/lykkebusiness/mt/compare/v2.21.7...v2.23.0
