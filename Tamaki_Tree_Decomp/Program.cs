using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Tamaki_Tree_Decomp.Data_Structures;
using Tamaki_Tree_Decomp.Safe_Separators;

namespace Tamaki_Tree_Decomp
{
    public class Program
    {
#pragma warning disable CS0414
        static readonly string test_t0 = "..\\..\\Test Data\\tamaki test instances\\empty.gr";                 //
        static readonly string test_t1 = "..\\..\\Test Data\\tamaki test instances\\single-vertex.gr";         // 1
        static readonly string test_t2 = "..\\..\\Test Data\\tamaki test instances\\two-vertices.gr";          // 1 2
        static readonly string test_t3 = "..\\..\\Test Data\\tamaki test instances\\single-edge.gr";           // 1-2
        static readonly string test_t4 = "..\\..\\Test Data\\tamaki test instances\\wedge.gr";                 // 1-2-3
        static readonly string test_t5 = "..\\..\\Test Data\\tamaki test instances\\four_in_a_line.gr";        // 1-2-3-4
        static readonly string test_t6 = "..\\..\\Test Data\\tamaki test instances\\gr-only.gr";               // 1-2-3-4-5
        static readonly string test_t7 = "..\\..\\Test Data\\tamaki test instances\\p-num-vertices-larger.gr"; // 1-2-3-4-5 6

        // -------------------------------------

        static readonly string test_a0 = "..\\..\\Test Data\\test1.gr";
        static readonly string test_a1 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_clock_settime_clock_settime.gr";

        static readonly string test_a2 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_devio_bfind.gr";
        static readonly string td_o_a2 = "..\\..\\Test Data\\test decomps\\fuzix_devio_bfind.td";

        static readonly string test_a3 = "..\\..\\Test Data\\test2.gr";
        static readonly string test_a4 = "..\\..\\Test Data\\test3.gr";
        static readonly string test_a5 = "..\\..\\Test Data\\small_cycle.gr";
        static readonly string test_a6 = "..\\..\\Test Data\\double_cycle.gr";
        static readonly string test_a7 = "..\\..\\Test Data\\double_cycle_bridge.gr";
        static readonly string test_a8 = "..\\..\\Test Data\\cycle_cycle.gr";
        static readonly string test_a9 = "..\\..\\Test Data\\cycle_line.gr";
        static readonly string test_tr0 = "..\\..\\Test Data\\tree.gr";

        static readonly string test_ss0 = "..\\..\\Test Data\\cliques_4_3_4.gr";


        static readonly string test_s0 = "..\\..\\Test Data\\s0_fuzix_clock_settime_clock_settime.gr";
        static readonly string test_s1 = "..\\..\\Test Data\\s1_fuzix_clock_settime_clock_settime.gr";
        static readonly string test_s2 = "..\\..\\Test Data\\s3_fuzix_clock_settime_clock_settime.gr";

        static readonly string test_e0 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_filesys_getinode.gr";
        static readonly string test_e1 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\fuzix_devf_fd_transfer.gr";
        static readonly string test_e2 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\BalancedTree_3,5.gr";
        static readonly string test_e3 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\contiki_ifft_ifft.gr";
        static readonly string test_e4 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\easy\\contiki_powertrace_powertrace_print.gr";

        static readonly string test_m0 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\DesarguesGraph.gr";
        static readonly string test_m1 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\FlowerSnark.gr";
        static readonly string test_m2 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\GeneralizedPetersenGraph_10_4.gr";
        static readonly string test_m3 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\HoffmanGraph.gr";
        static readonly string test_m4 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\medium\\NauruGraph.gr";

        static readonly string test_h0 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\ClebschGraph.gr";
        static readonly string test_h1 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\contiki_dhcpc_handle_dhcp.gr";
        static readonly string test_h2 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\DoubleStarSnark.gr";
        static readonly string test_h3 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\fuzix_vfscanf_vfscanf.gr";
        static readonly string test_h4 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\McGeeGraph.gr";

        static readonly string test_r0 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_10_1.gr";
        static readonly string test_r1 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_20_0.gr";
        static readonly string test_r2 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_30_0.gr";
        static readonly string test_r3 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\GNP_20_40_0.gr";
        static readonly string test_r4 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_40_10_0.gr";
        static readonly string test_r5 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_40_10_1.gr";
        static readonly string test_r6 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_50_10_0.gr";
        static readonly string test_r7 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_50_10_1.gr";
        static readonly string test_r8 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_70_10_0.gr";
        static readonly string test_r9 = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\random\\RKT_20_80_10_1.gr";

        static readonly string test_n0 = "..\\..\\Test Data\\normalised_0.gr";
        static readonly string test_n1 = "..\\..\\Test Data\\normalised_1.gr";

#pragma warning restore CS0414

        static int workerThreads = 1;
        static int timePerInstance = 2000000;
        static int startingInstance = 0;

