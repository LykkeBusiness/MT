// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace MarginTrading.Backend.Filters
{
    internal abstract class EnvironmentFilterBase : IAsyncActionFilter
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _environmentNames;

        protected EnvironmentFilterBase(IWebHostEnvironment environment, params string[] environmentNames)
        {
            _environment = environment;
            _environmentNames = environmentNames;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var requirementSatisfied = _environmentNames.Any(
                name => _environment.IsEnvironment(name));
            if (!requirementSatisfied)
            {
                context.Result = new NotFoundResult();
                return;
            }

            await next.Invoke();
        }
    }
}