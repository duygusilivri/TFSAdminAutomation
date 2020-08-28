using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Framework.Common;

namespace Projects
{
    class Program
    {
        static string collectionUrl = "Your_collection_url";
        static string serverUrl = "Your_server_url";
        static VssConnection connection;

        static void Main(string[] args)
        {
            Console.WriteLine("Type your server Url without / at the end");
            serverUrl = Console.ReadLine(); 

            Console.WriteLine("Type 1 to list collection members, 2 to add collection members");
            string input = Console.ReadLine();
            if(input.Equals("1"))
            {
                GetCollectionMembers();
            }
            else if(input.Equals("2"))
            {
                AddCollectionMembers();
            }

            
            
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

        public static void GetCollectionMembers()
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            Uri configurationServerUri = new Uri(serverUrl);
            TfsConfigurationServer configurationServer = TfsConfigurationServerFactory.GetConfigurationServer(configurationServerUri);
            var tpcService = configurationServer.GetService<ITeamProjectCollectionService>();

            string[,] members = new string[100000, 5];
            int i = 0;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"c:\TfsAdminAutomationData\out_CollectionMembers.txt"))
            {
                file.WriteLine("Collection # Group # Domain # AccountName # DisplayName");
                foreach (Microsoft.TeamFoundation.Framework.Client.TeamProjectCollection tpc in tpcService.GetCollections())
                {
                    var tfsProjectCollection = new TfsTeamProjectCollection(new Uri(serverUrl + "/" + tpc.Name));

                    ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

                    var sec = tfsProjectCollection.GetService<IGroupSecurityService>();

                    var appGroups = sec.ListApplicationGroups(null); ;

                    foreach (var group in appGroups)
                    {
                        Identity[] groupMembers = sec.ReadIdentities(SearchFactor.Sid, new string[] { group.Sid }, QueryMembership.Expanded);
                        foreach (Identity member in groupMembers)
                        {
                            if (member.Members != null)
                            {
                                foreach (string memberSid in member.Members)
                                {
                                    Identity memberInfo = sec.ReadIdentity(SearchFactor.Sid, memberSid, QueryMembership.Expanded);
                                    if (memberInfo.Type != IdentityType.WindowsUser)
                                        continue;

                                    members[i, 0] = tpc.Name;
                                    members[i, 1] = "N/A";
                                    members[i, 2] = memberInfo.AccountName;
                                    members[i, 3] = memberInfo.Domain;
                                    members[i, 4] = group.DisplayName;
                                    file.WriteLine(tpc.Name + " # " + group.DisplayName + " # " + memberInfo.Domain + " # " + memberInfo.AccountName + " # " + memberInfo.DisplayName );
                                    i++;

                                }
                            }
                        }
                    }

                }
            }
                
            


        }


        public static void AddCollectionMembers()
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            Uri configurationServerUri = new Uri(serverUrl);
            TfsConfigurationServer configurationServer = TfsConfigurationServerFactory.GetConfigurationServer(configurationServerUri);
            var tpcService = configurationServer.GetService<ITeamProjectCollectionService>();

            string[,] members = new string[100000, 5];
            int i = 0;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"c:\TfsAdminAutomationData\out_CollectionMembersAdd.txt"))
            {
                file.WriteLine("Collection # Group # Domain # AccountName # DisplayName");
                foreach (Microsoft.TeamFoundation.Framework.Client.TeamProjectCollection tpc in tpcService.GetCollections())
                {
                    var tfsProjectCollection = new TfsTeamProjectCollection(new Uri(serverUrl + "/" + tpc.Name));

                    ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

                    var sec = tfsProjectCollection.GetService<IGroupSecurityService>();

                    var appGroups = sec.ListApplicationGroups(null); ;

                    foreach (var group in appGroups)
                    {
                        Identity[] groupMembers = sec.ReadIdentities(SearchFactor.Sid, new string[] { group.Sid }, QueryMembership.Expanded);
                        foreach (Identity member in groupMembers)
                        {
                            if (member.Members != null)
                            {
                                foreach (string memberSid in member.Members)
                                {
                                    Identity memberInfo = sec.ReadIdentity(SearchFactor.Sid, memberSid, QueryMembership.Expanded);
                                    if (memberInfo.Type != IdentityType.WindowsUser)
                                        continue;

                                    members[i, 0] = tpc.Name;
                                    members[i, 1] = "N/A";
                                    members[i, 2] = memberInfo.AccountName;
                                    members[i, 3] = memberInfo.Domain;
                                    members[i, 4] = group.DisplayName;
                                    file.WriteLine(tpc.Name + " # " + group.DisplayName + " # " + memberInfo.Domain + " # " + memberInfo.AccountName + " # " + memberInfo.DisplayName);

                                    if(memberInfo.Domain.Equals("AAD"))
                                    {
                                        string username = "AADTECH\\" + memberInfo.AccountName;
                                        string groupname = "[" + tpc.Name + "]\\" + group.DisplayName;
                                        var result = AddUserToGroup(username, groupname, tfsProjectCollection.Name);
                                        if (result)
                                            file.WriteLine("Added");
                                        else
                                            file.WriteLine("Cannot be added");
                                    }
                                        

                                    i++;

                                }
                            }
                        }
                    }

                }
            }
        }
        public static void ListAllMembers()
        {
            
            // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to Azure DevOps Services
            connection = new VssConnection(new Uri(collectionUrl), creds);
            /*
            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();
            TeamHttpClient teamClient = connection.GetClient<TeamHttpClient>();

            IEnumerable<TeamProjectReference> projects = projectClient.GetProjects(top: 10000).Result;

            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));
            var ims = tpc.GetService<IIdentityManagementService>();
            //IGroupSecurityService gss = tpc.GetService<IGroupSecurityService>();
            IIdentityManagementService gss = tpc.GetService<IIdentityManagementService>();*/


            Uri configurationServerUri = new Uri(serverUrl);
            TfsConfigurationServer configurationServer = TfsConfigurationServerFactory.GetConfigurationServer(configurationServerUri);
            var tpcService = configurationServer.GetService<ITeamProjectCollectionService>();

            string[,] members = new string[100000, 5];
            int i = 0;

            foreach (Microsoft.TeamFoundation.Framework.Client.TeamProjectCollection tpc in tpcService.GetCollections())
            {
                var tfsProjectCollection = new TfsTeamProjectCollection(new Uri(serverUrl + "/" + tpc.Name));

                ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();
                IEnumerable<TeamProjectReference> projects = projectClient.GetProjects(top: 10000).Result;
                List<TeamProjectReference> teamProjects = new List<TeamProjectReference>(projects);
                
                var sec = tfsProjectCollection.GetService<IGroupSecurityService>();


                foreach (var teamProject in teamProjects)
                {
                    Microsoft.TeamFoundation.Core.WebApi.TeamProject projectdet = projectClient.GetProject(teamProject.Name).Result;

                    Uri proj = new Uri(teamProject.Url);
                    var appGroups = sec.ListApplicationGroups(proj.AbsoluteUri); ;

                    foreach (var group in appGroups)
                    {
                        Identity[] groupMembers = sec.ReadIdentities(SearchFactor.Sid, new string[] { group.Sid }, QueryMembership.Expanded);
                        foreach (Identity member in groupMembers)
                        {
                            if (member.Members != null)
                            {
                                foreach (string memberSid in member.Members)
                                {
                                    Identity memberInfo = sec.ReadIdentity(SearchFactor.Sid, memberSid, QueryMembership.Expanded);
                                    if (memberInfo.Type != IdentityType.WindowsUser)
                                        continue;

                                    members[i, 0] = tfsProjectCollection.Name;
                                    members[i, 1] = teamProject.Name;
                                    members[i, 2] = memberInfo.AccountName;
                                    members[i, 3] = memberInfo.Domain;
                                    members[i, 4] = group.DisplayName;
                                    Console.WriteLine(tfsProjectCollection.Name + " " + teamProject.Name + " " + memberInfo.AccountName + " " + memberInfo.DisplayName + " " + group.DisplayName);
                                    i++;
                                    
                                }
                            }
                        }
                    }
                }
            }
            /*string[,] members = new string[100000, 4];
           
            int i = 0;

            foreach (var project in projects)
            {
                try
                {
                    Identity[] appGroups = gss.ListApplicationGroups(  .ListApplicationGroups(project.Url);

                    foreach (var appGroup in appGroups)
                    { 

                        //string adminGroupName = "[" + project.Name.ToString() + "]" + "\\Project Administrators";
                        //Identity SIDS = gss.ReadIdentity(SearchFactor.AccountName, adminGroupName, QueryMembership.Expanded);

                        Identity SIDS = gss.ReadIdentity(SearchFactor.AccountName, appGroup.AccountName, QueryMembership.Expanded);
                        Identity[] userIds = gss.ReadIdentities(SearchFactor.Sid, SIDS.Members, QueryMembership.None);

                        foreach (Identity id in userIds)
                        {
                            members[i, 0] = project.Name;
                            members[i, 1] = id.AccountName;
                            members[i, 2] = id.DisplayName;
                            members[i, 3] = appGroup.AccountName;
                            Console.WriteLine(project.Name + " " + id.AccountName + " " + id.DisplayName + " " +appGroup.AccountName);
                            i++;
                        }
                        Console.WriteLine("Number of total members: " + i);
                    }
                }
                catch (Exception ex)
                {
                    members[i, 0] = project.Name;
                    members[i, 1] = "N/A";
                    members[i, 2] = "N/A";
                    members[i, 3] = "N/A";
                    i++;
                }
            }*/


        }

        public static bool AddUserToGroup(string userName, string groupName, string collectionUrl)
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUrl));

            var ims = tpc.GetService<IIdentityManagementService>();

            try
            {
                var tfsGroupIdentity = ims.ReadIdentity(IdentitySearchFactor.AccountName,
                                                    groupName,
                                                    MembershipQuery.None,
                                                    ReadIdentityOptions.IncludeReadFromSource);

                var userIdentity = ims.ReadIdentity(IdentitySearchFactor.AccountName,
                                                        userName,
                                                        MembershipQuery.None,
                                                        ReadIdentityOptions.IncludeReadFromSource);

                ims.AddMemberToApplicationGroup(tfsGroupIdentity.Descriptor, userIdentity.Descriptor);

                Console.WriteLine("User added: " + userName + ", " + groupName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("User cannot be added: " + userName + ", " + groupName);
                return false;
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
