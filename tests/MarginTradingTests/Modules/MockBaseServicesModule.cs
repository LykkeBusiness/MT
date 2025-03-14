﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Common;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Session.AutorestClient;
using Lykke.Service.Session.AutorestClient.Models;
using Microsoft.Rest;
using Moq;
using Lykke.Service.Session;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Workflow;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace MarginTradingTests.Modules
{
    public class MockBaseServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherMock>().As<IThreadSwitcher>().SingleInstance();

            var emailService = new Mock<IEmailService>();
            var consoleWriterMock = new Mock<IConsole>();
            var sessionServiceMock = new Mock<ISessionService>();

            sessionServiceMock
                .Setup(item => item.ApiSessionGetPostWithHttpMessagesAsync(It.IsAny<ClientSessionGetRequest>(), null,
                    It.IsAny<CancellationToken>())).Returns(() => Task.FromResult(
                    new HttpOperationResponse<ClientSessionGetResponse>
                    {
                        Body = new ClientSessionGetResponse { Session = new ClientSessionModel { ClientId = "1" } }
                    }));

            var clientsRepositoryMock = new Mock<IClientsSessionsRepository>();
            clientsRepositoryMock.Setup(item => item.GetAsync(It.IsAny<string>())).Returns(() =>
                Task.FromResult((IClientSession)new ClientSession { ClientId = "1" }));

            var volumeEquivalentService = new Mock<IEquivalentPricesService>();

            var clientAccountMock = new Mock<IClientAccountService>();
            clientAccountMock.Setup(s => s.GetEmail(It.IsAny<string>())).ReturnsAsync("email@email.com");
            clientAccountMock.Setup(s => s.GetMarginEnabledAsync(It.IsAny<string>())).ReturnsAsync(
                new MarginEnabledSettingsModel() { Enabled = true, EnabledLive = true, TermsOfUseAgreed = true });
            clientAccountMock.Setup(s => s.GetNotificationId(It.IsAny<string>())).ReturnsAsync("notificationId");
            clientAccountMock.Setup(s => s.IsPushEnabled(It.IsAny<string>())).ReturnsAsync(true);

            builder.RegisterInstance(emailService.Object).As<IEmailService>();
            builder.RegisterType<PositionHistoryNotifications>().As<IRabbitMqNotifyService>();
            builder.RegisterInstance(consoleWriterMock.Object).As<IConsole>();
            builder.RegisterInstance(clientsRepositoryMock.Object).As<IClientsSessionsRepository>();
            builder.RegisterInstance(sessionServiceMock.Object).As<ISessionService>();
            builder.RegisterInstance(volumeEquivalentService.Object).As<IEquivalentPricesService>();
            builder.RegisterInstance(clientAccountMock.Object).As<IClientAccountService>();

            var dateServiceMock = new Mock<IDateService>();
            dateServiceMock.Setup(s => s.Now()).Returns(() => DateTime.UtcNow);
            builder.RegisterInstance(dateServiceMock.Object).As<IDateService>().SingleInstance();
            builder.RegisterInstance(new Mock<ICqrsEngine>(MockBehavior.Loose).Object).As<ICqrsEngine>()
                .SingleInstance();
            builder.RegisterInstance(new CqrsContextNamesSettings()).AsSelf().SingleInstance();
            builder.RegisterType<AccountsProjection>().AsSelf().SingleInstance();
            builder.RegisterType<CqrsSender>().As<ICqrsSender>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterInstance(Mock.Of<IChaosKitty>()).As<IChaosKitty>().SingleInstance();

            // register null logger for every ILogger<T>
            builder.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>)).SingleInstance();
        }
    }

    public class ThreadSwitcherMock : IThreadSwitcher
    {
        public void SwitchThread(Func<Task> taskProc)
        {
            taskProc().Wait();
        }
    }
}