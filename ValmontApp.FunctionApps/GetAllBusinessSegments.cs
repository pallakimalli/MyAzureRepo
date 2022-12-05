using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ValmontApp.Common;
using ValmontApp.Data.Models;
using static ValmontApp.Common.Constants;

namespace ValmontApp.FunctionApps
{
    public static class GetAllBusinessSegments
    {
        /// <summary>
        /// This method will fetch Business Segments details from Azure Storage Table based on the Identifier.
        /// returns JSON format of BusinessSegments Entity.
        /// </summary>
        /// <param name="Response"> BusinessSegments Entity details in JSON Format</param>
        /// <returns></returns>
        [FunctionName("GetAllBusinessSegments")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("GetAllBusinessSegments function invoked.");

            var container = IoCContainer.Create();
            var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();
            try
            {
                var rawBusinessSegment = await azureTableRepository.ReadAllAsync<BusinessSegment>(AzureBusinessSegmentTable);
                if (rawBusinessSegment != null)
                {
                    log.LogInformation($"Read All BusinessSegments successfully");
                }
                return new OkObjectResult(rawBusinessSegment);
            }
            catch (Exception exp)
            {
                log.LogInformation($"Error while fetching All BusinessSegments: " + $"{exp.Message}");
                return new OkObjectResult("{No Records Found }");
            }
        }
    }
}
