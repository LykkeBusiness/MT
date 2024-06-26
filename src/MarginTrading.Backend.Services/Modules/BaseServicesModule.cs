﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using Common.Log;
using Lykke.Common;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Modules
{
    public class BaseServicesModule : Module
    {
        private readonly MtBackendSettings _mtSettings;
        private readonly ILog _log;

        public BaseServicesModule(MtBackendSettings mtSettings, ILog log)
        {
            _mtSettings = mtSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherToNewTask>()
                .As<IThreadSwitcher>()
                .SingleInstance();

            builder.RegisterType<RabbitMqProducerContainer>()
                .As<IRabbitMqProducerContainer>()
                .SingleInstance();

            builder.RegisterType<RabbitMqNotifyService>()
                .As<IRabbitMqNotifyService>()
                .SingleInstance();

            builder.RegisterType<DateService>()
                .As<IDateService>()
                .SingleInstance();
        }
    }
}
