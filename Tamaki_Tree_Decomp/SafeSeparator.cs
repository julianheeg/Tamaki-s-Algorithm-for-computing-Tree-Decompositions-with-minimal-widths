using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Tamaki_Tree_Decomp.Data_Structures;
using static Tamaki_Tree_Decomp.Data_Structures.Graph;

namespace Tamaki_Tree_Decomp
{
    /// <summary>
    /// a class for finding safe separators and handling the reconstruction of partial tree decompositions
    /// </summary>
    public partial class SafeSeparator
    {
        Graph graph;

        public BitSet separator;
        SeparatorType separatorType;
        public int separatorSize;

        enum SeparatorType
        {
            NotConnected, Size1, Size2, Size3, Clique, AlmostClique, CliqueMinor
        }

        // TODO: make private
        public List<ReindexationMapping> reconstructionIndexationMappings; // mapping from reduced vertex id to original vertex id, by component

        private readonly bool verbose;

        public static bool separate = true; // this can be set to false in order to easily disable
                                            // the search for safe separators for debugging reasons
        public static bool size3Separate = true;

        public SafeSeparator(Graph graph, bool verbose = true)
        {
            this.graph = graph;

            this.verbose = verbose;
        }

        /// <summary>
        /// tries to separate the graph using safe separators. If successful the minK parameter is set to the maximum of minK and the separator size
        /// </summary>
        /// <param name="separatedGraphs">a list of the separated graphs, if a separator exists, else null</param>
        /// <param name="minK">the minimum tree width parameter. If a separator is found that is greater than minK, it is set to the separator size</param>
        /// <returns>true iff a separation has been performed</returns>
        public bool Separate(out List<Graph> separatedGraphs, ref int minK)
        {
            if (!separate)
            {
                graph = null;   // remove reference to the graph so that its ressources can be freed
                separatedGraphs = null;
                reconstructionIndexationMappings = null;
                return false;
            }

            if (TestNotConnected() || FindSize1Separator() || FindSize2Separator() || HeuristicDecomposition() || FindSize3Separator_Flow() || FindCliqueSeparator() || FindAlmostCliqueSeparator())
            {
                List<int> separatorVertices = separator.Elements();
                separatorSize = separatorVertices.Count;
                graph.MakeIntoClique(separatorVertices);

                if (verbose)
                {
                    Console.WriteLine("graph {0} contains a {1} as safe separator of type {2}", graph.graphID, separator.ToString(), separatorType);
                }

                separatedGraphs = graph.Separate(separator, out reconstructionIndexationMappings);

                if (minK < separatorSize)
                {
                    minK = separatorSize;
                }

                // remove reference to the graph so that its ressources can be freed
                graph = null;

                return true;
            }

            reconstructionIndexationMappings = null;
            separatedGraphs = null;
            graph = null;
            return false;
        }

        #region exact safe separator search

