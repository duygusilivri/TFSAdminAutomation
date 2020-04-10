using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net;
using System.IO;

namespace Repos
{
    class Program
    {
        public static string collectionUrl;
        public static string tfsAdminDomain;
        public static string tfsAdminUsername;
        public static string tfsAdminPassword;


        static void Main(string[] args)
        {
            collectionUrl = "Your_Collection_Url";
            tfsAdminDomain = "Your_Azure_DevOps_Server_Domain";
            tfsAdminUsername = "Your_Azure_DevOps_Server_Username";
            tfsAdminPassword = "Your_Azure_DevOps_Server_Password";

            string projectName = "Your_TeamProject_Name";
            string repoName = "Your_GitRepo_Name";
            CreateGitRepo(projectName, repoName);
        }

        public static void CreateGitRepo(string projectName, string repoName)
        {
            string url = collectionUrl + "/" + projectName + "/_apis/git/repositories/?api-version=5.0";
            string jsonContent = "{\"name\": \"" + repoName + "\"}"; 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(jsonContent);

            request.ContentLength = byteArray.Length;
            request.ContentType = @"application/json";

            request.Credentials = new NetworkCredential(tfsAdminUsername, tfsAdminPassword, tfsAdminDomain);

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            long length = 0;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    length = response.ContentLength;
                    Stream stream = response.GetResponseStream();
                    StreamReader sr = new StreamReader(stream);
                    string strsb = sr.ReadToEnd();
                    //Console.Write(strsb);
                    Console.WriteLine("Git repo created: " + projectName + ", " + repoName);
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                    Console.WriteLine(errorText);
                    Console.ReadKey();
                }
                throw;
            }
        }
    }
}
