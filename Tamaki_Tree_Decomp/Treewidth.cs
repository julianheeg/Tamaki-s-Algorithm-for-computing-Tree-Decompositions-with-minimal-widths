using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;
using System.IO;
using Tamaki_Tree_Decomp.Safe_Separators;
using System.Text;

namespace Tamaki_Tree_Decomp
{
    public static class Treewidth
    {
        static bool verbose;

        /// <summary>
        /// determines the tree width of a graph
        /// </summary>
        /// <param name="graph">the graph</param>
        /// <param name="treeDecomp">a tree decomposition for the graph</param>
        /// <returns>the graph's tree width</returns>
        public static int TreeWidth(Graph graph, out PTD treeDecomp, bool verbose = true)
        {
            Treewidth.verbose = verbose;
            Graph.verbose = verbose;
            ImmutableGraph.verbose = verbose;

            // edges cases
            if (graph.vertexCount == 0)
            {
                treeDecomp = new PTD(new BitSet(0));
                return 0;
            }
            else if (graph.vertexCount == 1)
            {
                BitSet onlyBag = new BitSet(1);
                onlyBag[0] = true;
                treeDecomp = new PTD(onlyBag, null, null, null, new List<PTD>());
                return 0;
            }

            return TreeWidth_Computation(graph, out treeDecomp);
        }

