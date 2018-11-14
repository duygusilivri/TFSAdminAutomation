using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dashboards
{
    class Program
    {
        static string collectionUrl = "https://duygua.visualstudio.com";
        static string personalAccessToken = "2qguzw7ppytyynnupkk4w3iztkizijsvhwyg2i5de327arry2oaq";

        static void Main(string[] args)
        {
            string projectName = "Parts%20Unlimited";
            string dashboardId = "8aeb8fb2-d101-4558-91ce-ef0b913a205d"; //You can get this dynamically using https://docs.microsoft.com/en-us/rest/api/vsts/dashboard/dashboards/get?view=vsts-rest-4.1
            CreateWorkItemQueryWidget(projectName, dashboardId);
        }

        public static void CreateWorkItemQueryWidget(string projectName, string dashboardId)
        {
            string url = collectionUrl + "/" + projectName + "/_apis/dashboard/dashboards/" + dashboardId + "/widgets?api-version=4.1-preview.2";
            string jsonContent = "{" +
                                    "\"name\": \"Work Assigned to Me\"," +
                                    "\"size\": " +
                                    "{" +
                                        "\"rowSpan\": \"1\"," +
                                        "\"columnSpan\": \"1\"" +
                                    "}," +
                                    "\"settings\": " +
                                    "\"{" +
                                        "\\\"queryId\\\": \\\"b4c11023-eb4d-4dd1-b6c9-ece360d5b41b\\\"," +  //You can get this id using https://docs.microsoft.com/en-us/rest/api/vsts/wit/queries/list?view=vsts-rest-4.1#examples
                                        "\\\"queryName\\\": \\\"Work Assigned To Me\\\"" +
                                    "}\"," +
                                    "\"contributionId\": \"ms.vss-dashboards-web.Microsoft.VisualStudioOnline.Dashboards.QueryScalarWidget\"" +
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

                Console.WriteLine("Work item query widget on dashboard created: ");
                Console.WriteLine("Response: " + responseBody);
            }
            Console.ReadKey();
        }
    }
}
