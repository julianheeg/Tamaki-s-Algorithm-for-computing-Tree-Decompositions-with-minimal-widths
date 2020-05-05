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
        }

        /// <summary>
        /// determines the tree width of this graph
        /// </summary>
        /// <param name="treeDecomp">the normalized canonical tree decomposition</param>
        /// <returns>the tree width</returns>
        public int TreeWidth(out PTD treeDecomp)
        {
            // edge cases
            // TODO: this isn't exhaustive
            
            if (vertexCount == 0)
            {
                treeDecomp = null;
                return 0;
            }
            else if (vertexCount == 1)
            {
                BitSet onlyBag = new BitSet(1);
                onlyBag[0] = true;
                treeDecomp = new PTD(onlyBag, null, null, new List<PTD>());
                return 0;
            }

            for (int k = 1; k <= vertexCount; k++)
            {
                if (HasTreeWidth(k, out treeDecomp))
                {
                    Console.WriteLine("graph has tree width " + k);
                    return k;
                }
                Console.WriteLine("graph has tree width bigger than " + k);
            }
            treeDecomp = null;
            return -1;
        }

        public int TreeWidth(int minK, out PTD treeDecomp)
        {
            if (vertexCount == 0)
            {
                treeDecomp = null;
                return minK;
            }
            else if (vertexCount == 1)
            {
                BitSet onlyBag = new BitSet(1);
                onlyBag[0] = true;
                treeDecomp = new PTD(onlyBag, null, null, new List<PTD>());
                return minK;
            }

            // TODO: use previous reductions, not make an entirely new one each time
            List<GraphReduction> reductions = new List<GraphReduction>();
            Graph reducedGraph = this;
            while (minK <= vertexCount)
            {
                // reduce graph
                GraphReduction red = new GraphReduction(reducedGraph, minK);
                bool reduced = red.Reduce(ref minK);
                if (reduced)
                {
                    reducedGraph = red.ToGraph();
                    reductions.Add(red);
                }
                if (reducedGraph.HasTreeWidth(minK, out treeDecomp))
                {
                    Console.WriteLine("graph has tree width " + minK);
                    for (int i = reductions.Count - 1; i >= 0; i--)
                    {
                        reductions[i].RebuildTreeDecomposition(ref treeDecomp);
                    }
                    return minK;
                }
                Console.WriteLine("graph has tree width bigger than " + minK);
                minK++;
            }
            treeDecomp = null;
            return -1;

            /*
            GraphReduction red = new GraphReduction(this, minK);
            bool reduced = red.Reduce(ref minK);
            if (!reduced)
            {
                for (int k = minK; k <= vertexCount; k++)
                {
                    if (HasTreeWidth(k, out treeDecomp))
                    {
                        Console.WriteLine("graph has tree width " + k);
                        return k;
                    }
                    Console.WriteLine("graph has tree width bigger than " + k);
                }
            }
            else
            {
                Graph reducedGraph = red.ToGraph();
                int treeWidth = reducedGraph.TreeWidth(minK, out PTD reducedTreeDecomp);
                red.RebuildTreeDecomposition(ref reducedTreeDecomp);
                treeDecomp = reducedTreeDecomp;
                return treeWidth;
            }
            treeDecomp = null;
            return -1;
            */
        }

        /// <summary>
        /// determines whether this graph has tree width k
        /// </summary>
        /// <param name="k">the desired tree width</param>
        /// <param name="treeDecomp">the normalized canonical tree decomposition if there is one, else null</param>
        /// <returns></returns>
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

            // --------- lines 1 to 3 ----------

            for (int i = 0; i < vertexCount; i++)
            {
                if (adjacencyList[i].Length <= k && IsPotMaxClique(neighborSetsWith[i], out BitSet outlet))
                {
                    PTD newOne = new PTD(neighborSetsWith[i], outlet);
                    //if (IsIncoming(newOne))
                    //{
                    if (!P_inlets.Contains(newOne.inlet))
                    {
                        P.Add(newOne); // ptd mit Tasche N[v] als einzelnen Knoten
                        P_inlets.Add(newOne.inlet);
                    }
                    //}
                }
            }

            // --------- lines 4 to 21 ---------

            int PIndex = 0;
            while (PIndex < P.Count)
            {
                if (PIndex > 10000)
                {
                    Console.WriteLine("PIndex > 10000. Stopping");
                    throw new Exception();
                }


                PTD Tau = P[PIndex];
                PIndex++;

                if (!IsConsistent(Tau))
                {
                    Console.WriteLine("Tau not consistent");
                }

                // --------- lines 5 and 6 ---------
                PTD Tau_wiggle = PTD.Line5(Tau);
                #region assertion
                Debug.Assert(Tau_wiggle.Bag.Count() <= k + 1);

                // TODO: remove if this assertion never fails
                BitSet outlet = new BitSet(Tau_wiggle.outlet);
                BitSet inlet = new BitSet(Tau_wiggle.inlet);
                RecalculateInletAndOutlet(Tau_wiggle);
                if (!outlet.Equals(Tau_wiggle.outlet))
                {
                    Console.WriteLine("hello");
                }
                Debug.Assert(outlet.Equals(Tau_wiggle.outlet));
                Debug.Assert(inlet.Equals(Tau_wiggle.inlet));
                #endregion

                //if (IsIncoming(Tau_wiggle))
                //{
                if (!U_inlets.Contains(Tau_wiggle.inlet))
                {
                    if (!IsConsistent(Tau_wiggle))
                    {
                        Console.WriteLine("Tau_wiggle not consistent");
                    }

                    U.Add(Tau_wiggle);
                    U_inlets.Add(Tau_wiggle.inlet);
                }
                //}

                // --------- lines 7 to 19 ---------

                int UIndex = 0;
                while (UIndex < U.Count)
                {
                    PTD Tau_prime = U[UIndex];
                    UIndex++;

                    if (!IsConsistent(Tau_prime))
                    {
                        Console.WriteLine("Tau_prime not consistent");
                    }

                    //---------lines 8 to 11--------
                    bool isChain = false;

                    if (!Tau_wiggle.Equals(Tau_prime))
                    {
                        // --------- line 9 ---------

                        Tau_wiggle = PTD.Line9(Tau_prime, Tau, this);

                        // --------- lines 10 and 11 ---------
                        if (Tau_wiggle.IsPossiblyUsable(k))
                        {
                            //if (IsIncoming(Tau_wiggle))
                            //{
                            if (!U_inlets.Contains(Tau_wiggle.inlet))
                            {
                                if (!IsConsistent(Tau_wiggle))
                                {
                                    Console.WriteLine("Tau_wiggle not consistent");
                                }
                                U.Add(Tau_wiggle);
                                U_inlets.Add(Tau_wiggle.inlet);
                            }
                            //}
                        }                        
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        isChain = true;
                    }

                    // --------- lines 12 and 13 ---------
                    if (IsPotMaxClique(Tau_wiggle.Bag))
                    {
                        //if (IsIncoming(Tau_wiggle, Tau, Tau_prime, isChain))
                        //{
                        if (Tau_wiggle.IsIncoming_UniqueRoot())
                        {
                            if (!P_inlets.Contains(Tau_wiggle.inlet))
                            {
                                if (!IsConsistent(Tau_wiggle))
                                {
                                    Console.WriteLine("Tau_wiggle not consistent");
                                }
                                P.Add(Tau_wiggle);
                                P_inlets.Add(Tau_wiggle.inlet);
                            }
                        }
                        //}
                    }

                    // --------- lines 14 to 16 ---------
                    for (int v = 0; v < vertexCount; v++)
                    {
                        if (!Tau_wiggle.outlet[v] && !Tau_wiggle.inlet[v])
                        {
                            if (adjacencyList[v].Length <= k && neighborSetsWith[v].IsSuperset(Tau_wiggle.Bag))
                            {
                                PTD newOne = PTD.Line16(Tau_wiggle, neighborSetsWith[v], this);
                                #region assertion
                                Debug.Assert(newOne.Bag.Count() <= k + 1);
                                #endregion
                                //if (IsIncoming(newOne, Tau, Tau_prime, isChain))
                                //{
                                if (newOne.IsIncoming_UniqueRoot())
                                {
                                    if (!P_inlets.Contains(newOne.inlet))
                                    {
                                        if (!IsConsistent(newOne))
                                        {
                                            Console.WriteLine("newOne not consistent");
                                        }
                                        P.Add(newOne);
                                        P_inlets.Add(newOne.inlet);
                                    }
                                }
                                //}
                            }
                        }
                    }

                    // --------- lines 17 to 19 ---------
                    List<int> X_r = Tau_wiggle.Bag.Elements();
                    for (int i = 0; i < X_r.Count; i++)
                    {
                        BitSet potMaxClique = new BitSet(Tau_wiggle.Bag);
                        potMaxClique.UnionWith(neighborSetsWith[X_r[i]]);
                        potMaxClique.ExceptWith(Tau_wiggle.inlet);
                        
                        if (IsPotMaxClique(potMaxClique, out _))
                        {
                            PTD newOne = PTD.Line19(Tau_wiggle, potMaxClique, this);
                            if (newOne.Bag.Count() <= k + 1)
                            {
                                //if (IsIncoming(newOne, Tau, Tau_prime, isChain))
                                //{
                                if (newOne.IsIncoming_UniqueRoot())
                                {
                                    if (!P_inlets.Contains(newOne.inlet))
                                    {
                                        if (!IsConsistent(newOne))
                                        {
                                            Console.WriteLine("newOne not consistent");
                                        }
                                        P.Add(newOne);
                                        P_inlets.Add(newOne.inlet);
                                    }
                                }
                                //}
                            }                            
                        }
                    }
                }


                // --------- lines 20 and 21 (moved here for earlier exit if a tree decomposition is found) ---------

                if (BitSet.UnionContainsAll(Tau.inlet, Tau.outlet, vertexCount))
                {
                    treeDecomp = Tau;
                    return true;
                }
            }

            // --------- lines 22 to 23 ---------

            treeDecomp = null;
            return false;
        }

        #region IsPotMaxClique implementations

        /// <summary>
        /// checks if K is a potential maximal clique
        /// </summary>
        /// <param name="K">the vertex set to test</param>
        /// <returns>true iff K is a potential maximal clique</returns>
        // TODO: erst mal HashSet, vielleicht noch ändern
        // Falls möglich, List oder sogar Array oder Bitset
        // TODO: cache lists, stack, sets, etc.
        public bool IsPotMaxClique(HashSet<int> K)
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

            // TODO: could be BitSet if need be, but would perhaps be slower (?)
            bool[] visited = new bool[vertexCount];
            foreach (int KVertex in K)
            {
                visited[KVertex] = true;
            }
            Stack<int> dfsStack = new Stack<int>();

            // for each component C there is an entry in this list that contains N(C)
            // TODO: could be bitset, might be faster
            List<HashSet<int>> componentNeighborsInK = new List<HashSet<int>>();

            // loop over all vertices in K
            foreach (int KVertex in K)
            {
                // loop over all neighbors of that K-vertex
                for (int i = 0; i < adjacencyList[KVertex].Length; i++)
                {
                    int KNeighbor = adjacencyList[KVertex][i];

                    // if that vertex hasn't been visited, it's part of a new component. Perform a depth first search
                    if (!visited[KNeighbor])
                    {
                        dfsStack.Push(KNeighbor);
                        HashSet<int> N_C = new HashSet<int>();    // cache N(C) for this component

                        while (dfsStack.Count > 0)
                        {
                            int vertex = dfsStack.Pop();
                            visited[vertex] = true;
                            for (int j = 0; j < adjacencyList[vertex].Length; j++)
                            {
                                int vNeighbor = adjacencyList[vertex][j];
                                if (K.Contains(vNeighbor))
                                {
                                    N_C.Add(vNeighbor);
                                }
                                else if (!visited[vNeighbor])
                                {
                                    dfsStack.Push(vNeighbor);
                                }
                            }
                        }

                        // check if that component is non-full, else return false
                        if (N_C.SetEquals(K))
                        {
                            return false;
                        }
                        componentNeighborsInK.Add(N_C);
                    }
                }
            }

            // ------ 2.------
            // check if K is cliquish
            foreach (int u in K)
            {
                foreach (int v in K)
                {
                    if (u < v)  // exclude the case where u==v and also avoid checking twice when the roles of u and v are reversed 
                    {
                        // if a.) doesn't hold ...
                        if (!adjacencyList[u].Contains(v))
                        {
                            bool b_satisfied = false;

                            // ... check if there is a component such that b.) doesn't hold ...
                            for (int i = 0; i < componentNeighborsInK.Count; i++)
                            {
                                if (componentNeighborsInK[i].Contains(u) && componentNeighborsInK[i].Contains(v))
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

            // TODO: could be BitSet if need be, but would perhaps be slower (?)
            BitSet visited = new BitSet(K);
            Stack<int> dfsStack = new Stack<int>();

            // for each component C there is an entry in this list that contains N(C)
            List<BitSet> componentNeighborsInK = new List<BitSet>();
            List<int> KBits = K.Elements();

            // loop over all vertices in K
            for (int i = 0; i < KBits.Count; i++)
            {
                int KVertex = KBits[i];
                // loop over all neighbors of that K-vertex
                for (int j = 0; j < adjacencyList[KVertex].Length; j++)
                {
                    int KNeighbor = adjacencyList[KVertex][j];

                    // if that vertex hasn't been visited, it's part of a new component. Perform a depth first search
                    if (!visited[KNeighbor])
                    {
                        dfsStack.Push(KNeighbor);
                        BitSet N_C = new BitSet(vertexCount);    // cache N(C) for this component

                        while (dfsStack.Count > 0)
                        {
                            int vertex = dfsStack.Pop();
                            visited[vertex] = true;
                            for (int k = 0; k < adjacencyList[vertex].Length; k++)
                            {
                                int vNeighbor = adjacencyList[vertex][k];
                                if (K[vNeighbor])
                                {
                                    N_C[vNeighbor] = true;
                                }
                                else if (!visited[vNeighbor])
                                {
                                    dfsStack.Push(vNeighbor);
                                }
                            }
                        }

                        // check if that component is non-full, else return false
                        if (N_C.Equals(K))
                        {
                            return false;
                        }
                        componentNeighborsInK.Add(N_C);
                    }
                }
            }

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
            
            // usual dfs stuff
            BitSet visited = new BitSet(K);
            Stack<int> dfsStack = new Stack<int>();

            // for each component C there is an entry in this list that contains N(C)
            List<BitSet> componentNeighborsInK = new List<BitSet>();
            List<int> KBits = K.Elements();

            // loop over all vertices in K
            for (int i = 0; i < KBits.Count; i++)
            {
                int KVertex = KBits[i];
                // loop over all neighbors of that K-vertex
                for (int j = 0; j < adjacencyList[KVertex].Length; j++)
                {
                    int KNeighbor = adjacencyList[KVertex][j];

                    // if that vertex hasn't been visited, it's part of a new component. Perform a depth first search
                    if (!visited[KNeighbor])
                    {
                        dfsStack.Push(KNeighbor);
                        BitSet N_C = new BitSet(vertexCount);    // cache N(C) for this component

                        while (dfsStack.Count > 0)
                        {
                            int vertex = dfsStack.Pop();
                            visited[vertex] = true;
                            for (int k = 0; k < adjacencyList[vertex].Length; k++)
                            {
                                int vNeighbor = adjacencyList[vertex][k];
                                if (K[vNeighbor])
                                {
                                    N_C[vNeighbor] = true;
                                }
                                else if (!visited[vNeighbor])
                                {
                                    dfsStack.Push(vNeighbor);
                                }
                            }
                        }

                        // check if that component is non-full, else return false
                        if (N_C.Equals(K))
                        {
                            boundary = null;
                            return false;
                        }
                        componentNeighborsInK.Add(N_C);
                    }
                }
            }

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
        /// possibly very naive way of recalculating the inlet and outlet of a PTD
        /// </summary>
        public void RecalculateInletAndOutlet(PTD ptd)
        {
            BitSet outlet = ptd.outlet;
            BitSet inlet = ptd.inlet;

            // TODO: not the naive way. Look at what changes and calculate inlet and outlet directly
            
            // inlets of children are also inlet of ptd
            // TODO: possibly unnecessary
            for (int i = 0; i < ptd.children.Count; i++)
            {
                inlet.UnionWith(ptd.children[i].inlet);
            }

            List<int> X_r = ptd.Bag.Elements();
            for (int i = 0; i < X_r.Count; i++)
            {
                int v = X_r[i];

                // v is in the outlet iff there exists an edge from v to a vertex that is neither in the inlet nor in the bag
                bool isInOutlet = false;
                for (int j = 0; j < adjacencyList[v].Length; j++)
                {
                    int neighbor = adjacencyList[v][j];
                    if (!inlet[neighbor] && !ptd.Bag[neighbor])
                    {
                        isInOutlet = true;
                        outlet[v] = true;
                        break;
                    }
                }
                if (!isInOutlet)
                {
                    inlet[v] = true;
                    outlet[v] = false;
                }
            }
        }

        public bool IsIncoming(PTD Tau_wiggle, PTD Tau, PTD Tau_prime, bool isChain)
        {
            // TODO: implement properly
            return true;

            
            // 1
            if (isChain)
            {
                return true;
            }

            // 2a
            if (Tau_wiggle.outlet.IsSuperset(Tau.outlet))
            {
                return false;
            }

            // 2b
            int f = Tau_prime.inlet.First();
            if (f < Tau.inlet.First() && f < Tau.outlet.First())
            {
                return true;
            }
            return false;
            

            /*
            // usual dfs stuff
            BitSet visited = new BitSet(ptd.inlet);
            visited.UnionWith(ptd.outlet);
            Stack<int> dfsStack = new Stack<int>();

            List<int> separatorVertices = ptd.outlet.Elements();

            for (int i = 0; i < separatorVertices.Count; i++)
            {
                int sVertex = separatorVertices[i];

                // loop over all neighbors of that s-vertex
                for (int j = 0; j < adjacencyList[sVertex].Length; j++)
                {
                    int sNeighbor = adjacencyList[sVertex][j];

                    // if that vertex hasn't been visited, it's part of a new component. Perform a depth first search
                    if (!visited[sNeighbor])
                    {
                        dfsStack.Push(sNeighbor);
                        BitSet N_C = new BitSet(vertexCount);    // cache N(C) for this component
                        int componentSmallest = vertexCount + 1;

                        while (dfsStack.Count > 0)
                        {
                            int vertex = dfsStack.Pop();
                            visited[vertex] = true;
                            if (vertex < componentSmallest)
                            {
                                componentSmallest = vertex;
                            }
                            for (int k = 0; k < adjacencyList[vertex].Length; k++)
                            {
                                int vNeighbor = adjacencyList[vertex][k];
                                if (ptd.outlet[vNeighbor])
                                {
                                    N_C[vNeighbor] = true;
                                }
                                else if (!visited[vNeighbor])
                                {
                                    dfsStack.Push(vNeighbor);
                                }
                            }
                        }

                        // check if that component is non-full, else return false
                        if (componentSmallest < inletSmallest && !ptd.outlet.IsSuperset(N_C))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
            */
        }

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

    }
}
