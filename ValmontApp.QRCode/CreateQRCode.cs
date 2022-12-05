using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QRCoder;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ValmontApp.QRCodeGeneration
{
    public static class CreateQRCode
    {
        public static readonly string connectionString = Environment.GetEnvironmentVariable("Storage");
        public static readonly string blobPath = Environment.GetEnvironmentVariable("blobPath");

        /// <summary>
        /// This method will generate QRCode Image based on url given as input.
        /// Returned as Image File Content.
        /// </summary>
        /// <param name="req">name</param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GenerateQRCode")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("GenerateQRCode function invoked.");

                string inputString = req.Query["name"];
                if (string.IsNullOrEmpty(inputString))
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);
                    inputString = inputString ?? data?.inputString;
                }

                if (string.IsNullOrEmpty(inputString))
                {
                    log.LogInformation("Input string is missing.");
                    return new BadRequestObjectResult("Name querystring is not available.");
                }

                var payload = inputString.ToString();
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                    QRCode qR = new QRCode(qrCodeData);

                    //Set logo in center of QR-code
                    Bitmap qrCodeImage = qR.GetGraphic(10, Color.Black, Color.White, (Bitmap)Bitmap.FromStream(GetImageAsByteArray(blobPath)), 35, 6);
                    byte[] dataResult = BitmapToBytes(qrCodeImage);
                    ImageModelContent imageModelContent = new ImageModelContent
                    {
                        ImageConetent = dataResult
                    };
                    return new OkObjectResult(imageModelContent);
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error while generating QR Code from input String: " + $"{ex.Message}");
                return new BadRequestObjectResult("Error : " + ex.Message);
            }
        }

        /// <summary>
        /// returns byte array of the given bitmap image
        /// </summary>
        /// <param name="img">image for converting bytes</param>
        /// <returns>array of bytes</returns>
        private static Byte[] BitmapToBytes(Bitmap img)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// returns stream of the given image
        /// </summary>
        /// <param name="imageFilePath">image path</param>
        /// <returns>memory stream</returns>
        private static MemoryStream GetImageAsByteArray(string imageFilePath)
        {
            byte[] bytes;
            using (WebClient client = new WebClient())
            {
                bytes = client.DownloadData(imageFilePath);

            }
            var stream = new MemoryStream(bytes);
            return stream;
        }


        /// <summary>
        /// Uploads the file content to Storage Container
        /// </summary>
        /// <param name="qrImageToUplaod">file content to Uplaod</param>
        /// <param name="containerName">Container name to which blob has to be uploaded</param>
        /// <param name="fileName">blob name to save</param>
        /// <returns>void</returns>
        private static async Task UploadDataAsync(Byte[] qrToUplaod, string containerName, string fileName)
        {
            try
            {
                var serviceClient = new BlobServiceClient(connectionString);
                var containerClient = serviceClient.GetBlobContainerClient(containerName);

                BlobClient blob = containerClient.GetBlobClient(fileName);
                BinaryData uploadData = new BinaryData(qrToUplaod);
                await blob.UploadAsync(uploadData, overwrite: true);
            }
            catch (Exception ex)
            {
                //nothing to do
            }
        }

        /// <summary>
        /// Uploads the Images to Storage Container
        /// </summary>
        /// <param name="qrImageToUplaod">Image to Uplaod</param>
        /// <param name="containerName">Container name to which blob has to be uploaded</param>
        /// <param name="fileName">blob name to save</param>
        /// <returns>Returns uploaded blob url</returns>
        private static async Task<string> UploadImageAsync(FileContentResult qrImageToUplaod, string containerName, string fileName)
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

    }

    public class ImageModelContent
    {
        public byte[] ImageConetent;
    }
}
