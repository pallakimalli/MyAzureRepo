using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ValmontApp.Common;
using ValmontApp.Data.Models;
using static ValmontApp.Common.Constants;
using Azure;


namespace ValmontApp.FunctionApps
{
    public static class UpdateUserProfile
    {
        public static readonly string connectionString = Environment.GetEnvironmentVariable("Storage");
        public static readonly string dataImageContainer = Environment.GetEnvironmentVariable("DataImageContainer");
        public static readonly string profileBaseURL = Environment.GetEnvironmentVariable("profileBaseURL");
        /// <summary>
        /// This method will update User details to Azure Storage Table based on the Identifier.
        /// returns JSON format of UserDetails Entity.
        /// </summary>
        /// <param name="request"> JSON Format with UserEntity attributes</param>
        /// <param name="HEADER"> EMAILID</param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("UpdateUserProfile")]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)] HttpRequest request,
            ILogger log)
        {
            log.LogInformation("UpdateUserProfile Function invoked.");

            try
            {
                string emailId = request.Headers["EMAILID"];
                var _identifier = emailId.Split('@')[0];

                var container = IoCContainer.Create();
                var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();

                string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<UsersEntity>(requestBody);

                var retrievedResult = await azureTableRepository.ReadAsync<UsersEntity>(AzureTableName, "user", _identifier);

                if (retrievedResult != null)
                {
                    log.LogInformation($"User Details fetched successfully from Azure Table");
                }

                ImageModelContent imageContent = new ImageModelContent();
                var imageByteArray = JsonConvert.DeserializeObject<ImageModelContent>(requestBody);
                var profileImgURL = string.Empty;
                if (imageByteArray.ImageConetent != null)
                {
                    profileImgURL = await UploadImageAsync(new FileContentResult(imageByteArray.ImageConetent, "image/jpeg"), dataImageContainer, _identifier.ToLower());
                    log.LogInformation($"Profile Image uploaded successfully.");
                }
                else
                {
                    log.LogInformation($"Profile Image uploading failed.");
                }

                ///////////////////////
                // Assign the result to a UsersEntity object.
                UsersEntity updateEntity = (UsersEntity)retrievedResult;

                if (updateEntity != null)
                {
                    //Change the description
                    updateEntity.Address1 = data.Address1;
                    updateEntity.Address2 = data.Address2;
                    updateEntity.BusinessSegment = data.BusinessSegment;
                    updateEntity.City = data.City;
                    updateEntity.Country = data.Country;
                    updateEntity.Mail = data.Mail;
                    updateEntity.MobilePhone = data.MobilePhone;
                    updateEntity.BusinessPhones = data.BusinessPhones;
                    updateEntity.State = data.State;
                    updateEntity.JobTitle = data.JobTitle;
                    updateEntity.Zip = data.Zip;
                    updateEntity.GivenName = data.GivenName;     //FirstName
                    updateEntity.Surname = data.Surname;         //LastName
                    updateEntity.ETag = "*";
                    updateEntity.DisplayName = data.GivenName + " " + data.Surname;
                    if (!string.IsNullOrEmpty(profileImgURL))
                        updateEntity.ProfilePic = profileImgURL;

                    var _valuesOfficePhone = String.Join(",", data.BusinessPhones);
                    updateEntity.OfficePhone = _valuesOfficePhone;

                    //// Create the InsertOrMerge TableOperation
                    var result = await azureTableRepository.UpdateMergeAsync(AzureTableName, updateEntity);

                    ///////////////returning the updated record///////////////////////
                    TableQuery<UsersEntity> userProfileQuery = new TableQuery<UsersEntity>().Where(
                    TableQuery.GenerateFilterCondition("Id", QueryComparisons.Equal, _identifier));

                    var rawUserProfile = await azureTableRepository.QueryAsync(AzureTableName, userProfileQuery);

                    if (rawUserProfile != null)
                    {
                        log.LogInformation($"User Profile details updated successfully to Azure Table");
                    }

                    // Create the Entity and set the partition & rowkey to signup, 
                    UsersEntity _userResponse = new UsersEntity
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
                    _userResponse.BusinessPhones = rawUserProfile.ToArray()[0].OfficePhone.Split(',');

                    return new OkObjectResult(_userResponse);
                }
            }
            catch (Exception exp)
            {
                log.LogInformation($"Error while updating the user's profile details: " + $"{exp.Message}");
                return new OkObjectResult(exp.Message);
            }
            return new OkObjectResult("No Record Updated.");
        }

        private static async Task<string> UploadImageAsync(FileContentResult qrImageToUplaod, string containerName, string fileName)
        {
            try
            {
                if (qrImageToUplaod.FileContents.Length > 0)
                {
                    var serviceClient = new BlobServiceClient(connectionString);
                    var containerClient = serviceClient.GetBlobContainerClient(containerName);

                    BlobClient blob = containerClient.GetBlobClient(fileName);
                    var blobHttpHeader = new BlobHttpHeaders();
                    blobHttpHeader.ContentType = qrImageToUplaod.ContentType;
                    await blob.UploadAsync(new MemoryStream(qrImageToUplaod.FileContents), blobHttpHeader);

                    return blob.Uri.AbsoluteUri;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception exp)
            {
                throw new Exception(exp.Message);
            }
        }
    }
}
