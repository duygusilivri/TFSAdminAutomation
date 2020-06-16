using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.Client;

namespace TaskAgent
{
    class Program
    {
        static string projectName = "Your_Project_Name";
        static string collectionUrl = "Your_Organization_Url";

        static void Main(string[] args)
        {
            UpdateVariableGroup();
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
            taskagentClient.UpdateVariableGroupAsync(projectName, 1, varGroupParam).GetAwaiter().GetResult();

        }
    }
}
