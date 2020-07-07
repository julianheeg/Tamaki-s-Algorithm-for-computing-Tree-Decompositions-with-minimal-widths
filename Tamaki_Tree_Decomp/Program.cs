using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        static readonly string test_s2 = "Test Data\\s3_fuzix_clock_settime_clock_settime.gr";

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

#pragma warning restore CS0414

        static int workerThreads = 1;
        static int timePerInstance = 180000;
        static int startingInstance = 0;

        static void Main(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int start))
            {
                startingInstance =  start / 2;
            }

            string filepath = PACE2017(33);
            //string filepath = "Test Data\\graphs_MC2020\\clique_graphs\\track1_002.gr";
            //string filepath = "01-07-2020 14-27-50\\002-18.gr";
            // string directory = "Test Data\\graphs_MC2020\\bipartite_graphs";
            //string directory = "Test Data\\graphs_MC2020\\clique_graphs";
            string directory = "Test Data\\graphs_MC2020";
            //string directory = "Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\";

            BitSet.plusOneInString = false;
            // Graph.dumpSubgraphs = true;
            // Graph.old = false;
            // SafeSeparator.separate = false;
            // GraphReduction.reduce = false;

            date_time_string = DateTime.Now.ToString();
            date_time_string = date_time_string.Replace('.', '-').Replace(':', '-');

            /*
            Graph g = new Graph(filepath);
            SafeSeparator ss = new SafeSeparator(g);
            foreach(int i in ss.Size1Separators(new List<int>() { 1 }))
            {
                Console.WriteLine(i);
            }
            */

            // TestSpecificTreewidth(filepath, 7);

            Run(filepath, true);

            //RunAllParallel(directory);

            // TestSpecificTreewidth(filepath, 16);

            Console.Read();
        }

        public static string date_time_string;
        static Thread[] threads;
        static Timer[] timers;
        static string[] filepaths;
        static string[] currentlyRunning;
        static int started = startingInstance;
        static readonly Object timerCallbackLock = new Object();

        /// <summary>
        /// runs the algorithm for all graphs in the directory (and subdirectories) in "workerThreads" many threads.
        /// </summary>
        /// <param name="directory"></param>
        private static void RunAllParallel(string directory)
        {
            filepaths = Directory.GetFiles(directory, "*.gr", SearchOption.AllDirectories);
            threads = new Thread[workerThreads];
            timers = new Timer[workerThreads];
            currentlyRunning = new string[workerThreads];
            started = startingInstance;
            lock (timerCallbackLock)
            {
                for (int i = 0; i < workerThreads && startingInstance + i < filepaths.Length; i++)
                {
                    int copy = i;
                    threads[copy] = new Thread(() =>
                        {
                            currentlyRunning[copy] = filepaths[startingInstance + copy];
                            Run(filepaths[startingInstance + copy], false);
                            // TODO: result
                            lock (timerCallbackLock)
                            {
                                threads[copy] = null;
                            }
                            AbortThread(copy);
                        }
                    );
                    threads[i].Start();
                    timers[i] = new Timer(new TimerCallback(AbortThread), i, timePerInstance, Timeout.Infinite);
                    started++;
                }
            }
        }

        /// <summary>
        /// callback to abort a running thread after a timeout and to restart computation on a new graph instance if there are any left.
        /// </summary>
        /// <param name="state">the thread index in the array of threads (and timer index in the array of timers)</param>
        private static void AbortThread(object state)
        {
            int threadIndex = (int)state;
            lock (timerCallbackLock)
            {
                if (threads[threadIndex] != null){
                    threads[threadIndex].Abort();
                    Console.WriteLine("graph {0} timed out", currentlyRunning[threadIndex]);
                }
                if (started < filepaths.Length) {
                    int copy = started;
                    threads[threadIndex] = new Thread(() =>
                        {
                            currentlyRunning[threadIndex] = filepaths[copy];
                            Run(filepaths[copy], false);
                            // TODO: result
                            lock (timerCallbackLock)
                            {
                                threads[threadIndex] = null;
                            }
                            AbortThread(state);
                        }
                    );
                    started++;
                    threads[threadIndex].Start();
                    timers[threadIndex].Change(timePerInstance, Timeout.Infinite);
                }
            }
        }

        private static int Run(string filepath, bool print)
        {
            Graph g = new Graph(filepath);
            if (g.vertexCount > 35000)
            {
                Console.WriteLine("graph {0} has more than 35000 vertices. Skipping...", filepath);
                return -1;
            }
            Stopwatch stopwatch = new Stopwatch();
            SafeSeparator.size3SeparationStopwatch = new Stopwatch();
            SafeSeparator.size3separators = 0;
            stopwatch.Start();
            int treeWidth = Treewidth.TreeWidth(g, out PTD output, print);
            stopwatch.Stop();
            Console.WriteLine("Tree decomposition of {0} found in {1}s. Treewidth is {2}.\nFound {3} size 3 separators in {4} total", filepath, stopwatch.Elapsed, treeWidth, SafeSeparator.size3separators, SafeSeparator.size3SeparationStopwatch.Elapsed);
            if (print)
            {
                output.Print();
            }

            g = new Graph(filepath);
            output.AssertValidTreeDecomposition(new ImmutableGraph(g));
            return treeWidth;
        }


        private static void TestSpecificTreewidth(string filepath, int actualTreewidth)
        {
            Graph g = new Graph(filepath);
            bool f = Treewidth.IsTreeWidthAtMost(g, actualTreewidth - 1, out PTD output);
            g = new Graph(filepath);
            bool t = Treewidth.IsTreeWidthAtMost(g, actualTreewidth, out output);
            Console.WriteLine(f);
            Console.WriteLine(t);
            g = new Graph(filepath);
            output.AssertValidTreeDecomposition(new ImmutableGraph(g));
        }

        private static string PACE2017(int number)
        {
            return String.Format("Test Data\\ex-instances-PACE2017-public\\ex{0:D3}.gr", number);
        }
    }
}
