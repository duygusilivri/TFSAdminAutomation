using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.Client;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TaskAgent
{
    class Program
    {
        static string projectName = "Your_Project_Name";
        static string collectionUrl = "Your_Organization_Url";
        public static string pat;

        static void Main(string[] args)
        {
            pat = "Your_Pat";
            CreateDeploymentPoolProvisionAllProjects();
        }

        public static void UpdateVariableGroup()
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            TaskAgentHttpClient taskagentClient = tpc.GetClient<TaskAgentHttpClient>();

            var varGroup = taskagentClient.GetVariableGroupAsync(projectName, 1).GetAwaiter().GetResult();
            int varCount = varGroup.Variables.Count;
            var v = varGroup.Variables["var1"];
            varGroup.Variables["var1"] = "val1";
            
            VariableGroupParameters varGroupParam = new VariableGroupParameters();
            varGroupParam.Variables = varGroup.Variables;
            varGroupParam.Name = varGroup.Name;
           
            //change 1 with your variable group id
            taskagentClient.UpdateVariableGroupAsync(1, varGroupParam).GetAwaiter().GetResult();

        }

        public static async void CreateDeploymentPoolProvisionAllProjects()
        {
            string poolName = "Your_Pool_Name";
            string poolId = "";
            string url = collectionUrl + "_apis/distributedtask/pools?api-version=6.0-preview.1";
            string jsonContent = "{" +
                                    "name: \"" + poolName + "\"," +
                                    "pooltype: 2" +
                                "}";


            var request = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", pat))));

            using (HttpResponseMessage response = client.PostAsync(url, request).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    DeploymentPoolItems deploymentPool = JsonConvert.DeserializeObject<DeploymentPoolItems>(responseBody);
                    poolId = deploymentPool.id.ToString();
                }
            }

            //Add all projects to the new deployment pool
            using (var client2 = new HttpClient())
            {
                client2.BaseAddress = new Uri(collectionUrl);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", pat))));
          
                HttpResponseMessage response2 = client2.GetAsync("_apis/projects?stateFilter=All").Result;
                

                if (response2.IsSuccessStatusCode)
                {
                    string responseBody2 = await response2.Content.ReadAsStringAsync();

                    RootObjectProject projects = null;
                    projects = JsonConvert.DeserializeObject<RootObjectProject>(responseBody2);

                    int count = projects.count;
                    for (int i = 0; i < count; i++)
                    {
                        string projectName = projects.value[i].name;
                    
                        string urlProject = collectionUrl + projectName + "/_apis/distributedtask/deploymentgroups?api-version=6.0-preview.1";
                        string jsonContentProject = "{" +
                                                "description: \"" + "A new pool at org level was added" + "\"," +
                                                "name: \"" + projectName + "-" + poolName + "\"," +
                                                "poolId: " + poolId +
                                            "}";


                       var request3 = new StringContent(jsonContentProject, Encoding.UTF8, "application/json");

                        HttpClient client3 = new HttpClient();
                        client3.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                            Convert.ToBase64String(
                                System.Text.ASCIIEncoding.ASCII.GetBytes(
                                    string.Format("{0}:{1}", "", pat))));

                        using (HttpResponseMessage response3 = client3.PostAsync(urlProject, request3).Result)
                        {
                            response3.EnsureSuccessStatusCode();
                            string responseBody3 = response3.Content.ReadAsStringAsync().Result;
                        }

                    }
                }
            }
        }

        public class RootObjectProject
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

        public class Properties
        {
        }

        public class Avatar
        {
            public string href { get; set; }
        }

        public class Links
        {
            public Avatar avatar { get; set; }
        }

        public class CreatedBy
        {
            public string displayName { get; set; }
            public string url { get; set; }
            public Links _links { get; set; }
            public string id { get; set; }
            public string uniqueName { get; set; }
            public string imageUrl { get; set; }
            public string descriptor { get; set; }
        }

        public class Owner
        {
            public string displayName { get; set; }
            public string url { get; set; }
            public Links _links { get; set; }
            public string id { get; set; }
            public string uniqueName { get; set; }
            public string imageUrl { get; set; }
            public string descriptor { get; set; }
        }

        public class DeploymentPoolItems
        {
            public Properties properties { get; set; }
            public DateTime createdOn { get; set; }
            public bool autoProvision { get; set; }
            public bool autoUpdate { get; set; }
            public bool autoSize { get; set; }
            public object targetSize { get; set; }
            public object agentCloudId { get; set; }
            public CreatedBy createdBy { get; set; }
            public Owner owner { get; set; }
            public int id { get; set; }
            public string scope { get; set; }
            public string name { get; set; }
            public bool isHosted { get; set; }
            public string poolType { get; set; }
            public int size { get; set; }
            public bool isLegacy { get; set; }
            public string options { get; set; }
        }
    }
}