        /// <summary>
        /// lists all articulation points of the graph
        /// This method can also be used to test for separators of size n by passing a set of n-1 vertices as
        /// ignoredVertices. and combining the result with the ignoredVertices.
        /// Code adapated from https://slideplayer.com/slide/12811727/
        /// </summary>
        /// <param name="availableVertices">a list of vertices to treat as existent</param>
        /// <returns>an iterator over all articulation points</returns>
        public static IEnumerable<int> ArticulationPoints(Graph graph, BitSet availableVertices = null)
        {
            if (graph.vertexCount == 0) // exit early if there are no vertices
            {
                yield break;
            }

            availableVertices = availableVertices ?? BitSet.All(graph.vertexCount);   // create an empty list if the given one is null in order to prevent NullReferenceExceptions

            int start = availableVertices.First();

            int[] count = new int[graph.vertexCount];                     // TODO: make member variable
            int[] reachBack = new int[graph.vertexCount];                 // TODO: make member variable
            Queue<int>[] children = new Queue<int>[graph.vertexCount];    // TODO: make member variable
            for (int i = 0; i < graph.vertexCount; i++)
            {
                count[i] = int.MaxValue;
                reachBack[i] = int.MaxValue;
                children[i] = new Queue<int>();
            }
            count[start] = 0;
            int numSubtrees = 0;
            
            for (int i = 0; i < graph.adjacencyList[start].Count; i++)
            {
                int rootNeighbor = graph.adjacencyList[start][i];
                if (count[rootNeighbor] == int.MaxValue && availableVertices[rootNeighbor])
                {
                    Stack<(int, int, int)> stack = new Stack<(int, int, int)>();
                    stack.Push((rootNeighbor, 1, start));
                    while (stack.Count > 0)
                    {
                        (int node, int timer, int fromNode) = stack.Peek();
                        if (count[node] == int.MaxValue)
                        {
                            count[node] = timer;
                            reachBack[node] = timer;

                            List<int> neighbors = graph.adjacencyList[node];
                            for (int j = 0; j < neighbors.Count; j++)
                            {
                                int neighbor = neighbors[j];
                                if (neighbor != fromNode && availableVertices[neighbor])
                                {
                                    children[node].Enqueue(neighbor);
                                }
                            }
                        }
                        else if (children[node].Count > 0)
                        {
                            int child = children[node].Dequeue();
                            if (count[child] < int.MaxValue)
                            {
                                if (reachBack[node] > count[child])
                                {
                                    reachBack[node] = count[child];
                                }
                            }
                            else
                            {
                                stack.Push((child, timer + 1, node));
                            }
                        }
                        else
                        {
                            if (node != rootNeighbor)
                            {
                                if (reachBack[node] >= count[fromNode])
                                {
                                    yield return fromNode;
                                }
                                if (reachBack[fromNode] > reachBack[node])
                                {
                                    reachBack[fromNode] = reachBack[node];
                                }
                            }
                            stack.Pop();
                        }
                    }

                    numSubtrees++;
                }
            }
            if (numSubtrees > 1)
            {
                yield return start;
            }
        }

        /// <summary>
        /// Tests if the graph is not connected. In that case the empty set is a separator of size 0 and it is saved in the "separator" member variable
        /// </summary>
        /// <returns>true iff the graph is not connected</returns>
        private bool TestNotConnected()
        {
            int componentCount = 0;
            BitSet nothing = new BitSet(graph.vertexCount);
            foreach ((BitSet, BitSet) _ in graph.ComponentsAndNeighbors(nothing))
            {
                componentCount++;
                if (componentCount >= 2)
                {
                    separator = nothing;
                    separatorType = SeparatorType.NotConnected;
                    return true;

                }
            }
            return false;
        }

