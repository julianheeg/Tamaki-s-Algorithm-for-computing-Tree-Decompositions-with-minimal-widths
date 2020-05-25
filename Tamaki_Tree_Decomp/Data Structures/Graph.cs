using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    public class Graph
    {
        public readonly int vertexCount = -1;
        public readonly int edgeCount;

        public readonly int[][] adjacencyList;
        public readonly BitSet[] neighborSetsWithout;   // contains N(v)
        public readonly BitSet[] neighborSetsWith;      // contains N[v]
        public readonly BitSet allVertices;

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
                                for (int i = 0; i < vertexCount; i++)
                                {
                                    tempAdjacencyList[i] = new List<int>();
                                }
                            }
                            else
                            {
                                int u = Convert.ToInt32(tokens[0])-1;
                                int v = Convert.ToInt32(tokens[1])-1;
                                tempAdjacencyList[u].Add(v);
                                tempAdjacencyList[v].Add(u);
                            }
                        }
                    }
                }

                // copy lists to arrays
                adjacencyList = new int[vertexCount][];
                for (int i = 0; i < vertexCount; i++)
                {
                    adjacencyList[i] = tempAdjacencyList[i].ToArray();
                }
                neighborSetsWithout = new BitSet[vertexCount];
                neighborSetsWith = new BitSet[vertexCount];
                // fill neighbor sets
                for (int i = 0; i < vertexCount; i++)
                {
                    neighborSetsWithout[i] = new BitSet(vertexCount, adjacencyList[i]);
                    neighborSetsWith[i] = new BitSet(neighborSetsWithout[i]);
                    neighborSetsWith[i][i] = true;
                }
                allVertices = BitSet.All(vertexCount);
                Console.WriteLine("Graph has been imported.");
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
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
        }

        /// <summary>
        /// determines the tree width of this graph
        /// </summary>
        /// <param name="treeDecomp">a normalized canonical tree decomposition for the graph</param>
        /// <returns>the graph's tree width</returns>
        public int TreeWidth(out PTD treeDecomp)
        {
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
                GraphReduction red = new GraphReduction(reducedGraph, minK);
                bool reduced = red.Reduce(ref minK);
                if (reduced)
                {
                    reducedGraph = red.ToGraph();
                    reductions.Add(red);
                }
                SafeSeparator safeSep = new SafeSeparator(reducedGraph);
                if (safeSep.Separate(out List<Graph> separatedGraphs, ref minK))
                {
                    PTD[] subTreeDecompositions = new PTD[separatedGraphs.Count];
                    for (int i = 0; i < subTreeDecompositions.Length; i++)
                    {
                        int subTreeWidth = separatedGraphs[i].TreeWidth(minK, out subTreeDecompositions[i]);
                        Debug.Assert(separatedGraphs[i].IsValidTreeDecomposition(subTreeDecompositions[i]));
                        if (subTreeWidth > minK)
                        {
                            minK = subTreeWidth;
                        }
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
                    Console.WriteLine("graph has tree width " + minK);
                    for (int i = reductions.Count - 1; i >= 0; i--)
                    {
                        reductions[i].RebuildTreeDecomposition(ref treeDecomp);
                    }
                    // TODO: CRITICAL: possibly recalculate vertices, inlets, outlets in the tree decomposition
                    return minK;
                }
                Console.WriteLine("graph has tree width bigger than " + minK);
                minK++;
            }
            // TODO: correct? also loop condition above
            treeDecomp = new PTD(allVertices);
            return vertexCount - 1;
        }

        // statistics
        int totalPCount = 0;
        int totalUCount = 0;
        int line13RejectCount = 0;
        int line13DuplicateCount = 0;

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

            // TODO P and U can be cached so that they're not destroyed and rebuilt when k is incremented. In that case, they need to be cleared at the end of the method ()
            // TODO: P can be Queue so that old entries are discarded and some memory is saved.
            List<PTD> P = new List<PTD>();
            HashSet<BitSet> P_inlets = new HashSet<BitSet>();
            List<PTD> U = new List<PTD>();
            HashSet<BitSet> U_inlets = new HashSet<BitSet>();

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
                            P.Add(p0); // ptd mit Tasche N[v] als einzelnen Knoten
                            P_inlets.Add(p0.inlet);
                        }
                    }
                }
            }

            // --------- lines 7 to 32 ----------

            // TODO: parallel for loop?
            for (int i = 0; i < P.Count; i++)
            {
                PTD Tau = P[i];

                // --------- TODO: line 8 ----------

                Debug.Assert(IsMinimalSeparator(Tau.outlet));

                // --------- line 9 ----------

                PTD Tau_wiggle_original = PTD.Line9(Tau);

                // --------- lines 10 ----------

                if (!U_inlets.Contains(Tau_wiggle_original.inlet))
                {
                    Debug.Assert(IsConsistent(Tau_wiggle_original));
                    U.Add(Tau_wiggle_original);
                    U_inlets.Add(Tau_wiggle_original.inlet);
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

                        // --------- line 13 ----------

                        Tau_wiggle = PTD.Line13(Tau_prime, Tau, this);

                        // --------- line 14 ----------

                        if (Tau_wiggle.Bag.Count() <= k+1 && Tau_wiggle.IsPossiblyUsable() && IsCliquish(Tau_wiggle.Bag))
                        {
                            if (!U_inlets.Contains(Tau_wiggle.inlet))
                            {
                                Debug.Assert(IsConsistent(Tau_wiggle));
                                U.Add(Tau_wiggle);
                                U_inlets.Add(Tau_wiggle.inlet);
                            }
                            else
                            {
                                line13DuplicateCount++;
                                continue;
                            }

                        }
                        // --------- line 15 ----------
                        else
                        {
                            line13RejectCount++;
                            continue;
                        }
                    }
                    else
                    {
                        tau_tauprime_combined = false;
                        Tau_wiggle = Tau_wiggle_original;
                    }
                    

                    // --------- lines 16 to 20 ----------

                    if (Tau_wiggle.Bag.Count() <= k+1 && IsPotMaxClique(Tau_wiggle.Bag))
                    {
                        // TODO: perhaps no need to copy
                        // TODO: can at least copy after tests are done
                        PTD p1 = new PTD(Tau_wiggle);

                        // TODO: check for normalized-ness
                        if (!tau_tauprime_combined || p1.IsIncoming(this))
                        {
                            if (p1.vertices.Equals(allVertices))
                            {
                                PrintStats(P, U);
                                treeDecomp = p1;
                                return true;
                            }

                            if (!P_inlets.Contains(p1.inlet))
                            {
                                if (IsMinimalSeparator(p1.outlet))
                                {
                                    Debug.Assert(IsConsistent(p1));
                                    P.Add(p1);
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

                            if (adjacencyList[v].Length <= k && neighborSetsWith[v].IsSuperset(Tau_wiggle.Bag) && IsPotMaxClique(neighborSetsWith[v]))
                            {
                                // --------- line 23 ----------
                                PTD p2 = PTD.Line23(Tau_wiggle, neighborSetsWith[v], this);


                                // --------- line 24 ----------
                                // TODO: check for normalized-ness
                                if (!tau_tauprime_combined || p2.IsIncoming(this))
                                {
                                    if (p2.vertices.Equals(allVertices))
                                    {
                                        PrintStats(P, U);
                                        treeDecomp = p2;
                                        return true;
                                    }
                                    
                                    // --------- line 26 ----------
                                    if (!P_inlets.Contains(p2.inlet))
                                    {
                                        if (IsMinimalSeparator(p2.outlet))
                                        {
                                            Debug.Assert(IsConsistent(p2));
                                            P.Add(p2);
                                            P_inlets.Add(p2.inlet);
                                        }
                                    }
                                }

                            }
                            // Daniela: continue, wenn adjacecyList[v].Length > k
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

                        if (potNewRootBag.Count() <= k + 1 && IsPotMaxClique(potNewRootBag))
                        {
                            // --------- line 29 ----------
                            PTD p3 = PTD.Line29(Tau_wiggle, potNewRootBag, this);

                            // --------- line 30 ----------
                            // TODO: check for normalized-ness
                            if (!tau_tauprime_combined || p3.IsIncoming(this))
                            {
                                if (p3.vertices.Equals(allVertices))
                                {
                                    PrintStats(P, U);
                                    treeDecomp = p3;
                                    return true;
                                }

                                // --------- line 32 ----------
                                if (!P_inlets.Contains(p3.inlet))
                                {
                                    if (IsMinimalSeparator(p3.outlet))
                                    {
                                        Debug.Assert(IsConsistent(p3));
                                        P.Add(p3);
                                        P_inlets.Add(p3.inlet);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("considered {0} partial tree decompositions", P.Count);
            totalPCount += P.Count;
            totalUCount += U.Count;
            treeDecomp = null;
            return false;
        }

        private void PrintStats(List<PTD> P, List<PTD> U)
        {
            Console.WriteLine("total P: {0}, total U: {1}, Line 13 rejections: {2}, Line 13 duplicates: {3}", totalPCount + P.Count, totalUCount + U.Count, line13RejectCount, line13DuplicateCount);
        }

        #region IsPotMaxClique implementations

        /*
         * TODO: depth first search can be done using bit operations and it may be faster that way
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
        public IEnumerable<Tuple<BitSet, BitSet>> ComponentsAndNeighbors(BitSet separator)
        {
            // TODO: dfs using sets
            BitSet visited = new BitSet(separator);
            Stack<int> dfsStack = new Stack<int>();

            List<int> separatorBits = separator.Elements();

            // loop over all vertices in the separator
            for (int i = 0; i < separatorBits.Count; i++)
            {
                int separatorVertex = separatorBits[i];
                // loop over all neighbors of that separator-vertex
                for (int j = 0; j < adjacencyList[separatorVertex].Length; j++)
                {
                    int separatorNeighbor = adjacencyList[separatorVertex][j];

                    // if that vertex hasn't been visited, it's part of a new component. Perform a depth first search
                    if (!visited[separatorNeighbor])
                    {
                        dfsStack.Push(separatorNeighbor);
                        BitSet component = new BitSet(vertexCount);
                        BitSet N_C = new BitSet(vertexCount);    // cache N(C) for this component

                        while (dfsStack.Count > 0)
                        {
                            int vertex = dfsStack.Pop();
                            visited[vertex] = true;
                            component[vertex] = true;
                            for (int k = 0; k < adjacencyList[vertex].Length; k++)
                            {
                                int vNeighbor = adjacencyList[vertex][k];
                                if (separator[vNeighbor])
                                {
                                    N_C[vNeighbor] = true;
                                }
                                else if (!visited[vNeighbor])
                                {
                                    dfsStack.Push(vNeighbor);
                                }
                            }
                        }

                        yield return new Tuple<BitSet, BitSet>(component, N_C);
                    }
                }
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
            foreach (Tuple<BitSet, BitSet> C_NC in ComponentsAndNeighbors(separator))
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
            //Console.WriteLine("kein minimaler Separator");
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
            foreach (Tuple<BitSet, BitSet> C_NC in ComponentsAndNeighbors(separator))
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

        public bool IsCliquish(BitSet K)
        {
            // ------ 1.------

            List<BitSet> componentNeighborsInK = new List<BitSet>();
            foreach (Tuple<BitSet, BitSet> C_NC in ComponentsAndNeighbors(K))
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
        /// checks if K is a potential maximal clique
        /// </summary>
        /// <param name="K">the vertex set to test</param>
        /// <returns>true iff K is a potential maximal clique</returns>
        // TODO: cache lists, stack, sets, etc.
        public bool IsPotMaxClique(BitSet K)
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
            foreach(Tuple<BitSet, BitSet> C_NC in ComponentsAndNeighbors(K))
            {
                BitSet N_C = C_NC.Item2;
                if (N_C.Equals(K))
                {
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
            foreach (Tuple<BitSet, BitSet> C_NC in ComponentsAndNeighbors(K))
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
            while((pos = bag.NextElement(pos)) != -1)
            {
                neighbors.UnionWith(neighborSetsWithout[pos]);
            }
            neighbors.ExceptWith(vertices);

            // create outlet
            BitSet outlet = new BitSet(vertexCount);
            pos = -1;

            while ((pos = neighbors.NextElement(pos)) != -1)
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

        #endregion
    }
}
