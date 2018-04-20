# Hands-on lab

**Goal**
1. Setup Azure Functions, Storage and CosmosDB for local development
2. Learn to integrate a web project with Azure Storage
3. Learn to create an Azure Function with Azure Storage trigger
4. Learn to use Azure Cognitive Services
5. Learn to store data in CosmosDB and use CosmosDB triggers in Azure Functions
6. Learn to deploy to Azure

**Pre-requisites**
1. Azure Account https://github.com/onetug/2018GlobalAzureBootcamp/blob/master/redeem.md
2. Visual Studio 2017 (Community edition ok) with Web and Azure workloads
3. Azure Functions SDK for Visual Studio https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio#prerequisites
4. Azure Storage Explorer https://azure.microsoft.com/en-us/features/storage-explorer/
5. Azure CosmosDB Emulator https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator
6. Azure Computer Vision API Keys https://azure.microsoft.com/en-us/try/cognitive-services/?api=computer-vision (click on Get API Key link)

**Init**
1. Make sure Azure Storage emulator is started on local machine
2. Make sure Azure CosmosDB emulator is started on local machine
3. Open Azure Storage Explorer and login with your Azure account

**Web Project**
1. Create an ASP.NET MVC Web Application and add nuget the following packages: *Microsoft.WindowsAzure.ConfigurationManager* and *WindowsAzure.Storage*

2. Go to HomeController and add an Upload Get action

        public ActionResult Upload(string fileName)
        {
            var original = GetContainer("original");
            var output = GetContainer("output");

            if (!string.IsNullOrEmpty(fileName))
            {
                ViewBag.Original = $"{original.Uri.AbsoluteUri}/{fileName}";
                ViewBag.Output = $"{output.Uri.AbsoluteUri}/{fileName}";
            }
            return View();
        }
  
3. Add an Upload Post action to catch Uploaded files
  
        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                string[] fileNameMinusPath = file.FileName.Split('\\');
                var original = GetContainer("original");
                CloudBlockBlob cloudBlockBlob = original.GetBlockBlobReference(fileNameMinusPath[fileNameMinusPath.Length - 1]);
                cloudBlockBlob.UploadFromStream(file.InputStream);
                return RedirectToAction("Upload", new { fileName = fileNameMinusPath[fileNameMinusPath.Length-1] });
            }
            return RedirectToAction("Upload");
        }

  
4. Add this method to initialize and handle the Azure Storage Containers
  
          private static CloudBlobContainer GetContainer(string name)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(name);
            container.CreateIfNotExists();
            var perms = new BlobContainerPermissions();
            perms.PublicAccess = BlobContainerPublicAccessType.Blob;
            container.SetPermissions(perms);
            return container;
        }

5. Add config setting for web project to use Azure Storage emulator locally. Note: when you deploy to live, replace this setting with the live keys

**to-do: add web.config key**

6. Add the form to handle the upload Upload.cshtml under Views->Home folder

**to-do: add cshtml code**

**Azure Function project**
1. Create New Azure Function project File->New->Project->Cloud->Azure Function. Allow any CLI installations that may pop up.

2. Add a Function with Blob Trigger and call is CreateThumbnail

3. Modify the Function method as shown

        const string subscriptionKey = "subscription key here";
        //uri for the vision api
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

4. Add the following methods inside the function static class

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

5. Add a Function with Blob Trigger and call is CosmosTrigger

6. Modify the Function method as shown

        [FunctionName("CosmosTrigger")]
        public static void Run([CosmosDBTrigger(
            databaseName: "photoDB",
            collectionName: "photoCollection",
            ConnectionStringSetting = "photos",
            CreateLeaseCollectionIfNotExists =true,
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, TraceWriter log)
        {
            if (input != null && input.Count > 0)
            {
                if ( (((dynamic)input[0]).originalsize/ ((dynamic)input[0]).newsize) > 20)
                {
                    log.Verbose("Great job thumbnailing");
                }
                log.Verbose("Documents modified " + input.Count);
                log.Verbose("First document Id " + input[0].Id);
            }
        }

7. Modify local.settings.json file



