// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Services;
using MarginTrading.Common.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/bug2826-restore")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class Bugs2628RestoreController : Controller
    {
        private readonly Bugs2826RestoreTool _tool;
        private static readonly DateTime HardCodedDay = new DateTime(2022, 11, 7);

        public Bugs2628RestoreController(Bugs2826RestoreTool tool)
        {
            _tool = tool;
        }

        [HttpPost]
        public async Task<IActionResult> Restore(DateTime? day, bool demoMode = true)
        {
            await _tool.Restore(day ?? HardCodedDay, demoMode);
            
            return Ok();
        }
        
        [HttpGet]
        public async Task<IActionResult> GetRestoreResult(DateTime? day)
        {
            var result = await _tool.FindRestoreResult(day ?? HardCodedDay);

            if (result == null)
                return NotFound();
            
            return Ok(result);
        }
        
        [HttpDelete]
        public async Task<IActionResult> RestoreCleanUp(DateTime? day)
        {
            var removed = await _tool.RestoreCleanup(day ?? HardCodedDay);
            
            return Ok(new {removed});
        }
    }
}