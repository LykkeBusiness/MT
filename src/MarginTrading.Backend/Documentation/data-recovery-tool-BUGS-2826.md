## Data recovery tool (BUGS-2826l)
### How to use data recovery too

The tool purpose is to recover trading core positions state. In particular, the `ChargedPnl` value of each position will be restored.

There are 3 endpoint to manage the restore process:

* `POST /api/bugs2826-restore` - start the restore process. The `day` parameter is not required and defaults to November 7, 2022. All `UnrealizedDailyPnL` records from `AccountHistory` table will be fetched for that day and their `ChangeAmount` value will be applied to the current state of the trading core. If position not found in trading core cache it probably has already been closed and such positions will have to be restored manually. The restore process runs in DEMO mode by default. To run it in production mode pass the parameter `demoMode=false` explicitly.
* `GET /api/restore` - get the restore process status. It is readonly operation and returns the status for long-running restore operation. When the status is `Finished` consider the restore process to be successfully completed. If the status is `Failed` then there most probably happened an error during the restore process. In this case, the `Error` will be logged, we'll have to understand why it happened, remove the obstacles and then run the restore process again. It is safe, the restore process is idempotent. The status response also contains the list of successfully restored positions (`FoundPositions`) and the list of positions that failed to find (`NotFoundPositions`). For not found positions there is an additional property `CurlCommands` which is an array of curl commands to be executed to compensate money. This information is quite important and should be used to deal with corresponding accounts manually later (execute curl requests). Save the response for further processing.
* `DELETE /api/restore` - removes the status information and metadata about the restore process. Once it is removed the restore process can be started again, please be careful with that. This endpoint is not supposed to be used in production. It is provided just for testing purposes and for cleaning up the database after the restore process is finished successfully.

### How to use curl commands to compensate money
Here is an example of curl command to compensate money:
```bash
curl -X POST 'http://am-host-url-to-provide/api/accounts/AE02/balance/withdraw' -H 'Content-Type: application/json-patch+json' -H 'Accept: text/plain' -d '{\"OperationId\":\"f36e6a87-f6fe-489e-a12c-1443d3f7e262\",\"AmountDelta\":2.240000000000,\"Reason\":\"BUGS-2826 compensation upon position [TVQITFY0IF]\",\"AdditionalInfo\":\"\",\"AssetPairId\":\"EUR\",\"TradingDay\":\"2022-11-10T11:40:54.6666769Z\"}'
```

Please note, before running it you have to replace the string `am-host-url-to-provide` with whatever account management host address in your environment is.