using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;

namespace BackgroundJobs
{
    class Program
    {
        static void Main(string[] args)
        {
            QueueADSyncJob();
        }

        private static void QueueADSyncJob()
        {
            //cHANGE THE LIBRARIES UNDER THE DLL FOLDER WITH THE VERSIONS YOU COPY FROM YOUR SERVER FOLDER.
            //EG FOR 2020, C:\Program Files\Azure DevOps Server 2020\Tools
            //THEN CHANGE THE REFERENCES
            string _myUri = @"YOUR_SERVER_URL";  //REPLACE WITH YOUR SERVER URL
            Guid adSync = new Guid("544dd581-f72a-45a9-8de0-8cd3a5f29dfe");

            TfsConfigurationServer configurationServer =
                TfsConfigurationServerFactory.GetConfigurationServer(new Uri(_myUri));
            configurationServer.EnsureAuthenticated();
            ITeamFoundationJobService jobService = configurationServer.GetService<ITeamFoundationJobService>();

            int triggered = jobService.QueueJobNow(adSync, true);

            Console.WriteLine("Job Triggered");
            Console.ReadLine();
        }
    }
}
