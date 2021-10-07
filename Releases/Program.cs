using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

namespace Releases
{
    class Program
    {
        public static string personalAccessToken;
        public static string projectUrl;
        public static string collectionUrl = "Your_Collection_Url";
        public static VssConnection connection;


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

        public static async System.Threading.Tasks.Task UpdateClassicReleaseDefinition()
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to TFS
            connection = new VssConnection(new Uri(collectionUrl), creds);

            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();
            ReleaseHttpClient releaseClient = connection.GetClient<ReleaseHttpClient>();

            try
            {
                IEnumerable<TeamProjectReference> projects = projectClient.GetProjects(top: 1000).Result;
                List<TeamProjectReference> projectsList = new List<TeamProjectReference>(projects);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"c:\out_Releases.txt"))
                {
                    foreach (TeamProjectReference project in projectsList)
                    {
                        file.WriteLine();
                        file.WriteLine();
                        file.WriteLine("Team Project: " + project.Name);


                        List<ReleaseDefinition> releaseDefinitions = releaseClient.GetReleaseDefinitionsAsync(project: project.Name, expand: ReleaseDefinitionExpands.Environments).Result;

                        foreach (ReleaseDefinition releaseDefinition in releaseDefinitions)
                        {
                            file.WriteLine();
                            file.WriteLine("-Release Definition: " + releaseDefinition.Name);
                            var fullDefinition = await releaseClient.GetReleaseDefinitionAsync(project.Name, releaseDefinition.Id);
                            List<ReleaseDefinitionEnvironment> releaseDefinitionEnvironments = fullDefinition.Environments.ToList();
                            foreach (ReleaseDefinitionEnvironment releaseDefinitionEnvironment in releaseDefinitionEnvironments)
                            {
                                file.WriteLine("--Release Definition Environment: " + releaseDefinitionEnvironment.Name);
                                foreach (DeployPhase releaseDefinitionPhase in releaseDefinitionEnvironment.DeployPhases)
                                {
                                    file.WriteLine("---Release Definition Phase: " + releaseDefinitionPhase.Name);

                                    List<WorkflowTask> tempSteps = releaseDefinitionPhase.WorkflowTasks.ToList();
                                    WorkflowTask newstep = new WorkflowTask();

                                    int order = 0;
                                    foreach (WorkflowTask oldStep in tempSteps)
                                    {
                                        file.WriteLine("----Release Definition Step " + oldStep.Name);
                                        //Change the default input of the extract files task from *.zip to **/*.zip
                                        Guid extractTask = new Guid("5e1e3830-fbfb-11e5-aab1-090c92bc4988"); 
                                        if (oldStep.TaskId.Equals(extractTask))
                                        {
                                            newstep = oldStep;
                                            IDictionary<string, string> inputs = newstep.Inputs;
                                            inputs.Remove("archiveFilePatterns");
                                            inputs.Add("archiveFilePatterns", "**/*.zip");
                                            releaseDefinitionPhase.WorkflowTasks.RemoveAt(order);
                                            releaseDefinitionPhase.WorkflowTasks.Insert(order, newstep);
                                            file.WriteLine("-----Release Definition Step Changed");
                                        }
                                        order++;
                                    }
                                }
                            }
                            await releaseClient.UpdateReleaseDefinitionAsync(fullDefinition, project.Name);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string exception = ex.ToString();
            }
        }
    }
}
