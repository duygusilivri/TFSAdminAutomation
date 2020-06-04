using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Git
{
    class Program
    {
        static string personalAccessToken = "Your_PAT";
        static string projectUrl = "Your_Project_Url";
        static string repoId = "Your_Repo_Id";


        static void Main(string[] args)
        {
            GetActivePRCountAsync();
        }

        public static async Task GetActivePRCountAsync()
        {
            

            //encode your personal access token                   
            string credentials = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", personalAccessToken)));

            //use the httpclient
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(projectUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                //connect to the REST endpoint            
                HttpResponseMessage response = client.GetAsync("_apis/git/repositories/"+repoId+"/pullrequests?searchCriteria.targetRefName=refs/heads/master&searchCriteria.status=active&api-version=5.1").Result;

                if (response.IsSuccessStatusCode)
                {
                    //var value = response.Content.ReadAsStringAsync().Result;

                    string responseBody = await response.Content.ReadAsStringAsync();

                    string count = responseBody.Substring(responseBody.IndexOf("count") + 7);
                    count = count.TrimEnd('}');
                    int prCount = Int16.Parse(count);
                }
            }
        }

    }
}