        /// <summary>
        /// performs graph reduction and graph splitting until no further graph simplification is possible.
        /// Then the treewidth and tree decompositions for the simplified graphs are computed and put back together to
        /// give a treewidth and tree decomposition for the original graph. 
        /// </summary>
        /// <param name="graph">the original graph</param>
        /// <param name="treeDecomp">a tree decomposition for the original graph</param>
        /// <returns>the original graph's tree width</returns>
        private static int TreeWidth_Computation(Graph graph, out PTD treeDecomp)
        {
            /*
             * 
             *  What is going on here:
             *  We keep lists with two indices:
             *      1.  subGraphs, graphReductions, ptds:
             *              An entry in each of these corresponds to one subgraph.
             *              So subGraphs[i] has been reduced using the GraphReduction objects in graphReductions[i] its ptd is saved in ptds[i]
             *      2.  safeSeparators, safeSeparatorSubgraphIndices, childrenLists:
             *              safeSeparators contains all SafeSeparator objects that have been used to split the graph
             *              for each index j, safeSeparatorSubgraphIndices[j] contains the index i of the subGraph at which safeSeparators[j] has been used to split the graph
             *              for each index j, childrenLists[j] contains the list of indices i of the subGraphs that result from the separation at safeSeparators[j]
             *  
             *  The algorithm is implemented as follows:
             *  
             *      1.  We iteratively do the following, until we have iterated over every subGraph
             *          a.  Take a graph with index i from the subGraphs list.
             *          b.  Iterate over possible treewidths k:
             *          
             *              i.   Reduce the graph using the graph reduction rules.
             *                   (We keep a list for the graph reductions because we can apply more rules the higher the lower bound is.
             *                   Thus, we have to revert all the changes later in the opposite order.)
             *                   
             *              ii.  If the graph has been reduced in the previous step, or if we have just taken the graph from the list, we
             *                   have a new graph at hand. Thus, safe separators could exist, which we test for. If so, the graph is
             *                   separated and the subgraphs are added to the subgraphs list. Because the ptd of the current graph is
             *                   dependent on the subgraphs, we continue immediately with the next graph. The ptds[i] stays empty until we
             *                   recombine the ptds of the subgraphs towards the end of the function.
             *                   
             *              iii. We test if the graph has treewidth k. If so, we revert the changes made using graphReductions[i] and save
             *                   the ptd in ptds[i]. If not, we increase k and continue iterating at 2, trying to find the correct treewidth
             *                   and a valid tree decomposition for the current graph
             *               
             *      2.  Now, we have all ptds except those of graphs that have been safe separated.
             *          For each graph that has been safe separated:
             *          a.  Get its children ptds and recombine them.
             *          b.  Revert the changes made by the graphReductions applied to the graph.
             *          c.  Save the ptd at the position corresponding to the graph
             *  
             *      3. The tree decomposition for the input graph is in ptds[0]
             * 
             */
            outletsAlreadyChecked = new HashSet<BitSet>();

            int minK = 0;

            List<Graph> subGraphs = new List<Graph>();                        // index i corresponds to the i-th subgraph created
            List<List<GraphReduction>> graphReductions = new List<List<GraphReduction>>();  // index i corresponds to the list of graph reductions made to subgraph i
            List<SafeSeparator> safeSeparators = new List<SafeSeparator>();                 // index j corresponds to the j-th safe separator found
            List<int> safeSeparatorSubgraphIndices = new List<int>();                       // index j contains the index i of the subgraph where a safe separator has been found
            List<List<int>> childrenLists = new List<List<int>>();                          // index j contains the indices of the subgraphs for the safe separator object j
            List<PTD> ptds = new List<PTD>();   // the ptds for each subgraph. If the subgraph has a safe separator, that position is set to null at first and the correct ptd is inserted later

            subGraphs.Add(graph);

            treeDecomp_Counts = new List<int>();
            
            // loop over all subgraphs
            for (int i = 0; i < subGraphs.Count; i++)
            {
                graph = subGraphs[i];
                subGraphs[i] = null;
                bool firstIterationOnGraph = true;
                graphReductions.Add(new List<GraphReduction>());

                // loop over all possible tree widths for the current graph
                while (minK < graph.vertexCount - 1)
                {
                    // perform graph reduction
                    GraphReduction graphReduction = new GraphReduction(graph, minK);
                    bool reduced = graphReduction.Reduce(ref minK);
                    if (reduced)
                    {
                        graphReductions[graphReductions.Count - 1].Add(graphReduction);
                    }

                    // break early if the graph doesn't contain any vertices anymore
                    if (graph.vertexCount == 0)
                    {
                        PTD subGraphTreeDecomp = new PTD(new BitSet(0));
                        for (int j = graphReductions[i].Count - 1; j >= 0; j--)
                        {
                            graphReductions[i][j].RebuildTreeDecomposition(ref subGraphTreeDecomp);
                        }
                        ptds.Add(subGraphTreeDecomp);
                        break;
                    }

                    // only try to find safe separators if the graph has been reduced in this iteration or if this iteration is the first one.
                    // Else there is no chance that a new safe separator can be found
                    bool separated = false;
                    if (reduced || firstIterationOnGraph)
                    {
                        firstIterationOnGraph = false;
                        // try to find safe separator
                        SafeSeparator safeSeparator = new SafeSeparator(graph, verbose);
                        if (safeSeparator.Separate(out List<Graph> separatedGraphs, ref minK))
                        {
                            separated = true;
                            List<int> children = new List<int>();
                            // if there is one, put the children in the list to be processed
                            for (int j = 0; j < separatedGraphs.Count; j++)
                            {
                                children.Add(subGraphs.Count + j);
                            }
                            subGraphs.AddRange(separatedGraphs);

                            safeSeparators.Add(safeSeparator);
                            safeSeparatorSubgraphIndices.Add(i);
                            childrenLists.Add(children);
                            ptds.Add(null);

                            // continue with the next graph
                            break;
                        }
                    }

                    // only check tree width if the graph has not been separated. If it has, the tree decomposition is built later from the subgraphs
                    if (!separated)
                    {
                        ss = new SafeSeparator(graph);
                        if (reduced)
                        {
                            outletsAlreadyChecked.Clear();
                        }

                        ImmutableGraph immutableGraph = new ImmutableGraph(graph);
                        if (HasTreeWidth(immutableGraph, minK, out PTD subGraphTreeDecomp, out BitSet outletSafeSeparator))
                        {
#if DEBUG
                            subGraphTreeDecomp.AssertValidTreeDecomposition(immutableGraph);
#endif
                            for (int j = graphReductions[i].Count - 1; j >= 0; j--)
                            {
                                graphReductions[i][j].RebuildTreeDecomposition(ref subGraphTreeDecomp);
                            }
                            ptds.Add(subGraphTreeDecomp);

                            if (Graph.dumpSubgraphs)
                            {
                                graph.Dump();
                            }

                            if (verbose)
                            {
                                // "at most" because we only test for the lowest bound we have for the entire graph, not for exact treewidth of any one subgraph
                                Console.WriteLine("graph {0} has treewidth at most {1}.", graph.graphID, minK);
                            }
                            break;
                        }
                        else if (outletSafeSeparator != null)
                        {
                            Console.WriteLine("found outlet clique minor");
                            List<Graph> separatedGraphs = ss.ApplyExternallyFoundSafeSeparator(outletSafeSeparator, SafeSeparator.SeparatorType.CliqueMinor, ref minK);

                            separated = true;
                            List<int> children = new List<int>();
                            // if there is one, put the children in the list to be processed
                            for (int j = 0; j < separatedGraphs.Count; j++)
                            {
                                children.Add(subGraphs.Count + j);
                            }
                            subGraphs.AddRange(separatedGraphs);

                            safeSeparators.Add(ss);
                            safeSeparatorSubgraphIndices.Add(i);
                            childrenLists.Add(children);
                            ptds.Add(null);

                            // continue with the next graph
                            break;
                        }
                    }
                    if (verbose)
                    {
                        Console.WriteLine("graph {0} has treewidth larger than {1}.", graph.graphID, minK);
                    }
                    minK++;
                }

                if (ptds.Count == i)    // if graph is smaller than the minimum bound for tree width
                {
                    ptds.Add(new PTD(BitSet.All(graph.vertexCount)));
                }
            }

            Debug.Assert(safeSeparators.Count == childrenLists.Count);

            // recombine subgraphs that have been safe separated
            for (int j = safeSeparators.Count - 1; j >= 0;  j--)
            {
                List<PTD> childrenPTDs = new List<PTD>();
                List<int> childrenSubgraphIndices = childrenLists[j];
                for (int i = 0; i < childrenSubgraphIndices.Count; i++)
                {
                    childrenPTDs.Add(ptds[childrenSubgraphIndices[i]]);
                }
                int parentIndex = safeSeparatorSubgraphIndices[j];
                ptds[parentIndex] = safeSeparators[j].RecombineTreeDecompositions(childrenPTDs);
                PTD ptd = ptds[parentIndex];
                for (int i = graphReductions[parentIndex].Count - 1; i >= 0; i--)
                {
                    graphReductions[parentIndex][i].RebuildTreeDecomposition(ref ptd);
                }
                ptds[parentIndex] = ptd;
            }

            if (treeDecomp_Counts.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < treeDecomp_Counts.Count; i++)
                {
                    sb.Append(treeDecomp_Counts[i] + ", ");
                }
                sb.Remove(sb.Length - 2, 2);
                Console.WriteLine("found             " + sb.ToString() + "\n\nsubtree decompositions\n");
            }

