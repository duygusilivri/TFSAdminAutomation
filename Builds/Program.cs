using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;

namespace Builds
{
    class Program
    {
        public static string collectionUrl;
        public static string projectUrl;
        public static string tfsAdminDomain;
        public static string tfsAdminUsername;
        public static string tfsAdminPassword;
        public static string pat;
        public static VssConnection connection;

        static void Main(string[] args)
        {
            collectionUrl = "Your_Collection_Url";
            projectUrl = "Your_Project_Url";
            tfsAdminDomain = "Your_Azure_DevOps_Server_Domain";
            tfsAdminUsername = "Your_Azure_DevOps_Server_Username";
            tfsAdminPassword = "Your_Azure_DevOps_Server_Password";
            pat = "Your_Pat";

            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to TFS
            connection = new VssConnection(new Uri(collectionUrl), creds);

            string choice;
            do
            {
                Console.WriteLine("\n\nType\n1 to create build definitions from a template\n0 to Exit");
                choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        {
                            Console.WriteLine("Which project?");
                            string projectName = Console.ReadLine();
                            CreateBuildDefinition(projectName);
                            break;
                        }
                    case "2":
                        {
                            QueueNewBuild();
                            break;
                        }
                }

            } while (choice != "0");
        }

        public async static void CreateBuildDefinition(string teamProject)
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            var bhc = tpc.GetClient<BuildHttpClient>();

            string templateProject = "TEMPLATE_PROJECT";
            int templateId = FindBuildDefinitionId(templateProject, "TEMPLATE_BUILD");

            BuildDefinition buildDefTemplate = (await bhc.GetDefinitionAsync(templateProject, templateId));
            buildDefTemplate.Project = null;
            buildDefTemplate.Name = "NewBuild";
            var repository = buildDefTemplate.Repository;
            buildDefTemplate.Repository = null;
            repository.Url = null;
            repository.Id = null;
            repository.Name = teamProject;
            buildDefTemplate.Repository = repository;
            var queue = buildDefTemplate.Queue;
            buildDefTemplate.Queue = null;
            AgentPoolQueue newQueue = new AgentPoolQueue();
            newQueue.Name = queue.Name;
            buildDefTemplate.Queue = newQueue;

            buildDefTemplate.Variables.Clear();

            BuildDefinitionVariable var1 = new BuildDefinitionVariable();
            var1.Value = "value";
            buildDefTemplate.Variables.Add("key", var1);

            await bhc.CreateDefinitionAsync(buildDefTemplate, teamProject);
        }


        public static int FindBuildDefinitionId(string projectName, string definitionName)
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            var bhc = tpc.GetClient<BuildHttpClient>();

            List<BuildDefinitionReference> buildDefinitions = new List<BuildDefinitionReference>();

            // Iterate (as needed) to get the full set of build definitions 
            string continuationToken = null;
            do
            {
                IPagedList<BuildDefinitionReference> buildDefinitionsPage = bhc.GetDefinitionsAsync2(
                    project: projectName,
                    continuationToken: continuationToken).Result;

                buildDefinitions.AddRange(buildDefinitionsPage);

                continuationToken = buildDefinitionsPage.ContinuationToken;
            } while (!String.IsNullOrEmpty(continuationToken));

            // Show the build definitions 
            foreach (BuildDefinitionReference definition in buildDefinitions)
            {
                if (definition.Name.ToString().Equals(definitionName))
                    return definition.Id;
            }
            return 0;
        }

        private static void QueueNewBuild()
        {
            string url = projectUrl + "_apis/build/builds?api-version=5.1";
            //change your build definition id
            string jsonContent = "{" +
                                    "\"definition\": " +
                                    "{" +
                                        "\"id\": \"14\"" +
                                    "}" +
                                "}";

            var request = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpClient client2 = new HttpClient();
            client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", pat))));


            using (HttpResponseMessage response2 = client2.PostAsync(url, request).Result)
            {
                response2.EnsureSuccessStatusCode();
                string responseBody2 = response2.Content.ReadAsStringAsync().Result;
            }
        }
    }
}
