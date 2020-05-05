using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    class Program
    {
        static readonly string test_t0 = "Test Data\\tamaki test instances\\empty.gr";                 //
        static readonly string test_t1 = "Test Data\\tamaki test instances\\single-vertex.gr";         // 1
        static readonly string test_t2 = "Test Data\\tamaki test instances\\two-vertices.gr";          // 1 2
        static readonly string test_t3 = "Test Data\\tamaki test instances\\single-edge.gr";           // 1-2
        static readonly string test_t4 = "Test Data\\tamaki test instances\\wedge.gr";                 // 1-2-3
        static readonly string test_t5 = "Test Data\\tamaki test instances\\four_in_a_line.gr";        // 1-2-3-4
        static readonly string test_t6 = "Test Data\\tamaki test instances\\gr-only.gr";               // 1-2-3-4-5
        static readonly string test_t7 = "Test Data\\tamaki test instances\\p-num-vertices-larger.gr"; // 1-2-3-4-5 6

        // -------------------------------------

        static readonly string test_a0 = "Test Data\\test1.gr";
        static readonly string test_a1 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_clock_settime_clock_settime.gr";

        static readonly string test_a2 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_devio_bfind.gr";
        static readonly string td_o_a2 = "Test Data\\test decomps\\fuzix_devio_bfind.td";

        static readonly string test_a3 = "Test Data\\test2.gr";
        static readonly string test_a4 = "Test Data\\test3.gr";


        static readonly string test_s0 = "Test Data\\s0_fuzix_clock_settime_clock_settime.gr";
        static readonly string test_s1 = "Test Data\\s1_fuzix_clock_settime_clock_settime.gr";
        static readonly string test_s3 = "Test Data\\s3_fuzix_clock_settime_clock_settime.gr";

        static readonly string test_e0 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_filesys_getinode.gr";
        static readonly string test_e1 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_devf_fd_transfer.gr";
        static readonly string test_e2 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\BalancedTree_3,5.gr";
        static readonly string test_e3 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\contiki_ifft_ifft.gr";
        static readonly string test_e4 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\contiki_powertrace_powertrace_print.gr";

        static readonly string test_m0 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\DesarguesGraph.gr";
        static readonly string test_m1 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\FlowerSnark.gr";
        static readonly string test_m2 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\GeneralizedPetersenGraph_10_4.gr";
        static readonly string test_m3 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\HoffmanGraph.gr";
        static readonly string test_m4 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\NauruGraph.gr";

        static void Main(string[] args)
        {
            string filepath = test_m4;
            Graph g = new Graph(filepath);
            Graph debug = new Graph(filepath);

            
            g.TreeWidth(0, out PTD output);

            output.Print();
            Debug.Assert(debug.IsValidTreeDecomposition(output));
            

            /*
            PTD tamaki = PTD.ImportPTD("Test Data\\test decomps\\Tamaki_NauruGraph.td");
            Debug.Assert(debug.IsValidTreeDecomposition(tamaki));
            tamaki.Print();
            */

            bool testOwn = false;
            if (testOwn)
            {
                Console.WriteLine("\n--------own decomposition--------\n");
                PTD own = PTD.ImportPTD(td_o_a2);
                Debug.Assert(g.IsValidTreeDecomposition(own));
                own.Print();
            }

            Console.Read();
        }
    }
}