            treeDecomp = ptds[0];
            return minK;
        }

        /// <summary>
        /// determines whether the tree width of a graph is at most a given value.
        /// (Really only used for faster testing. Will be obsolete when the idea to reuse the ptds and ptdurs during later iterations is implemented.)
        /// </summary>
        /// <param name="g">the graph</param>
        /// <param name="k">the upper bound</param>
        /// <param name="treeDecomp">a normalized canonical tree decomposition for the graph, iff the tree width is at most k, else null</param>
        /// <returns>true, iff the tree width is at most k</returns>
        public static bool IsTreeWidthAtMost(Graph graph, int k, out PTD treeDecomp)
        {
            // edges cases
            if (graph.vertexCount == 0)
            {
                treeDecomp = new PTD(new BitSet(0));
                return k == -1;
            }
            else if (graph.vertexCount == 1)
            {
                BitSet onlyBag = new BitSet(1);
                onlyBag[0] = true;
                treeDecomp = new PTD(onlyBag, null, null, null, new List<PTD>());
                return k == 0;
            }

            outletsAlreadyChecked = new HashSet<BitSet>();

            int minK = k;   // check equality with k after reduction and safe separation

            List<Graph> subGraphs = new List<Graph>();                        // index i corresponds to the i-th subgraph created
            List<List<GraphReduction>> graphReductions = new List<List<GraphReduction>>();  // index i corresponds to the list of graph reductions made to subgraph i
            List<SafeSeparator> safeSeparators = new List<SafeSeparator>();                 // index j corresponds to the j-th safe separator found
            List<int> safeSeparatorSubgraphIndices = new List<int>();                       // index j contains the index i of the subgraph where a safe separator has been found
            List<List<int>> childrenLists = new List<List<int>>();                          // index j contains the indices of the subgraphs for the safe separator object j
            List<PTD> ptds = new List<PTD>();   // the ptds for each subgraph. If the subgraph has a safe separator, that position is set to null at first and the correct ptd is inserted later

            subGraphs.Add(graph);

            // loop over all subgraphs
            for (int i = 0; i < subGraphs.Count; i++)
            {
                graph = subGraphs[i];
                subGraphs[i] = null;
                graphReductions.Add(new List<GraphReduction>());

                // perform graph reduction
                GraphReduction graphReduction = new GraphReduction(graph, k);
                bool reduced = graphReduction.Reduce(ref minK);
                if (minK > k)
                {
                    treeDecomp = null;
                    return false;
                }
                if (reduced)
                {
                    graphReductions[graphReductions.Count - 1].Add(graphReduction);
                }

                // break early if the graph doesn't contain any vertices anymore
                if (graph.vertexCount == 0)
                {
                    PTD subGraphTreeDecomp = new PTD(new BitSet(0));
                    for (int j = graphReductions[i].Count - 1; j >= 0; j--)
                    {
                        graphReductions[i][j].RebuildTreeDecomposition(ref subGraphTreeDecomp);
                    }
                    ptds.Add(subGraphTreeDecomp);
                    continue;
                }

                // only try to find safe separators if the graph has been reduced in this iteration or if this iteration is the first one.
                // Else there is no chance that a new safe separator can be found
                bool separated = false;

                // try to find safe separator
                SafeSeparator safeSeparator = new SafeSeparator(graph);
                if (safeSeparator.Separate(out List<Graph> separatedGraphs, ref minK))
                {
                    if (minK > k)
                    {
                        treeDecomp = null;
                        return false;
                    }
                    separated = true;
                    List<int> children = new List<int>();
                    // if there is one, put the children in the list to be processed
                    for (int j = 0; j < separatedGraphs.Count; j++)
                    {
                        children.Add(subGraphs.Count + j);
                    }
                    subGraphs.AddRange(separatedGraphs);
                    safeSeparators.Add(safeSeparator);
                    safeSeparatorSubgraphIndices.Add(i);
                    childrenLists.Add(children);
                    ptds.Add(null);

                    // continue with the next graph
                    continue;
                }                

                // only check tree width if the graph has not been separated. If it has, the tree decomposition is built later from the subgraphs
                if (!separated)
                {
                    ss = new SafeSeparator(graph);
                    outletsAlreadyChecked.Clear();
                    ImmutableGraph immutableGraph = new ImmutableGraph(graph);
                    if (HasTreeWidth(immutableGraph, minK, out PTD subGraphTreeDecomp, out BitSet outletSafeSeparator))
                    {
#if DEBUG
                        subGraphTreeDecomp.AssertValidTreeDecomposition(immutableGraph);
#endif
                        for (int j = graphReductions[i].Count - 1; j >= 0; j--)
                        {
                            graphReductions[i][j].RebuildTreeDecomposition(ref subGraphTreeDecomp);
                        }
                        ptds.Add(subGraphTreeDecomp);
                        continue;
                    }
                    else
                    {
                        if (outletSafeSeparator != null)
                        {
                            separatedGraphs = ss.ApplyExternallyFoundSafeSeparator(outletSafeSeparator, SafeSeparator.SeparatorType.CliqueMinor, ref minK);

                            separated = true;
                            List<int> children = new List<int>();
                            // if there is one, put the children in the list to be processed
                            for (int j = 0; j < separatedGraphs.Count; j++)
                            {
                                children.Add(subGraphs.Count + j);
                            }
                            subGraphs.AddRange(separatedGraphs);

                            safeSeparators.Add(ss);
                            safeSeparatorSubgraphIndices.Add(i);
                            childrenLists.Add(children);
                            ptds.Add(null);

                            // continue with the next graph
                            continue;
                        }
                        else
                        {
                            treeDecomp = null;
                            return false;
                        }
                    }
                }
                minK++;

                if (ptds.Count == i)    // if the vertex set is smaller than the minimum bound for tree width, make a bag that contains all vertices
                {
                    ptds.Add(new PTD(BitSet.All(graph.vertexCount)));
                }
            }

            Debug.Assert(safeSeparators.Count == childrenLists.Count);

            // recombine subgraphs that have been safe separated
            for (int j = safeSeparators.Count - 1; j >= 0; j--)
            {
                List<PTD> childrenPTDs = new List<PTD>();
                List<int> childrenSubgraphIndices = childrenLists[j];
                for (int i = 0; i < childrenSubgraphIndices.Count; i++)
                {
                    childrenPTDs.Add(ptds[childrenSubgraphIndices[i]]);
                }
                int parentIndex = safeSeparatorSubgraphIndices[j];
                ptds[parentIndex] = safeSeparators[j].RecombineTreeDecompositions(childrenPTDs);
                PTD ptd = ptds[parentIndex];
                for (int i = graphReductions[parentIndex].Count - 1; i >= 0; i--)
                {
                    graphReductions[parentIndex][i].RebuildTreeDecomposition(ref ptd);
                }
                ptds[parentIndex] = ptd;
            }

            treeDecomp = ptds[0];
            return true;
        }