        static void Main(string[] args)
        {
            // used for naming files when something needs to be written onto disk
            date_time_string = DateTime.Now.ToString();
            date_time_string = date_time_string.Replace('.', '-').Replace(':', '-');

            //string filepath = null;
            //string filepath = test_a0;
            string filepath = PACE2017(191);
            //string filepath = "..\\..\\Test Data\\Debug\\127-22.gr";
            //string filepath = "..\\..\\Test Data\\graphs_MC2020\\clique_graphs\\track1_034.gr";
            //string filepath = "..\\..\\Test Data\\graphs_MC2020\\bipartite_graphs\\track1_014.gr";
            //string filepath = Console.ReadLine();

            /*
            if (args.Length > 0)
            {
                filepath = args[0];
            }
            */
            

            //string directory = "..\\..\\Test Data\\graphs_MC2020\\bipartite_graphs";
            //string directory = "..\\..\\Test Data\\graphs_MC2020\\clique_graphs";
            //string directory = "..\\..\\Test Data\\graphs_MC2020";
            //string directory = "..\\..\\Test Data\\pace16-tw-instances-20160307\\tw-exact\\hard\\";
            string directory = "..\\..\\Test Data\\ex-instances-PACE2017-public\\";
            int directoryStartIndex = 0;
            int directoryEndIndex = 200;

            BitSet.plusOneInString = false;
            Graph.dumpSubgraphs = true;   // dumps graphs only in DEBUG mode!
            //SafeSeparator.separate = false;
            //GraphReduction.reduce = false;
            Treewidth.completeHeuristically = true;
            Treewidth.heuristicCompletionFrequency = 20;
            Treewidth.heuristicInletMax = 1f;
            Treewidth.heuristicInletMin = 0f;
            Treewidth.maxTestsPerGraphAndK = 20;
            Treewidth.heuristic = Heuristics.Heuristic.min_degree;

            Treewidth.moreThan2ComponentsOptimization = true;
            Treewidth.keepOnlyPTDsWithLargerInletIfSameOutlet = true;  // not yet verified if implementation is correct
            PTD.testIfAddingOneVertexToBagFormsPMC = false;
            ImmutableGraph.cachePMC = false;
            LowerBound.calculateLowerBound = true;

            
            //(filepath, true);
            //Run(filepath, false, false);
            //RunAll_Parallel(directory);
            //RunAll_Sequential(directory, directoryStartIndex, directoryEndIndex);
            EvaluateParameterImpact(directory, ref PTD.testIfAddingOneVertexToBagFormsPMC, directoryStartIndex, directoryEndIndex);

            //Treewidth.PrintStats_kMinus(12);
            Console.WriteLine("total time for lower bound calculation: {0}", LowerBound.stopWatch.Elapsed);
            Console.WriteLine("articulation point stopwatch: {0}, only neighbor stopwatch: {1}", PTD.articulationPointCandidatesStopwatch.Elapsed, PTD.onlyNeighborStopwatch.Elapsed);
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
        private static void RunAll_Parallel(string directory)
        {
            filepaths = Directory.GetFiles(directory, "*.gr", SearchOption.AllDirectories);

            // if checking PACE 2017, exclude ex003.gr because it takes forever
            if (directory.EndsWith("ex-instances-PACE2017-public\\"))
            {
                string[] filepaths_temp = new string[99];
                filepaths_temp[0] = filepaths[0];
                for (int i = 1; i < 99; i++)
                {
                    filepaths_temp[i] = filepaths[i + 1];
                }
                filepaths = filepaths_temp;
            }

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
                            try
                            {
                                Run(filepaths[startingInstance + copy], false);
                            }
                            catch(OutOfMemoryException)
                            {
                                Console.WriteLine("program ran out of memory while executing algorithm for {0}.", currentlyRunning[copy]);
                            }
                            lock (timerCallbackLock)
                            {
                                threads[copy] = null;
                            }
                            AbortThread(copy);
                        }
                    );
                    threads[copy].Start();
                    timers[copy] = new Timer(new TimerCallback(AbortThread), copy, timePerInstance, Timeout.Infinite);
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
                            try
                            {
                                Run(filepaths[copy], false);
                            }
                            catch (OutOfMemoryException)
                            {
                                Console.WriteLine("program ran out of memory while executing algorithm for {0}.", currentlyRunning[threadIndex]);
                            }
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

        private static void RunAll_Sequential(string directory, int start=0, int end=int.MaxValue)
        {
            Stopwatch timer = new Stopwatch();
            int counter = -1;
            timer.Start();
            foreach(String filepath in Directory.GetFiles(directory, "*.gr", SearchOption.AllDirectories))
            {
                counter++;
                if (counter < start / 2 || counter > end / 2)
                {
                    continue;
                }
                Run(filepath, false);
            }
            timer.Stop();
            Console.WriteLine("\n-------------------------------------------------------------------------------------\n\ntotal time for directory: " + timer.Elapsed.ToString());
        }

