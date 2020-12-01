using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;
using System.IO;
using Tamaki_Tree_Decomp.Safe_Separators;
using static Tamaki_Tree_Decomp.Heuristics;
using System.Text;
using System.Linq;

namespace Tamaki_Tree_Decomp
{
    public static class Treewidth
    {
        static bool verbose;
        public static bool completeHeuristically = false;
        public static int heuristicCompletionFrequency = 1;

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

            heuristicCompletionCalls = 0;
            heuristicCompletionsSuccessful = 0;

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
                treeDecomp = new PTD(onlyBag, null, null, null, null, new List<PTD>());
                return 0;
            }

            return TreeWidth_Computation(graph, out treeDecomp);
        }

        public static int lowerBound = 0;

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
            
            int minK = LowerBound.getLowerBound(graph);
            //int minK = 0;

            List<Graph> subGraphs = new List<Graph>();                        // index i corresponds to the i-th subgraph created
            List<List<GraphReduction>> graphReductions = new List<List<GraphReduction>>();  // index i corresponds to the list of graph reductions made to subgraph i
            List<SafeSeparator> safeSeparators = new List<SafeSeparator>();                 // index j corresponds to the j-th safe separator found
            List<int> safeSeparatorSubgraphIndices = new List<int>();                       // index j contains the index i of the subgraph where a safe separator has been found
            List<List<int>> childrenLists = new List<List<int>>();                          // index j contains the indices of the subgraphs for the safe separator object j
            List<PTD> ptds = new List<PTD>();   // the ptds for each subgraph. If the subgraph has a safe separator, that position is set to null at first and the correct ptd is inserted later

            Dictionary<int, PTD> subgraphIndexToAlreadyCalculatedPTDsMapping = new Dictionary<int, PTD>();  // if a safe separator is found during the "HasTreewidth" calculation, then we have 
                                                                                                            // found already a ptd associated with one component of it. This mapping maps the 
                                                                                                            // graphID of the corresponding subgraph that results from splitting the graph at 
                                                                                                            // that separator to that ptd. Then it doesn't have to be calculated again.

            subGraphs.Add(graph);

            // loop over all subgraphs
            for (int i = 0; i < subGraphs.Count; i++)
            {
                graph = subGraphs[i];
                subGraphs[i] = null;
                bool firstIterationOnGraph = true;
                graphReductions.Add(new List<GraphReduction>());

                if (subgraphIndexToAlreadyCalculatedPTDsMapping.TryGetValue(graph.graphID, out PTD alreadyCalculatedPTD))
                {
                    ptds.Add(alreadyCalculatedPTD);
                    continue;
                }
                

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
                        if (reduced || firstIterationOnGraph)
                        {
                            outletsAlreadyChecked.Clear();
                        }

                        if (Graph.dumpSubgraphs)
                        {
                            graph.Dump();
                        }

                        ImmutableGraph immutableGraph = new ImmutableGraph(graph);
                        if (HasTreeWidth(immutableGraph, minK, out PTD subGraphTreeDecomp, out BitSet outletSafeSeparator, graph))
                        {
                            PrintPTDURSize();
                            AccumulateStats_kMinus();

#if DEBUG
                            subGraphTreeDecomp.AssertValidTreeDecomposition(immutableGraph);
#endif
                            for (int j = graphReductions[i].Count - 1; j >= 0; j--)
                            {
                                graphReductions[i][j].RebuildTreeDecomposition(ref subGraphTreeDecomp);
                            }
                            ptds.Add(subGraphTreeDecomp);

                            if (verbose)
                            {
                                // "at most" because we only test for the lowest bound we have for the entire graph, not for exact treewidth of any one subgraph
                                Console.WriteLine("graph {0} has treewidth at most {1}.", graph.graphID, minK);
                            }
                            break;
                        }
                        else if (outletSafeSeparator != null)
                        {
                            PrintPTDURSize();
                            AccumulateStats_kMinus();
                            if (verbose)
                            {
                                Console.WriteLine("found outlet clique minor");
                            }
                            List<Graph> separatedGraphs = ss.ApplyExternallyFoundSafeSeparator(outletSafeSeparator, SafeSeparator.SeparatorType.CliqueMinor, ref minK,
                                    out int alreadyCalculatedComponentIndex, subGraphTreeDecomp.inlet);

                            separated = true;

                            subgraphIndexToAlreadyCalculatedPTDsMapping.Add(subGraphs.Count + alreadyCalculatedComponentIndex, subGraphTreeDecomp);

                            List<int> children = new List<int>();
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
                        PrintPTDURSize();
                        AccumulateStats_kMinus();
                    }
                    if (verbose)
                    {
                        Console.WriteLine("graph {0} has treewidth larger than {1}.", graph.graphID, minK);
                    }
                    minK++;
                    firstIterationOnGraph = false;
                }

                if (ptds.Count == i)    // if graph is smaller than the minimum bound for tree width
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
                treeDecomp = new PTD(onlyBag, null, null, null, null, new List<PTD>());
                return k == 0;
            }

            if (k < LowerBound.getLowerBound(graph))
            {
                treeDecomp = null;
                return false;
            }

            outletsAlreadyChecked = new HashSet<BitSet>();

            int minK = k;   // check equality with k after reduction and safe separation

            List<Graph> subGraphs = new List<Graph>();                        // index i corresponds to the i-th subgraph created
            List<List<GraphReduction>> graphReductions = new List<List<GraphReduction>>();  // index i corresponds to the list of graph reductions made to subgraph i
            List<SafeSeparator> safeSeparators = new List<SafeSeparator>();                 // index j corresponds to the j-th safe separator found
            List<int> safeSeparatorSubgraphIndices = new List<int>();                       // index j contains the index i of the subgraph where a safe separator has been found
            List<List<int>> childrenLists = new List<List<int>>();                          // index j contains the indices of the subgraphs for the safe separator object j
            List<PTD> ptds = new List<PTD>();   // the ptds for each subgraph. If the subgraph has a safe separator, that position is set to null at first and the correct ptd is inserted later

            Dictionary<int, PTD> subgraphIndexToAlreadyCalculatedPTDsMapping = new Dictionary<int, PTD>();  // if a safe separator is found during the "HasTreewidth" calculation, then we have 
                                                                                                            // found already a ptd associated with one component of it. This mapping maps the 
                                                                                                            // graphID of the corresponding subgraph that results from splitting the graph at 
                                                                                                            // that separator to that ptd. Then it doesn't have to be calculated again.

            subGraphs.Add(graph);

            // loop over all subgraphs
            for (int i = 0; i < subGraphs.Count; i++)
            {
                graph = subGraphs[i];
                subGraphs[i] = null;
                graphReductions.Add(new List<GraphReduction>());

                if (subgraphIndexToAlreadyCalculatedPTDsMapping.TryGetValue(graph.graphID, out PTD alreadyCalculatedPTD))
                {
                    ptds.Add(alreadyCalculatedPTD);
                    continue;
                }
                
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
                    if (HasTreeWidth(immutableGraph, minK, out PTD subGraphTreeDecomp, out BitSet outletSafeSeparator, graph))
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
                            separatedGraphs = ss.ApplyExternallyFoundSafeSeparator(outletSafeSeparator, SafeSeparator.SeparatorType.CliqueMinor, ref minK,
                                    out int alreadyCalculatedComponentIndex, subGraphTreeDecomp.inlet);

                            separated = true;

                            subgraphIndexToAlreadyCalculatedPTDsMapping.Add(subGraphs.Count + alreadyCalculatedComponentIndex, subGraphTreeDecomp);

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


        public static bool keepOnlyPTDsWithLargerInletIfSameOutlet = true;

        public static bool newCliquishTest = true;

        /// <summary>
        /// determines whether this graph has tree width k, or, if a safe separator is found by a heuristic,
        /// that safe separator is given out instead, so that this function can be called on the subgraphs.
        /// </summary>
        /// <param name="graph">the graph</param>
        /// <param name="k">the desired tree width</param>
        /// <param name="treeDecomp">a tree decomposition with width <paramref name="k"/> if there is one, else null. This is also null when a safe separator is found instead.</param>
        /// <param name="outletSafeSeparator">a safe separator, if one is found during the execution, else null</param>
        /// <returns>true, iff this graph has tree width <paramref name="k"/></returns>
        private static bool HasTreeWidth(ImmutableGraph graph, int k, out PTD treeDecomp, out BitSet outletSafeSeparator, Graph mutableGraph)
        {
            if (graph.vertexCount == 0)
            {
                treeDecomp = new PTD(new BitSet(0));
                outletSafeSeparator = null;
                return true;
            }
            
            P_size = 0;

            heuristicCompletionCallsPerGraphAndK = 0;

            Stack<PTD> P = new Stack<PTD>();
            Dictionary<BitSet, PTD> P_inlets = new Dictionary<BitSet, PTD>();

            List<PTD> U = new List<PTD>();
            // basically the same as P_inlets, but here the index of the PTD in U is saved along with the inlet
            Dictionary<BitSet, int> U_inletsWithIndex = new Dictionary<BitSet, int>();

            bool[] isNvSmallEnoughAndPotMaxClique = new bool[graph.vertexCount];    // TODO: make static and reset from calling function

            // a dictionary mapping all encountered outlets to a list of ptd inlets who have that outlet
            Dictionary<BitSet, List<BitSet>> outletToInletMapping = new Dictionary<BitSet, List<BitSet>>();

            // a set containing all inlets to whose outlets a larger inlet has been found later
            HashSet<BitSet> smallInlets = new HashSet<BitSet>();

            // a mapping from components that are waited on to the ptds that have this component as an inlet
            Dictionary<BitSet, List<PTD>> componentToPTDsMapping = new Dictionary<BitSet, List<PTD>>();

            // a mapping from ptds to a list of comopnent inlets that need to be attached to them
            Dictionary<PTD, List<BitSet>> PTDToComponentsMapping = new Dictionary<PTD, List<BitSet>>();

            PTDSizePerPTDURSize = new int[k + 2, k + 2];
            PTDURSizeNotBuilt = new int[k + 2];

             

            // ---------line 1 is in the method that calls this one----------

            // --------- lines 2 to 6 ---------- (5 is skipped and tested in the method that calls this one)

            for (int v = 0; v < graph.vertexCount; v++)
            {
                if (graph.adjacencyList[v].Length <= k && graph.IsPotMaxClique(graph.closedNeighborhood[v]))
                {
                    BitSet outlet = new BitSet(graph.allVertices);
                    outlet.ExceptWith(graph.closedNeighborhood[v]);
                    outlet = graph.Neighbors(outlet);
                    isNvSmallEnoughAndPotMaxClique[v] = true;
                    PTD p0 = new PTD(graph.closedNeighborhood[v], outlet);
                    if (!P_inlets.ContainsKey(p0.inlet) && !p0.IsIncoming(graph))
                    {
                        if (graph.IsMinimalSeparator_ReturnComponents(outlet, out List<BitSet> components))
                        {
                            AddToP(p0, components, graph, P, P_inlets, outletToInletMapping, smallInlets, componentToPTDsMapping, PTDToComponentsMapping);
                        }

                        // TODO: heuristic completion here also
                    }
                }
                else
                {
                    isNvSmallEnoughAndPotMaxClique[v] = false;
                }
            }

            // --------- lines 7 to 32 ----------

            int heuristicCompletionIn = 1;
            //for (int i = 0; i < P.Count; i++)
            while (P.Count > 0)
            {
                // PTD Tau = P[i];
                PTD Tau = P.Pop();

                Debug.Assert(graph.IsMinimalSeparator(Tau.outlet));

                if (smallInlets.Contains(Tau.inlet))
                {
                    //continue;
                }

                // --------- line 9 ----------

                PTD Tau_wiggle_original = PTD.Line9(Tau);

                // --------- lines 10 ----------

                Tau_wiggle_original.AssertConsistency(graph.vertexCount, fullConsistencyCheck: !moreThan2ComponentsOptimization);

                // add the new ptdur if there are no equivalent ptdurs
                // if it has a smaller root bag than an equivalent ptdur, replace that one instead
                if (U_inletsWithIndex.TryGetValue(Tau_wiggle_original.inlet, out int index))
                {
                    PTD equivalentPtdur = U[index];
                    if (Tau_wiggle_original.Bag.Count() < equivalentPtdur.Bag.Count())
                    {
                        U[index] = Tau_wiggle_original;
                    }
                    else
                    {
                        continue;
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
                    PTD Tau_wiggle = null;

                    // --------- lines 12 to 15 ----------

                    if (!Tau_wiggle_original.Equivalent(Tau_prime))
                    {
                        // --------- line 13 with early continue if bag size is too big or tree is not possibly usable or bag is not cliquish----------

                        if (!newCliquishTest && (!PTD.Line13_CheckBagSize_CheckPossiblyUsable(Tau_prime, Tau, graph, k, out Tau_wiggle) || !graph.IsCliquish(Tau_wiggle.Bag))
                            || newCliquishTest && !PTD.Line13_CheckBagSize_CheckPossiblyUsable_CheckCliquish(Tau_prime, Tau, graph, k, out Tau_wiggle, mutableGraph))
                        {
                            // ---------- line 15  ----------
                            continue;
                        }

                        // add the new ptdur if there are no equivalent ptdurs
                        // if it has a smaller root bag than an equivalent ptdur, replace that one instead
                        if (U_inletsWithIndex.TryGetValue(Tau_wiggle.inlet, out index))
                        {
                            PTD equivalentPtdur = U[index];
                            if (Tau_wiggle.Bag.Count() < equivalentPtdur.Bag.Count())
                            {
                                Tau_wiggle.AssertConsistency(graph.vertexCount, fullConsistencyCheck: !moreThan2ComponentsOptimization);
                                U[index] = Tau_wiggle;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            Tau_wiggle.AssertConsistency(graph.vertexCount, fullConsistencyCheck: !moreThan2ComponentsOptimization);
                            U_inletsWithIndex.Add(Tau_wiggle.inlet, U.Count);
                            U.Add(Tau_wiggle);
                        }
                    }
                    else
                    {
                        Tau_wiggle = Tau_wiggle_original;
                    }

                    bool getsCompleted = false;
                    uint PTDURSize = Tau_wiggle.Bag.Count();

                    // --------- lines 16 to 20 ----------

                    // bag should always be small enough by construction
                    Debug.Assert(Tau_wiggle.Bag.Count() <= k + 1);

                    if (PTDURSize == k+1 || graph.IsPotMaxClique(Tau_wiggle.Bag))    // if size = k + 1, then the pmc check has been done already
                    {
                        // TODO: outlet from the isPotMaxClique calculation may be able to be used
                        // TODO: p1 is the same as tau_wiggle, so we can copy after tests are done. (We could also move this case to the end and not copy at all.)
                        //       Only do this if it becomes an issue because the code becomes less readable.

                        PTD p1 = new PTD(Tau_wiggle);

                        if (p1.vertices.Equals(graph.allVertices))
                        {
                            getsCompleted = true;
                            PTDSizePerPTDURSize[PTDURSize, p1.Bag.Count()]++;
                            treeDecomp = p1;
                            treeDecomp.RemoveDuplicateBags();
                            outletSafeSeparator = null;
                            return true;
                        }

                        // TODO: reorder checks
                        if (!P_inlets.ContainsKey(p1.inlet) && graph.IsMinimalSeparator_ReturnComponents(p1.outlet, out List<BitSet> components) && !p1.IsIncoming(graph) && p1.IsNormalized())
                        {
                            getsCompleted = true;
                            PTDSizePerPTDURSize[PTDURSize, p1.Bag.Count()]++;

                            //----
                            heuristicCompletionIn--;
                            if (completeHeuristically && heuristicCompletionIn == 0 && TryHeuristicCompletion(graph, p1, k))
                            {
                                treeDecomp = p1;
                                treeDecomp.RemoveDuplicateBags();
                                outletSafeSeparator = null;
                                return true;
                            }
                            if (heuristicCompletionIn < 0)
                            {
                                heuristicCompletionIn = heuristicCompletionFrequency;
                            }
                            //---


                            if (OutletIsSafeSeparator(p1, graph))
                            {
                                Console.WriteLine("found a clique minor that is an outlet of a tree");
                                outletSafeSeparator = Tau.outlet;
                                treeDecomp = p1;
                                treeDecomp.RemoveDuplicateBags();
                                return false;
                            }

                            p1.AssertConsistency(graph.vertexCount, fullConsistencyCheck: !moreThan2ComponentsOptimization);
                            AddToP(p1, components, graph, P, P_inlets, outletToInletMapping, smallInlets, componentToPTDsMapping, PTDToComponentsMapping);

                        }

                        // if the bag is already a pmc, then no proper superset can be one. Therefore we can continue immediately.
                        continue;
                    }

                    // --------- lines 21 to 26 ----------

                    for (int v = 0; v < graph.vertexCount; v++)
                    {
                        if (!Tau_wiggle.vertices[v])
                        {
                            // --------- lines 22 to 26 ----------

                            if (isNvSmallEnoughAndPotMaxClique[v] && graph.closedNeighborhood[v].IsSupersetOf(Tau_wiggle.Bag))
                            {
                                // --------- line 23 ----------
                                PTD p2 = PTD.Line23(Tau_wiggle, graph.closedNeighborhood[v], graph);


                                if (p2.vertices.Equals(graph.allVertices))
                                {
                                    getsCompleted = true;
                                    PTDSizePerPTDURSize[PTDURSize, p2.Bag.Count()]++;
                                    treeDecomp = p2;
                                    treeDecomp.RemoveDuplicateBags();
                                    outletSafeSeparator = null;
                                    return true;

                                }

                                // --------- line 24 ----------
                                heuristicCompletionIn--;
                                if (!P_inlets.ContainsKey(p2.inlet) && graph.IsMinimalSeparator_ReturnComponents(p2.outlet, out List<BitSet> components) && !p2.IsIncoming(graph) && p2.IsNormalized())
                                {
                                    getsCompleted = true;
                                    PTDSizePerPTDURSize[PTDURSize, p2.Bag.Count()]++;

                                    //---
                                    if (completeHeuristically && heuristicCompletionIn == 0 && TryHeuristicCompletion(graph, p2, k))
                                    {
                                        treeDecomp = p2;
                                        treeDecomp.RemoveDuplicateBags();
                                        outletSafeSeparator = null;
                                        return true;
                                    }
                                    if (heuristicCompletionIn < 0)
                                    {
                                        heuristicCompletionIn = heuristicCompletionFrequency;
                                    }
                                    //---


                                    // --------- line 26 ----------
                                    if (OutletIsSafeSeparator(p2, graph))
                                    {
                                        Console.WriteLine("found a clique minor that is an outlet of a tree");
                                        outletSafeSeparator = Tau.outlet;
                                        treeDecomp = p2;
                                        treeDecomp.RemoveDuplicateBags();
                                        return false;
                                    }

                                    p2.AssertConsistency(graph.vertexCount, fullConsistencyCheck: !moreThan2ComponentsOptimization);
                                    AddToP(p2, components, graph, P, P_inlets, outletToInletMapping, smallInlets, componentToPTDsMapping, PTDToComponentsMapping);

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
                        BitSet potNewRootBag = new BitSet(graph.openNeighborhood[v]);
                        potNewRootBag.ExceptWith(Tau_wiggle.inlet);
                        potNewRootBag.UnionWith(Tau_wiggle.Bag);

                        if (potNewRootBag.Count() <= k + 1 && graph.IsPotMaxClique(potNewRootBag))
                        {
                            // TODO: outlet from the isPotMaxClique calculation may be able to be used

                            // --------- line 29 ----------
                            PTD p3 = PTD.Line29(Tau_wiggle, potNewRootBag, graph);

                            if (p3.vertices.Equals(graph.allVertices))
                            {
                                getsCompleted = true;
                                PTDSizePerPTDURSize[PTDURSize, p3.Bag.Count()]++;
                                treeDecomp = p3;
                                treeDecomp.RemoveDuplicateBags();
                                outletSafeSeparator = null;
                                return true;
                            }

                            // --------- line 30 ----------
                            if (!P_inlets.ContainsKey(p3.inlet) && graph.IsMinimalSeparator_ReturnComponents(p3.outlet, out List<BitSet> components) && !p3.IsIncoming(graph) && p3.IsNormalized())
                            {
                                getsCompleted = true;
                                PTDSizePerPTDURSize[PTDURSize, p3.Bag.Count()]++;

                                //---
                                heuristicCompletionIn--;
                                if (completeHeuristically && heuristicCompletionIn == 0 && TryHeuristicCompletion(graph, p3, k))
                                {
                                    treeDecomp = p3;
                                    treeDecomp.RemoveDuplicateBags();
                                    outletSafeSeparator = null;
                                    return true;
                                }
                                if (heuristicCompletionIn < 0)
                                {
                                    heuristicCompletionIn = heuristicCompletionFrequency;
                                }
                                //---


                                // --------- line 32 ----------                               
                                if (OutletIsSafeSeparator(p3, graph))
                                {
                                    Console.WriteLine("found a clique minor that is an outlet of a tree");
                                    outletSafeSeparator = Tau.outlet;
                                    treeDecomp = p3;
                                    treeDecomp.RemoveDuplicateBags();
                                    return false;
                                }

                                p3.AssertConsistency(graph.vertexCount, fullConsistencyCheck: !moreThan2ComponentsOptimization);

                                AddToP(p3, components, graph, P, P_inlets, outletToInletMapping, smallInlets, componentToPTDsMapping, PTDToComponentsMapping);
                            }
                        }
                    }
                    if (!getsCompleted)
                    {
                        PTDURSizeNotBuilt[PTDURSize]++;
                    }
                }
            }

            if (verbose)
            {
                // Console.WriteLine("considered {0} PTDs and {1} PTDURs", P_inlets.Count, U.Count);
                Console.WriteLine("considered {0} PTDs and {1} PTDURs", P_size, U.Count);
            }

            treeDecomp = null;
            outletSafeSeparator = null;
            return false;
        }

        [ThreadStatic]
        private static int[,] PTDSizePerPTDURSize;
        [ThreadStatic]
        private static int[] PTDURSizeNotBuilt;
        public static int digitsInPTDURToPTDStatistic = 5;
        public static int totalBarVolume = 100;
        public static bool printStats = false;

        static void PrintPTDURSize()
        {
            if (!printStats)
            {
                return;
            }

            // calculate bars
            int[] barLengths = new int[PTDSizePerPTDURSize.GetLength(0)];
            int totalPTDURsNotBuild = PTDURSizeNotBuilt.Sum();
            if (totalPTDURsNotBuild != 0)
            {
                for (int ptdurSize = 1; ptdurSize < PTDSizePerPTDURSize.GetLength(0); ptdurSize++)
                {
                    barLengths[ptdurSize] = 100 * PTDURSizeNotBuilt[ptdurSize] / totalPTDURsNotBuild;
                }
            }
            
            StringBuilder sb = new StringBuilder();
            // table header
            sb.Append(new String(' ', 16));
            for (int ptdSize = 1; ptdSize < PTDSizePerPTDURSize.GetLength(1); ptdSize++)
            {
                sb.AppendFormat($"{{0,{digitsInPTDURToPTDStatistic}}} ", ptdSize);
            }
            sb.Append("  ptdurs not completed    bar chart");
            Console.WriteLine(sb.ToString());
            // table body
            for (int ptdurSize = 1; ptdurSize < PTDSizePerPTDURSize.GetLength(0); ptdurSize++)
            {
                sb = new StringBuilder();
                sb.Append(String.Format("ptdur size {0,2}:   ", ptdurSize));
                for (int ptdSize = 1; ptdSize < PTDSizePerPTDURSize.GetLength(1); ptdSize++)
                {
                    int count = PTDSizePerPTDURSize[ptdurSize, ptdSize];
                    if (count != 0)
                    {
                        sb.AppendFormat($"{{0,{digitsInPTDURToPTDStatistic}}} ", count);
                    }
                    else
                    {
                        sb.Append(new String(' ', digitsInPTDURToPTDStatistic + 1));
                    }
                }
                if (PTDURSizeNotBuilt[ptdurSize] != 0)
                {
                    sb.AppendFormat($"{{0,15}}    ", PTDURSizeNotBuilt[ptdurSize]);
                    sb.Append(new String('#', barLengths[ptdurSize]));
                }
                Console.WriteLine(sb.ToString());
            }
        }

        static int[,] PTDSizePerPTDURSize_kMinus = new int[1000,1000];
        static int[] PTDURSizeNotBuilt_kMinus = new int[1000];

        static void AccumulateStats_kMinus()
        {
            int arrayLength = PTDSizePerPTDURSize.GetLength(0);
            int start = PTDSizePerPTDURSize_kMinus.GetLength(0) - arrayLength;
            for (int ptdurSize = 0; ptdurSize < arrayLength; ptdurSize++)
            {
                for (int ptdSize = 0; ptdSize < arrayLength; ptdSize++)
                {
                    PTDSizePerPTDURSize_kMinus[start + ptdurSize, start + ptdSize] += PTDSizePerPTDURSize[ptdurSize, ptdSize];
                }
                PTDURSizeNotBuilt_kMinus[start + ptdurSize] += PTDURSizeNotBuilt[ptdurSize];
            }
        }

        public static int digitsInPTDURToPTDStatistic_kMinus = 12;

        public static void PrintStats_kMinus(int kMinusMax)
        {
            int arrayLength = PTDSizePerPTDURSize_kMinus.GetLength(0);

            // calculate bars
            int[] barLengths = new int[arrayLength];
            int totalPTDURsNotBuild_kMinus = PTDURSizeNotBuilt_kMinus.Sum();
            if (totalPTDURsNotBuild_kMinus != 0)
            {
                for (int ptdurSize_kMinus = 0; ptdurSize_kMinus < arrayLength; ptdurSize_kMinus++)
                {
                    barLengths[ptdurSize_kMinus] = 100 * PTDURSizeNotBuilt_kMinus[arrayLength - 1 - ptdurSize_kMinus] / totalPTDURsNotBuild_kMinus;
                }
            }

            StringBuilder sb = new StringBuilder();
            // table header
            sb.Append(new String(' ', 24));
            for (int ptdSize_kMinus = 0; ptdSize_kMinus < kMinusMax; ptdSize_kMinus++)
            {
                sb.AppendFormat($"k-{{0,{-digitsInPTDURToPTDStatistic_kMinus+2}}} ", ptdSize_kMinus);
            }
            Console.WriteLine(sb.ToString());
            // table body
            for (int ptdurSize_kMinus = 0; ptdurSize_kMinus < kMinusMax; ptdurSize_kMinus++)
            {
                sb = new StringBuilder();
                sb.Append(String.Format("ptdur size k-{0,-2}:   ", ptdurSize_kMinus));
                for (int ptdSize_kMinus = 0; ptdSize_kMinus < kMinusMax; ptdSize_kMinus++)
                {
                    int count = PTDSizePerPTDURSize_kMinus[arrayLength - 1 - ptdurSize_kMinus, arrayLength - 1 - ptdSize_kMinus];
                    if (count != 0)
                    {
                        sb.AppendFormat($"{{0,{digitsInPTDURToPTDStatistic_kMinus}}} ", count);
                    }
                    else
                    {
                        sb.Append(new String(' ', digitsInPTDURToPTDStatistic_kMinus + 1));
                    }
                }
                Console.WriteLine(sb.ToString());
            }

            Console.WriteLine("\nptdurs not completed:");
            for (int ptdurSize_kMinus = 0; ptdurSize_kMinus < kMinusMax; ptdurSize_kMinus++)
            {
                sb = new StringBuilder();
                sb.Append(String.Format("ptdur size k-{0,-2}:   ", ptdurSize_kMinus));
                if (PTDURSizeNotBuilt_kMinus[arrayLength - 1 - ptdurSize_kMinus] != 0)
                {
                    sb.AppendFormat($"{{0,18}}    ", PTDURSizeNotBuilt_kMinus[arrayLength - 1 - ptdurSize_kMinus]);
                    sb.Append($"  {barLengths[ptdurSize_kMinus * 100 / totalBarVolume],2}%  ");
                    sb.Append(new String('#', barLengths[ptdurSize_kMinus]));
                }
                Console.WriteLine(sb.ToString());
            }
        }

        public static bool moreThan2ComponentsOptimization = true;
        public static int P_size = 0;

        /// <summary>
        /// adds a ptd to P, or if moreThan2ComponentsOptimization is active and the ptd's outlet has more than 2 components delays the addition
        /// to P until ptds covering the remaining non-incoming components have been found. In the latter case, they are added as children directly to the
        /// first PTD.
        /// </summary>
        /// <param name="ptd">the ptd to add</param>
        /// <param name="components">the components associated with the ptd's outlet</param>
        /// <param name="graph">the underlying graph</param>
        /// <param name="P">the stack P to add the ptd to</param>
        /// <param name="P_inlets">a mapping from inlets of the ptds in P to the ptds themselves</param>
        /// <param name="outletToInletMapping">a mapping from minimal separators to a list of inlets of ptds who have that minimal separator as an outlet</param>
        /// <param name="smallInlets">currently not in use</param>
        /// <param name="componentToPTDsMapping">a mapping from components to ptds which have that component as their inlet</param>
        /// <param name="PTDToComponentsMapping">a mapping from delayed ptds to components that they require before being added to P</param>
        /// <param name="isParent">pass true, only if called recursively from within this function</param>
        /// <param name="pushParents">a list of delayed ancestors to push onto P. This list is populated throughout recursive calls instead of adding the ancestors directly
        ///                             because it roughly preserves the order of a depth-first search. At the end of the recursive calls, all of the ancestors are added to P</param>
        private static void AddToP(PTD ptd, List<BitSet> components, ImmutableGraph graph, Stack<PTD> P, Dictionary<BitSet, PTD> P_inlets, Dictionary<BitSet, List<BitSet>> outletToInletMapping, HashSet<BitSet> smallInlets, Dictionary<BitSet, List<PTD>> componentToPTDsMapping, Dictionary<PTD, List<BitSet>> PTDToComponentsMapping, bool isParent=false, List<PTD> pushParents=null)
        {
            if (moreThan2ComponentsOptimization)
            {
                // test if this ptd is supposed to be a child of another ptd
                if (componentToPTDsMapping.TryGetValue(ptd.inlet, out List<PTD> parents))
                {
                    if (!isParent)
                    {
                        pushParents = new List<PTD>();
                    }

                    bool alwaysFullComponent = true;

                    // update all ancestors
                    for (int i = 0; i < parents.Count; i++)
                    {
                        parents[i].AddChildNotNormalized(new PTD(ptd), graph);
                        List<BitSet> parentRemainingComponents = PTDToComponentsMapping[parents[i]];
                        bool removed = parentRemainingComponents.Remove(ptd.inlet);
                        parents[i].AssertConsistency(graph.vertexCount, fullConsistencyCheck: false);
                        Debug.Assert(removed);
                        if (parentRemainingComponents.Count == 0)
                        {
                            AddToP(parents[i], graph.Components(parents[i].outlet), graph, P, P_inlets, outletToInletMapping, smallInlets, componentToPTDsMapping, PTDToComponentsMapping, isParent: true, pushParents: pushParents);
                        }

                        if (!parents[i].outlet.Equals(ptd.outlet))
                        {
                            alwaysFullComponent = false;
                        }
                    }
                    // mark this ptd's inlet as no longer required
                    componentToPTDsMapping.Remove(ptd.inlet);

                    // return early if this ptd is a full component associated with each of its parents' outlets
                    if (alwaysFullComponent)
                    {
                        // but push the parents beforehand
                        P_size += pushParents.Count;
                        for (int i = pushParents.Count - 1; i >= 0; i--)
                        {
                            P.Push(pushParents[i]);

                            Debug.Assert(P_inlets.ContainsKey(pushParents[i].inlet));
                        }
                        return;
                    }
                }

                bool componentAdded = true;                     // TODO: add P_inlets before any component is really added, then if ptd (really) changed add it again
                if (!isParent)
                {
                    // if this function is called for the first time for this ptd, add ptds for components that already have a ptd covering them
                    if (components.Count > 2)
                    {
                        componentAdded = false;
                        bool nonFullNonIncomingComponentsExist = false;

                        // determine incoming component
                        int incomingComponentIndex = -1;
                        int incomingDefiningVertex = int.MaxValue;
                        for (int i = 0; i < components.Count; i++)
                        {
                            int componentSmallestVertex = components[i].First();
                            if (componentSmallestVertex < incomingDefiningVertex)
                            {
                                incomingComponentIndex = i;
                                incomingDefiningVertex = componentSmallestVertex;
                            }
                            if (!nonFullNonIncomingComponentsExist &&  i!=0 && !graph.Neighbors(components[i]).Equals(ptd.outlet))
                            {
                                nonFullNonIncomingComponentsExist = true;
                            }
                        }
                        Debug.Assert(incomingComponentIndex == 0);



                        // loop over components
                        List<BitSet> remainingRequiredComponents = new List<BitSet>();
                        for (int i = 0; i < components.Count; i++)
                        {
                            BitSet currentComponent = components[i];
                            if (i != incomingComponentIndex && !ptd.inlet.Intersects(currentComponent))
                            {
                                componentAdded = true;
                                // if component has a ptd already, add it
                                if (P_inlets.TryGetValue(currentComponent, out PTD child))
                                {
                                    ptd.AddChildNotNormalized(child, graph);
                                }
                                // otherwise the component to the remaining component list
                                else
                                {
                                    remainingRequiredComponents.Add(currentComponent);
                                    if (componentToPTDsMapping.TryGetValue(currentComponent, out List<PTD> PTDsDependentOnComponent))
                                    {
                                        PTDsDependentOnComponent.Add(ptd);
                                    }
                                    else
                                    {
                                        componentToPTDsMapping.Add(currentComponent, new List<PTD>() { ptd });
                                    }
                                }
                                if (!P_inlets.ContainsKey(ptd.inlet))
                                {
                                    P_inlets.Add(new BitSet(ptd.inlet), new PTD(ptd));
                                }
                            }
                        }

                        // if there are remaining required components, mark them as remaining and return
                        if (remainingRequiredComponents.Count > 0)
                        {
                            PTDToComponentsMapping.Add(ptd, remainingRequiredComponents);

                            // TODO: add something to p_inlets? (be careful, hash must not change!!) (also can't add prematurely combined inlet unless the incoming component is full)
                            ptd.AssertConsistency(graph.vertexCount, fullConsistencyCheck: false);

                            return;
                        }
                    }
                }

                if (keepOnlyPTDsWithLargerInletIfSameOutlet)
                {
                    // test if a ptd with a strictly larger inlet exists already. In that case skip this ptd
                    if (outletToInletMapping.TryGetValue(ptd.outlet, out List<BitSet> inlets))
                    {
                        for (int i = inlets.Count - 1; i >= 0; i--)
                        {
                            if (inlets[i].IsSupersetOf(ptd.inlet))
                            {
                                if (pushParents != null)
                                {
                                    for (int j = pushParents.Count - 1; j >= 0; j--)
                                    {
                                        P.Push(pushParents[j]);
                                        if (!P_inlets.ContainsKey(pushParents[j].inlet))
                                        {
                                            throw new Exception();
                                        }
                                    }
                                }
                                return;
                            }
                            else if (ptd.inlet.IsSupersetOf(inlets[i]))
                            {
                                //smallInlets.Add(inlets[i]);
                                inlets.RemoveAt(i);
                            }
                        }
                        inlets.Add(ptd.inlet);
                    }
                    else
                    {
                        outletToInletMapping.Add(ptd.outlet, new List<BitSet>() { ptd.inlet });
                    }
                }

                ptd.AssertConsistency(graph.vertexCount, fullConsistencyCheck: false);
                if (isParent)
                {
                    pushParents.Add(ptd);
                }
                else
                {
                    P.Push(ptd);
                    P_size++;
                    if (pushParents != null)
                    {
                        P_size += pushParents.Count;
                        for (int i = pushParents.Count - 1; i >= 0; i--)
                        {
                            P.Push(pushParents[i]);
                        }
                    }
                }
                if (componentAdded)
                {
                    if (!P_inlets.ContainsKey(ptd.inlet))
                    {
                        P_inlets.Add(ptd.inlet, ptd);
                    }
                    else
                    {
                        Debug.Assert(P_inlets[ptd.inlet].vertices.Equals(ptd.vertices));
                    }
                }
            }
            else
            {
                ptd.AssertConsistency(graph.vertexCount);
                P.Push(ptd);
                P_size++;
                P_inlets.Add(ptd.inlet, ptd);
            }
        }

        /// <summary>
        /// tests heuristically whether the outlet of a given ptd is a safe separator.
        /// This method can return false negatives, but no false positives.
        /// </summary>
        /// <param name="ptd">the ptd whose outlet to test</param>
        /// <returns>true, if the heuristic determines that the outlet is a safe separator, otherwise false</returns>
        private static bool OutletIsSafeSeparator(PTD ptd, ImmutableGraph graph)
        {
            // test outlet for clique minor
            if (testOutletIsCliqueMinor && !outletsAlreadyChecked.Contains(ptd.outlet))
            {
                outletsAlreadyChecked.Add(ptd.outlet);
                return ss.IsSafeSeparator_Heuristic(ptd.outlet);
            }
            else
            {
                return false;
            }
        }

        public static int heuristicCompletionCalls = 0;
        public static int heuristicCompletionsSuccessful = 0;
        public static Heuristic heuristic = Heuristic.min_degree;
        public static float heuristicInletMin = 0;
        public static float heuristicInletMax = 1;

        public static int maxTestsPerGraphAndK = int.MaxValue;
        public static int currentTestsPerGraphAndK = 0;

        public static int heuristicCompletionCallsPerGraphAndK = 0;
        public static List<int> heuristicCompletionSuccesses = new List<int>();
        public static int currentGraphID = -1;
        public static int currentK = -1;
        public static int graphsTested = 0;

        /// <summary>
        /// given a ptd, try to complete it into a tree decomposition of the entire graph using a simple heuristic
        /// </summary>
        /// <param name="immutableGraph">the graph</param>
        /// <param name="ptd">the ptd</param>
        /// <param name="k">the currently tested width</param>
        /// <returns>a ptd for the entire graph, if one can be found, else null</returns>
        private static bool TryHeuristicCompletion(ImmutableGraph immutableGraph, PTD ptd, int k)
        {
            // reset on a new graph or on new k
            if (currentGraphID != immutableGraph.graphID || currentK != k)
            {
                if (currentGraphID != immutableGraph.graphID)
                {
                    graphsTested++;
                }
                currentGraphID = immutableGraph.graphID;
                currentK = k;
                currentTestsPerGraphAndK = 0;
            }

            // test if inlet to vertices ratio is in specified range
            float inletToVerticesRatio = (float)ptd.inlet.Count() / immutableGraph.vertexCount;
            if (inletToVerticesRatio < heuristicInletMin || inletToVerticesRatio > heuristicInletMax)
            {
                return false;
            }

            currentTestsPerGraphAndK++;
            if (currentTestsPerGraphAndK > maxTestsPerGraphAndK)
            {
                return false;
            }

            heuristicCompletionCalls++;
            heuristicCompletionCallsPerGraphAndK++;

            // create graph
            List<int>[] adjacencyList = new List<int>[immutableGraph.vertexCount];
            for (int u = 0; u < immutableGraph.vertexCount; u++)
            {
                adjacencyList[u] = new List<int>();
                if (!ptd.inlet[u])
                {
                    for (int i = 0; i < immutableGraph.adjacencyList[u].Length; i++)
                    {
                        int v = immutableGraph.adjacencyList[u][i];
                        if (!ptd.inlet[v])
                        {
                            adjacencyList[u].Add(v);
                        }
                    }
                }
            }

            Graph graph = new Graph(adjacencyList);
            int inletVertex = -1;
            while((inletVertex = ptd.inlet.NextElement(inletVertex, false)) != -1)
            {
                graph.Remove(inletVertex);
            }
            graph.MakeIntoClique(ptd.outlet.Elements());

            // calculate a list of the bags and (a subset of) the parent bags that they need to be added to.
            Stack<(BitSet, BitSet)> bagsAndParentsStack = new Stack<(BitSet, BitSet)>();
            foreach ((BitSet bag, BitSet parent) in HeuristicBagsAndNeighbors(graph, heuristic))
            {
                if (bag.Count() > k + 1)
                {
                    return false;
                }
                bagsAndParentsStack.Push((bag, parent));
            }
            // return also if the remaining clique is too large
            if (graph.notRemovedVertexCount > k + 1)
            {
                return false;
            }

            // build the ptd from those bags
            PTD otherPTD = new PTD(graph.notRemovedVertices);
            // TODO: use some superset query data structure instead of the following
            List<PTD> subtreeList = new List<PTD> { otherPTD }; // a list of all nodes in the PTD for easy iteration
            while (bagsAndParentsStack.Count > 0)
            {
                (BitSet currentBag, BitSet currentParent) = bagsAndParentsStack.Pop();
                PTD currentNode = new PTD(currentBag);
#if DEBUG
                bool hasChanged = false;
#endif
                // in order from most recent to least recent. Otherwise a node could have two children that contain a 
                // vertex that the node does not contain, thus violating the consistency property.
                for (int i = subtreeList.Count - 1; i >= 0; i--)
                {
                    PTD currentSubtree = subtreeList[i];
                    if (currentSubtree.Bag.IsSupersetOf(currentParent))
                    {                        
                        currentSubtree.children.Add(currentNode);
#if DEBUG
                        hasChanged = true;
#endif
                        break;
                    }
                }
#if DEBUG
                Debug.Assert(hasChanged);
#endif
                subtreeList.Add(currentNode);
            }

            // TODO: make canonical

            PTD.Reroot(ref otherPTD, ptd.outlet);

            ptd.children.Add(otherPTD);

            ptd.RemoveDuplicateBags();

            ptd.AssertValidTreeDecomposition(immutableGraph);

            //Console.WriteLine("heuristic completion: {0}th call, graph: {1}, inlet: {2}, percent: {3}", heuristicCompletionFunctionCounter, graph.vertexCount, ptd.inlet.Count(), ptd.inlet.Count()*100/graph.vertexCount);

            heuristicCompletionsSuccessful++;


            for (int i = heuristicCompletionSuccesses.Count; i <= heuristicCompletionCallsPerGraphAndK; i++)
            {
                heuristicCompletionSuccesses.Add(0);
            }
            heuristicCompletionSuccesses[heuristicCompletionCallsPerGraphAndK]++;
            heuristicCompletionCallsPerGraphAndK = 0;

            return true;
        }
    }
}
