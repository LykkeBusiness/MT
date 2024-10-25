using MessagePack;

using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Events.ExternalContracts;

[MessagePackObject]
public class VolumePrice
{
    [JsonProperty("volume"), Key(0)]
    public decimal Volume { get; set; }

    [JsonProperty("price"), Key(1)]
    public decimal Price { get; set; }
}
