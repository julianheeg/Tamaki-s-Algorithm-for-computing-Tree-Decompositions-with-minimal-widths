using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    public static class Treewidth
    {
        static bool verbose;

        /// <summary>
        /// determines the tree width of a graph
        /// </summary>
        /// <param name="graph">the graph</param>
        /// <param name="treeDecomp">a normalized canonical tree decomposition for the graph</param>
        /// <returns>the graph's tree width</returns>
        public static int TreeWidth(Graph graph, out PTD treeDecomp, bool verbose = true)
        {
            Graph.verbose = verbose;
            ImmutableGraph.verbose = verbose;
            return TreeWidth(graph, 0, out treeDecomp);
        }

        private static int TreeWidth(Graph graph, int minK, out PTD treeDecomp)
        {
            // edges cases
            if (graph.vertexCount == 0)
            {
                treeDecomp = new PTD(new BitSet(0));
                return minK;
            }
            else if (graph.vertexCount == 1)
            {
                BitSet onlyBag = new BitSet(1);
                onlyBag[0] = true;
                treeDecomp = new PTD(onlyBag, null, null, null, new List<PTD>());
                return minK;
            }


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
                bool firstIterationOnGraph = true;
                graphReductions.Add(new List<GraphReduction>());

                // loop over all possible tree widths for the current graph
                while(minK < graph.vertexCount - 1)
                {
                    // perform graph reduction
                    GraphReduction graphReduction = new GraphReduction(graph, minK);
                    bool reduced = graphReduction.Reduce(ref minK);
                    if (reduced)
                    {
                        graphReductions[graphReductions.Count - 1].Add(graphReduction);
                    }

                    // only try to find safe separators if the graph has been reduced in this iteration or if this iteration is the first one.
                    // Else there is no chance that a new safe separator can be found
                    bool separated = false;
                    if (reduced || firstIterationOnGraph) {
                        firstIterationOnGraph = false;
                        // try to find safe separator
                        SafeSeparator safeSeparator = new SafeSeparator(graph);
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
                        ImmutableGraph immutableGraph = new ImmutableGraph(graph);
                        if (HasTreeWidth(immutableGraph, minK, out PTD subGraphTreeDecomp))
                        {
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
                    }
                    Console.WriteLine("graph {0} has treewidth larger than {1}.", graph.graphID, minK);
                    minK++;
                }

                if (ptds.Count == i)    // if graph is smaller than the minimum bound for tree width
                {
                    ptds.Add(new PTD(BitSet.All(graph.vertexCount)));   // TODO: correct?
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
                    ImmutableGraph immutableGraph = new ImmutableGraph(graph);
                    if (HasTreeWidth(immutableGraph, minK, out PTD subGraphTreeDecomp))
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
                        treeDecomp = null;
                        return false;
                    }
                }
                minK++;

                if (ptds.Count == i)    // if the vertex set is smaller than the minimum bound for tree width, make a bag that contains all vertices
                {
                    ptds.Add(new PTD(BitSet.All(graph.vertexCount)));   // TODO: correct?
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

        /// <summary>
        /// determines whether the tree width of a graph is at most a given value.
        /// (Really only used for faster testing. Will be obsolete when the idea to reuse the ptds and ptdurs during later iterations is implemented.)
        /// </summary>
        /// <param name="g">the graph</param>
        /// <param name="k">the upper bound</param>
        /// <param name="treeDecomp">a normalized canonical tree decomposition for the graph, iff the tree width is at most k, else null</param>
        /// <returns>true, iff the tree width is at most k</returns>
        public static bool IsTreeWidthAtMost2(Graph graph, int k, out PTD treeDecomp)
        {
            throw new NotImplementedException();
            /*
            // TODO: return root with empty bag instead
            if (graph.vertexCount == 0)
            {
                treeDecomp = null;
                return k == -1;
            }
            else if (graph.vertexCount == 1)
            {
                BitSet onlyBag = new BitSet(1);
                onlyBag[0] = true;
                treeDecomp = new PTD(onlyBag, null, null, null, new List<PTD>());
                return k == 0;
            }

            int minK = k;

            // TODO: use previous reductions, not make an entirely new one each time
            // TODO: when finding all clique/almost-clique separators, only do that once
            List<GraphReduction> reductions = new List<GraphReduction>();
            ImmutableGraph reducedGraph = graph;

            // reduce graph
            GraphReduction red = new GraphReduction(reducedGraph, k);
            bool reduced = red.Reduce(ref minK);
            if (minK > k)
            {
                treeDecomp = null;
                return false;
            }
            if (reduced)
            {
                reducedGraph = red.ToGraph();
                reductions.Add(red);
            }
            SafeSeparator safeSep = new SafeSeparator(reducedGraph);
            if (safeSep.Separate(out List<ImmutableGraph> separatedGraphs, ref minK))
            {
                if (minK > k)
                {
                    treeDecomp = null;
                    return false;
                }

                PTD[] subTreeDecompositions = new PTD[separatedGraphs.Count];
                for (int i = 0; i < subTreeDecompositions.Length; i++)
                {
                    if (IsTreeWidthAtMost(separatedGraphs[i], k, out subTreeDecompositions[i]))
                    {
#if DEBUG
                        subTreeDecompositions[i].AssertValidTreeDecomposition(separatedGraphs[i]);
#endif
                    }
                    else
                    {
                        treeDecomp = null;
                        return false;
                    }
                }
                treeDecomp = safeSep.RecombineTreeDecompositions(subTreeDecompositions);

                for (int i = reductions.Count - 1; i >= 0; i--)
                {
                    reductions[i].RebuildTreeDecomposition(ref treeDecomp);
                }
                return true;
            }

            if (HasTreeWidth(reducedGraph, minK, out treeDecomp))
            {
                Console.WriteLine("graph has tree width " + minK);
                for (int i = reductions.Count - 1; i >= 0; i--)
                {
                    reductions[i].RebuildTreeDecomposition(ref treeDecomp);
                }
                return true;
            }

            return false;
            */
        }

        /// <summary>
        /// determines whether this graph has tree width k
        /// </summary>
        /// <param name="k">the desired tree width</param>
        /// <param name="treeDecomp">a normalized canonical tree decomposition if there is one, else null</param>
        /// <returns>true, iff this graph has tree width k</returns>
        private static bool HasTreeWidth(ImmutableGraph graph, int k, out PTD treeDecomp)
        {
            if (graph.vertexCount == 0)
            {
                treeDecomp = new PTD(new BitSet(0));
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
                                treeDecomp = p1;
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
                                        treeDecomp = p2;
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
            //return !tau_tauprime_combined;                                                                              // test
            return !tau_tauprime_combined || !toTest.IsIncoming(graph);                                                   // mine
            //return (!tau_tauprime_combined || toTest.IsNormalized_Daniela(Tau)) && !toTest.IsIncoming_Daniela(this);  // Daniela old

            //return !tau_tauprime_combined || (toTest.IsNormalized_Daniela(Tau) && !toTest.IsIncoming_Daniela(this));  // Daniela very old
        }
    }
}
