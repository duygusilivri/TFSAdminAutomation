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
using Microsoft.TeamFoundation.Core.WebApi;

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
            //collectionUrl = "Your_Collection_Url";
            projectUrl = "Your_Project_Url";
            tfsAdminDomain = "Your_Azure_DevOps_Server_Domain";
            tfsAdminUsername = "Your_Azure_DevOps_Server_Username";
            tfsAdminPassword = "Your_Azure_DevOps_Server_Password";
            pat = "Your_Pat";

            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to TFS
            //connection = new VssConnection(new Uri(collectionUrl), creds);

            string choice;
            do
            {
                Console.WriteLine("\n\nType\n5 to update build completion triggers\n0 to Exit");
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
                    case "3":
                        {
                            Console.WriteLine("Which collection?");
                            collectionUrl = Console.ReadLine();
                            Console.WriteLine("Which project?");
                            string projectName = Console.ReadLine();
                            Console.WriteLine("Which buildId?");
                            string buildId = Console.ReadLine();
                            UpdateBuildDefinitionRetention(projectName, int.Parse(buildId));
                            break;
                        }
                    case "4":
                        {
                            Console.WriteLine("Which collection?");
                            collectionUrl = Console.ReadLine();
                            UpdateAllBuildDefinitionRetention();
                            break;
                        }
                    case "5":
                        {
                            Console.WriteLine("Which collection?");
                            collectionUrl = Console.ReadLine();
                            UpdateBuildDefinitionTrigger("Parts Unlimited", 32, 37);
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

        public async static void UpdateBuildDefinitionRetention(string teamProject, int buildId)
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            var bhc = tpc.GetClient<BuildHttpClient>();

            
            BuildDefinition buildDefTemplate = (await bhc.GetDefinitionAsync(teamProject, buildId));
            buildDefTemplate.RetentionRules[0].ArtifactsToDelete.Clear();


            await bhc.UpdateDefinitionAsync(buildDefTemplate, teamProject);
        }

        public async static void UpdateBuildDefinitionTrigger(string teamProject, int mainBuildId, int buildTriggerId)
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            var bhc = tpc.GetClient<BuildHttpClient>();


            BuildDefinition buildDef = (await bhc.GetDefinitionAsync(teamProject, mainBuildId));

            BuildDefinition buildDefTrigger = (await bhc.GetDefinitionAsync(teamProject, buildTriggerId));
            BuildCompletionTrigger buildTriggerToAdd = new BuildCompletionTrigger();
            List<string> branchFilters = new List<string>();
            branchFilters.Add("+refs/heads/master");
            buildTriggerToAdd.Definition = buildDefTrigger;
            buildTriggerToAdd.BranchFilters = branchFilters;
            buildTriggerToAdd.RequiresSuccessfulBuild = true;

            buildDef.Triggers.Add(buildTriggerToAdd);

            await bhc.UpdateDefinitionAsync(buildDef, teamProject);
        }

        public async static void UpdateAllBuildDefinitionRetention()
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            var bhc = tpc.GetClient<BuildHttpClient>();

            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();
            connection = new VssConnection(new Uri(collectionUrl), creds);
            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

            List<BuildDefinitionReference> buildDefinitions = new List<BuildDefinitionReference>();


            // Call to get the list of projects 
            IEnumerable<TeamProjectReference> projects = projectClient.GetProjects(top: 1000).Result;

            foreach (var project in projects)
            {
                
                // Iterate (as needed) to get the full set of build definitions 
                string continuationToken = null;
                do
                {
                    IPagedList<BuildDefinitionReference> buildDefinitionsPage = bhc.GetDefinitionsAsync2(
                        project: project.Name,
                        continuationToken: continuationToken).Result;

                    buildDefinitions.AddRange(buildDefinitionsPage);

                    continuationToken = buildDefinitionsPage.ContinuationToken;
                } while (!String.IsNullOrEmpty(continuationToken));
            }

            try
            {
                foreach (BuildDefinitionReference definition in buildDefinitions)
                {
                    string teamProject = definition.Project.Name;
                    string definitionName = definition.Name;
                    int definitionId = definition.Id;
                    try
                    { 
                        BuildDefinition buildDefTemplate = (await bhc.GetDefinitionAsync(teamProject, definitionId));
                        buildDefTemplate.RetentionRules[0].ArtifactsToDelete.Clear();
                        await bhc.UpdateDefinitionAsync(buildDefTemplate, teamProject);

                        Console.WriteLine("Completed - "+ "Project Name: " + teamProject +"  - Build Definition Name: " + definitionName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error - " + "Project Name: " + teamProject + "  - Build Definition Name: " + definitionName + "Error Msg: " + ex.Message);
                    }

                }
                Console.Write("UPDATE COMPLETED");
            }
            catch (Exception ex)
            {
                string exception = ex.ToString();
            }
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