        /// <summary>
        /// Tests if the graph can be separated with a separator of size 1. If so, the separator is saved in the "separator" member variable
        /// </summary>
        /// <returns>true iff a size 1 separator exists</returns>
        private bool FindSize1Separator()
        {
            foreach (int a in ArticulationPoints(graph))
            {
                separator = new BitSet(graph.vertexCount);
                separator[a] = true;
                separatorType = SeparatorType.Size1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tests if the graph can be separated with a safe separator of size 2. If so, the separator is saved in the "separator" member variable
        /// </summary>
        /// <returns>true iff a size 2 separator exists</returns>
        public bool FindSize2Separator()
        {
            BitSet notIgnoredVertices = BitSet.All(graph.vertexCount);

            // loop over every pair of vertices
            for (int u = 0; u < graph.vertexCount; u++)
            {
                notIgnoredVertices[u] = false;
                foreach (int a in ArticulationPoints(graph, notIgnoredVertices))
                {
                    separator = new BitSet(graph.vertexCount);
                    separator[u] = true;
                    separator[a] = true;
                    separatorType = SeparatorType.Size2;
                    return true;
                }
                notIgnoredVertices[u] = true;                
            }
            return false;
        }

        [ThreadStatic]
        public static Stopwatch size3SeparationStopwatch;
        [ThreadStatic]
        public static int size3separators = 0;

        /*
        /// <summary>
        /// Tests if the graph can be separated with a safe separator of size 3. If so, the separator is saved in the "separator" member variable
        /// </summary>
        /// <returns>true iff a safe size 3 separator exists</returns>
        public bool FindSize3Separator()
        {
            if (!size3Separate || graph.vertexCount < 5)
            {
                return false;
            }
            if (size3SeparationStopwatch == null)
            {
                size3SeparationStopwatch = new Stopwatch();
            }
            size3SeparationStopwatch.Start();
            BitSet notIgnoredVertices = BitSet.All(graph.vertexCount);

            // loop over every pair of vertices
            for (int u = 0; u < graph.vertexCount; u++)
            {
                notIgnoredVertices[u] = false;
                for (int v = u + 1; v < graph.vertexCount; v++)
                {
                    notIgnoredVertices[v] = false;
                    foreach (int a in ArticulationPoints(graph, notIgnoredVertices))
                    {
                        // TODO: needs only have at least two vertices. Don't use the Components and neighbors function
                        BitSet candidateSeparator = new BitSet(graph.vertexCount);
                        candidateSeparator[u] = true;
                        candidateSeparator[v] = true;
                        candidateSeparator[a] = true;

                        bool safe = true;

                        foreach ((BitSet component, BitSet neighbors) in graph.ComponentsAndNeighbors(candidateSeparator))
                        {
                            Debug.Assert(component.Count() > 0);
                            if (component.Count() == 1)
                            {
                                safe = false;
                                break;
                            }
                        }

                        if (safe)
                        {
                            separator = candidateSeparator;
                            size3SeparationStopwatch.Stop();
                            size3separators++;
                            separatorType = SeparatorType.Size3;
                            return true;
                        }
                    }
                    notIgnoredVertices[v] = true;
                }
                notIgnoredVertices[u] = true;
            }
            size3SeparationStopwatch.Stop();
            return false;
        }
        */

        #region size 3 separator - flow

        /// <summary>
        /// Tests if the graph can be separated with a safe separator of size 3. If so, the separator is saved in the "separator" member variable
        /// </summary>
        /// <returns>true iff a safe size 3 separator exists</returns>
        public bool FindSize3Separator_Flow()
        {
            if (graph.vertexCount < 5)
            {
                return false;
            }
            if (size3SeparationStopwatch == null)
            {
                size3SeparationStopwatch = new Stopwatch();
            }
            size3SeparationStopwatch.Start();


            // find a vertex with degree larger than 3 if possible (this would reduce the maximum number of graphs to check from 9 to 3, or from 3 to 1)
            bool vDegreeLargerThan3 = false;
            int v = -1;
            for (int i = 0; i < graph.vertexCount; i++)
            {
                if (graph.adjacencyList[i].Count > 3)
                { 
                    v = i;
                    vDegreeLargerThan3 = true;
                    break;
                }
            }
            // if all vertices have degree <= 3, then take any vertex
            if (!vDegreeLargerThan3)
            {
                v = 0;
            }

            // check for each eligible other vertex w if there is a flow of exactly 3 between v and w, and if so, find a safe separator of size 3
            for (int w = 0; w < graph.vertexCount; w++)
            {
                if (w != v && !graph.neighborSetsWithout[v][w])
                {
                    List<Graph> v_ContractedGraphs = new List<Graph>(3);
                    List<Graph> graphsToCheck = new List<Graph>();
                    List<Graph> fullyContractedGraphs = new List<Graph>();

                    // create a list of graphs whose flow needs to be determined.
                    // TODO: move before for loop
                    if (vDegreeLargerThan3)
                    {
                        v_ContractedGraphs.Add(graph);
                    }
                    else
                    {
                        // build a list of graphs where a neighbor is contracted to v
                        for (int i = 0; i < graph.adjacencyList[v].Count; i++)
                        {
                            int neighbor = graph.adjacencyList[v][i];
                            if (graph.neighborSetsWithout[neighbor][w]) // continue if the neighbor to be contracted into v has also neighbor w
                            {
                                continue;
                            }
                            Graph g = new Graph(graph);
                            g.Contract(v, neighbor);
                            // TODO: reduce g?
                            v_ContractedGraphs.Add(g);
                        }
                    }
                    for (int j = 0; j < v_ContractedGraphs.Count; j++)
                    {
                        Graph graph = v_ContractedGraphs[j];
                        // (contains only the original graph if deg(w) > 3, else up to 3 graphs where w and one of its neighbors are contracted)
                        if (graph.adjacencyList[w].Count <= 3)
                        {
                            for (int i = 0; i < graph.adjacencyList[w].Count; i++)
                            {
                                int neighbor = graph.adjacencyList[w][i];
                                if (graph.neighborSetsWithout[neighbor][v]) // continue if the neighbor to be contracted into w has also neighbor v
                                {
                                    continue;
                                }
                                Graph g = new Graph(graph);
                                g.Contract(w, neighbor);
                                // TODO: reduce g?
                                fullyContractedGraphs.Add(g);
                                graphsToCheck.Add(BuildGraph_VertexCapacityOne(g));
                            }
                        }
                        else
                        {
                            fullyContractedGraphs.Add(graph);
                            graphsToCheck.Add(BuildGraph_VertexCapacityOne(graph));
                        }
                    }

                    for (int i = 0; i < graphsToCheck.Count; i++)
                    {
                        if (FordFulkerson_Test3Flow(graphsToCheck[i], 2 * v + 1, 2 * w, out BitSet[] flowPathVertices))
                        {
                            // determine the path with the highest amount of vertices (we will loop over the two smaller paths)
                            int largestPathCount = 0;
                            if (flowPathVertices[largestPathCount].Count() < flowPathVertices[1].Count())
                            {
                                largestPathCount = 1;
                            }
                            if (flowPathVertices[largestPathCount].Count() < flowPathVertices[2].Count())
                            {
                                largestPathCount = 2;
                            }

                            // determine the smaller paths
                            int notLargest1 = (largestPathCount + 1) % 3;
                            int notLargest2 = (largestPathCount + 2) % 3;

                            BitSet availableVertices = new BitSet(fullyContractedGraphs[i].notRemovedVertices);
                            BitSet path1 = flowPathVertices[notLargest1];
                            BitSet path2 = flowPathVertices[notLargest2];

                            // loop: remove one vertex each from the two smaller paths and see if an articulation point exists.
                            // If so, the removed vertices and the articulation point are the safe separator
                            List<int> path1Elements = path1.Elements(); // loop over elements list here because the the 'nextElement' method can't be used inside a nested loop
                            for(int j = 0; j < path1Elements.Count; j++)
                            {
                                int a = path1Elements[j];
                                availableVertices[a] = false;

                                int b = -1;
                                while ((b = path2.NextElement(b)) != -1)
                                {
                                    availableVertices[b] = false;

                                    foreach(int c in ArticulationPoints(fullyContractedGraphs[i], availableVertices))
                                    {
                                        separator = new BitSet(graph.vertexCount);
                                        separator[a] = true;
                                        separator[b] = true;
                                        separator[c] = true;
                                        separatorType = SeparatorType.Size3;
                                        size3separators++;
                                        size3SeparationStopwatch.Stop();

                                        // TODO: potential need to call graph.reduce() first. (In that case the separator needs to be reduced also!)
                                        Debug.Assert(new ImmutableGraph(graph).IsMinimalSeparator(separator));
                                        return true;
                                    }

                                    availableVertices[b] = true;
                                }

                                availableVertices[a] = true;
                            }

                            Trace.Fail("A safe separator of size 3 exists, but the separator hasn't been found");
                        }
                    }
                }
            }

            // if deg v <= k: check if N(v) splits G into at least 3 components, at least two of which have size >= 2, if so return
            if (graph.adjacencyList[v].Count <= 3)
            {
                int components = 0;
                int size2OrLargerComponents = 0;
                foreach ((BitSet component, BitSet _) in graph.ComponentsAndNeighbors(graph.neighborSetsWithout[v]))
                {
                    components++;
                    if (component.Count() >= 2)
                    {
                        size2OrLargerComponents++;
                    }
                    if (components >= 3 && size2OrLargerComponents >= 2)
                    {
                        separator = new BitSet(graph.neighborSetsWithout[v]);
                        separatorType = SeparatorType.Size3;
                        size3separators++;
                        size3SeparationStopwatch.Stop();
                        return true;
                    }
                }
            }

            // "remove" v from graph and test for safe size 2 separator where at least two components have size at least 2
            BitSet notIgnoredVertices = BitSet.All(graph.vertexCount);
            notIgnoredVertices[v] = false;
            // loop over every pair of vertices
            for (int u = 0; u < graph.vertexCount; u++)
            {
                if (u != v)
                {
                    notIgnoredVertices[u] = false;
                    foreach (int a in ArticulationPoints(graph, notIgnoredVertices))
                    {
                        BitSet s = new BitSet(graph.vertexCount);
                        s[v] = true;
                        s[u] = true;
                        s[a] = true;

                        int size2OrLargerComponents = 0;
                        foreach ((BitSet component, BitSet _) in graph.ComponentsAndNeighbors(s))
                        {
                            if (component.Count() >= 2)
                            {
                                size2OrLargerComponents++;
                            }
                            if (size2OrLargerComponents == 2)
                            {
                                separator = s;
                                separatorType = SeparatorType.Size3;
                                size3separators++;
                                size3SeparationStopwatch.Stop();
                                return true;
                            }
                        }
                    }
                    notIgnoredVertices[u] = true;
                }
            }

            size3SeparationStopwatch.Stop();
            return false;
        }

        /// <summary>
        /// builds a directed graph where each vertex in a given graph is split into an in-vertex and an out-vertex with a single edge from in-vertex to out-vertex.
        /// Calculating a flow on the resulting graph will ensure that the flow paths are vertex-disjoint in the original graph.
        /// A vertex i in the original graph will be split into vertices with index 2*i for the in-vertex and index 2*i+1 for the out-vertex.
        /// </summary>
        /// <param name="graph">the graph to process</param>
        /// <returns>a directed graph as given in the description above</returns>
        Graph BuildGraph_VertexCapacityOne(Graph graph)
        {
            List<int>[] adjacencyList = new List<int>[graph.vertexCount * 2];
            for (int i = 0; i < graph.vertexCount; i++)
            {
                adjacencyList[2 * i] = new List<int>(1) { 2 * i + 1 };  // in-vertex has a single edge to out-vertex
                adjacencyList[2 * i + 1] = new List<int>(graph.adjacencyList[i].Count);
                for (int j = 0; j < graph.adjacencyList[i].Count; j++)
                {
                    int oldNeighbor = graph.adjacencyList[i][j];
                    adjacencyList[2 * i + 1].Add(2 * oldNeighbor);
                }
            }

            return new Graph(adjacencyList);
        }

        /// <summary>
        /// tests whether the maximum flow in the graph from a source to a sink is equal to 3, and if so gives the vertices that lie on 3 vertex-disjoint paths along which such a flow flows.
        /// Those vertices are indexed in terms of an original undirected graph that underlies the graph passed to this method, and out of which a directed graph has been built using the method above.
        /// </summary>
        /// <param name="graph">the graph whose flow to test</param>
        /// <param name="s">the source</param>
        /// <param name="t">the sink</param>
        /// <param name="flowPathVertices">3 sets of vertices, each one corresponding to a flow path that is vertex disjoint with the other two flow paths, if the flow is 3, else null</param>
        /// <returns>true if the flow from source to sink is exactly 3</returns>
        bool FordFulkerson_Test3Flow(Graph graph, int s, int t, out BitSet[] flowPathVertices)
        {
            Dictionary<int, sbyte>[] residualFlow = new Dictionary<int, sbyte>[graph.vertexCount]; // an edge is represented as a tuple of (other vertex, current flow)
            for (int i = 0; i < graph.vertexCount; i++)
            {
                if (graph.notRemovedVertices[i])
                {
                    residualFlow[i] = new Dictionary<int, sbyte>();
                }
            }
            for (int i = 0; i < graph.vertexCount; i++)
            { 
                if (graph.notRemovedVertices[i])
                { 
                    for (int j = 0; j < graph.adjacencyList[i].Count; j++)
                    {
                        int neighbor = graph.adjacencyList[i][j];
                        residualFlow[i][neighbor] = 1;

                        if (!residualFlow[neighbor].ContainsKey(i))
                        {
                            residualFlow[neighbor][i] = 0;
                        }
                    }
                }
            }
            int[] parent = new int[graph.vertexCount];

            // calculate flow
            int max_flow = 0;
            // while an augmenting path exists, increase flow
            while (Size3Separate_Flow_ResidualGraphBFS(residualFlow, s, t, parent))
            {
                max_flow++;
                if (max_flow == 4)
                {
                    flowPathVertices = null;
                    return false;
                }
                // update residual capacities and reverse edges
                for (int v = t; v != s; v = parent[v])
                {
                    int u = parent[v];
                    residualFlow[u][v]--;
                    residualFlow[v][u]++;
                }
            }

            if (max_flow < 3)
            {
                flowPathVertices = null;
                return false;
            }

            // extract vertices that lie on the three flow paths
            flowPathVertices = new BitSet[3];
            int flowPathVerticesIndex = 0;

            // find all three start vertices
            for (int i = 0; i < graph.adjacencyList[s].Count; i++)
            {
                int currentVertex = graph.adjacencyList[s][i];
                if (residualFlow[s][currentVertex] != 0)
                {
                    continue;
                }
                
                currentVertex = graph.adjacencyList[currentVertex][0];

                BitSet pathVertices = new BitSet(graph.vertexCount / 2);                
                
                // follow the paths to the sink
                while (currentVertex != t+1) {
                    pathVertices[currentVertex / 2] = true;
                    // find the next vertex on the path
                    for (int j = 0; j < graph.adjacencyList[currentVertex].Count; j++)
                    {
                        int neighbor = graph.adjacencyList[currentVertex][j];
                        if (residualFlow[currentVertex][neighbor] == 0)
                        {
                            Debug.Assert(graph.adjacencyList[neighbor].Count == 1);
                            currentVertex = graph.adjacencyList[neighbor][0];
                            break;
                        }
                    }
                }

                flowPathVertices[flowPathVerticesIndex] = pathVertices;
                flowPathVerticesIndex++;
                Debug.Assert(flowPathVerticesIndex <= 3);
            }

            Debug.Assert(!flowPathVertices[0].Intersects(flowPathVertices[1]));
            Debug.Assert(!flowPathVertices[0].Intersects(flowPathVertices[2]));
            Debug.Assert(!flowPathVertices[1].Intersects(flowPathVertices[2]));

            return true;
        }

        /// <summary>
        /// tests if there is a path from a source to a sink in a graph and fills a bfs parent array in the process 
        /// code adapted from https://www.geeksforgeeks.org/ford-fulkerson-algorithm-for-maximum-flow-problem/
        /// </summary>
        /// <param name="residualFlow">the residual flow (residualFlow[u][v] contains the residual flow along the edge (u,v))</param>
        /// <param name="s">the source</param>
        /// <param name="t">the sink</param>
        /// <param name="parent">the bfs parent array</param>
        /// <returns>true, iff such a path exists</returns>
        bool Size3Separate_Flow_ResidualGraphBFS(Dictionary<int, sbyte>[] residualFlow, int s, int t, int[] parent)
        {
            // Create a visited array and mark  
            // all vertices as not visited 
            bool[] visited = new bool[residualFlow.Length];
            for (int i = 0; i < residualFlow.Length; ++i)
                visited[i] = false;

            // Create a queue, enqueue source vertex and mark 
            // source vertex as visited 
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(s);
            visited[s] = true;
            parent[s] = -1;

            // Standard BFS Loop 
            while (queue.Count != 0)
            {
                int u = queue.Dequeue();

                foreach (KeyValuePair<int, sbyte> neighborFlow in residualFlow[u])
                {
                    int v = neighborFlow.Key;
                    if (!visited[v] && neighborFlow.Value > 0)
                    {
                        queue.Enqueue(v);
                        parent[v] = u;
                        visited[v] = true;

                        // break early if sink is found
                        if (v == t)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        #endregion

        [ThreadStatic]
        public static Stopwatch cliqueSeparatorStopwatch;
        [ThreadStatic]
        public static int cliqueSeparators = 0;

        /// <summary>
        /// Tests if the graph can be separated by a clique separator. If so, the separator is saved in the "separator" member variable
        /// The algorithm is based on 
        ///     An Introduction to Clique Minimal Separator Decomposition
        ///     by Anne Berry, Romain Pogorelcnik, Geneviève Simonet
        ///     found here: https://hal-lirmm.ccsd.cnrs.fr/lirmm-00485851/document
        /// </summary>
        /// <param name="ignoredVertex">an optional vertex that can be treated as non-existent (used for finding almost clique separators)</param>
        /// <returns>true, iff the graph contains a clique separator</returns>
        public bool FindCliqueSeparator(int ignoredVertex = -1)
        {            
            if (graph.notRemovedVertexCount == 0)
            {
                return false;
            }
            if (cliqueSeparatorStopwatch == null)
            {
                cliqueSeparatorStopwatch = new Stopwatch();
            }
            cliqueSeparatorStopwatch.Start();

            MCS_M_Plus(out int[] alpha, out Graph H, out BitSet X);

            bool cliqueSeparatorExists = Atoms(alpha, H, X, ignoredVertex);

            cliqueSeparatorStopwatch.Stop();

            if (cliqueSeparatorExists)
            {
                separatorType = SeparatorType.Clique;
            }

            return cliqueSeparatorExists;
        }


        [ThreadStatic]
        public static Stopwatch almostCliqueSeparatorStopwatch;
        [ThreadStatic]
        public static int almostCliqueSeparators = 0;

        /// <summary>
        /// Tests if the graph can be separated by an almost clique separator. If so, the separator is saved in the "separator" member variable
        /// </summary>
        /// <returns>true, iff the graph contains an almost clique separator</returns>
        public bool FindAlmostCliqueSeparator()
        {
            if (almostCliqueSeparatorStopwatch == null)
            {
                almostCliqueSeparatorStopwatch = new Stopwatch();
            }
            almostCliqueSeparatorStopwatch.Start();

            int almostCliqueVertex = 0;
            bool almostCliqueSeparatorExists = false;
            while (almostCliqueVertex < graph.vertexCount) {
                if (FindCliqueSeparator(almostCliqueVertex))
                {
                    almostCliqueSeparatorExists = true;
                    break;
                }
                almostCliqueVertex++;
            }

            if (almostCliqueSeparatorExists)
            {
                // separator has been set already by the findCliqueSeparator method. We only need to set the almost clique vertex.
                separator[almostCliqueVertex] = true;
                separatorType = SeparatorType.AlmostClique;
            }
            almostCliqueSeparatorStopwatch.Stop();

            return almostCliqueSeparatorExists;
        }

        /// <summary>
        /// An Introduction to Clique Minimal Separator Decomposition
        /// Anne Berry, Romain Pogorelcnik, Geneviève Simonet
        /// </summary>
        /// <param name="alpha">a perfect elimination ordering for H</param>
        /// <param name="H">a triangulation of G</param>
        /// <param name="X">a set of vertices that generate minimal separators of H</param>
        private void MCS_M_Plus(out int[] alpha, out Graph H, out BitSet X)
        {
            // init
            alpha = new int[graph.vertexCount];
            HashSet<(int, int)> F = new HashSet<(int, int)>();
            Graph G_prime = new Graph(graph);
            bool[] reached = new bool[graph.vertexCount];
            BitSet[] reach = new BitSet[graph.vertexCount];   // TODO: HashSet?
            int[] labels = new int[graph.vertexCount];
            for (int i = 0; i < graph.vertexCount; i++)
            {
                labels[i] = 0;
            }
            int s = -1;
            X = new BitSet(graph.vertexCount);

            for (int i = graph.vertexCount - 1; i >= 0; i--)
            {
                // choose a vertex x of G_prime of maximal label
                int x = -1; // TODO: use priority queue?
                int maxLabel = -1;
                for (int j = 0; j < graph.vertexCount; j++)
                {
                    if (maxLabel < labels[j] && G_prime.notRemovedVertices[j])
                    {
                        maxLabel = labels[j];
                        x = j;
                    }
                }

                List<int> Y = G_prime.adjacencyList[x];

                if (labels[x] <= s)
                {
                    X[x] = true;
                }

                s = labels[x];

                Array.Clear(reached, 0, graph.vertexCount); // TODO: correct? vertices in G_prime need to be marked unreached -> vertices that are already removed shoudn't be accessed anyway
                reached[x] = true;

                for (int j = 0; j < graph.vertexCount; j++)   // for j=0 to n-1   <---- n-1 kann evtl auch die Größe des verbleibenden Graphen sein
                {
                    reach[j] = new BitSet(graph.vertexCount);
                }

                int y;
                for (int j = 0; j < Y.Count; j++)
                {
                    y = Y[j];
                    reached[y] = true;
                    reach[labels[y]][y] = true;
                }

                for (int j = 0; j < graph.vertexCount; j++)
                {
                    while (!reach[j].IsEmpty())
                    {
                        y = reach[j].First();
                        reach[j][y] = false;

                        int z;
                        for (int k = 0; k < G_prime.adjacencyList[y].Count; k++)
                        {
                            z = G_prime.adjacencyList[y][k];

                            if (!reached[z])
                            {
                                reached[z] = true;
                                if (labels[z] > j)
                                {
                                    Y.Add(z);
                                    reach[labels[z]][z] = true;
                                }
                                else
                                {
                                    reach[j][z] = true;
                                }
                            }
                        }
                    }
                }

                for (int j = 0; j < Y.Count; j++)
                {
                    y = Y[j];
                    if (!F.Contains((y, x)) && !graph.neighborSetsWithout[x][y])
                    {
                        F.Add((x, y));
                    }
                    labels[y]++;
                }

                alpha[i] = x;

                // remove x from G'
                G_prime.Remove(x);
            }

            // build H = (V, E+F)
            H = new Graph(graph);
            foreach ((int u, int v) in F)
            {
                H.AddEdge(u, v);
            }
        }

        /// <summary>
        /// Finds a clique separator if there exists one. If so, the clique separator is saved in the "separator" member variable
        /// The algorithm is based on
        ///     An Introduction to Clique Minimal Separator Decomposition
        ///     by Anne Berry, Romain Pogorelcnik, Geneviève Simonet
        ///     found here: https://hal-lirmm.ccsd.cnrs.fr/lirmm-00485851/document
        /// </summary>
        /// <param name="alpha">a perfect elimination ordering for H</param>
        /// <param name="H">a triangulation of G</param>
        /// <param name="X">a set of vertices that generate minimal separators of H</param>
        /// <param name="ignoredVertex">an optional vertex to treat as non-existent (useful for finding almost cliques)</param>
        /// <returns>true, iff there exists a clique separator</returns>
        private bool Atoms(int[] alpha, Graph H, BitSet X, int ignoredVertex)
        {
            Graph H_prime = new Graph(H);   // TODO: reuse H here?

            if (ignoredVertex != -1)
            {
                H_prime.Remove(ignoredVertex);
            }

            for (int i = 0; i < graph.vertexCount; i++)
            {
                int x = alpha[i];
                if (X[x])
                {
                    List<int> S = H_prime.adjacencyList[x];

                    // test if S is clique in G
                    // (intersect the neighbors of x with the neighbors' neighbors and test if that is equal to x' neighbors)
                    BitSet intersection = new BitSet(H_prime.neighborSetsWithout[x]);   // TODO: reuse to avoid garbage
                    for (int j = 0; j < S.Count; j++)
                    {
                        intersection.IntersectWith(graph.neighborSetsWith[S[j]]);  // TODO: one could implement a test for an early exit
                    }
                    if (intersection.Equals(H_prime.neighborSetsWithout[x]))
                    {
                        separator = H_prime.neighborSetsWithout[x];
                        cliqueSeparators++;
                        return true;
                    }
                }

                // remove x from H'
                H_prime.Remove(x);
            }

            return false;
        }

        #endregion

        #region recombination

        /// <summary>
        /// recombines tree decompositions for the subgraphs into a tree decomposition for the original graph
        /// </summary>
        /// <param name="ptds">the tree decompositions for each of the subgraphs</param>
        /// <returns>a tree decomposition for the original graph</returns>
        public PTD RecombineTreeDecompositions(List<PTD> ptds)
        {
            // re-index the tree decompositions for the subgraphs
            for (int i = 0; i < ptds.Count; i++)
            {
                ptds[i].Reindex(reconstructionIndexationMappings[i]);
            }

            // find separator node in the first ptd
            PTD separatorNode = null;
            Stack<PTD> nodeStack = new Stack<PTD>();
            nodeStack.Push(ptds[0]);
            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();

                // exit when separator node is found
                if (currentNode.Bag.IsSupersetOf(separator))
                {
                    separatorNode = currentNode;
                    nodeStack.Clear();
                    break;
                }

                // push children onto the stack
                foreach (PTD childNode in currentNode.children)
                {
                    nodeStack.Push(childNode);
                }
            }


            // reroot the other tree decompositions and append them to the first one at the separator node
            for (int i = 1; i < ptds.Count; i++)
            {
                separatorNode.children.Add(ptds[i].Reroot(separator));
            }

            return ptds[0];
        }

        #endregion
    }
}
