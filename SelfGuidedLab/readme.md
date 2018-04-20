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



6. Add the form to handle the upload Upload.cshtml under Views->Home folder





