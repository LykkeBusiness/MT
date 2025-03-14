// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Extensions.DependencyInjection;
using MarginTrading.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using MarginTrading.Backend.Services.Services;

namespace MarginTrading.Backend.Modules
{
    public class MarginTradingCommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var services = new ServiceCollection();
            builder.Populate(services);
            builder.RegisterType<ConvertService>().As<IConvertService>().SingleInstance();
        }
    }
}