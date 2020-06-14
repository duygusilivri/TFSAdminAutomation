using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Releases
{
    class Program
    {
        public static string personalAccessToken;
        public static string projectUrl;


        static void Main(string[] args)
        {
            projectUrl = "Your_Project_Url";
            personalAccessToken = "Your_Pat";

            QueueNewRelease();
        }

        public static void QueueNewRelease()
        {
            string url = projectUrl + "_apis/release/releases?api-version=5.1";
            //change your release definition id
            string jsonContent = "{" +
                                    "\"definitionId\": 4," +
                                    "\"description\": \"description\"," +
                                    "\"isDraft\": false," +
                                    "\"reason\": \"test\"," +
                                    "\"manualEnvironments\": null" +
                                "}";

            var request = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", personalAccessToken))));


            using (HttpResponseMessage response = client.PostAsync(url, request).Result)
            {
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                JObject obj = JObject.Parse(responseBody);
                string releaseID = (string)obj["id"];
                Console.WriteLine("Release ID: " + releaseID);
            }

        }
    }
}
