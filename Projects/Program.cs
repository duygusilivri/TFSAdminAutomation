using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;

namespace Projects
{
    class Program
    {
        static string collectionUrl = "Your_Azure_DevOps_Url";
        static VssConnection connection;

        static void Main(string[] args)
        {
            // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to Azure DevOps Services
            connection = new VssConnection(new Uri(collectionUrl), creds);
            GetAllTeamProjects();
        }
        public static void GetAllTeamProjects()
        {
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
    }
}
