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
using Microsoft.TeamFoundation.Framework.Common;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace Projects
{
    class Program
    {
        static string collectionUrl = "Your_collection_url";
        static string serverUrl = "Your_server_url";
        static VssConnection connection;

        static void Main(string[] args)
        {
            Console.WriteLine("Type 1 to list collection members, 2 to add collection members, 3 to add AAD Groups to Team Projects");
            string input = Console.ReadLine();
            if (input.Equals("1"))
            {
                Console.WriteLine("Type your server Url without / at the end");
                serverUrl = Console.ReadLine();

                GetCollectionMembers();
            }
            else if (input.Equals("2"))
            {
                Console.WriteLine("Type your server Url without / at the end");
                serverUrl = Console.ReadLine();

                string fromDomain = "";
                string toDomain = "";
                string collectionName = "";
                string groupName = "";
                Console.WriteLine("Collection Name");
                collectionName = Console.ReadLine();
                Console.WriteLine("Group Name");
                groupName = Console.ReadLine();
                Console.WriteLine("From Domain");
                fromDomain = Console.ReadLine();
                Console.WriteLine("To Domain");
                toDomain = Console.ReadLine();

                AddCollectionMembers(collectionName, groupName, fromDomain, toDomain);
            }
            else if (input.Equals("3"))
            {
                string orgUrl = "";
                Console.WriteLine("Organization Url");
                orgUrl = Console.ReadLine();
                string fileName = "";
                Console.WriteLine("File Path:");
                fileName = Console.ReadLine();

                AddAADGroupToProjectsFromFile(orgUrl,fileName);
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

            // Connect to Azure DevOps Services
            connection = new VssConnection(new Uri(serverUrl), creds);

            Uri configurationServerUri = new Uri(serverUrl);
            TfsConfigurationServer configurationServer = TfsConfigurationServerFactory.GetConfigurationServer(configurationServerUri);
            var tpcService = configurationServer.GetService<ITeamProjectCollectionService>();


            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"c:\TfsAdminAutomationData\out_CollectionMembers.txt"))
            {
                file.WriteLine("Collection # Group # Domain # AccountName # DisplayName");
                foreach (Microsoft.TeamFoundation.Framework.Client.TeamProjectCollection tpc in tpcService.GetCollections())
                {
                    var tfsProjectCollection = new TfsTeamProjectCollection(new Uri(serverUrl + "/" + tpc.Name));

                    ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

                    var sec = tfsProjectCollection.GetService<IGroupSecurityService>();

                    var appGroups = sec.ListApplicationGroups(null);

                    foreach (var group in appGroups)
                    {
                        try
                        {
                            Identity[] groupMembers = sec.ReadIdentities(SearchFactor.Sid, new string[] { group.Sid }, QueryMembership.Expanded);
                            foreach (Identity member in groupMembers)
                            {
                                if (member.Members != null)
                                {
                                    foreach (string memberSid in member.Members)
                                    {
                                        try
                                        {
                                            Identity memberInfo = sec.ReadIdentity(SearchFactor.Sid, memberSid, QueryMembership.Expanded);
                                            if (memberInfo.Type != IdentityType.WindowsUser)
                                                continue;

                                            file.WriteLine(tpc.Name + " # " + group.DisplayName + " # " + memberInfo.Domain + " # " + memberInfo.AccountName + " # " + memberInfo.DisplayName);
                                        }
                                        catch (Exception ex)
                                        {
                                            file.WriteLine("Could not read member info");
                                            file.WriteLine(ex.InnerException);
                                            continue;
                                        }


                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            file.WriteLine("Could not read group members: " + group.AccountName);
                            file.WriteLine(ex.InnerException);
                            continue;
                        }

                    }

                }
            }




        }

        public static void AddCollectionMembers(string collectionName, string groupName, string fromDomain, string toDomain)
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            // Connect to Azure DevOps Services
            connection = new VssConnection(new Uri(serverUrl), creds);

            Uri configurationServerUri = new Uri(serverUrl);
            TfsConfigurationServer configurationServer = TfsConfigurationServerFactory.GetConfigurationServer(configurationServerUri);
            var tpcService = configurationServer.GetService<ITeamProjectCollectionService>();


            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"c:\TfsAdminAutomationData\out_CollectionMembersAdd.txt"))
            {
                file.WriteLine("Collection # Group # Domain # AccountName # DisplayName");
                foreach (Microsoft.TeamFoundation.Framework.Client.TeamProjectCollection tpc in tpcService.GetCollections())
                {
                    if (!tpc.Name.Equals(collectionName))
                        continue;

                    var tfsProjectCollection = new TfsTeamProjectCollection(new Uri(serverUrl + "/" + tpc.Name));

                    ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

                    var sec = tfsProjectCollection.GetService<IGroupSecurityService>();

                    var appGroups = sec.ListApplicationGroups(null);

                    foreach (var group in appGroups)
                    {
                        if (!group.DisplayName.Equals(groupName))
                            continue;

                        try
                        {
                            Identity[] groupMembers = sec.ReadIdentities(SearchFactor.Sid, new string[] { group.Sid }, QueryMembership.Expanded);
                            foreach (Identity member in groupMembers)
                            {
                                if (member.Members != null)
                                {
                                    foreach (string memberSid in member.Members)
                                    {
                                        try
                                        {
                                            Identity memberInfo = sec.ReadIdentity(SearchFactor.Sid, memberSid, QueryMembership.Expanded);
                                            if (memberInfo.Type != IdentityType.WindowsUser)
                                                continue;

                                            file.WriteLine(tpc.Name + " # " + group.DisplayName + " # " + memberInfo.Domain + " # " + memberInfo.AccountName + " # " + memberInfo.DisplayName);

                                            if (memberInfo.Domain.Equals(fromDomain))
                                            {
                                                string username = toDomain + "\\" + memberInfo.AccountName;
                                                string groupname = "[" + tpc.Name + "]\\" + group.DisplayName;
                                                var result = AddUserToGroup(username, groupname, tfsProjectCollection.Name);
                                                if (result)
                                                    file.WriteLine("Added");
                                                else
                                                    file.WriteLine("Cannot be added");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            file.WriteLine("Could not read member info");
                                            file.WriteLine(ex.InnerException);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            file.WriteLine("Could not read group members: " + group.AccountName);
                            file.WriteLine(ex.InnerException);
                            continue;
                        }

                    }

                }
            }
        }

        public static void AddAADGroupToProjectsFromFile(string OrgUrl, string fileName)
        {
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(fileName);
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            int rowCount = xlRange.Rows.Count;
            int colCount = xlRange.Columns.Count;

            for (int i = 2; i <= rowCount; i++)
            {
                string teamProject = "";
                string aadGroupName = "";
                string customTpGroupName = "";
                for (int j = 1; j <= colCount; j++)
                {
                    if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
                    {
                        if (j == 1)
                            teamProject = xlRange.Cells[i, j].Value2.ToString();
                        else if (j == 2)
                            customTpGroupName = xlRange.Cells[i, j].Value2.ToString();
                        else if (j == 3)
                            aadGroupName = xlRange.Cells[i, j].Value2.ToString();
                    }
                }

                AddAADGroupToTPCustomGroup(teamProject, customTpGroupName, aadGroupName, OrgUrl);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);

            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);

            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);   
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

        public static bool AddAADGroupToTPCustomGroup(string teamProject, string tpCustomGroupName, string aadGroupName, string organizationUrl)
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            var tpc = new TfsTeamProjectCollection(new Uri(organizationUrl), creds); 
            tpc.Connect(Microsoft.TeamFoundation.Framework.Common.ConnectOptions.IncludeServices);

            IIdentityManagementService ims = tpc.GetService<IIdentityManagementService>();

            string tpCustomGroupNameFull = "[" + teamProject + "]" + "\\" + tpCustomGroupName;
            string aadGroupNameFull = "[TEAM FOUNDATION]" + "\\" + aadGroupName;  //for AAD Groups

            try
            {
                var tfsGroupIdentity = ims.ReadIdentity(IdentitySearchFactor.AccountName,
                                                    tpCustomGroupNameFull,
                                                    MembershipQuery.None,
                                                    ReadIdentityOptions.IncludeReadFromSource);

                var aadGroupIdentity = ims.ReadIdentity(IdentitySearchFactor.AccountName,
                                                        aadGroupNameFull,
                                                        MembershipQuery.None,
                                                        ReadIdentityOptions.IncludeReadFromSource);

                ims.AddMemberToApplicationGroup(tfsGroupIdentity.Descriptor, aadGroupIdentity.Descriptor);

                Console.WriteLine("Group added: " + aadGroupName + " to " + tpCustomGroupName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Group cannot be added: " + aadGroupName + ", " + tpCustomGroupName);
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
