// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Text;
using MarginTrading.Backend.Services.Events;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MarginTrading.Backend.RestoreTool.Controllers;

[ApiController]
[Route("service")]
public class ServiceController : ControllerBase
{
    private readonly Handler _handler = new Handler();
    
    [HttpPost("position-history-event")]
    public ActionResult GenerateMissedHistoryEvent([FromBody] OrderExecutedEventArgs sourceEvent)
    {
        var historyEvent = _handler.ConsumeEvent(sourceEvent);
        
        if (historyEvent == null)
            return Problem("History event is not generated");

        var content = JsonConvert.SerializeObject(historyEvent, Formatting.Indented);

        return File(Encoding.UTF8.GetBytes(content), "application/json", $"{historyEvent.PositionSnapshot.Id}.json");
    }

    [HttpPost("position-history-event-1IM3I1I2X4")]
    public ActionResult GenerateMissedHistoryEventCustom()
    {
        using var s = new StreamReader("1IM3I1I2X4.json");
        var json = s.ReadToEnd();
        var sourceEvent = JsonConvert.DeserializeObject<OrderExecutedEventArgs>(json);
        
        var historyEvent = _handler.ConsumeEvent(sourceEvent);
        
        if (historyEvent == null)
            return Problem("History event is not generated");

        var content = JsonConvert.SerializeObject(historyEvent, Formatting.Indented);

        return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/json")
        {
            FileDownloadName = $"{historyEvent.PositionSnapshot.Id}.json"
        };
    }
}