        [ThreadStatic]
        static SafeSeparator ss;    // a safe separator object for testing if a given outlet is a clique minor of the underlying graph
        [ThreadStatic]
        static HashSet<BitSet> outletsAlreadyChecked;  // contains all outlets that have already been tested if they are a clique minor
        [ThreadStatic]
        public static bool testOutletIsCliqueMinor = true;  // switch for controlling whether the outlet-is-clique-minor test is executed

        /// <summary>
        /// determines whether this graph has tree width k, or, if a safe separator is found by a heuristic,
        /// that safe separator is given out instead, so that this function can be called on the subgraphs.
        /// </summary>
        /// <param name="graph">the graph</param>
        /// <param name="k">the desired tree width</param>
        /// <param name="treeDecomp">a tree decomposition with width <paramref name="k"/> if there is one, else null. This is also null when a safe separator is found instead.</param>
        /// <param name="outletSafeSeparator">a safe separator, if one is found during the execution, else null</param>
        /// <returns>true, iff this graph has tree width <paramref name="k"/></returns>
        private static bool HasTreeWidth(ImmutableGraph graph, int k, out PTD treeDecomp, out BitSet outletSafeSeparator)
        {
            if (graph.vertexCount == 0)
            {
                treeDecomp = new PTD(new BitSet(0));
                outletSafeSeparator = null;
                return true;
            }

            // TODO: P and U can actually be reused from the previous iteration
            Stack<PTD> P = new Stack<PTD>();    // stack seems to be so much faster than a list in the last iteration,
                                                // at least for the 2017 instances.
            HashSet<BitSet> P_inlets = new HashSet<BitSet>();

            List<PTD> U = new List<PTD>();
            // basically the same as P_inlets, but here the index of the PTD in U is saved along with the inlet
            Dictionary<BitSet, int> U_inletsWithIndex = new Dictionary<BitSet, int>();

            bool[] isNvSmallEnoughAndPotMaxClique = new bool[graph.vertexCount];    // TODO: make static and reset from calling function

            // ---------line 1 is in the method that calls this one----------

            // --------- lines 2 to 6 ---------- (5 is skipped and tested in the method that calls this one)

            for (int v = 0; v < graph.vertexCount; v++)
            {
                if (graph.adjacencyList[v].Length <= k && graph.IsPotMaxClique(graph.neighborSetsWith[v], out BitSet outlet))
                {
                    isNvSmallEnoughAndPotMaxClique[v] = true;
                    PTD p0 = new PTD(graph.neighborSetsWith[v], outlet);
                    if (!P_inlets.Contains(p0.inlet))
                    {
                        if (graph.IsMinimalSeparator(outlet))
                        {
                            P.Push(p0); // ptd mit Tasche N[v] als einzelnen Knoten
                            P_inlets.Add(p0.inlet);
                        }
                    }
                }
                else
                {
                    isNvSmallEnoughAndPotMaxClique[v] = false;
                }
            }

            // --------- lines 7 to 32 ----------

            //for (int i = 0; i < P.Count; i++)
            while (P.Count > 0)
            {
                // PTD Tau = P[i];
                PTD Tau = P.Pop();

                Debug.Assert(graph.IsMinimalSeparator(Tau.outlet));

                // test outlet for clique minor
                // TODO: move to wherever anything is added to P, so that outlet is tested earlier. Might be bad for cache locality, though -> test if worth it.
                // TODO: one tree decomposition is found already. Don't calculate that again
                if (testOutletIsCliqueMinor && !outletsAlreadyChecked.Contains(Tau.outlet))
                {
                    outletsAlreadyChecked.Add(Tau.outlet);
                    if (ss.IsSafeSeparator_Heuristic(Tau.outlet))
                    {
                        Console.WriteLine("found a clique minor that is an outlet of a tree");
                        outletSafeSeparator = Tau.outlet;
                        treeDecomp = null;
                        return false;
                    }
                }

                // --------- line 9 ----------

                PTD Tau_wiggle_original = PTD.Line9(Tau);

                // --------- lines 10 ----------

                Tau_wiggle_original.AssertConsistency(graph.vertexCount);

                // add the new ptdur if there are no equivalent ptdurs
                // if it has a smaller root bag than an equivalent ptdur, replace that one instead
                if (U_inletsWithIndex.TryGetValue(Tau_wiggle_original.inlet, out int index))
                {
                    PTD equivalentPtdur = U[index];
                    if (Tau_wiggle_original.Bag.Count() < equivalentPtdur.Bag.Count())
                    {
                        U[index] = Tau_wiggle_original;
                    }
                }
                else
                {
                    U_inletsWithIndex.Add(Tau_wiggle_original.inlet, U.Count);
                    U.Add(Tau_wiggle_original);
                }

                // --------- lines 11 to 32 ----------

                for (int j = 0; j < U.Count; j++)
                {
                    PTD Tau_prime = U[j];
                    PTD Tau_wiggle;
                    bool tau_tauprime_combined;

                    // --------- lines 12 to 15 ----------

                    if (!Tau_wiggle_original.Equivalent(Tau_prime))
                    {
                        tau_tauprime_combined = true;

                        // --------- line 13 with early continue if bag size is too big or tree is not possibly usable ----------

                        if (!PTD.Line13_CheckBagSize_CheckPossiblyUsable(Tau_prime, Tau, graph, k, out Tau_wiggle))
                        {
                            // ---------- line 15 (first two cases) ----------
                            continue;
                        }

                        // --------- lines 14 and 15 (only the check for cliquish remains) ----------

                        if (graph.IsCliquish(Tau_wiggle.Bag))
                        {
                            // add the new ptdur only if no equivalent ptdur exists
                            if (!U_inletsWithIndex.ContainsKey(Tau_wiggle.inlet))
                            {
                                Tau_wiggle.AssertConsistency(graph.vertexCount);
                                U_inletsWithIndex.Add(Tau_wiggle.inlet, U.Count);
                                U.Add(Tau_wiggle);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        tau_tauprime_combined = false;
                        Tau_wiggle = Tau_wiggle_original;
                    }

                    // --------- lines 16 to 20 ----------

                    Debug.Assert(Tau_wiggle.Bag.Count() <= k + 1);

                    // TODO: count should always be small enough due to construction. Move count into assertion
                    if (Tau_wiggle.Bag.Count() <= k + 1 && graph.IsPotMaxClique(Tau_wiggle.Bag, out _))
                    {
                        // TODO: outlet from the isPotMaxClique calculation may be able to be used
                        // TODO: p1 is the same as tau_wiggle, so we can copy after tests are done. (We could also move this case to the end and not copy at all.)
                        //       Only do this if it becomes an issue because the code becomes less readable.
                        PTD p1 = new PTD(Tau_wiggle);

                        if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p1, graph))
                        {
                            if (p1.vertices.Equals(graph.allVertices))
                            {
                                treeDecomp = p1;
                                outletSafeSeparator = null;
                                return true;
                            }
                            

                            if (!P_inlets.Contains(p1.inlet))
                            {
                                if (graph.IsMinimalSeparator(p1.outlet))
                                {
                                    p1.AssertConsistency(graph.vertexCount);
                                    P.Push(p1);
                                    P_inlets.Add(p1.inlet);
                                }
                            }
                        }
                    }

                    // --------- lines 21 to 26 ----------

                    for (int v = 0; v < graph.vertexCount; v++)
                    {
                        if (!Tau_wiggle.vertices[v])
                        {
                            // --------- lines 22 to 26 ----------

                            if (isNvSmallEnoughAndPotMaxClique[v] && graph.neighborSetsWith[v].IsSupersetOf(Tau_wiggle.Bag))
                            {
                                // --------- line 23 ----------
                                PTD p2 = PTD.Line23(Tau_wiggle, graph.neighborSetsWith[v], graph);

                                // --------- line 24 ----------
                                if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p2, graph))
                                {
                                    if (p2.vertices.Equals(graph.allVertices))
                                    {
                                        treeDecomp = p2;
                                        outletSafeSeparator = null;
                                        return true;
                                    }
                                    

                                    // --------- line 26 ----------
                                    if (!P_inlets.Contains(p2.inlet))
                                    {
                                        if (graph.IsMinimalSeparator(p2.outlet))
                                        {
                                            p2.AssertConsistency(graph.vertexCount);
                                            P.Push(p2);
                                            P_inlets.Add(p2.inlet);
                                        }
                                    }
                                }

                            }
                        }
                    }

                    // --------- lines 27 to 32 ----------

                    List<int> X_r = Tau_wiggle.Bag.Elements();
                    for (int l = 0; l < X_r.Count; l++)
                    {
                        int v = X_r[l];

                        // --------- line 28 ----------
                        BitSet potNewRootBag = new BitSet(graph.neighborSetsWithout[v]);
                        potNewRootBag.ExceptWith(Tau_wiggle.inlet);
                        potNewRootBag.UnionWith(Tau_wiggle.Bag);

                        if (potNewRootBag.Count() <= k + 1 && graph.IsPotMaxClique(potNewRootBag, out _))
                        {
                            // TODO: outlet from the isPotMaxClique calculation may be able to be used

                            // --------- line 29 ----------
                            PTD p3 = PTD.Line29(Tau_wiggle, potNewRootBag, graph);

                            // --------- line 30 ----------
                            if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p3, graph))
                            {
                                if (p3.vertices.Equals(graph.allVertices))
                                {
                                    treeDecomp = p3;
                                    outletSafeSeparator = null;
                                    return true;
                                }
                                

                                // --------- line 32 ----------
                                if (!P_inlets.Contains(p3.inlet))
                                {
                                    if (graph.IsMinimalSeparator(p3.outlet))
                                    {
                                        p3.AssertConsistency(graph.vertexCount);
                                        P.Push(p3);
                                        P_inlets.Add(p3.inlet);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (verbose)
            {
                Console.WriteLine("considered {0} PTDs and {1} PTDURs", P.Count, U.Count);
            }

            treeDecomp = null;
            outletSafeSeparator = null;
            return false;
        }

        [ThreadStatic]
        static List<int> treeDecomp_Counts;

        /// <summary>
        /// determines whether this graph has tree width k, or, if a safe separator is found by a heuristic,
        /// that safe separator is given out instead, so that this function can be called on the subgraphs.
        /// </summary>
        /// <param name="graph">the graph</param>
        /// <param name="k">the desired tree width</param>
        /// <param name="treeDecomp">a tree decomposition with width <paramref name="k"/> if there is one, else null. This is also null when a safe separator is found instead.</param>
        /// <param name="outletSafeSeparator">a safe separator, if one is found during the execution, else null</param>
        /// <returns>true, iff this graph has tree width <paramref name="k"/></returns>
        private static bool HasTreeWidth_CountTrees(ImmutableGraph graph, int k, out PTD treeDecomp, out BitSet outletSafeSeparator)
        {
            int tree_decomposition_count = 0;
            PTD first_decomp = null;

            if (graph.vertexCount == 0)
            {
                treeDecomp = new PTD(new BitSet(0));
                outletSafeSeparator = null;
                return true;
            }

            // TODO: P and U can actually be reused from the previous iteration
            Stack<PTD> P = new Stack<PTD>();    // stack seems to be so much faster than a list in the last iteration,
                                                // at least for the 2017 instances.
            HashSet<BitSet> P_inlets = new HashSet<BitSet>();

            List<PTD> U = new List<PTD>();
            // basically the same as P_inlets, but here the index of the PTD in U is saved along with the inlet
            Dictionary<BitSet, int> U_inletsWithIndex = new Dictionary<BitSet, int>();

            // ---------line 1 is in the method that calls this one----------

            // --------- lines 2 to 6 ---------- (5 is skipped and tested in the method that calls this one)

            for (int v = 0; v < graph.vertexCount; v++)
            {
                if (graph.adjacencyList[v].Length <= k && graph.IsPotMaxClique(graph.neighborSetsWith[v], out BitSet outlet))
                {
                    PTD p0 = new PTD(graph.neighborSetsWith[v], outlet);
                    if (!P_inlets.Contains(p0.inlet))
                    {
                        if (graph.IsMinimalSeparator(outlet))
                        {
                            P.Push(p0); // ptd mit Tasche N[v] als einzelnen Knoten
                            P_inlets.Add(p0.inlet);
                        }
                    }
                }
            }

            // --------- lines 7 to 32 ----------

            //for (int i = 0; i < P.Count; i++)
            while (P.Count > 0)
            {
                // PTD Tau = P[i];
                PTD Tau = P.Pop();

                Debug.Assert(graph.IsMinimalSeparator(Tau.outlet));

                // test outlet for clique minor
                // TODO: move to wherever anything is added to P, so that outlet is tested earlier. Might be bad for cache locality, though -> test if worth it.
                if (testOutletIsCliqueMinor && !outletsAlreadyChecked.Contains(Tau.outlet))
                {
                    outletsAlreadyChecked.Add(Tau.outlet);
                    if (ss.IsSafeSeparator_Heuristic(Tau.outlet))
                    {
                        Console.WriteLine("found a clique minor that is an outlet of a tree");
                        outletSafeSeparator = Tau.outlet;
                        treeDecomp = null;
                        return false;
                    }
                }

                // --------- line 9 ----------

                PTD Tau_wiggle_original = PTD.Line9(Tau);

                // --------- lines 10 ----------

                Tau_wiggle_original.AssertConsistency(graph.vertexCount);

                // add the new ptdur if there are no equivalent ptdurs
                // if it has a smaller root bag than an equivalent ptdur, replace that one instead
                if (U_inletsWithIndex.TryGetValue(Tau_wiggle_original.inlet, out int index))
                {
                    PTD equivalentPtdur = U[index];
                    if (Tau_wiggle_original.Bag.Count() < equivalentPtdur.Bag.Count())
                    {
                        U[index] = Tau_wiggle_original;
                    }
                }
                else
                {
                    U_inletsWithIndex.Add(Tau_wiggle_original.inlet, U.Count);
                    U.Add(Tau_wiggle_original);
                }

                // --------- lines 11 to 32 ----------

                for (int j = 0; j < U.Count; j++)
                {
                    PTD Tau_prime = U[j];
                    PTD Tau_wiggle;
                    bool tau_tauprime_combined;

                    // --------- lines 12 to 15 ----------

                    if (!Tau_wiggle_original.Equivalent(Tau_prime))
                    {
                        tau_tauprime_combined = true;

                        // --------- line 13 with early continue if bag size is too big or tree is not possibly usable ----------

                        if (!PTD.Line13_CheckBagSize_CheckPossiblyUsable(Tau_prime, Tau, graph, k, out Tau_wiggle))
                        {
                            // ---------- line 15 (first two cases) ----------
                            continue;
                        }

                        // --------- lines 14 and 15 (only the check for cliquish remains) ----------

                        // TODO: cache cliquish, and see if that makes a difference
                        if (graph.IsCliquish(Tau_wiggle.Bag))
                        {
                            // add the new ptdur only if no equivalent ptdur exists
                            if (!U_inletsWithIndex.ContainsKey(Tau_wiggle.inlet))
                            {
                                Tau_wiggle.AssertConsistency(graph.vertexCount);
                                U_inletsWithIndex.Add(Tau_wiggle.inlet, U.Count);
                                U.Add(Tau_wiggle);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        tau_tauprime_combined = false;
                        Tau_wiggle = Tau_wiggle_original;
                    }

                    // --------- lines 16 to 20 ----------

                    Debug.Assert(Tau_wiggle.Bag.Count() <= k + 1);

                    // TODO: count should always be small enough due to construction. Move count into assertion
                    if (Tau_wiggle.Bag.Count() <= k + 1 && graph.IsPotMaxClique(Tau_wiggle.Bag, out _))
                    {
                        // TODO: outlet from the isPotMaxClique calculation may be able to be used
                        // TODO: p1 is the same as tau_wiggle, so we can copy after tests are done. (We could also move this case to the end and not copy at all.)
                        //       Only do this if it becomes an issue because the code becomes less readable.
                        PTD p1 = new PTD(Tau_wiggle);

                        if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p1, graph))
                        {
                            if (p1.vertices.Equals(graph.allVertices))
                            {
                                tree_decomposition_count++;
                                if (first_decomp == null)
                                {
                                    first_decomp = p1;
                                }
                                /*
                                treeDecomp = p1;
                                outletSafeSeparator = null;
                                return true;
                                */
                            }


                            if (!P_inlets.Contains(p1.inlet))
                            {
                                if (graph.IsMinimalSeparator(p1.outlet))
                                {
                                    p1.AssertConsistency(graph.vertexCount);
                                    P.Push(p1);
                                    P_inlets.Add(p1.inlet);
                                }
                            }
                        }
                    }

                    // --------- lines 21 to 26 ----------

                    for (int v = 0; v < graph.vertexCount; v++)
                    {
                        if (!Tau_wiggle.vertices[v])
                        {
                            // --------- lines 22 to 26 ----------

                            // TODO: isPotMaxClique can be pre calculated for every v and has in fact been done already during the leaf generation.
                            //       Just use a bool array for that (or BitSet) and get true or false here.
                            //       Change order of the check in that case so that this query is made first.

                            if (graph.adjacencyList[v].Length <= k + 1 && graph.neighborSetsWith[v].IsSupersetOf(Tau_wiggle.Bag) && graph.IsPotMaxClique(graph.neighborSetsWith[v], out _))
                            {
                                // --------- line 23 ----------
                                PTD p2 = PTD.Line23(Tau_wiggle, graph.neighborSetsWith[v], graph);

                                // --------- line 24 ----------
                                if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p2, graph))
                                {
                                    if (p2.vertices.Equals(graph.allVertices))
                                    {
                                        tree_decomposition_count++;
                                        if (first_decomp == null)
                                        {
                                            first_decomp = p2;
                                        }
                                        /*
                                        treeDecomp = p2;
                                        outletSafeSeparator = null;
                                        return true;
                                        */
                                    }


                                    // --------- line 26 ----------
                                    if (!P_inlets.Contains(p2.inlet))
                                    {
                                        if (graph.IsMinimalSeparator(p2.outlet))
                                        {
                                            p2.AssertConsistency(graph.vertexCount);
                                            P.Push(p2);
                                            P_inlets.Add(p2.inlet);
                                        }
                                    }
                                }

                            }
                        }
                    }

                    // --------- lines 27 to 32 ----------

                    List<int> X_r = Tau_wiggle.Bag.Elements();
                    for (int l = 0; l < X_r.Count; l++)
                    {
                        int v = X_r[l];

                        // --------- line 28 ----------
                        BitSet potNewRootBag = new BitSet(graph.neighborSetsWithout[v]);
                        potNewRootBag.ExceptWith(Tau_wiggle.inlet);
                        potNewRootBag.UnionWith(Tau_wiggle.Bag);

                        if (potNewRootBag.Count() <= k + 1 && graph.IsPotMaxClique(potNewRootBag, out _))
                        {
                            // TODO: outlet from the isPotMaxClique calculation may be able to be used

                            // --------- line 29 ----------
                            PTD p3 = PTD.Line29(Tau_wiggle, potNewRootBag, graph);

                            // --------- line 30 ----------
                            if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p3, graph))
                            {
                                if (p3.vertices.Equals(graph.allVertices))
                                {
                                    tree_decomposition_count++;
                                    if (first_decomp == null)
                                    {
                                        first_decomp = p3;
                                    }
                                    /*
                                    treeDecomp = p3;
                                    outletSafeSeparator = null;
                                    return true;
                                    */
                                }


                                // --------- line 32 ----------
                                if (!P_inlets.Contains(p3.inlet))
                                {
                                    if (graph.IsMinimalSeparator(p3.outlet))
                                    {
                                        p3.AssertConsistency(graph.vertexCount);
                                        P.Push(p3);
                                        P_inlets.Add(p3.inlet);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //if (verbose)
            //{
                Console.WriteLine("considered {0} PTDs and {1} PTDURs", P.Count, U.Count);
            //}

            if (tree_decomposition_count > 0)
            {
                treeDecomp_Counts.Add(tree_decomposition_count);
                treeDecomp = first_decomp;
                outletSafeSeparator = null;
                return true;
            }

            treeDecomp = null;
            outletSafeSeparator = null;
            return false;
        }

        /// <summary>
        /// tests if a PTD is incoming and normalized
        /// </summary>
        /// <param name="Tau"></param>
        /// <param name="tau_tauprime_combined"></param>
        /// <param name="toTest"></param>
        /// <param name="graph">the underlying graph</param>
        /// <returns></returns>
        private static bool PassesIncomingAndNormalizedTest(PTD Tau, bool tau_tauprime_combined, PTD toTest, ImmutableGraph graph)
        {
            //return true;
            //return !tau_tauprime_combined; // WRONG!!!                                                                             // test

            //return !toTest.IsIncoming(graph);  // test

            return !tau_tauprime_combined || !toTest.IsIncoming(graph);                                                   // mine
            //return (!tau_tauprime_combined || toTest.IsNormalized_Daniela(Tau)) && !toTest.IsIncoming_Daniela(graph);  // Daniela old

            //return !tau_tauprime_combined || (toTest.IsNormalized_Daniela(Tau) && !toTest.IsIncoming_Daniela(graph));  // Daniela very old
        }
    }
}
