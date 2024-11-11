using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using MessagePack;

using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Events.ExternalContracts;

[MessagePackObject]
public class ExternalExchangeOrderbookMessage
{
    [JsonProperty("source"), Key(0), CanBeNull]
    public string ExchangeName { get; set; }

    [JsonProperty("asset"), Key(1), CanBeNull]
    public string AssetPairId { get; set; }

    [JsonProperty("timestamp"), Key(2)]
    public DateTime Timestamp { get; set; }

    [JsonProperty("asks"), Key(3), ItemCanBeNull]
    public List<VolumePrice> Asks { get; set; }

    [JsonProperty("bids"), Key(4), ItemCanBeNull]
    public List<VolumePrice> Bids { get; set; }
}
