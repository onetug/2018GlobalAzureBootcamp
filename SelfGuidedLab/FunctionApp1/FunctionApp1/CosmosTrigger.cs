using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace FunctionApp1
{
    public static class CosmosTrigger
    {
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
    }
}
