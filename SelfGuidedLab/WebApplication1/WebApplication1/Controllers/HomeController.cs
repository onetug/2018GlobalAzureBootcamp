using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Queue storage types
using Newtonsoft.Json;
namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

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
    }
}