        private static void EvaluateParameterImpact(string directory, ref bool parameter, int start = 0, int end = int.MaxValue)
        {
            int counter = -1;
            Stopwatch timer = new Stopwatch();
            Stopwatch timer1 = new Stopwatch();
            Stopwatch timer2 = new Stopwatch();
            TimeSpan totalDifference = new TimeSpan(0);
            TimeSpan highestDifference = new TimeSpan(int.MinValue);
            TimeSpan lowestDifference = new TimeSpan(int.MaxValue);
            float highestRelativeDifference = 0;
            float lowestRelativeDifference = float.MaxValue;

            timer.Start();
            foreach (String filepath in Directory.GetFiles(directory, "*.gr", SearchOption.AllDirectories))
            {
                counter++;
                if (counter < start / 2 || counter > end / 2)
                {
                    continue;
                }

                // run without parameter active
                parameter = false;
                timer1.Restart();
                Run(filepath, false, false);
                timer1.Stop();

                // run with parameter active
                parameter = true;
                timer2.Restart();
                Run(filepath, false, false);
                timer2.Stop();

                // calculate statistics
                TimeSpan difference = timer2.Elapsed - timer1.Elapsed;
                float relativeDifference = (float)timer2.ElapsedMilliseconds / timer1.ElapsedMilliseconds;
                totalDifference += difference;
                if (difference > highestDifference)
                {
                    highestDifference = difference;
                }
                if (difference < lowestDifference)
                {
                    lowestDifference = difference;
                }
                if (timer1.ElapsedMilliseconds > 5000 || timer2.ElapsedMilliseconds > 5000)
                {
                    if (relativeDifference > highestRelativeDifference)
                    {
                        highestRelativeDifference = relativeDifference;
                    }
                    if (relativeDifference < lowestRelativeDifference)
                    {
                        lowestRelativeDifference = relativeDifference;
                    }
                }


                //Console.WriteLine("----------------------------------------------------------------------------------------");
                Console.WriteLine("{0}: difference relative: {1:F2}, difference total: {2}, total time: {3}", filepath.Substring(filepath.Length - 8), relativeDifference, difference, timer1.Elapsed + timer2.Elapsed);
                //Console.WriteLine("----------------------------------------------------------------------------------------");
            }
            timer.Stop();
            Console.WriteLine("\n-------------------------------------------------------------------------------------\n" +
                "\ntotal time for directory: {0}" +
                "\ntotal difference: {1}" +
                "\nhighest difference: {2}" +
                "\nlowest difference: {3}" +
                "\nhighest relative difference above 10s: {4:F2}" +
                "\nlowest relative difference above 10s:  {5:F2}",
                timer.Elapsed, totalDifference, highestDifference, lowestDifference, highestRelativeDifference, lowestRelativeDifference);
        }

        private static int Run(string filepath, bool printTD=true, bool printStats=true)
        {
            Graph g = new Graph(filepath);
            
            Stopwatch stopwatch = new Stopwatch();
            SafeSeparator.size3SeparationStopwatch = new Stopwatch();
            SafeSeparator.size3separators = 0;
            SafeSeparator.cliqueSeparatorStopwatch = new Stopwatch();
            SafeSeparator.cliqueSeparators = 0;
            SafeSeparator.almostCliqueSeparatorStopwatch = new Stopwatch();
            SafeSeparator.almostCliqueSeparators = 0;
            stopwatch.Start();
            int treeWidth = Treewidth.TreeWidth(g, out PTD output, printTD);
            stopwatch.Stop();

            if (printTD)
            {
                output.Print();
            }

            if (printStats)
            {
                Console.WriteLine("Tree decomposition of {0} found in {1} time. Treewidth is {2}.\n" +
                        "Found {3} size 3 separators in {4} total time.\n" +
                        "Found {5} clique Separators in {6} total time.\n" +
                        "Found {7} almost clique separators in {8} total time.\n",
                        filepath, stopwatch.Elapsed, treeWidth, SafeSeparator.size3separators, SafeSeparator.size3SeparationStopwatch.Elapsed,
                        SafeSeparator.cliqueSeparators - SafeSeparator.almostCliqueSeparators, SafeSeparator.cliqueSeparatorStopwatch.Elapsed - SafeSeparator.almostCliqueSeparatorStopwatch.Elapsed,
                        SafeSeparator.almostCliqueSeparators, SafeSeparator.almostCliqueSeparatorStopwatch.Elapsed);
            }
            g = new Graph(filepath);
            output.AssertValidTreeDecomposition(new ImmutableGraph(g));
            return treeWidth;
        }


        public static bool TestSpecificTreewidth(string filepath, int actualTreewidth)
        {
            Graph g = new Graph(filepath);
            bool f = Treewidth.IsTreeWidthAtMost(g, actualTreewidth - 1, out PTD output);
            g = new Graph(filepath);
            bool t = Treewidth.IsTreeWidthAtMost(g, actualTreewidth, out output);
            Console.WriteLine(f);
            Console.WriteLine(t);
            g = new Graph(filepath);
            if (t)
            {
                output.AssertValidTreeDecomposition(new ImmutableGraph(g));
            }

            return !f && t;
        }

        private static string PACE2017(int number)
        {
            return String.Format("..\\..\\Test Data\\ex-instances-PACE2017-public\\ex{0:D3}.gr", number);
        }
    }
}
