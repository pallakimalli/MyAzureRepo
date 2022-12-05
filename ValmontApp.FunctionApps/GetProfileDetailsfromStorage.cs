using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;
using System.Threading.Tasks;
using ValmontApp.Common;
using ValmontApp.Data.Models;
using static ValmontApp.Common.Constants;

namespace ValmontApp.FunctionApps
{
    public static class GetProfileDetailsfromStorage
    {
        /// <summary>
        /// This method will fetch User details from Azure Storage Table based on the Identifier.
        /// returns JSON format of UserDetails Entity.
        /// </summary>
        /// <param name="Request HEADER"> EMAILID</param>
        /// <param name="Response"> User Entity details in JSON Format</param>
        /// <returns></returns>
        [FunctionName("GetProfileDetailsfromStorage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetProfileDetailsfromStorage function invoked.");

            string emailId = req.Headers["EMAILID"];
            var _identifier = emailId.Split('@')[0];

            var container = IoCContainer.Create();
            var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();

            try
            {
                TableQuery<UsersEntity> userDetailQuery = new TableQuery<UsersEntity>().Where(
                    TableQuery.GenerateFilterCondition("Id", QueryComparisons.Equal, _identifier));

                var rawUserProfile = await azureTableRepository.QueryAsync(AzureTableName, userDetailQuery);

                if (rawUserProfile != null)
                {
                    log.LogInformation($"Profile details fetched successfully to Azure Table");
                }

                // Create the Entity and set the partition & rowkey to signup, 
                UsersEntity _user = new UsersEntity
                {
                    PartitionKey = rawUserProfile.ToArray()[0].PartitionKey,
                    RowKey = rawUserProfile.ToArray()[0].RowKey,
                    Timestamp = rawUserProfile.ToArray()[0].Timestamp,
                    Address1 = rawUserProfile.ToArray()[0].Address1,
                    Address2 = rawUserProfile.ToArray()[0].Address2,
                    BusinessSegment = rawUserProfile.ToArray()[0].BusinessSegment,
                    City = rawUserProfile.ToArray()[0].City,
                    Country = rawUserProfile.ToArray()[0].Country,
                    Mail = rawUserProfile.ToArray()[0].Mail,
                    Id = rawUserProfile.ToArray()[0].Id,
                    MobilePhone = rawUserProfile.ToArray()[0].MobilePhone,
                    OfficePhone = rawUserProfile.ToArray()[0].OfficePhone,
                    ProfilePic = rawUserProfile.ToArray()[0].ProfilePic,
                    QRCode = rawUserProfile.ToArray()[0].QRCode,
                    State = rawUserProfile.ToArray()[0].State,
                    JobTitle = rawUserProfile.ToArray()[0].JobTitle,
                    Website = rawUserProfile.ToArray()[0].Website,
                    Zip = rawUserProfile.ToArray()[0].Zip,
                    GivenName = rawUserProfile.ToArray()[0].GivenName,
                    DisplayName = rawUserProfile.ToArray()[0].DisplayName,
                    Surname = rawUserProfile.ToArray()[0].Surname,
                    ProfileURL = rawUserProfile.ToArray()[0].ProfileURL,
                };
                _user.BusinessPhones = rawUserProfile.ToArray()[0].OfficePhone.Split(',');

                return new OkObjectResult(_user);
            }
            catch(Exception exp)
            {
                log.LogInformation($"Error while fetching User Profile details. " + $"{exp.Message}");
                return new OkObjectResult("No Records Found" + exp.Message);
            }
        }
    }
}
