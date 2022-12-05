using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;
using ValmontApp.Common;
using ValmontApp.Data.Models;
using static ValmontApp.Common.Constants;

namespace ValmontApp.FunctionApps
{
    public static class GetLocations
    {
        /// <summary>
        /// This method will fetch Country State City details from Azure Storage Table based on the Identifier.
        /// returns JSON format of CountryStateCity Entity.
        /// </summary>
        /// <param name="Response"> CountryStateCity Entity details in JSON Format</param>
        /// <returns></returns>
        [FunctionName("GetLocations")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("GetLocations function invoked.");

            var container = IoCContainer.Create();
            var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();

            try
            {
                var rawUserLocation = await azureTableRepository.ReadAllAsync<Location>(AzureLocationTable);
                if (rawUserLocation != null)
                {
                    log.LogInformation($"Country, State and City details fetched successfully from Azure Table");
                }
                return new OkObjectResult(rawUserLocation);
            }
            catch (Exception exp)
            {
                log.LogInformation($"Error while fetching Country, State and City details. " + $"{exp.Message}");
                return new OkObjectResult("{No Records Found }");
            }
        }



        /// <summary>
        /// This method will fetch Country State City details from Azure Storage Table based on the Identifier.
        /// returns JSON format of CountryStateCity Entity.
        /// </summary>
        /// <param name="Response"> CountryStateCity Entity details in JSON Format</param>
        /// <returns></returns>
        [FunctionName("GetLocationByCountry")]
        public static async Task<IActionResult> GetAllCountryStateCityByCountry(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetLocationByCountry function invoked.");

            string _request = req.Headers["COUNTRY"];
            var _country = _request.Split('@')[0];

            var container = IoCContainer.Create();
            var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();

            try
            {
                TableQuery<Location> userLocationQuery = new TableQuery<Location>().Where(
                    TableQuery.GenerateFilterCondition("Country", QueryComparisons.Equal, _country));

                var rawUserLocation = await azureTableRepository.QueryAsync(AzureLocationTable, userLocationQuery);

                if (rawUserLocation != null)
                {
                    log.LogInformation($"Country, State and City details by Country Filter  fetched successfully from Azure Table");
                }

                return new OkObjectResult(rawUserLocation);
            }
            catch (Exception exp)
            {
                log.LogInformation($"Error while fetching Country, State and City details by Country Filter . " + $"{exp.Message}");
                return new OkObjectResult("{No Records Found }");
            }
        }

        
         
        /// <summary>
        /// This method will fetch Country State City details from Azure Storage Table based on the Identifier.
        /// returns JSON format of CountryStateCity Entity.
        /// </summary>
        /// <param name="Response"> CountryStateCity Entity details in JSON Format</param>
        /// <returns></returns>
        [FunctionName("GetLocationByState")]
        public static async Task<IActionResult> GetAllCountryStateCityByState(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetLocationByState function invoked.");

            string _request = req.Headers["STATE"];
            var _state = _request.Split('@')[0];

            var container = IoCContainer.Create();
            var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();

            try
            {
                TableQuery<Location> userLocationQuery = new TableQuery<Location>().Where(
                    TableQuery.GenerateFilterCondition("State", QueryComparisons.Equal, _state));

                var rawUserLocation = await azureTableRepository.QueryAsync(AzureLocationTable, userLocationQuery);

                if (rawUserLocation != null)
                {
                    log.LogInformation($"Country, State and City details by State Filter  fetched successfully from Azure Table");
                }
                return new OkObjectResult(rawUserLocation);
            }
            catch (Exception exp)
            {
                log.LogInformation($"Error while fetching Country, State and City details by State Filter . " + $"{exp.Message}");
                return new OkObjectResult("{No Records Found }");
            }
        }



    }
}
