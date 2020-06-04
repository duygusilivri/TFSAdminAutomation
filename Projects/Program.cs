using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Projects
{
    class Program
    {
        static string collectionUrl = "Your_Azure_DevOps_Url";
        static VssConnection connection;

        static void Main(string[] args)
        {
            GetAllTeamProjects_Library();
            GetAllTeamProjects_Rest();
        }
        public static void GetAllTeamProjects_Library()
        {
            // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to Azure DevOps Services
            connection = new VssConnection(new Uri(collectionUrl), creds);

            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

            IEnumerable<TeamProjectReference> projects = projectClient.GetProjects(top: 1000).Result;

            foreach (var project in projects)
            {
                try
                {
                    string projectName = project.Name.ToString();                
                }
                catch (Exception ex)
                {
                    
                }
            }
        }

        public static async void GetAllTeamProjects_Rest()
        {
            //encode your personal access token                   
            string credentials = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", "your_pat")));

            //use the httpclient
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(collectionUrl);  
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                //connect to the REST endpoint            
                HttpResponseMessage response = client.GetAsync("_apis/projects?stateFilter=All&api-version=5.1").Result;

                if (response.IsSuccessStatusCode)
                {
                    //var value = response.Content.ReadAsStringAsync().Result;

                    string responseBody = await response.Content.ReadAsStringAsync();

                    RootObject projects = null;
                    projects = JsonConvert.DeserializeObject<RootObject>(responseBody);

                    int count = projects.count;
                    for (int i = 0; i < count; i++)
                    {
                        Console.WriteLine(projects.value[i].name);
                    }
                }
            }
        }

        public class RootObject
        {
            public int count { get; set; }
            public List<ProjectItems> value { get; set; }
        }

        public class ProjectItems
        {

            public string id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string url { get; set; }
            public string state { get; set; }
            public int revision { get; set; }
            public string visibility { get; set; }
            public DateTime lastUpdateTime { get; set; }

        }
    }
}
