using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp.Safe_Separators
{
    public partial class SafeSeparator
    {
        [ThreadStatic]
        public static Stopwatch size3SeparationStopwatch;
        [ThreadStatic]
        public static int size3separators = 0;

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
                if (w != v && !graph.openNeighborhood[v][w])
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
                            if (graph.openNeighborhood[neighbor][w]) // continue if the neighbor to be contracted into v has also neighbor w
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
                                if (graph.openNeighborhood[neighbor][v]) // continue if the neighbor to be contracted into w has also neighbor v
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
                            for (int j = 0; j < path1Elements.Count; j++)
                            {
                                int a = path1Elements[j];
                                availableVertices[a] = false;

                                int b = -1;
                                while ((b = path2.NextElement(b)) != -1)
                                {
                                    availableVertices[b] = false;

                                    foreach (int c in ArticulationPoints(fullyContractedGraphs[i], availableVertices))
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
                foreach ((BitSet component, BitSet _) in graph.ComponentsAndNeighbors(graph.openNeighborhood[v]))
                {
                    components++;
                    if (component.Count() >= 2)
                    {
                        size2OrLargerComponents++;
                    }
                    if (components >= 3 && size2OrLargerComponents >= 2)
                    {
                        separator = new BitSet(graph.openNeighborhood[v]);
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
                while (currentVertex != t + 1)
                {
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
    }
}
