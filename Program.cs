using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
class token_response{
    public string? accessToken { get; set; }
    public string code { get; set; } = "";
    public string? message { get; set; }
}
class job_response{
    public Job? job { get; set; }
    public string code { get; set; } = "";
    public string? message { get; set; }
}
class result_response{
    public string code { get; set; } = "";
    public string? message { get; set; }

}
class Job{
    public string id { get; set; } ="";
    public string type { get; set; } = "";
    public string? strand { get; set; }
    public string? strandEncoded { get; set; }
    public string? geneEncoded { get; set; }
}
class Solver_Dna
{
    //private string token;
    private const string base_url = "https://gene.lacuna.cc/";
    private string username = "";
    private string password = "";
    private string authtoken = "";
    HttpClient client = new HttpClient();
    public Solver_Dna()
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
            Console.WriteLine($"{http_response.code}");
            Console.WriteLine("Error getting the token");
            System.Environment.Exit(1);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", http_response.accessToken);
        return http_response.accessToken;
    }
    async private Task<Job> getJob()
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
    private String encodeDna(string dna)
    {
        if (dna.Length % 4 != 0)
        {
            Console.WriteLine($"Impossible to encode dna {dna}");
            System.Environment.Exit(1);
        }
        Byte[] DnaBytes = new byte[dna.Length / 4];
        Int16 DnaByte;
        for (int i = 0; i < dna.Length / 4; i++)
        {
            DnaByte = 0;
            for (int j = 0; j < 4; j++)
            {
                DnaByte *= 4;
                switch (dna[i * 4 + j])
                {
                    case 'A':
                        break;
                    case 'C':
                        DnaByte += 1;
                        break;
                    case 'T':
                        DnaByte += 3;
                        break;
                    case 'G':
                        DnaByte += 2;
                        break;
                    default:
                        Console.WriteLine($"Invalid dna sequence {dna}");
                        System.Environment.Exit(1);
                        break;
                }
            }
            //Console.WriteLine(tempdna);
            DnaBytes[i] = (byte)DnaByte;
        }
        return System.Convert.ToBase64String(DnaBytes);
    }
    private string decodeDna(string dna)
    {
        byte[] dnabytes = System.Convert.FromBase64String(dna);
        char[] dnaDecoded = new char[dnabytes.Length * 4];
        byte DnaByte;
        for (int i = 0; i < dnabytes.Length; i++)
        {
            DnaByte = dnabytes[i];
            for (int j = 0; j < 4; j++)
            {
                switch ((DnaByte >> ((3 - j) * 2)) & 3)
                {
                    case 0:
                        dnaDecoded[i * 4 + j] = 'A';
                        break;
                    case 1:
                        dnaDecoded[i * 4 + j] = 'C';
                        break;
                    case 2:
                        dnaDecoded[i * 4 + j] = 'G';
                        break;
                    case 3:
                        dnaDecoded[i * 4 + j] = 'T';
                        break;
                }
            }
        }
        return String.Join("", dnaDecoded);
    }
    private string getDnaTemplate(string dna)
    {
        char[] dnaTemplate = new char[dna.Length];
        for (int i = 0; i < dna.Length; i++)
        {
            switch (dna[i])
            {
                case 'A':
                    dnaTemplate[i] = 'T';
                    break;
                case 'T':
                    dnaTemplate[i] = 'A';
                    break;
                case 'C':
                    dnaTemplate[i] = 'G';
                    break;
                case 'G':
                    dnaTemplate[i] = 'C';
                    break;
            }
        }
        return String.Join("", dnaTemplate);
    }
    private bool checkDna(string encodedDna, string EncodedGene)
    {
        var dna = decodeDna(encodedDna);
        var gene = decodeDna(EncodedGene);
        if (gene.Length == 0)
        {
            return true;
        }
        if (dna.Length == 0)
        {
            return false;
        }
        if (dna[0] != 'C' || dna[1] != 'A' || dna[2] != 'T')
        {
            dna = getDnaTemplate(dna);
            if (dna[0] != 'C' || dna[1] != 'A' || dna[2] != 'T')
            {
                Console.WriteLine($"Invalid dna sequence {dna}");
                System.Environment.Exit(1);
            }
        }

        var minActive = (gene.Length / 2) + 1;
        for (int i = 0; i < dna.Length - minActive + 1; i++)
        {
            for (int j = 0; j < gene.Length - minActive + 1; j++)
            {
                if (dna.Substring(i, minActive).Equals(gene.Substring(j, minActive)))
                {
                    return true;
                }
            }
        }
        return false;
    }
    async Task<string> sendTask(string id, Object body, string endpoint)
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
            Console.WriteLine($"success sending job {id}");
            return "Success";
        }

        Console.WriteLine($"erro sending job {id} code {http_response.code}");
        System.Environment.Exit(1);
        return "Error";
    }
    public void main_loop()
    {
        Job job;
        String code;
        while (true)
        {
            job = getJob().Result;
            switch (job.type)
            {
                case "DecodeStrand":
                    if (job.strandEncoded is null)
                    {
                        Console.WriteLine($"erro getting job {job.id} {job.type}");
                        System.Environment.Exit(1);
                    }
                    code = sendTask(job.id, new { strand = decodeDna(job.strandEncoded) }, "decode").Result;
                    break;
                case "EncodeStrand":
                    if (job.strand is null)
                    {
                        Console.WriteLine($"erro getting job {job.id} {job.type}");
                        System.Environment.Exit(1);
                    }
                    code = sendTask(job.id, new { strandEncoded = encodeDna(job.strand) }, "encode").Result;
                    break;
                case "CheckGene":
                    if (job.strandEncoded is null || job.geneEncoded is null)
                    {
                        Console.WriteLine($"erro getting job {job.id} {job.type}");
                        System.Environment.Exit(1);
                    }
                    code = sendTask(job.id, new { isActivated = checkDna(job.strandEncoded, job.geneEncoded) }, "gene").Result;
                    break;
            }
        }
    }
}
class main{
    public static void Main(){
        var solver = new Solver_Dna();
        solver.main_loop();
    }

}
