using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Diagnostics;
using RestSharp;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue
using System.Net;

namespace InitialFunction
{
    public static class Initial
    {
        [FunctionName("Initial")]
        public static void Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            const string storageAccountName = "cafexdb";
            const string storageAccountKey = "HSg3bD5XZdkp0A3QoMW6K+5qHfUCpWXoZgfftRyoKIJp4dWFEV1IHV2vGPmWcdF5sL+JWDUeqcgYKTL5chGcMg==";

            string org_id = req.Query["org_id"];
            string authorization = req.Query["authorization"];
            Debug.WriteLine("org_id : "+org_id);
            Debug.WriteLine("authorization : " + authorization);

            string requestBody = new StreamReader(req.Body).ReadToEnd();

            dynamic data = JsonConvert.DeserializeObject(requestBody);
            org_id = org_id ?? data?.org_id;

            if (org_id != null && authorization!= null )
            {
                string session_id = "97649434294967498";
                string url = "https://service.na1.liveassistfor365.com/api/transcript/v1/organization/"+org_id+"/chatsession/" + session_id;

                var client = new RestClient(url);
                var request = new RestRequest(Method.GET);
                request.AddHeader("postman-token", "5d2a6c60-2b0f-5074-1b84-848ce83bbcc2");
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("authorization", "Bearer "+ authorization);
                IRestResponse response = client.Execute(request);
                var content = response.Content;
                //Console.WriteLine(content);



                var storageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
                var tableClient = storageAccount.CreateCloudTableClient();
                var chatTable = tableClient.GetTableReference("chat");
                chatTable.CreateIfNotExistsAsync();
                


              
               
                var queueClient = storageAccount.CreateCloudQueueClient();
                var queue = queueClient.GetQueueReference("myqueue");
                queue.CreateIfNotExistsAsync();

                Console.WriteLine("completed");



            }
            else
            {
                Console.WriteLine("incorrect org name ");
            }
        }
    }

    public class ScoreEntity : TableEntity
    {
        public ScoreEntity() { }

        public ScoreEntity(string sessionid, string chatdata)
        {
            PartitionKey = sessionid;
            RowKey = chatdata;

        }

        public string SessionId => PartitionKey;
        public string ChatData => RowKey;

        public override string ToString() => $"{SessionId} {ChatData}";

    }

}
