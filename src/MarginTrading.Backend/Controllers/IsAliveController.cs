﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Responses;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly MarginTradingSettings _settings;
        private readonly IDateService _dateService;

        public IsAliveController(
            MarginTradingSettings settings,
            IDateService dateService)
        {
            _settings = settings;
            _dateService = dateService;
        }
        
        [HttpGet]
        public IsAliveResponse Get()
        {
            return new IsAliveResponse
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Env = _settings.Env,
                ServerTime = _dateService.Now()
            };
        }
    }
}
