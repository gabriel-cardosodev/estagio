using Cliethttp;
namespace DnaSolver
{
    class Solver_Dna
    {
        Client client;
        public Solver_Dna(Client client){
            this.client = client;
        }
        private String encodeDna(string dna)
        {
            if (dna.Length % 4 != 0)
            {
                Console.WriteLine($"Impossible to encode dna {dna}");
                System.Environment.Exit(1);
            }
            Byte[] DnaBytes = new byte[dna.Length / 4];
            Byte  DnaByte;
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
                DnaBytes[i] = DnaByte;
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
                    //get the bits of current part of dna
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

        public void main_loop()
        {
            Job job;
            String code;
            while (true)
            {
                job = client.getJob().Result;
                switch (job.type)
                {
                    case "DecodeStrand":
                        if (job.strandEncoded is null)
                        {
                            Console.WriteLine($"erro getting job {job.id} {job.type}");
                            System.Environment.Exit(1);
                        }
                        code = client.sendTask(job.id, new { strand = decodeDna(job.strandEncoded) }, "decode").Result;
                        break;
                    case "EncodeStrand":
                        if (job.strand is null)
                        {
                            Console.WriteLine($"erro getting job {job.id} {job.type}");
                            System.Environment.Exit(1);
                        }
                        code = client.sendTask(job.id, new { strandEncoded = encodeDna(job.strand) }, "encode").Result;
                        break;
                    case "CheckGene":
                        if (job.strandEncoded is null || job.geneEncoded is null)
                        {
                            Console.WriteLine($"erro getting job {job.id} {job.type}");
                            System.Environment.Exit(1);
                        }
                        code = client.sendTask(job.id, new { isActivated = checkDna(job.strandEncoded, job.geneEncoded) }, "gene").Result;
                        break;
                }
            }
        }
    }
}