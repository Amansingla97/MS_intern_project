using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Timers;

namespace http2timer
{


    public static class Function1
    {

        static string org_id = "";
        static string authorization = "";

        [FunctionName("Function1")]
        
        public static void Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            org_id = req.Query["org_id"];
            authorization = req.Query["authorization"];
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            dynamic data = JsonConvert.DeserializeObject(requestBody);
            org_id = org_id ?? data?.org_id;
            SetTimer();
        }

        private static Timer aTimer;
        private static void SetTimer()
        {
            // Create a timer with a 1 minute interval.
            aTimer = new System.Timers.Timer(60000);
            // Hook up the Elapsed event for the timer. 

            aTimer.Elapsed += OnTimedEvent;

            //aTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e, org_id,authorization);
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private async static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {

            
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                              e.SignalTime);
            const string storageAccountName = "cafexdb";
            const string storageAccountKey = "HSg3bD5XZdkp0A3QoMW6K+5qHfUCpWXoZgfftRyoKIJp4dWFEV1IHV2vGPmWcdF5sL+JWDUeqcgYKTL5chGcMg==";

            var storageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();
            var chatTable = tableClient.GetTableReference("chat");
            await chatTable.CreateIfNotExistsAsync();

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("myqueue");
            await queue.CreateIfNotExistsAsync();

            // method to get data from CRM and enter in the enque .... here first entry is entired manualy ;
          
            await queue.AddMessageAsync(new CloudQueueMessage("97649434294967498"));
       
            // Fetch the queue attributes.
            await queue.FetchAttributesAsync();

            // Retrieve the cached approximate message count.
            int? cachedMessageCount = queue.ApproximateMessageCount;
            Debug.WriteLine("11111111111111111111111111111111111111111111111111111111111111111111111111:::::::::::::" + cachedMessageCount);


            int numberofqueries = (int)cachedMessageCount;




            while (numberofqueries != 0)
            {
                if (org_id != null && authorization != null)

                {

                // Peek at the next message
                var peekedMessage = (await queue.PeekMessageAsync()).AsString;
                    

                    string session_id = peekedMessage.ToString();
                    
                    string url = "https://service.na1.liveassistfor365.com/api/transcript/v1/organization/" + org_id + "/chatsession/" + session_id;

                    var client = new RestClient(url);
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("postman-token", "5d2a6c60-2b0f-5074-1b84-848ce83bbcc2");
                    request.AddHeader("cache-control", "no-cache");
                    request.AddHeader("authorization", "Bearer " + authorization);
                    IRestResponse response = client.Execute(request);
                    var content = response.Content;
                //Console.WriteLine(content);


                // Get the next message
                var retrievedMessage =  await queue.GetMessageAsync();

                    //Process the message in less than 30 seconds, and then delete the message
                   await queue.DeleteMessageAsync(retrievedMessage);

                    numberofqueries--;
                    


                    //chatTable.ExecuteAsync(TableOperation.Insert(new ScoreEntity("97649434294967498", content)));
                    await chatTable.ExecuteBatchAsync(new TableBatchOperation(){
                    TableOperation.Insert(new ScoreEntity(session_id, content))
                     });

                    Console.WriteLine("Completed");
                }
                else
                {
                    Console.WriteLine("incorrect org name ");
                }
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
