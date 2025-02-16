﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Caches;
using Rocks.Caching;

namespace MarginTrading.Backend.Services.Modules
{
    public class CacheModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AssetPairsCache>()
                .As<IAssetPairsCache>()
                .As<IAssetPairsInitializableCache>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<RunningLiquidationRepository>()
                .As<IRunningLiquidationRepository>()
                .SingleInstance();

            builder.RegisterDecorator<RunningLiquidationLogger, IRunningLiquidationRepository>();
            
            builder.RegisterType<AccountsCacheService>()
                .AsSelf()
                .As<IAccountsCacheService>()
                .SingleInstance();

            builder.RegisterType<OrdersCache>()
                .As<IOrderReader>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<SentimentCache>()
                .As<ISentimentCache>()
                .SingleInstance();

            builder.RegisterType<MemoryCacheProvider>()
                   .As<ICacheProvider>()
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<ClientProfileSettingsCache>()
                .As<IClientProfileSettingsCache>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
