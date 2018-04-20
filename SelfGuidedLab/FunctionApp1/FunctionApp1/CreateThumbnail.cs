using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace FunctionApp1
{
    public static class CreateThumbnail
    {
        // **********************************************
        // *** Update or verify the following values. ***
        // **********************************************

        // Replace the subscriptionKey string value with your valid subscription key.
        const string subscriptionKey = "";

        // Replace or verify the region.
        //
        // You must use the same region in your REST API call as you used to obtain your subscription keys.
        // For example, if you obtained your subscription keys from the westus region, replace 
        // "westcentralus" in the URI below with "westus".
        //
        // NOTE: Free trial subscription keys are generated in the westcentralus region, so if you are using
        // a free trial subscription key, you should not need to change this region.
        const string uriBase = "https://eastus.api.cognitive.microsoft.com/vision/v1.0/generateThumbnail";

        [FunctionName("CreateThumbnail")]
        public static void Run([BlobTrigger("original/{name}", Connection = "AzureWebJobsStorage")]Stream original, string name, TraceWriter log,
            [Blob("output/{name}", FileAccess.Write, Connection = "AzureWebJobsStorage")] Stream thumbnail,
            [DocumentDB(
            "photoDB",
            "photoCollection",
            CreateIfNotExists = true,
            CollectionThroughput = 400,
            Id = "Id",
            ConnectionStringSetting = "photos")] ICollector<dynamic> outputDocument)
        {
            //
            //
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {original.Length} Bytes");
            var result = MakeThumbNailRequest(original);
            result.Wait();
            thumbnail.Write(result.Result, 0, result.Result.Length);
            outputDocument.Add((dynamic)new { name = name, originalsize = original.Length, newsize = result.Result.Length });
        }

        /// <summary>
        /// Gets a thumbnail image from the specified image file by using the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file to use to create the thumbnail image.</param>
        static async Task<byte[]> MakeThumbNailRequest(Stream stream)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters.
            string requestParameters = "width=200&height=150&smartCropping=true";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                using (ByteArrayContent content = new ByteArrayContent(ms.ToArray()))
                {
                    // This example uses content type "application/octet-stream".
                    // The other content types you can use are "application/json" and "multipart/form-data".
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    // Execute the REST API call.
                    response = await client.PostAsync(uri, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Display the response data.

                        // Get the image data.
                        return await response.Content.ReadAsByteArrayAsync();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

    }
}
