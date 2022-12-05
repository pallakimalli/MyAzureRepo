using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ValmontApp.Common;
using ValmontApp.Data.Models;
using static ValmontApp.Common.Constants;


namespace ValmontApp.FunctionApps
{
    public static class GetUserFromAD
    {
        public static readonly string connectionString = Environment.GetEnvironmentVariable("Storage");
        public static readonly string dataContainer = Environment.GetEnvironmentVariable("DataContainer");
        public static readonly string AppServiceClientId = Environment.GetEnvironmentVariable("AppServiceClientId");
        public static readonly string AppServiceTenantId = Environment.GetEnvironmentVariable("AppServiceTenantId");
        public static readonly string AppServiceClientSecret = Environment.GetEnvironmentVariable("AppServiceClientSecret");
        public static readonly string qrCodeFunctionAppURL = Environment.GetEnvironmentVariable("qrCodeFunctionAppURL");
        public static readonly string profileBaseURL = Environment.GetEnvironmentVariable("profileBaseURL");
        public static readonly string ClientId = Environment.GetEnvironmentVariable("ClientId");
        public static readonly string ClientSecret = Environment.GetEnvironmentVariable("ClientSecret");
        public static readonly string qrCodeScope = Environment.GetEnvironmentVariable("qrCodeScope");

        /// <summary>
        /// This method returns Logged In UserDetails.  EmailID will be validated to check User's First Login or not.
        /// If First Login , then User detais will be pulled from Azure AD
        /// If not First Login then User Details will be pulled from Azure Storage Table
        /// In First Login, QR Code is generated and uploaded into blob container (images) and Profile URL also generated.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GetUserFromAD")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetUserFromAD Function invoked.");

            var container = IoCContainer.Create();
            var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();

            try
            {
                string _emailId = req.Headers["EMAILID"];
                var _identifier = _emailId.Split('@')[0];

                ///////////////Start//////////ValidateUserExistsorNot///////////////////////////
                //// Retrieve the storage account from the connection string

                TableQuery<UsersEntity> userDetailQuery = new TableQuery<UsersEntity>().Where(
                    TableQuery.GenerateFilterCondition("Id", QueryComparisons.Equal, _identifier));

                var rawUserProfile = await azureTableRepository.QueryAsync(AzureTableName, userDetailQuery);

                ///////////////End///////////ValidateUserExistsorNot/////////////////////////


                if (Convert.ToInt32(rawUserProfile.Count()) > 0)
                {
                    // Create the Entity and set the partition & rowkey to signup, 
                    List<UsersEntity> _userList = new List<UsersEntity>();
                    log.LogInformation($"User Profile fetched from Azure Storage Table");
                    UsersEntity _existingUser = new UsersEntity
                    {
                        PartitionKey = rawUserProfile.ToArray()[0].PartitionKey,
                        RowKey = rawUserProfile.ToArray()[0].RowKey,
                        GivenName = rawUserProfile.ToArray()[0].GivenName,
                        DisplayName = rawUserProfile.ToArray()[0].DisplayName,
                        Surname = rawUserProfile.ToArray()[0].Surname,
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
                        ProfileURL = rawUserProfile.ToArray()[0].ProfileURL,
                    };

                    _existingUser.BusinessPhones = rawUserProfile.ToArray()[0].OfficePhone.Split(',');
                    _userList.Add(_existingUser);
                    return new OkObjectResult(_userList);
                }
                else
                {
                    var profileURL = profileBaseURL + _identifier.ToLower();

                    ////////////////QR Code Generation//////////////////
                    var blobURL = await UploadQRCode(_identifier.ToLower(), profileURL);

                    /////////////// Pulling Data From Azure AD //////////////////////////
                    var _azUser = await GetAzureUserfromAD(_emailId);
                    log.LogInformation($"User Profile details fetched from Azure Active Directory");

                    //// Create the Entity and set the partition & rowkey to signup,
                    List<UsersEntity> _userListAz = new List<UsersEntity>();
                    UsersEntity _user = new UsersEntity
                    {
                        PartitionKey = "user",
                        RowKey = _identifier,
                        GivenName = _azUser.GivenName,
                        DisplayName = _azUser.GivenName + " " + _azUser.Surname,
                        Surname = _azUser.Surname,
                        Timestamp = DateTime.UtcNow,
                        Address1 = _azUser.StreetAddress,
                        City = _azUser.City,
                        Country = _azUser.Country,
                        Mail = _azUser.Mail,
                        ProfileURL = profileURL,
                        QRCode = blobURL,
                        State = _azUser.State,
                        JobTitle = _azUser.JobTitle,
                        Zip = _azUser.PostalCode,
                        Id = _identifier,
                        MobilePhone = _azUser.MobilePhone,
                        BusinessPhones = _azUser.BusinessPhones.Select(it => it).ToArray(),
                    };
                    var _valuesOfficePhone = String.Join(",", _azUser.BusinessPhones);
                    _user.OfficePhone = _valuesOfficePhone;

                    _userListAz.Add(_user);

                    ////////////To Populate Mobile Phone with Business Phone if empty and viceversa.//////
                    if (string.IsNullOrEmpty(_azUser.MobilePhone) && _azUser.BusinessPhones.Count() > 0)
                    {
                        _user.MobilePhone = _user.BusinessPhones[0];
                    }
                    if (_azUser.BusinessPhones.Count() == 0 && !string.IsNullOrEmpty(_azUser.MobilePhone))
                    {
                        _user.BusinessPhones = new[] { _user.MobilePhone };
                        _user.OfficePhone = _user.MobilePhone;
                    }
                    ////////////////////////////////////////////////////////////////////////////////
                    var result = await azureTableRepository.AddAsync(AzureTableName, _user);

                    if (result != null)
                    {
                        log.LogInformation($"Record Added successfully to Azure Storage Table");
                    }
                    ////////////////////////////////////////////////////////////////////////////////

                    //To Identify User with First Login, QRCode is returned as Null.
                    _userListAz[0].QRCode = null;
                    return new OkObjectResult(_userListAz);

                }
            }
            catch (Exception exp)
            {
                log.LogInformation($"Error while retrieving the user's details: " + $"{exp.Message}");
            }
            return new OkObjectResult("Error while retrieving the user's details.");
        }


