namespace SendFilesModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using System.Net.Http;
    using System.Collections.Generic;

    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static string POST_URL = Environment.GetEnvironmentVariable("POST_URL");
        private const string sourceFolder = "FilesToUpload";
        private const string localFileName = "helloWorld.txt";

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Send file every 10 seconds
        /// </summary>
        static async Task Init()
        {
            Console.WriteLine("POST_URL: {0}", POST_URL);
            string localPath = System.IO.Directory.GetCurrentDirectory();
            string sourceFile = Path.Combine(new string[]{localPath, sourceFolder, localFileName});
            while(true)
            {
                Thread.Sleep(10000);
                await SendFile(sourceFile);
            }
        }

        /// <summary>
        /// Send file through HTTP POST request
        /// </summary>
        static async Task SendFile(string fileName)
        {
            Console.WriteLine("Sending file: {0}", fileName);

            string fileContent;
            if (!File.Exists(fileName))
            {
                Console.WriteLine("File does not exist");
                return;
            }

            fileContent = await File.ReadAllTextAsync(fileName);
            var values = new Dictionary<string, string>
            {
                { fileName , fileContent }
            };
            try
            {
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync(POST_URL, content);
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: {0}", ex);
            }
            return;
        }

        
    }
}
