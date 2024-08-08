// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MarginTrading.Backend.Filters
{
    internal static class EnvironmentConstants
    {
        public const string DeploymentEnvironment = "Deployment";
        public const string TestEnvironment = "Test";
    }
    
    internal sealed class DevelopmentEnvironmentFilter : EnvironmentFilterBase
    {
        public DevelopmentEnvironmentFilter(IWebHostEnvironment environment)
            : base(
                environment,
                Environments.Development,
                EnvironmentConstants.DeploymentEnvironment,
                EnvironmentConstants.TestEnvironment)
        {
        }
    }
}