        private static async Task<string> ParamsToStringAsync(Dictionary<string, string> urlParams)
        {
            using (HttpContent content = new FormUrlEncodedContent(urlParams))
                return await content.ReadAsStringAsync();
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

        private static async Task<ImageModelContent> GetData(string purl, Uri endPoint)
        {
            var Url = endPoint;
            ImageModelContent imageContent = new ImageModelContent();
            Dictionary<string, object> dictData = new Dictionary<string, object>();
            dictData.Add("name", purl);

            string tokenQRCode = await GetAccessToken();

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, Url))
            using (var httpContent = CreateHttpContent(dictData))
            {
                request.Content = httpContent;
                request.Headers.Add("Authorization", "Bearer " + tokenQRCode);
                using (var response = client
                     .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                     )
                {
                    var resualtList = response.Result.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    string stringData = Encoding.UTF8.GetString(resualtList);
                    imageContent = JsonConvert.DeserializeObject<ImageModelContent>(stringData);
                }
            }
            return imageContent;
        }

        public static void SerializeJsonIntoStream(object value, Stream stream)
        {
            using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            using (var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var js = new JsonSerializer();
                js.Serialize(jtw, value);
                jtw.Flush();
            }
        }

        private static HttpContent CreateHttpContent(object content)
        {
            HttpContent httpContent = null;
            if (content != null)
            {
                var ms = new MemoryStream();
                SerializeJsonIntoStream(content, ms);
                ms.Seek(0, SeekOrigin.Begin);
                httpContent = new StreamContent(ms);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            return httpContent;
        }

        private static async Task<string> UploadQRCode(string _id, string profileURL)
        {
            Dictionary<string, string> query = new Dictionary<string, string>();
            query.Add("name", profileURL);
            string queryParam = await ParamsToStringAsync(query);

            Uri url = new Uri(qrCodeFunctionAppURL +"&"+ queryParam);
            var qrCodeContent = await GetData(profileURL, url);

            var blobURLImg = await UploadImageAsync(new FileContentResult(qrCodeContent.ImageConetent, "image/jpeg"), dataContainer, _id);

            return blobURLImg;
        }

        private static async Task<User> GetAzureUserfromAD(string _emailId)
        {
            var clientApplicationBuilder = ConfidentialClientApplicationBuilder.Create(AppServiceClientId)
                                                                                .WithTenantId(AppServiceTenantId)
                                                                                .WithClientSecret(AppServiceClientSecret)
                                                                                .Build();

            var _client = new GraphServiceClient(new ClientCredentialProvider(clientApplicationBuilder));

            var _azUser = await _client.Users
                               .Request()
                               .Select(aadUser => new
                               {
                                   aadUser.Id,
                                   aadUser.UserPrincipalName,
                                   aadUser.DisplayName,
                                   aadUser.GivenName,
                                   aadUser.Surname,
                                   aadUser.City,
                                   aadUser.MailNickname,
                                   aadUser.UserType,
                                   aadUser.StreetAddress,
                                   aadUser.BusinessPhones,
                                   aadUser.JobTitle,
                                   aadUser.Mail,
                                   aadUser.OfficeLocation,
                                   aadUser.Country,
                                   aadUser.MobilePhone,
                                   aadUser.PostalCode,
                                   aadUser.State,
                               })
                               .Filter($"mail eq '{_emailId}'")
                               .GetAsync()
                               .ConfigureAwait(false);

            return (User)_azUser.CurrentPage[0];
        }

        private static async Task<string> GetAccessToken()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://login.microsoftonline.com/8346fbf0-fbc5-44ab-b351-632105f6266f/oauth2/v2.0/token");

                    // We want the response to be JSON.
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Build up the data to POST.
                    List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                    postData.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                    postData.Add(new KeyValuePair<string, string>("client_id", ClientId));
                    postData.Add(new KeyValuePair<string, string>("client_secret", ClientSecret));
                    postData.Add(new KeyValuePair<string, string>("scope", qrCodeScope));
                    FormUrlEncodedContent content = new FormUrlEncodedContent(postData);

                    // Post to the Server and parse the response.
                    var response = client.PostAsync("Token", content).ConfigureAwait(false).GetAwaiter();
                    string jsonString = await response.GetResult().Content.ReadAsStringAsync();
                    string data = JObject.Parse(jsonString)["access_token"].ToString();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }


}
