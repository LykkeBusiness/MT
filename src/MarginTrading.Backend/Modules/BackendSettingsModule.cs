﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using Lykke.SettingsReader;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Settings;

namespace MarginTrading.Backend.Modules
{
    public class BackendSettingsModule : Module
    {
        private readonly IReloadingManager<MtBackendSettings> _settings;

        public BackendSettingsModule(IReloadingManager<MtBackendSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.Nested(s => s.MtBackend)).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MtBackend).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MtStpExchangeConnectorClient).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MtBackend.RequestLoggerSettings).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MtBackend.SpecialLiquidation).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.RiskInformingSettings ??
                                     new RiskInformingSettings {Data = new RiskInformingParams[0]}).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MtBackend.OvernightMargin).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MtBackend.SnapshotMonitorSettings).SingleInstance();
        }
    }
}
