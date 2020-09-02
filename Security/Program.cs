using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Security.Client;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Security
{
    class Program
    {
        public static string collectionUrl;
        public static string serverUrl;
        public static VssConnection connection;

        static void Main(string[] args)
        {
            Console.WriteLine("Server URL");
            serverUrl = Console.ReadLine();

            Console.WriteLine("Collection URL");
            collectionUrl = Console.ReadLine();

            Console.WriteLine("Type 1 to list permissions, Type 2 to list and remove permissions");
            string option = Console.ReadLine();

            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();

            connection = new VssConnection(new Uri(collectionUrl), creds);

            if(option.Equals("1"))
            {
                ListGitNamespacePermissions();
            }
            else if(option.Equals("2"))
            {
                ListAndRemoveGitNamespacePermissions();
            }   
        }

        public static void ListGitNamespacePermissions()
        {
            SecurityHttpClient securityClient = connection.GetClient<SecurityHttpClient>();

            Guid g = Guid.Parse("2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87"); //Git security namespace 

            IEnumerable<Microsoft.VisualStudio.Services.Security.SecurityNamespaceDescription> namespaces = securityClient.QuerySecurityNamespacesAsync(g).Result;
            Microsoft.VisualStudio.Services.Security.SecurityNamespaceDescription gitNamespace = namespaces.First();

            IEnumerable<Microsoft.VisualStudio.Services.Security.AccessControlList> acls = securityClient.QueryAccessControlListsAsync(
                 g,
                 string.Empty,
                 descriptors: null,
                 includeExtendedInfo: false,
                 recurse: true).Result;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"c:\TFSAdminAutomationData\out_GitAccessControlLists.txt"))
            {
                int counter = 0;
                file.WriteLine("token | inherit? | count of ACEs");
                file.WriteLine("------+----------+--------------");
                foreach (Microsoft.VisualStudio.Services.Security.AccessControlList acl in acls)
                {
                    counter++;
                    string[] tokenParser = acl.Token.Split('/');
                    if (tokenParser.Length != 2) //we are interested in team project level git security
                    {
                        continue;
                    }
                    file.WriteLine();
                    file.WriteLine();
                    file.WriteLine("{0} | {1} | {2} ACEs", acl.Token, acl.InheritPermissions, acl.AcesDictionary.Count());
                    file.WriteLine("Project Name: " + GetProjectName(tokenParser[1]));
                    file.WriteLine("Expanding ACL for {0} ({1} ACEs)", acl.Token, acl.AcesDictionary.Count());
                    // get the details for Git permissions 
                    Dictionary<int, string> permission = GetGitPermissionNames();
                    // use the Git permissions data to expand the ACL 
                    foreach (var kvp in acl.AcesDictionary)
                    {
                        // in the key-value pair, Key is an identity and Value is an ACE (access control entry) 
                        // allow and deny are bit flags indicating which permissions are allowed/denied 
                        string identity = kvp.Key.Identifier.ToString();
                        file.WriteLine("Identity {0}", identity);
                        string identityName = GetNameFromIdentity(identity);
                        file.WriteLine("Identity Name {0}", identityName);
                        if (!identityName.EndsWith("Project Administrators"))
                        {
                            continue;
                        }
                        string allowed = GetPermissionString(kvp.Value.Allow, permission);
                        string denied = GetPermissionString(kvp.Value.Deny, permission);

                        file.WriteLine("  Allowed: {0} (value={1})", allowed, kvp.Value.Allow);
                        file.WriteLine("  Denied: {0} (value={1})", denied, kvp.Value.Deny);
                    }
                }
            }
        }


        public static void ListAndRemoveGitNamespacePermissions()
        {
            SecurityHttpClient securityClient = connection.GetClient<SecurityHttpClient>();

            Guid g = Guid.Parse("2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87"); //Git security namespace 

            IEnumerable<Microsoft.VisualStudio.Services.Security.SecurityNamespaceDescription> namespaces = securityClient.QuerySecurityNamespacesAsync(g).Result;
            Microsoft.VisualStudio.Services.Security.SecurityNamespaceDescription gitNamespace = namespaces.First();

            IEnumerable<Microsoft.VisualStudio.Services.Security.AccessControlList> acls = securityClient.QueryAccessControlListsAsync(
                 g,
                 string.Empty,
                 descriptors: null,
                 includeExtendedInfo: false,
                 recurse: true).Result;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"c:\TFSAdminAutomationData\out_GitAccessControlLists.txt"))
            {
                int counter = 0;
                file.WriteLine("token | inherit? | count of ACEs");
                file.WriteLine("------+----------+--------------");
                foreach (Microsoft.VisualStudio.Services.Security.AccessControlList acl in acls)
                {
                    counter++;
                    string[] tokenParser = acl.Token.Split('/');
                    if (tokenParser.Length != 2) //we are interested in team project level git security
                    {
                        continue;
                    }
                    file.WriteLine();
                    file.WriteLine();
                    file.WriteLine("{0} | {1} | {2} ACEs", acl.Token, acl.InheritPermissions, acl.AcesDictionary.Count());
                    file.WriteLine("Project Name: " + GetProjectName(tokenParser[1]));
                    file.WriteLine("Expanding ACL for {0} ({1} ACEs)", acl.Token, acl.AcesDictionary.Count());
                    // get the details for Git permissions 
                    Dictionary<int, string> permission = GetGitPermissionNames();
                    // use the Git permissions data to expand the ACL 
                    foreach (var kvp in acl.AcesDictionary)
                    {
                        // in the key-value pair, Key is an identity and Value is an ACE (access control entry) 
                        // allow and deny are bit flags indicating which permissions are allowed/denied 
                        string identity = kvp.Key.Identifier.ToString();
                        file.WriteLine("Identity {0}", identity);
                        string identityName = GetNameFromIdentity(identity);
                        file.WriteLine("Identity Name {0}", identityName);
                        if (!identityName.EndsWith("Project Administrators"))
                        {
                            continue;
                        }
                        string allowed = GetPermissionString(kvp.Value.Allow, permission);
                        string denied = GetPermissionString(kvp.Value.Deny, permission);

                        file.WriteLine("  Allowed: {0} (value={1})", allowed, kvp.Value.Allow);
                        file.WriteLine("  Denied: {0} (value={1})", denied, kvp.Value.Deny);

                        if (allowed.Contains("4096"))
                        {
                            //Remove "remove others' locks permission from project administrators"
                            try
                            {
                                securityClient.RemovePermissionAsync(g, acl.Token, kvp.Key, 4096);
                                file.WriteLine("Removed permission");
                            }
                            catch (Exception ex)
                            {
                                file.WriteLine("Could not remove permission");
                            }
                        }
                    }
                }
            }
        }

        private static string GetNameFromIdentity(string identity)
        {
            TfsConfigurationServer tcs = new TfsConfigurationServer(new Uri(serverUrl));

            IIdentityManagementService ims = tcs.GetService<IIdentityManagementService>();

            TeamFoundationIdentity tfi = ims.ReadIdentity(IdentitySearchFactor.AccountName, "[TEAM FOUNDATION]\\Team Foundation Valid Users", MembershipQuery.Expanded, ReadIdentityOptions.None);

            TeamFoundationIdentity[] ids = ims.ReadIdentities(tfi.Members, MembershipQuery.None, ReadIdentityOptions.None);

            foreach (TeamFoundationIdentity id in ids)
            {
                if (id.Descriptor.Identifier.ToString().Equals(identity))
                {
                    return id.DisplayName;
                }

            }
            return "N/A";
        }

        private static string GetPermissionString(int bitsSet, Dictionary<int, string> bitMeanings)
        {
            List<string> permissionStrings = new List<string>();
            foreach (var kvp in bitMeanings)
            {
                if ((bitsSet & kvp.Key) == kvp.Key)
                {
                    permissionStrings.Add(kvp.Value + "(" + kvp.Key + ")");
                }
            }

            string value = string.Join(", ", permissionStrings.ToArray());
            if (string.IsNullOrEmpty(value))
            {
                return "<none>";
            }
            return value;
        }

        private static Dictionary<int, string> GetGitPermissionNames()
        {
            SecurityHttpClient securityClient = connection.GetClient<SecurityHttpClient>();

            IEnumerable<Microsoft.VisualStudio.Services.Security.SecurityNamespaceDescription> namespaces;

            Guid g = Guid.Parse("2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87");
            namespaces = securityClient.QuerySecurityNamespacesAsync(g).Result;

            Microsoft.VisualStudio.Services.Security.SecurityNamespaceDescription gitNamespace = namespaces.First();

            Dictionary<int, string> permission = new Dictionary<int, string>();
            foreach (Microsoft.VisualStudio.Services.Security.ActionDefinition actionDef in gitNamespace.Actions)
            {
                permission[actionDef.Bit] = actionDef.DisplayName;
            }

            return permission;
        }

        public static string GetProjectName(string id)
        {
            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

            IEnumerable<TeamProjectReference> projects = projectClient.GetProjects(top: 1000).Result;

            foreach (var project in projects)
            {
                try
                {
                    string projectid = project.Id.ToString();
                    if (projectid.Equals(id))
                    {
                        return project.Name.ToString();
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return "";
        }
    }
}
