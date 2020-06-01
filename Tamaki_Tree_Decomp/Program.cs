#define statistics
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
#pragma warning disable CS0414
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
        static readonly string test_a5 = "Test Data\\small_cycle.gr";
        static readonly string test_a6 = "Test Data\\double_cycle.gr";
        static readonly string test_a7 = "Test Data\\double_cycle_bridge.gr";
        static readonly string test_a8 = "Test Data\\cycle_cycle.gr";
        static readonly string test_a9 = "Test Data\\cycle_line.gr";
        static readonly string test_tr0 = "Test Data\\tree.gr";

        static readonly string test_ss0 = "Test Data\\cliques_4_3_4.gr";


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

        static readonly string test_h0 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\ClebschGraph.gr";
        static readonly string test_h1 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\contiki_dhcpc_handle_dhcp.gr";
        static readonly string test_h2 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\DoubleStarSnark.gr";
        static readonly string test_h3 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\fuzix_vfscanf_vfscanf.gr";
        static readonly string test_h4 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\McGeeGraph.gr";

        static readonly string test_r0 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_10_1.gr";
        static readonly string test_r1 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_20_0.gr";
        static readonly string test_r2 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_30_0.gr";
        static readonly string test_r3 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_40_0.gr";
        static readonly string test_r4 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_40_10_0.gr";
        static readonly string test_r5 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_40_10_1.gr";
        static readonly string test_r6 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_50_10_0.gr";
        static readonly string test_r7 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_50_10_1.gr";
        static readonly string test_r8 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_70_10_0.gr";
        static readonly string test_r9 = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_80_10_1.gr";

        static readonly string pace17_001 = "Test Data\\ex-instances-PACE2017-public\\ex001.gr";
        static readonly string pace17_193 = "Test Data\\ex-instances-PACE2017-public\\ex193.gr";

        static readonly string pace2017_ex193_004 = "Test Data\\ex193 safe sep components\\004 - 10_T9.gr";
        static readonly string pace2017_ex193_006 = "Test Data\\ex193 safe sep components\\006 - 10_T9.gr";
        static readonly string pace2017_ex193_008 = "Test Data\\ex193 safe sep components\\008 - 10_T9.gr";
        static readonly string pace2017_ex193_010 = "Test Data\\ex193 safe sep components\\010 - 10_T9.gr";
        static readonly string pace2017_ex193_012 = "Test Data\\ex193 safe sep components\\012 - 10_T9.gr";
        static readonly string pace2017_ex193_014 = "Test Data\\ex193 safe sep components\\014 - 10_T9.gr";
        static readonly string pace2017_ex193_016 = "Test Data\\ex193 safe sep components\\016 - 10_T9.gr";
        static readonly string pace2017_ex193_018 = "Test Data\\ex193 safe sep components\\018 - 10_T9.gr";
        static readonly string pace2017_ex193_020 = "Test Data\\ex193 safe sep components\\020 - 10_T9.gr";
        static readonly string pace2017_ex193_022 = "Test Data\\ex193 safe sep components\\022 - 10_T9.gr";
        static readonly string pace2017_ex193_024 = "Test Data\\ex193 safe sep components\\024 - 10_T9.gr";
        static readonly string pace2017_ex193_026 = "Test Data\\ex193 safe sep components\\026 - 10_T9.gr";
        static readonly string pace2017_ex193_029 = "Test Data\\ex193 safe sep components\\029 - 11_T10.gr";
        static readonly string pace2017_ex193_031 = "Test Data\\ex193 safe sep components\\031 - 11_T10.gr";
        static readonly string pace2017_ex193_033 = "Test Data\\ex193 safe sep components\\033 - 11_T10.gr";
        static readonly string pace2017_ex193_035 = "Test Data\\ex193 safe sep components\\035 - 11_T10.gr";
        static readonly string pace2017_ex193_037 = "Test Data\\ex193 safe sep components\\037 - 11_T10.gr";
        static readonly string pace2017_ex193_039 = "Test Data\\ex193 safe sep components\\039 - 11_T10.gr";
        static readonly string pace2017_ex193_041 = "Test Data\\ex193 safe sep components\\041 - 11_T10.gr";
        static readonly string pace2017_ex193_043 = "Test Data\\ex193 safe sep components\\043 - 11_T10.gr";
        static readonly string pace2017_ex193_044 = "Test Data\\ex193 safe sep components\\044 - 12_T10.gr";
        static readonly string pace2017_ex193_046 = "Test Data\\ex193 safe sep components\\046 - 12_T10.gr";
        static readonly string pace2017_ex193_048 = "Test Data\\ex193 safe sep components\\048 - 12_T10.gr";
        static readonly string pace2017_ex193_050 = "Test Data\\ex193 safe sep components\\050 - 12_T10.gr";
        static readonly string pace2017_ex193_052 = "Test Data\\ex193 safe sep components\\052 - 12_T10.gr";
        static readonly string pace2017_ex193_054 = "Test Data\\ex193 safe sep components\\054 - 12_T10.gr";
        static readonly string pace2017_ex193_056 = "Test Data\\ex193 safe sep components\\056 - 12_T10.gr";
        static readonly string pace2017_ex193_058 = "Test Data\\ex193 safe sep components\\058 - 12_T10.gr";
        static readonly string pace2017_ex193_060 = "Test Data\\ex193 safe sep components\\060 - 12_T10.gr";
        static readonly string pace2017_ex193_062 = "Test Data\\ex193 safe sep components\\062 - 12_T10.gr";


