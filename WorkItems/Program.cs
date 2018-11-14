using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WorkItems
{
    class Program
    {
        static string collectionUrl = "Your_Azure_DevOps_Url";
        static string personalAccessToken = "Your_PAT";
        

        static void Main(string[] args)
        {
            string projectName = "Parts%20Unlimited";
            string workItemType = "Task";
            CreateWorkItem(projectName, workItemType);
        }

        public static void CreateWorkItem(string projectName, string workItemType)
        {
            string url = collectionUrl + "/" + projectName + "/_apis/wit/workitems/$" + workItemType + "?api-version=4.1";
            string jsonContent = "[" +
                                    "{" +
                                        "\"op\": \"add\"," +
                                        "\"path\": \"/fields/System.Title\"," +
                                        "\"from\": null," +
                                        "\"value\": \"Sample work item\"" +
                                    "}" +
                                "]";

            var request = new StringContent(jsonContent, Encoding.UTF8, "application/json-patch+json");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", personalAccessToken))));


            using (HttpResponseMessage response = client.PostAsync(url, request).Result)
            {
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;

                Console.WriteLine("Work item created: ");
                Console.WriteLine("Response: " + responseBody);
            }
            Console.ReadKey();
        }
    }
}
