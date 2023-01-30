using DnaSolver;
using Cliethttp;
class main{
    public static void Main(){
        var client = new Client();
        var solver = new Solver_Dna(client);
        solver.main_loop();
    }

}
