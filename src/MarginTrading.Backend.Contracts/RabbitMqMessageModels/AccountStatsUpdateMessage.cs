﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.RabbitMqMessageModels
{
    public class AccountStatsUpdateMessage
    {
        public AccountStatsContract[] Accounts { get; set; }
    }
}