#pragma warning restore CS0414

        public static string date_time_string;

        static void Main(string[] args)
        {
            string filepath = PACE2017(5);
            // string filepath = test_e0;

            BitSet.plusOneInString = false;
            // SafeSeparator.separate = false; // ###############################################################################################
            // GraphReduction.reduce = false;  // ###############################################################################################

            date_time_string = DateTime.Now.ToString();
            date_time_string = date_time_string.Replace('.', '-').Replace(':', '-');
            Console.WriteLine(date_time_string);
            
            
            Graph g = new Graph(filepath);
            Graph debug = new Graph(filepath);

            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            g.TreeWidth(0, out PTD output);

            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0}s", stopwatch.Elapsed);

            output.Print();
            Debug.Assert(debug.IsValidTreeDecomposition(output));

            if (!debug.IsValidTreeDecomposition(output))
            {
                Console.WriteLine("######################## tree decomposition is invalid #######################");
            }
            
            
            // TEST
            /*
            bool f = g.IsTreeWidthAtMost(9, out PTD output);
            bool t = g.IsTreeWidthAtMost(10, out output);
            Console.WriteLine(f);
            Console.WriteLine(t);
            if (!debug.IsValidTreeDecomposition(output))
            {
                Console.WriteLine("######################## tree decomposition is invalid #######################");
            }
            else
            {
                Console.WriteLine("tree decomposition is valid");
            }
            */
            
            // TEST END

            /*
            int counter = 0;
            Graph graph = new Graph(test_m4);
            foreach (IEnumerable<int> subset in SubSetsOf(Enumerable.Range(0, graph.vertexCount).ToArray()))
            {
                BitSet test = new BitSet(graph.vertexCount, subset.ToArray());
                graph.ComponentsAndNeighbors(test);
                counter++;
                if (counter % 100000 == 0)
                {
                    Console.WriteLine(counter);
                }
            }
            Console.WriteLine("Test finished");
            */

            /*
            string testFile1 = "Test Data\\test1.gr";
            Graph graph = new Graph(testFile1);
            
            BitSet b = new BitSet(6, new int[] { 0, 1 ,2 });
            
            Debug.Assert(graph.IsPotMaxClique(b));
            */

            /*
            SafeSeparator ss = new SafeSeparator(g);
            foreach(BitSet candidate in ss.CandidateSeparators())
            {
                Console.WriteLine("currently testing {0}", candidate);
                if (ss.IsSafeSeparator(candidate))
                {
                    Console.WriteLine("{0} is a safe separator", candidate);
                    //Console.Read();
                }
            }
            Console.WriteLine("test for safe separators concluded");
            */

            /*
            PTD tamaki = PTD.ImportPTD("Test Data\\test decomps\\Tamaki_NauruGraph.td");
            Debug.Assert(debug.IsValidTreeDecomposition(tamaki));
            tamaki.Print();
            */

            /*
            bool testOwn = false;
            if (testOwn)
            {
                Console.WriteLine("\n--------own decomposition--------\n");
                PTD own = PTD.ImportPTD(td_o_a2);
                Debug.Assert(g.IsValidTreeDecomposition(own));
                own.Print();
            }
            */

            Console.Read();
        }

        private static List<T[]> CreateSubsets<T>(T[] originalArray)
        {
            List<T[]> subsets = new List<T[]>();

            for (int i = 0; i < originalArray.Length; i++)
            {
                int subsetCount = subsets.Count;
                subsets.Add(new T[] { originalArray[i] });

                for (int j = 0; j < subsetCount; j++)
                {
                    T[] newSubset = new T[subsets[j].Length + 1];
                    subsets[j].CopyTo(newSubset, 0);
                    newSubset[newSubset.Length - 1] = originalArray[i];
                    subsets.Add(newSubset);
                }
            }

            return subsets;
        }

        private static IEnumerable<IEnumerable<T>> SubSetsOf<T>(IEnumerable<T> source)
        {
            if (!source.Any())
                return Enumerable.Repeat(Enumerable.Empty<T>(), 1);

            var element = source.Take(1);

            var haveNots = SubSetsOf(source.Skip(1));
            var haves = haveNots.Select(set => element.Concat(set));

            return haves.Concat(haveNots);
        }

        private static string PACE2017(int number)
        {
            return String.Format("Test Data\\ex-instances-PACE2017-public\\ex{0:D3}.gr", number);
        }
    }
}
