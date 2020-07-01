#define statistics

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// a class for representing graphs and calculating tree decompositions
    /// </summary>
    public class Graph
    {
        public static bool old = true;

        public readonly int vertexCount = -1;
        public readonly int edgeCount;

        public readonly int[][] adjacencyList;
        public readonly BitSet[] neighborSetsWithout;   // contains N(v)
        public readonly BitSet[] neighborSetsWith;      // contains N[v]
        public readonly BitSet allVertices;

        private readonly int degree = 0;

        private readonly int graphID;
        private static int graphCount = 0;

        public static bool verbose = true;
        
        #region constructors

        /// <summary>
        /// constructs a graph from a .gr file
        /// </summary>
        /// <param name="filepath">the path to that file</param>
        public Graph(string filepath)
        {
            try
            {
                // temporary because we use arrays later instead of lists
                List<int>[] tempAdjacencyList = null;
                
                using (StreamReader sr = new StreamReader(filepath))
                {
                    while (!sr.EndOfStream)
                    {
                        String line = sr.ReadLine();
                        if (line.StartsWith("c"))
                        {
                            continue;
                        }
                        else
                        {
                            String[] tokens = line.Split(' ');
                            if (tokens[0] == "p")
                            {
                                vertexCount = Convert.ToInt32(tokens[2]);
                                edgeCount = Convert.ToInt32(tokens[3]);

                                tempAdjacencyList = new List<int>[vertexCount];
                                neighborSetsWithout = new BitSet[vertexCount];
                                for (int i = 0; i < vertexCount; i++)
                                {
                                    tempAdjacencyList[i] = new List<int>();
                                    neighborSetsWithout[i] = new BitSet(vertexCount);
                                }
                            }
                            else
                            {
                                int u = Convert.ToInt32(tokens[0])-1;
                                int v = Convert.ToInt32(tokens[1])-1;

                                if (!neighborSetsWithout[u][v])
                                {
                                    tempAdjacencyList[u].Add(v);
                                    tempAdjacencyList[v].Add(u);
                                    neighborSetsWithout[u][v] = true;
                                    neighborSetsWithout[v][u] = true;
                                }
                            }
                        }
                    }
                }

                // copy lists to arrays
                adjacencyList = new int[vertexCount][];
                for (int i = 0; i < vertexCount; i++)
                {
                    adjacencyList[i] = tempAdjacencyList[i].ToArray();
                    if (adjacencyList[i].Length > degree)
                    {
                        degree = adjacencyList[i].Length;
                    }
                }
                neighborSetsWith = new BitSet[vertexCount];
                // fill neighbor sets
                for (int i = 0; i < vertexCount; i++)
                {
                    neighborSetsWith[i] = new BitSet(neighborSetsWithout[i]);
                    neighborSetsWith[i][i] = true;
                }
                allVertices = BitSet.All(vertexCount);

                graphID = graphCount;
                graphCount++;

                // Console.WriteLine("Graph {0} has been imported.", graphID);

                Debug.Assert(NoLoopsAndDoubleEdges());
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        /// <summary>
        /// constructs a graph from an adjacency list
        /// </summary>
        /// <param name="adjacencyList">the adjacecy list for this graph</param>
        public Graph(List<int>[] adjacencyList)
        {
            vertexCount = adjacencyList.Length;
            this.adjacencyList = new int[vertexCount][];
            for (int i = 0; i < vertexCount; i++)
            {
                this.adjacencyList[i] = adjacencyList[i].ToArray();
                if (this.adjacencyList[i].Length > degree)
                {
                    degree = this.adjacencyList[i].Length;
                }
                edgeCount += this.adjacencyList[i].Length;
            }
            edgeCount /= 2;

            // fill neighbor sets
            neighborSetsWithout = new BitSet[vertexCount];
            neighborSetsWith = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                neighborSetsWithout[i] = new BitSet(vertexCount, this.adjacencyList[i]);
                neighborSetsWith[i] = new BitSet(neighborSetsWithout[i]);
                neighborSetsWith[i][i] = true;
            }

            allVertices = BitSet.All(vertexCount);

            graphID = graphCount;
            graphCount++;

            Debug.Assert(NoLoopsAndDoubleEdges());
        }

        #endregion

        #region algorithm

        /// <summary>
        /// determines the tree width of this graph
        /// </summary>
        /// <param name="treeDecomp">a normalized canonical tree decomposition for the graph</param>
        /// <returns>the graph's tree width</returns>
        public int TreeWidth(out PTD treeDecomp, bool verbose = true)
        {
            Graph.verbose = verbose;
            return TreeWidth(0, out treeDecomp);
        }

        /// <summary>
        /// determines the tree width of this graph while taking a known lower bound into account
        /// </summary>
        /// <param name="minK">the lower bound. Pass 0 if none is known.</param>
        /// <param name="treeDecomp">a normalized canonical tree decomposition for the graph</param>
        /// <returns>the graph's tree width</returns>
        public int TreeWidth(int minK, out PTD treeDecomp)
        {
            // TODO: return root with empty bag instead
            if (vertexCount == 0)
            {
                treeDecomp = null;
                return minK;
            }
            else if (vertexCount == 1)
            {
                BitSet onlyBag = new BitSet(1);
                onlyBag[0] = true;
                treeDecomp = new PTD(onlyBag, null, null, null, new List<PTD>());
                return minK;
            }

            // TODO: use previous reductions, not make an entirely new one each time
            // TODO: when finding all clique/almost-clique separators, only do that once
            List<GraphReduction> reductions = new List<GraphReduction>();
            Graph reducedGraph = this;
            while (minK < vertexCount - 1)
            {
                // reduce graph
                GraphReduction red = new GraphReduction(reducedGraph, minK, verbose);
                bool reduced = red.Reduce(ref minK);
                if (reduced)
                {
                    reducedGraph = red.ToGraph();
                    reductions.Add(red);
                }
                SafeSeparator safeSep = new SafeSeparator(reducedGraph, verbose);
                if (safeSep.Separate(out List<Graph> separatedGraphs, ref minK))
                {
                    PTD[] subTreeDecompositions = new PTD[separatedGraphs.Count];
                    for (int i = 0; i < subTreeDecompositions.Length; i++)
                    {
                        separatedGraphs[i].Dump();
                        int subTreeWidth = separatedGraphs[i].TreeWidth(minK, out subTreeDecompositions[i]);
                        Debug.Assert(separatedGraphs[i].IsValidTreeDecomposition(subTreeDecompositions[i]));
                        bool possiblyInduced = subTreeWidth == minK;
                        if (subTreeWidth > minK)
                        {
                            minK = subTreeWidth;
                        }
                        separatedGraphs[i].RenameDumped(subTreeWidth, possiblyInduced);
                    }
                    treeDecomp = safeSep.RecombineTreeDecompositions(subTreeDecompositions);
                    
                    for (int i = reductions.Count - 1; i >= 0; i--)
                    {
                        reductions[i].RebuildTreeDecomposition(ref treeDecomp);
                    }
                    return minK;
                }

                if (reducedGraph.HasTreeWidth(minK, out treeDecomp))
                {
                    if (verbose)
                    {
                        Console.WriteLine("graph {0} has tree width {1}", graphID, minK);
                    }
                    for (int i = reductions.Count - 1; i >= 0; i--)
                    {
                        reductions[i].RebuildTreeDecomposition(ref treeDecomp);
                    }

                    return minK;
                }
                if (verbose)
                {
                    Console.WriteLine("graph {0} has tree width bigger than {1}", graphID, minK);
                }
                minK++;               
            }
            treeDecomp = new PTD(allVertices);
            return vertexCount - 1;
        }

        /// <summary>
        /// determines whether the tree width of this graph is at most a given value.
        /// (Really only used for faster testing. Will be obsolete when the idea to reuse the ptds and ptdurs during later iterations is implemented.)
        /// </summary>
        /// <param name="k">the upper bound</param>
        /// <param name="treeDecomp">a normalized canonical tree decomposition for the graph, iff the tree width is at most k, else null</param>
        /// <returns>true, iff the tree width is at most k</returns>
        public bool IsTreeWidthAtMost(int k, out PTD treeDecomp)
        {
            // TODO: return root with empty bag instead
            if (vertexCount == 0)
            {
                treeDecomp = null;
                return k == -1;
            }
            else if (vertexCount == 1)
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
            Graph reducedGraph = this;

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
            if (safeSep.Separate(out List<Graph> separatedGraphs, ref minK))
            {
                if (minK > k)
                {
                    treeDecomp = null;
                    return false;
                }

                PTD[] subTreeDecompositions = new PTD[separatedGraphs.Count];
                for (int i = 0; i < subTreeDecompositions.Length; i++)
                {
                    if (separatedGraphs[i].IsTreeWidthAtMost(k, out subTreeDecompositions[i]))
                    {
                        Debug.Assert(separatedGraphs[i].IsValidTreeDecomposition(subTreeDecompositions[i]));
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

            if (reducedGraph.HasTreeWidth(minK, out treeDecomp))
            {
                Console.WriteLine("graph has tree width " + minK);
                for (int i = reductions.Count - 1; i >= 0; i--)
                {
                    reductions[i].RebuildTreeDecomposition(ref treeDecomp);
                }
                return true;
            }
           
            return false;
        }

        /// <summary>
        /// determines whether this graph has tree width k
        /// </summary>
        /// <param name="k">the desired tree width</param>
        /// <param name="treeDecomp">a normalized canonical tree decomposition if there is one, else null</param>
        /// <returns>true, iff this graph has tree width k</returns>
        public bool HasTreeWidth(int k, out PTD treeDecomp)
        {
            if (vertexCount == 0)
            {
                treeDecomp = null;
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

            for (int v = 0; v < vertexCount; v++)
            {
                if (adjacencyList[v].Length <= k && IsPotMaxClique(neighborSetsWith[v], out BitSet outlet))
                {
                    PTD p0 = new PTD(neighborSetsWith[v], outlet);
                    if (!P_inlets.Contains(p0.inlet))
                    {
                        if (IsMinimalSeparator(outlet))
                        {
                            //P.Add(p0); // ptd mit Tasche N[v] als einzelnen Knoten
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

                Debug.Assert(IsMinimalSeparator(Tau.outlet));

                // --------- line 9 ----------

                PTD Tau_wiggle_original = PTD.Line9(Tau);

                // --------- lines 10 ----------

                Debug.Assert(IsConsistent(Tau_wiggle_original));

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
                        
                        if (!PTD.Line13_CheckBagSize_CheckPossiblyUsable(Tau_prime, Tau, this, k, out Tau_wiggle))
                        {
                            // ---------- line 15 (first two cases) ----------
                            continue;
                        }

                        // --------- lines 14 and 15 (only the check for cliquish remains) ----------
                        
                        // TODO: cache cliquish, and see if that makes a difference
                        if (IsCliquish(Tau_wiggle.Bag))
                        {
                            // add the new ptdur only if no equivalent ptdur exists
                            if (!U_inletsWithIndex.ContainsKey(Tau_wiggle.inlet))
                            {
                                Debug.Assert(IsConsistent(Tau_wiggle));
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
                    if (Tau_wiggle.Bag.Count() <= k+1 && IsPotMaxClique(Tau_wiggle.Bag, out _))
                    {
                        // TODO: outlet from the isPotMaxClique calculation may be able to be used
                        // TODO: p1 is the same as tau_wiggle, so we can copy after tests are done. (We could also move this case to the end and not copy at all.)
                        //       Only do this if it becomes an issue because the code becomes less readable.
                        PTD p1 = new PTD(Tau_wiggle);

                        if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p1))
                        {
                            if (p1.vertices.Equals(allVertices))
                            {
                                treeDecomp = p1;
                                return true;
                            }

                            if (!P_inlets.Contains(p1.inlet))
                            {
                                if (IsMinimalSeparator(p1.outlet))
                                {
                                    Debug.Assert(IsConsistent(p1));
                                    //P.Add(p1);
                                    P.Push(p1);
                                    P_inlets.Add(p1.inlet);
                                }
                            }
                        }
                    }

                    // --------- lines 21 to 26 ----------

                    for (int v = 0; v < vertexCount; v++)
                    {
                        if (!Tau_wiggle.vertices[v])
                        {
                            // --------- lines 22 to 26 ----------

                            // TODO: isPotMaxClique can be pre calculated for every v and has in fact been done already during the leaf generation.
                            //       Just use a bool array for that (or BitSet) and get true or false here.
                            //       Change order of the check in that case so that this query is made first.

                            if (adjacencyList[v].Length <= k + 1 && neighborSetsWith[v].IsSupersetOf(Tau_wiggle.Bag) && IsPotMaxClique(neighborSetsWith[v], out _))
                            {
                                // --------- line 23 ----------
                                PTD p2 = PTD.Line23(Tau_wiggle, neighborSetsWith[v], this);

                                // --------- line 24 ----------
                                if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p2))
                                {
                                    if (p2.vertices.Equals(allVertices))
                                    {
                                        treeDecomp = p2;
                                        return true;
                                    }
                                    
                                    // --------- line 26 ----------
                                    if (!P_inlets.Contains(p2.inlet))
                                    {
                                        if (IsMinimalSeparator(p2.outlet))
                                        {
                                            Debug.Assert(IsConsistent(p2));
                                            //P.Add(p2);
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
                        BitSet potNewRootBag = new BitSet(neighborSetsWithout[v]);
                        potNewRootBag.ExceptWith(Tau_wiggle.inlet);
                        potNewRootBag.UnionWith(Tau_wiggle.Bag);

                        if (potNewRootBag.Count() <= k + 1 && IsPotMaxClique(potNewRootBag, out _))
                        {
                            // TODO: outlet from the isPotMaxClique calculation may be able to be used

                            // --------- line 29 ----------
                            PTD p3 = PTD.Line29(Tau_wiggle, potNewRootBag, this);

                            // --------- line 30 ----------
                            if (PassesIncomingAndNormalizedTest(Tau, tau_tauprime_combined, p3))
                            {
                                if (p3.vertices.Equals(allVertices))
                                {
                                    treeDecomp = p3;
                                    return true;
                                }

                                // --------- line 32 ----------
                                if (!P_inlets.Contains(p3.inlet))
                                {
                                    if (IsMinimalSeparator(p3.outlet))
                                    {
                                        Debug.Assert(IsConsistent(p3));
                                        //P.Add(p3);
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

        #endregion

        /// <summary>
        /// tests if a PTD is incoming and normalized
        /// </summary>
        /// <param name="Tau"></param>
        /// <param name="tau_tauprime_combined"></param>
        /// <param name="toTest"></param>
        /// <returns></returns>
        private bool PassesIncomingAndNormalizedTest(PTD Tau, bool tau_tauprime_combined, PTD toTest)
        {
            //return true;
            //return !tau_tauprime_combined;                                                                              // test
            return !tau_tauprime_combined || !toTest.IsIncoming(this);                                                   // mine
            //return (!tau_tauprime_combined || toTest.IsNormalized_Daniela(Tau)) && !toTest.IsIncoming_Daniela(this);  // Daniela old

            //return !tau_tauprime_combined || (toTest.IsNormalized_Daniela(Tau) && !toTest.IsIncoming_Daniela(this));  // Daniela very old
        }


        #region neighbors, components, cliquish, pmc, etc.

        /*
         * TODO: depth first search can be done using bit operations and it may be faster that way
         *       This is implemented in some of the functions here already, but not in all
         * 
         *  for every vertex v that is not in the separator{
         *      BitSet component = new BitSet(neighborSetsWith[v]);
         *      component.ExceptWith(separator);
         *      
         *      BitSet tempNeighbors = new BitSet(neighborSetsWithout[v]);
         *      tempNeighbors.ExceptWith(separator);
         *      while (tempNeighbors != 0){
         *          List nnnnnnn = tempNeighbors.Elements()
         *          tempNeighbors = empty;
         *          for every vertex u in nnnnnnn {
         *              tempNeighbors.unionWith(neighborSetsWithout[u];
         *              component.UnionWith(u); // or neighborSetsWith[u];
         *          }
         *          component.exceptWith(separator);
         *          tempNeighbors.exceptWith(separator);
         *      }
         *  }
         *  
         * */
        

        /// <summary>
        /// determines the components associated with a separator and also which of the vertices in the separator
        /// are neighbors of vertices in those components (which are a subset of the separator)
        /// </summary>
        /// <param name="separator">the separator</param>
        /// <returns>an enumerable consisting of tuples of i) component C and ii) neighbors N(C)</returns>
        public IEnumerable<(BitSet, BitSet)> ComponentsAndNeighbors(BitSet separator)
        {
            BitSet unvisited = new BitSet(separator);
            unvisited.Flip(vertexCount);

            // find components as long as they exist
            int startingVertex = -1;
            while ((startingVertex = unvisited.NextElement(startingVertex, getsReduced: true)) != -1)
            {
                BitSet currentIterationFrontier = new BitSet(vertexCount);
                currentIterationFrontier[startingVertex] = true;
                BitSet nextIterationFromtier = new BitSet(vertexCount);
                BitSet component = new BitSet(vertexCount);
                BitSet neighbors = Neighbors(component);

                while (!currentIterationFrontier.IsEmpty())
                {
                    nextIterationFromtier.Clear();

                    int redElement = -1;
                    while ((redElement = currentIterationFrontier.NextElement(redElement, getsReduced: false)) != -1)
                    {
                        nextIterationFromtier.UnionWith(neighborSetsWithout[redElement]);
                    }

                    component.UnionWith(currentIterationFrontier);
                    neighbors.UnionWith(nextIterationFromtier);
                    nextIterationFromtier.ExceptWith(separator);
                    nextIterationFromtier.ExceptWith(component);

                    currentIterationFrontier.CopyFrom(nextIterationFromtier);
                }

                // debug.Add((new BitSet(purple), new BitSet(neighbors)));
                unvisited.ExceptWith(component);
                neighbors.IntersectWith(separator);
                Debug.Assert(neighbors.Equals(Neighbors(component)));
                yield return (component, neighbors);
            }
        }

        /// <summary>
        /// determines the neighbor set of a set of vertices
        /// </summary>
        /// <param name="vertexSet">the set of vertices</param>
        /// <returns>the neighbor set of that set of vertices</returns>
        public BitSet Neighbors(BitSet vertexSet)
        {
            BitSet result = new BitSet(vertexCount);
            List<int> vertices = vertexSet.Elements();
            for (int i = 0; i < vertices.Count; i++)
            {
                result.UnionWith(neighborSetsWithout[vertices[i]]);
            }
            result.ExceptWith(vertexSet);
            return result;
        }

        /// <summary>
        /// tests if a set of vertices is a minimal separator
        /// </summary>
        /// <param name="separator">the set of vertices to test</param>
        /// <returns>true iff separator is a minimal separator</returns>
        public bool IsMinimalSeparator(BitSet separator)
        {
            int fullComponents = 0;
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(separator))
            {
                if (C_NC.Item2.Equals(separator))
                {
                    fullComponents++;
                    if (fullComponents == 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// tests whether a set of vertices is a minimal separator and if so, computes the components associated with it
        /// </summary>
        /// <param name="separator">the set of vertices</param>
        /// <param name="components">a list of the components associated with the set of vertices</param>
        /// <returns>true iff separator is a minimal separator</returns>
        public bool IsMinimalSeparator_ReturnComponents(BitSet separator, out List<BitSet> components)
        {
            components = new List<BitSet>();
            int fullComponents = 0;
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(separator))
            {
                components.Add(C_NC.Item1);
                if (C_NC.Item2.Equals(separator))
                {
                    fullComponents++;
                }
            }
            if (fullComponents >= 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// determines if a given vertex set is cliquish
        /// </summary>
        /// <param name="K">the vertex set</param>
        /// <returns>true, iff the vertex set is cliquish</returns>
        public bool IsCliquish(BitSet K)
        {
            // ------ 1.------

            List<BitSet> componentNeighborsInK = new List<BitSet>();
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(K))
            {
                BitSet N_C = C_NC.Item2;
                componentNeighborsInK.Add(C_NC.Item2);
            }
            List<int> KBits = K.Elements();

            // ------ 2.------
            // check if K is cliquish

            for (int i = 0; i < KBits.Count; i++)
            {
                int u = KBits[i];
                for (int j = i + 1; j < KBits.Count; j++)   // check only u and v with u < v
                {
                    int v = KBits[j];

                    // if a.) doesn't hold ...
                    if (!neighborSetsWithout[u][v])
                    {
                        bool b_satisfied = false;

                        // ... check if there is a component such that b.) doesn't hold ...
                        for (int k = 0; k < componentNeighborsInK.Count; k++)
                        {
                            if (componentNeighborsInK[k][u] && componentNeighborsInK[k][v])
                            {
                                b_satisfied = true;
                                break;
                            }
                        }
                        // ... if neither a.) nor b.) hold, K is not a potential maximal clique
                        if (!b_satisfied)
                        {
                            return false;
                        }
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// checks if K is a potential maximal clique and if so, computes the boundary vertices of K
        /// </summary>
        /// <param name="K">the vertex set to test</param>
        /// <param name="boundary">the boundary vertices of K if K is a potentially maximal clique</param>
        /// <returns>true and a bitset of the boundary vertices, iff K is a potential maximal clique. If K ist not a potential maximal clique, null is returned for the boundary vertices</returns>
        // TODO: cache lists, stack, sets, etc.
        public bool IsPotMaxClique(BitSet K, out BitSet boundary)
        {
            /*
             *  This method operates by the following principle:
             *  K is potential maximal clique <=>
             *      1. G\K has no full-components associated with K
             *      and
             *      2. K is cliquish
             * 
             *  For 1. we need to separate G into components C associated with K and check if N(C) is a proper subset of K,
             *      or in this case equivalently, if N(C) != K
             *  
             *  For 2. we need to check for each pair of vertices u,v in K if either of these conditions hold:
             *      a.) u,v are neighbors
             *      or
             *      b.) there exists C such that u,v are in N(C)
             * 
             *  During 1., while we determine the components, we save N(C) for each component so that we can use it in 2. later
             * 
             */


            // ------ 1.------

            List<BitSet> componentNeighborsInK = new List<BitSet>();
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(K))
            {
                BitSet N_C = C_NC.Item2;
                if (N_C.Equals(K))
                {
                    boundary = null;
                    return false;
                }
                componentNeighborsInK.Add(C_NC.Item2);
            }
            List<int> KBits = K.Elements();

            // ------ 2.------
            // check if K is cliquish

            for (int i = 0; i < KBits.Count; i++)
            {
                int u = KBits[i];
                for (int j = i + 1; j < KBits.Count; j++)   // check only u and v with u < v
                {
                    int v = KBits[j];

                    // if a.) doesn't hold ...
                    if (!neighborSetsWithout[u][v])
                    {
                        bool b_satisfied = false;

                        // ... check if there is a component such that b.) doesn't hold ...
                        for (int k = 0; k < componentNeighborsInK.Count; k++)
                        {
                            if (componentNeighborsInK[k][u] && componentNeighborsInK[k][v])
                            {
                                b_satisfied = true;
                                break;
                            }
                        }
                        // ... if neither a.) nor b.) hold, K is not a potential maximal clique
                        if (!b_satisfied)
                        {
                            boundary = null;
                            return false;
                        }
                    }

                }
            }

            boundary = new BitSet(vertexCount);
            for (int i = 0; i < componentNeighborsInK.Count; i++)
            {
                boundary.UnionWith(componentNeighborsInK[i]);
            }
            return true;
        }

        #endregion

        /// <summary>
        /// computes the outlet of the union of a ptd with a component
        /// </summary>
        /// <param name="ptd">the ptd</param>
        /// <param name="component">the component</param>
        /// <returns>the vertices in the outlet of ptd that are adjacent to vertices that are neither contained in the ptd nor in the component</returns>
        public BitSet UnionOutlet(PTD ptd, BitSet component)
        {
            
            BitSet unionOutlet = new BitSet(vertexCount);
            BitSet unionVertices = new BitSet(ptd.vertices);
            unionVertices.UnionWith(component);

            List<int> outletVertices = ptd.outlet.Elements();
            for (int i = 0; i < outletVertices.Count; i++)
            {
                int u = outletVertices[i];
                for (int j = 0; j < adjacencyList[u].Length; j++)
                {
                    int v = adjacencyList[u][j];
                    if (!unionVertices[v])
                    {
                        unionOutlet[u] = true;
                        break;
                    }
                }
            }
            return unionOutlet;
        }

        /// <summary>
        /// determines the outlet of a PTD with a given bag as the root node and a given vertex set
        /// </summary>
        /// <param name="bag">the PTD's root bag</param>
        /// <param name="vertices">the PTD's vertex set</param>
        /// <returns>the outlet of the PTD</returns>
        public BitSet Outlet(BitSet bag, BitSet vertices)
        {
            /*
            // create neighbor set
            List<int> elements = bag.Elements();
            BitSet bitSet = new BitSet(vertexCount);
            for (int i = 0; i < elements.Count; i++)
            {
                bitSet.UnionWith(neighborSetsWithout[elements[i]]);
            }
            bitSet.ExceptWith(vertices);

            // create outlet
            elements = bitSet.Elements();
            bitSet.Clear();
            for (int i = 0; i < elements.Count; i++)
            {
                bitSet.UnionWith(neighborSetsWithout[elements[i]]);
            }
            bitSet.IntersectWith(vertices);
            return bitSet;
            */
            
            // create neighbor set            
            BitSet neighbors = new BitSet(vertexCount);
            int pos = -1;
            while((pos = bag.NextElement(pos, getsReduced: false)) != -1)
            {
                neighbors.UnionWith(neighborSetsWithout[pos]);
            }
            neighbors.ExceptWith(vertices);

            // create outlet
            BitSet outlet = new BitSet(vertexCount);
            pos = -1;

            while ((pos = neighbors.NextElement(pos, getsReduced: false)) != -1)
            {
                outlet.UnionWith(neighborSetsWithout[pos]);
            }
            outlet.IntersectWith(vertices);

            return outlet;
            
        }

        #region debug

        /// <summary>
        /// tests whether a given ptd is consistent
        /// </summary>
        /// <param name="ptd">the ptd to test</param>
        /// <returns>true iff the ptd doesn't violate the consistency property</returns>
        private bool IsConsistent(PTD ptd)
        {
            // edge cases
            if (ptd == null)
            {
                if (vertexCount == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // create a list of all bags
            List<BitSet> bagsList = new List<BitSet>();
            List<int> parentBags = new List<int>();
            Stack<PTD> childrenStack = new Stack<PTD>();
            Stack<int> parentStack = new Stack<int>();
            childrenStack.Push(ptd);
            parentStack.Push(-1);
            while (childrenStack.Count > 0)
            {
                PTD current = childrenStack.Pop();
                int parent = parentStack.Pop();

                bagsList.Add(current.Bag);
                parentBags.Add(parent);
                foreach (PTD child in current.children)
                {
                    childrenStack.Push(child);
                    parentStack.Push(bagsList.Count - 1);
                }
            }

            // check consistency
            for (int i = 0; i < vertexCount; i++)
            {
                /*
                 *  key insight: all bags containing i form a subtree.
                 *  Therefore, in order for the tree decomposition to be consistent, there must be only one root for all subtrees containing i 
                 */
                HashSet<int> ancestors = new HashSet<int>();
                for (int j = 0; j < bagsList.Count; j++)
                {
                    if (bagsList[j][i] == true)
                    {
                        int currentAncestor = j;
                        int parentBag = parentBags[j];
                        while (parentBag != -1 && bagsList[parentBag][i])
                        {
                            currentAncestor = parentBag;
                            parentBag = parentBags[currentAncestor];
                            ;
                        }
                        ancestors.Add(currentAncestor);
                        if (ancestors.Count == 2)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// tests whether the given tree decomposition is a valid tree decomposition for this graph
        /// </summary>
        /// <param name="td">the tree decomposition to test</param>
        /// <returns>true iff the tree decomposition is valid</returns>
        public bool IsValidTreeDecomposition(PTD td)
        {
            // edge cases
            if (td == null)
            {
                if (vertexCount == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // create a list of all bags
            List<BitSet> bagsList = new List<BitSet>();
            List<int> parentBags = new List<int>();
            Stack<PTD> childrenStack = new Stack<PTD>();
            Stack<int> parentStack = new Stack<int>();
            childrenStack.Push(td);
            parentStack.Push(-1);
            while (childrenStack.Count > 0)
            {
                PTD current = childrenStack.Pop();
                int parent = parentStack.Pop();

                bagsList.Add(current.Bag);
                parentBags.Add(parent);
                foreach(PTD child in current.children)
                {
                    childrenStack.Push(child);
                    parentStack.Push(bagsList.Count - 1);
                }
            }

            // check vertex cover
            for (int i = 0; i < vertexCount; i++)
            {
                bool isCovered = false;
                foreach(BitSet bag in bagsList)
                {
                    if (bag[i])
                    {
                        isCovered = true;
                        break;
                    }
                }
                if (!isCovered)
                {
                    return false;
                }
            }

            // check edge cover
            for (int u = 0; u < vertexCount; u++)
            {
                foreach (int v in adjacencyList[u])
                {
                    bool isCovered = false;
                    foreach (BitSet bag in bagsList)
                    {
                        if (bag[u] && bag[v])
                        {
                            isCovered = true;
                            break;
                        }
                    }
                    if (!isCovered)
                    {
                        return false;
                    }
                }
            }

            // check consistency
            for (int i = 0; i < vertexCount; i++)
            {
                /*
                 *  key insight: all bags containing i form a subtree.
                 *  Therefore, in order for the tree decomposition to be consistent, there must be only one root for all subtrees containing i 
                 */
                HashSet<int> ancestors = new HashSet<int>();
                for (int j = 0; j < bagsList.Count; j++)
                {
                    if (bagsList[j][i] == true)
                    {
                        int currentAncestor = j;
                        int parentBag = parentBags[j];
                        while (parentBag != -1 && bagsList[parentBag][i])
                        {
                            currentAncestor = parentBag;
                            parentBag = parentBags[currentAncestor];
                            ;
                        }
                        ancestors.Add(currentAncestor);
                        if (ancestors.Count == 2)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        private bool NoLoopsAndDoubleEdges()
        {
            for (int v = 0; v < vertexCount; v++)
            {
                BitSet neighbors = new BitSet(vertexCount);
                for (int j = 0; j < adjacencyList[v].Length; j++)
                {
                    int neighbor = adjacencyList[v][j];
                    if (neighbors[neighbor])
                    {
                        return false;
                    }
                    neighbors[neighbor] = true;
                }
                if (!neighbors.Equals(neighborSetsWithout[v]))
                {
                    return false;
                }
            }
            return true;
        }


        public static bool dumpSubgraphs = false;

        /// <summary>
        /// writes the graph to disk
        /// </summary>
        [Conditional("DEBUG")]
        public void Dump()
        {
            if (dumpSubgraphs)
            {
                // TODO: doesn't work in all languages/cultures
                Directory.CreateDirectory(Program.date_time_string);
                using (StreamWriter sw = new StreamWriter(String.Format(Program.date_time_string + "\\{0:D3}-.gr", graphID)))
                {
                    int vertexCount = adjacencyList.Length;
                    int edgeCount = 0;
                    for (int i = 0; i < vertexCount; i++)
                    {
                        edgeCount += adjacencyList[i].Length;
                    }
                    edgeCount /= 2;

                    sw.WriteLine(String.Format("p tw {0} {1}", vertexCount, edgeCount));
                    Console.WriteLine("Dumped graph {0} with {1} vertices and {2} edges", graphID, vertexCount, edgeCount);

                    for (int u = 0; u < vertexCount; u++)
                    {
                        for (int j = 0; j < adjacencyList[u].Length; j++)
                        {
                            int v = adjacencyList[u][j];
                            if (u < v)
                            {
                                sw.WriteLine(String.Format("{0} {1}", u + 1, v + 1));
                            }
                        }
                    }
                }
            }
        }

        /// changes the graph's name on disk so that it reflects which tree width this algorithm has determined for it
        /// <param name="treeWidth">the tree width</param>
        /// <param name="possiblyInduced">whether that tree width might be larger than if it would be if the subgraph was run alone</param>
        [Conditional("DEBUG")]
        public void RenameDumped(int treeWidth, bool possiblyInduced)
        {
            if (dumpSubgraphs)
            {
                File.Move(String.Format(Program.date_time_string + "\\{0:D3}-.gr", graphID), String.Format(Program.date_time_string + "\\{0:D3}-{1:D1}{2}.gr", graphID, treeWidth, possiblyInduced ? "i" : ""));
                Console.WriteLine("dumped graph {0} with {1} vertices and {2} edges has tree width {3}", graphID, vertexCount, edgeCount, treeWidth);
            }
        }

        #endregion
    }
}
