using System.Net.Http.Json;
using System.Net.Http.Headers;
namespace Cliethttp
{
    class token_response
    {
        public string? accessToken { get; set; }
        public string code { get; set; } = "";
        public string? message { get; set; }
    }
    class job_response
    {
        public Job? job { get; set; }
        public string code { get; set; } = "";
        public string? message { get; set; }
    }
    class result_response
    {
        public string code { get; set; } = "";
        public string? message { get; set; }

    }
    class Job
    {
        public string id { get; set; } = "";
        public string type { get; set; } = "";
        public string? strand { get; set; }
        public string? strandEncoded { get; set; }
        public string? geneEncoded { get; set; }
    }
    class Client
    {
        private const string base_url = "https://gene.lacuna.cc/";
        private string username = "";
        private string password = "";
        private string authtoken = "";
        HttpClient client = new HttpClient();
        public Client()
        {
            this.username = Environment.GetEnvironmentVariable("user") ?? "";
            this.password = Environment.GetEnvironmentVariable("pass") ?? "";
            this.authtoken = "";//get_token().Result ;
        }
        async private Task<string> getToken()
        {
            var request_json = new { username = username, password = password };
            var http_response = await (await client.PostAsJsonAsync($"{base_url}/api/users/login", request_json)).Content.ReadFromJsonAsync<token_response>();
            if (http_response is null || http_response.code == "Error" || http_response.accessToken is null)
            {
                Console.WriteLine($"{http_response?.code}");
                Console.WriteLine("Error getting the token");
                System.Environment.Exit(1);
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", http_response.accessToken);
            return http_response.accessToken;
        }
        async public Task<Job> getJob()
        {
            var request_body = new { user = username, password = password };
            var http_response = await (await client.GetAsync($"{base_url}/api/dna/jobs")).Content.ReadFromJsonAsync<job_response>();
            if (http_response is null || http_response.code == "Unauthorized")
            {
                this.authtoken = await getToken();
                http_response = await (await client.GetAsync($"{base_url}/api/dna/jobs")).Content.ReadFromJsonAsync<job_response>();
            }
            if (http_response is null || http_response.code == "Error" || http_response.job is null)
            {
                Console.WriteLine("Error getting the job");
                System.Environment.Exit(1);
            }
            return http_response.job;
        }
        async public Task<string> sendTask(string id, Object body, string endpoint)
        {
            var http_response = await (await client.PostAsJsonAsync($"{base_url}/api/dna/jobs/{id}/{endpoint}", body)).Content.ReadFromJsonAsync<result_response>();
            if (http_response is null || http_response.code == "Unauthorized")
            {
                this.authtoken = await this.getToken();
                http_response = await (await client.PostAsJsonAsync($"{base_url}/api/dna/jobs/{id}/{endpoint}", body)).Content.ReadFromJsonAsync<result_response>();
            }
            if (http_response is null)
            {
                Console.WriteLine($"erro sending job {id}");
                System.Environment.Exit(1);
            }
            if (http_response.code == "Success")
            {
                Console.WriteLine($"success sending job {id} {endpoint}");
                return "Success";
            }

            Console.WriteLine($"erro sending job {id} code {http_response.code}");
            System.Environment.Exit(1);
            return "Error";
        }
    }
}