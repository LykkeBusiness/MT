﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Data;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Dapper;
using Lykke.SettingsReader;
using Lykke.Snow.Common;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services;
using MarginTrading.SqlRepositories.Repositories;
using MarginTrading.SqlRepositories;
using IdentityEntity = MarginTrading.AzureRepositories.Entities.IdentityEntity;
using OperationLogEntity = MarginTrading.AzureRepositories.OperationLogEntity;

namespace MarginTrading.Backend.Modules
{
    public class BackendRepositoriesModule : Module
    {
        private readonly IReloadingManager<MarginTradingSettings> _settings;
        private readonly ILog _log;
        private const string OperationsLogName = "MarginTradingBackendOperationsLog";

        public BackendRepositoriesModule(IReloadingManager<MarginTradingSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            if (_settings.CurrentValue.Db.StorageMode == StorageMode.Azure)
            {
                builder.Register(ctx => _settings.CurrentValue.UseSerilog
                        ? (IOperationsLogRepository) new SerilogOperationsLogRepository(_log)
                        : new OperationsLogRepository(AzureTableStorage<OperationLogEntity>.Create(
                            _settings.Nested(s => s.Db.LogsConnString), OperationsLogName, _log)))
                    .SingleInstance();

                builder.Register<IMarginTradingBlobRepository>(ctx =>
                        AzureRepoFactories.MarginTrading.CreateBlobRepository(
                            _settings.Nested(s => s.Db.StateConnString)))
                    .SingleInstance();

                builder.Register(c =>
                {
                    var settings = c.Resolve<IReloadingManager<MarginTradingSettings>>();

                    return settings.CurrentValue.UseDbIdentityGenerator
                        ? (IIdentityGenerator) new AzureIdentityGenerator(
                            AzureTableStorage<IdentityEntity>.Create(settings.Nested(s => s.Db.MarginTradingConnString),
                                "Identity", _log))
                        : (IIdentityGenerator) new SimpleIdentityGenerator();
                }).As<IIdentityGenerator>().SingleInstance();
                
                builder.RegisterType<AzureRepositories.OperationExecutionInfoRepository>()
                    .As<IOperationExecutionInfoRepository>()
                    .WithParameter(new NamedParameter("connectionStringManager",
                        _settings.Nested(x => x.Db.MarginTradingConnString)))
                    .SingleInstance();

                builder.RegisterDecorator<RfqExecutionInfoRepositoryDecorator, IOperationExecutionInfoRepository>();
                
                builder.RegisterType<AzureRepositories.OpenPositionsRepository>()
                    .As<IOpenPositionsRepository>()
                    .WithParameter(new NamedParameter("connectionStringManager",
                        _settings.Nested(x => x.Db.MarginTradingConnString)))
                    .SingleInstance();
                
                builder.RegisterType<AzureRepositories.AccountStatRepository>()
                    .As<IAccountStatRepository>()
                    .WithParameter(new NamedParameter("connectionStringManager",
                        _settings.Nested(x => x.Db.MarginTradingConnString)))
                    .SingleInstance();

                builder.RegisterType<AzureRepositories.TradingEngineSnapshotsRepository>()
                    .As<ITradingEngineSnapshotsRepository>()
                    .SingleInstance();
            }
            else if (_settings.CurrentValue.Db.StorageMode == StorageMode.SqlServer)
            {
                builder.Register(ctx => _settings.CurrentValue.UseSerilog
                        ? (IOperationsLogRepository) new SerilogOperationsLogRepository(_log)
                        : new SqlOperationsLogRepository(ctx.Resolve<IDateService>(),
                            OperationsLogName, _settings.CurrentValue.Db.LogsConnString))
                    .SingleInstance();

                builder.Register<IMarginTradingBlobRepository>(ctx =>
                        new SqlBlobRepository(_settings.CurrentValue.Db.StateConnString))
                    .SingleInstance();

                builder.Register(c => c.Resolve<IReloadingManager<MarginTradingSettings>>().CurrentValue
                        .UseDbIdentityGenerator
                        ? (IIdentityGenerator) new SqlIdentityGenerator()
                        : (IIdentityGenerator) new SimpleIdentityGenerator())
                    .As<IIdentityGenerator>()
                    .SingleInstance();
                
                builder.RegisterType<SqlRepositories.Repositories.OperationExecutionInfoRepository>()
                    .As<IOperationExecutionInfoRepository>()
                    .WithParameter(new NamedParameter("connectionString", 
                        _settings.CurrentValue.Db.SqlConnectionString))
                    .SingleInstance();

                builder.RegisterDecorator<RfqExecutionInfoRepositoryDecorator, IOperationExecutionInfoRepository>();

                builder.RegisterType<SqlRepositories.Repositories.OpenPositionsRepository>()
                    .As<IOpenPositionsRepository>()
                    .WithParameter(new NamedParameter("connectionString", 
                        _settings.CurrentValue.Db.SqlConnectionString))
                    .SingleInstance();

                builder.RegisterType<SqlRepositories.Repositories.AccountStatRepository>()
                    .As<IAccountStatRepository>()
                    .WithParameter(new NamedParameter("connectionString", 
                        _settings.CurrentValue.Db.SqlConnectionString))
                    .SingleInstance();

                builder.RegisterType<SqlRepositories.Repositories.OrdersHistoryRepository>()
                    .As<IOrdersHistoryRepository>()
                    .WithParameter(new NamedParameter("connectionString", 
                        _settings.CurrentValue.Db.OrdersHistorySqlConnectionString))
                    .WithParameter(new NamedParameter("tableName", 
                        _settings.CurrentValue.Db.OrdersHistoryTableName))
                    .WithParameter(new NamedParameter("getLastSnapshotTimeoutS",
                        _settings.CurrentValue.Db.QueryTimeouts.GetLastSnapshotTimeoutS))
                    .SingleInstance();

                builder.RegisterType<SqlRepositories.Repositories.PositionsHistoryRepository>()
                    .As<IPositionsHistoryRepository>()
                    .WithParameter(new NamedParameter("connectionString", 
                        _settings.CurrentValue.Db.PositionsHistorySqlConnectionString))
                    .WithParameter(new NamedParameter("tableName", 
                        _settings.CurrentValue.Db.PositionsHistoryTableName))
                    .WithParameter(new NamedParameter("getLastSnapshotTimeoutS",
                        _settings.CurrentValue.Db.QueryTimeouts.GetLastSnapshotTimeoutS))
                    .SingleInstance();

                builder.RegisterType<AccountHistoryRepository>()
                    .As<IAccountHistoryRepository>()
                    .WithParameter(new NamedParameter("connectionString", 
                        _settings.CurrentValue.Db.SqlConnectionString))
                    .SingleInstance();

                builder.RegisterType<SqlRepositories.Repositories.TradingEngineSnapshotsRepository>()
                    .As<ITradingEngineSnapshotsRepository>()
                    .SingleInstance();
                
                builder.RegisterType<SqlRepositories.Repositories.OperationExecutionPauseRepository>()
                    .As<IOperationExecutionPauseRepository>()
                    .WithParameter(new NamedParameter("connectionString", 
                        _settings.CurrentValue.Db.SqlConnectionString))
                    .SingleInstance();
                
                builder.RegisterDecorator<RfqExecutionPauseRepositoryDecorator, IOperationExecutionPauseRepository>();
            }
            
            builder.RegisterType<MatchingEngineInMemoryRepository>().As<IMatchingEngineRepository>().SingleInstance();

            builder.Register(c =>
            {
                var settings = c.Resolve<IReloadingManager<MarginTradingSettings>>();

                return settings.CurrentValue.UseDbIdentityGenerator
                    ? (IIdentityGenerator) new AzureIdentityGenerator(
                        AzureTableStorage<IdentityEntity>.Create(settings.Nested(s => s.Db.MarginTradingConnString),
                            "Identity", _log))
                    : (IIdentityGenerator) new SimpleIdentityGenerator();
            }).As<IIdentityGenerator>().SingleInstance();
            
            //SQL PLACE
            builder.RegisterType<AccountMarginFreezingRepository>()
                .As<IAccountMarginFreezingRepository>()
                .SingleInstance();
            
            builder.RegisterType<AccountMarginUnconfirmedRepository>()
                .As<IAccountMarginUnconfirmedRepository>()
                .SingleInstance();
            
            InitializeDapper();
        }

        private static void InitializeDapper()
        {
            SqlMapper.AddTypeMap(typeof(Initiator), DbType.String);
        }
    }
}