using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// a class for representing a mutable graph, i. e. a graph that is suited for preprocessing.
    /// Such a graph is not suited to run expensive algorithms on. Use the ImmutableGraph class for that.
    /// </summary>
    public class Graph
    {
        public List<int>[] adjacencyList;
        public BitSet[] openNeighborhood;   // contains N(v)
        public BitSet[] closedNeighborhood;      // contains N[v]
        public BitSet notRemovedVertices    { get; private set; }
        public int vertexCount              { get; private set; }
        public int notRemovedVertexCount    { get; private set; } 
        public int edgeCount                { get; private set; }

        private bool isReduced = false;

        public readonly int graphID;
        private static int graphCount = 0;

        public static bool verbose = true;

        public Graph(string filepath)
        {

            try
            {
                using (StreamReader sr = new StreamReader(filepath))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.StartsWith("c"))
                        {
                            continue;
                        }
                        else
                        {
                            string[] tokens = line.Split(' ');
                            if (tokens[0] == "p")
                            {
                                vertexCount = Convert.ToInt32(tokens[2]);
                                notRemovedVertexCount = vertexCount;
                                edgeCount = Convert.ToInt32(tokens[3]);

                                adjacencyList = new List<int>[vertexCount];
                                for (int i = 0; i < vertexCount; i++)
                                {
                                    adjacencyList[i] = new List<int>();
                                }
                                openNeighborhood = new BitSet[vertexCount];
                                for (int i = 0; i < vertexCount; i++)
                                {
                                    openNeighborhood[i] = new BitSet(vertexCount);
                                }
                            }
                            else
                            {
                                int u = Convert.ToInt32(tokens[0]) - 1;
                                int v = Convert.ToInt32(tokens[1]) - 1;

                                if (!openNeighborhood[u][v])
                                {
                                    adjacencyList[u].Add(v);
                                    adjacencyList[v].Add(u);
                                    openNeighborhood[u][v] = true;
                                    openNeighborhood[v][u] = true;
                                }
                            }
                        }
                    }
                }

                closedNeighborhood = new BitSet[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    closedNeighborhood[i] = new BitSet(openNeighborhood[i]);
                    closedNeighborhood[i][i] = true;
                }

                notRemovedVertices = BitSet.All(vertexCount);

                graphID = graphCount;
                graphCount++;
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        /// <summary>
        /// creates a graph from an adjacency list. (The adjacency list is shared, not copied.)
        /// </summary>
        /// <param name="adjacencyList">the graph's adjacency list</param>
        public Graph(List<int>[] adjacencyList)
        {
            vertexCount = adjacencyList.Length;
            edgeCount = 0;

            this.adjacencyList = adjacencyList;
            openNeighborhood = new BitSet[vertexCount];
            closedNeighborhood = new BitSet[vertexCount];
            for (int u = 0; u < vertexCount; u++)
            {
                edgeCount += adjacencyList[u].Count;
                openNeighborhood[u] = new BitSet(vertexCount);
                for (int j = 0; j < adjacencyList[u].Count; j++)
                {
                    int v = adjacencyList[u][j];
                    openNeighborhood[u][v] = true;
                }
                closedNeighborhood[u] = new BitSet(openNeighborhood[u]);
                closedNeighborhood[u][u] = true;
            }
            edgeCount /= 2;

            notRemovedVertices = BitSet.All(vertexCount);
            notRemovedVertexCount = vertexCount;

            graphID = graphCount;
            graphCount++;
        }

        /// <summary>
        /// copy constructor. Constructs a deep copy of the given graph
        /// </summary>
        /// <param name="graph">the graph to copy from</param>
        public Graph(Graph graph)
        {
            vertexCount = graph.vertexCount;
            edgeCount = graph.edgeCount;
            notRemovedVertices = new BitSet(graph.notRemovedVertices);
            notRemovedVertexCount = graph.notRemovedVertexCount;
            adjacencyList = new List<int>[vertexCount];
            openNeighborhood = new BitSet[vertexCount];
            closedNeighborhood = new BitSet[vertexCount];
            for (int i = 0; i < graph.adjacencyList.Length; i++)
            {
                adjacencyList[i] = new List<int>(graph.adjacencyList[i]);
                openNeighborhood[i] = new BitSet(graph.openNeighborhood[i]);
                closedNeighborhood[i] = new BitSet(graph.closedNeighborhood[i]);
            }

            graphID = graphCount;
            graphCount++;
        }

        /// <summary>
        /// separates the graph at a given safe separator into subgraphs.
        /// If a tree decomposition for one subgraph has already been calculated, the index of that subgraph is also given out
        /// </summary>
        /// <param name="separator">the safe separator</param>
        /// <param name="reconstructionIndexationMappings">the corresponding mapping for reindexing the vertices in the subgraphs back to their old indices within this graph</param>
        /// <param name="alreadyCalculatedComponent">Optionally, a component whose subgraph has an already calculated PTD.
        ///                                         (This happens when a safe separator is found during the "HasTreewidth" calculation.)</param>
        /// <param name="alreadyCalculatedComponentIndex">-1, if no alreadyCalculatedComponent is passed, else the index in the list of subgraphs of the subgraph corresponding to that component</param>
        /// <returns>a list of the subgraphs</returns>
        public List<Graph> Separate(BitSet separator, out List<ReindexationMapping> reconstructionIndexationMappings, out int alreadyCalculatedComponentIndex, BitSet alreadyCalculatedComponent=null)
        {
            alreadyCalculatedComponentIndex = -1;
            List<Graph> subGraphs = new List<Graph>();
            List<int> separatorVertices = separator.Elements();
            reconstructionIndexationMappings = new List<ReindexationMapping>();
            
            // for each component 
            foreach ((BitSet component, BitSet _) in ComponentsAndNeighbors(separator))
            {
                if (alreadyCalculatedComponent != null && component.Equals(alreadyCalculatedComponent))
                {
                    alreadyCalculatedComponentIndex = subGraphs.Count;
                }
                List<int> vertices = component.Elements();
                component.UnionWith(separator);

                // map vertices from this graph to the new subgraph and vice versa
                Dictionary<int, int> reductionMapping = new Dictionary<int, int>(vertexCount + separatorVertices.Count);
                ReindexationMapping reconstructionIndexationMapping = new ReindexationMapping(vertexCount);
                reconstructionIndexationMappings.Add(reconstructionIndexationMapping);
                for (int i = 0; i < vertices.Count; i++)
                {
                    reductionMapping[vertices[i]] = i;
                    reconstructionIndexationMapping.Add(vertices[i]);
                }

                // don't forget the separator
                for (int i = 0; i < separatorVertices.Count; i++)
                {
                    int u = separatorVertices[i];
                    reductionMapping[u] = vertices.Count + i;
                    reconstructionIndexationMapping.Add(u);
                }

                // create new adjacency list
                List<int>[] subAdjacencyList = new List<int>[vertices.Count + separatorVertices.Count];
                for (int i = 0; i < vertices.Count; i++)
                {
                    int oldVertex = vertices[i];
                    int newVertex = reductionMapping[oldVertex];
                    subAdjacencyList[newVertex] = new List<int>(adjacencyList[oldVertex].Count);
                    foreach (int oldNeighbor in adjacencyList[oldVertex])
                    {
                        int newNeighbor = reductionMapping[oldNeighbor];
                        subAdjacencyList[newVertex].Add(newNeighbor);
                    }
                }

                // also for the separator
                for (int i = 0; i < separatorVertices.Count; i++)
                {
                    int u = separatorVertices[i];
                    subAdjacencyList[vertices.Count + i] = new List<int>();
                    foreach (int oldNeighbor in adjacencyList[u])
                    {
                        if (component[oldNeighbor])
                        {
                            int newNeighbor = reductionMapping[oldNeighbor];
                            subAdjacencyList[vertices.Count + i].Add(newNeighbor);
                        }
                    }
                }

                Graph subGraph = new Graph(subAdjacencyList);
                subGraphs.Add(subGraph);
            }

            // print some stats
            if (verbose)
            {
                Console.WriteLine("splitted graph {0} with {1} vertices and {2} edges into {3} smaller graphs:", graphID, vertexCount, edgeCount, subGraphs.Count);
                foreach (Graph subGraph in subGraphs)
                {
                    Console.WriteLine("        graph {0} with {1} vertices and {2} edges", subGraph.graphID, subGraph.vertexCount, subGraph.edgeCount);
                }
            }

            return subGraphs;
        }

        /// <summary>
        /// removes a vertex and all its incident edges from this graph
        /// </summary>
        /// <param name="vertex">the vertex to remove</param>
        public void Remove(int vertex)
        {
            for (int i = 0; i < adjacencyList[vertex].Count; i++)
            {
                int neighbor = adjacencyList[vertex][i];
                adjacencyList[neighbor].Remove(vertex);
                openNeighborhood[neighbor][vertex] = false;
                closedNeighborhood[neighbor][vertex] = false;
            }

            notRemovedVertices[vertex] = false;
            notRemovedVertexCount--;
            edgeCount -= adjacencyList[vertex].Count;
            isReduced = true;
        }

        /// <summary>
        /// Inserts an edge into the graph
        /// </summary>
        /// <param name="u">one endpoint of the edge to be inserted</param>
        /// <param name="v">the other endpoint of the edge to be inserted</param>
        public void AddEdge(int u, int v)
        {
            Debug.Assert(!closedNeighborhood[u][v]);
            adjacencyList[u].Add(v);
            adjacencyList[v].Add(u);
            openNeighborhood[u][v] = true;
            openNeighborhood[v][u] = true;
            closedNeighborhood[u][v] = true;
            closedNeighborhood[v][u] = true;
            edgeCount++;
        }

        /// <summary>
        /// contracts an edge
        /// </summary>
        /// <param name="u">the end vertex that 'absorbs' v</param>
        /// <param name="v">the end vertex that is contracted into u</param>
        public void Contract(int u, int v)
        {
            // assert that u and v are indeed neighbors and also not equal to each other
            Debug.Assert(openNeighborhood[u][v]);

            for (int i = 0; i < adjacencyList[v].Count; i++)
            {
                int w = adjacencyList[v][i];

                // remove v from its neighbors' adjacencies
                adjacencyList[w].Remove(v);
                openNeighborhood[w][v] = false;
                closedNeighborhood[w][v] = false;
                edgeCount--;

                // add u to the neighbors' adjacencies
                if (u != w && !openNeighborhood[w][u])
                {
                    adjacencyList[u].Add(w);
                    adjacencyList[w].Add(u);
                    openNeighborhood[u][w] = true;
                    openNeighborhood[w][u] = true;
                    closedNeighborhood[u][w] = true;
                    closedNeighborhood[w][u] = true;
                    edgeCount++;
                }
            }
            adjacencyList[v].Clear();
            closedNeighborhood[v].Clear();
            openNeighborhood[v].Clear();
            notRemovedVertexCount--;
            notRemovedVertices[v] = false;

            // TODO: perhaps return an object containing information on how to 'uncontract' the graph.
            //       This would eliminate the need to copy the graph, but could complicate things in the Ford-Fulkerson algorithm.
        }

        /// <summary>
        /// completes the given vertex list into a clique
        /// </summary>
        /// <param name="cliqueToBe">the vertices that shall be made into a clique</param>
        public void MakeIntoClique(List<int> cliqueToBe)
        {
            for (int i = 0; i < cliqueToBe.Count; i++)
            {
                int u = cliqueToBe[i];
                for (int j = i + 1; j < cliqueToBe.Count; j++)
                {
                    int v = cliqueToBe[j];
                    if (!openNeighborhood[u][v])
                    {
                        adjacencyList[u].Add(v);
                        adjacencyList[v].Add(u);
                        openNeighborhood[u][v] = true;
                        openNeighborhood[v][u] = true;
                        closedNeighborhood[u][v] = true;
                        closedNeighborhood[v][u] = true;

                        edgeCount++;
                    }
                }
            }
        }

        /// <summary>
        /// replaces the old adjacency list with a new one where all removed vertices no longer occupy a spot in the list. A mapping that maps the new vertices back to the old ones is returned.
        /// </summary>
        /// <param name="reconstructionIndexationMapping"></param>
        public void Reduce(out ReindexationMapping reconstructionIndexationMapping)
        {
            if (!isReduced)
            {
                reconstructionIndexationMapping = null;
                return;
            }

            BuildReindexationMappings(out reconstructionIndexationMapping, out Dictionary<int, int> reductionMapping);

            vertexCount = 0;
            edgeCount = 0;

            // use that mapping to reduce the adjacency list for this graph
            List<int>[] reducedAdjacencyList = new List<int>[reductionMapping.Count];
            int vertex = -1;
            while ((vertex = notRemovedVertices.NextElement(vertex)) != -1)
            {
                vertexCount++;

                List<int> currentVertexAdjacencies = new List<int>(adjacencyList[vertex].Count);
                for (int j = 0; j < adjacencyList[vertex].Count; j++)
                {
                    int neighbor = adjacencyList[vertex][j];
                    currentVertexAdjacencies.Add(reductionMapping[neighbor]);
                }
                reducedAdjacencyList[reductionMapping[vertex]] = currentVertexAdjacencies;
                edgeCount += currentVertexAdjacencies.Count;
            }
            edgeCount /= 2;
            notRemovedVertexCount = vertexCount;

            adjacencyList = reducedAdjacencyList;
            notRemovedVertices = BitSet.All(vertexCount);
            openNeighborhood = new BitSet[vertexCount];
            closedNeighborhood = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                openNeighborhood[i] = new BitSet(vertexCount, adjacencyList[i]);
                closedNeighborhood[i] = new BitSet(openNeighborhood[i]);
                closedNeighborhood[i][i] = true;
            }

            isReduced = false;

            if (verbose)
            {
                Console.WriteLine("reduced graph {0} to {1} vertices and {2} edges", graphID, reducedAdjacencyList.Length, edgeCount);
            }
        }

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
            if (isReduced)
            {
                unvisited.IntersectWith(notRemovedVertices);
            }

            // find components as long as they exist
            int startingVertex = -1;
            while ((startingVertex = unvisited.NextElement(startingVertex, isConsumed: true)) != -1)
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
                    while ((redElement = currentIterationFrontier.NextElement(redElement, isConsumed: false)) != -1)
                    {
                        nextIterationFromtier.UnionWith(openNeighborhood[redElement]);
                    }

                    component.UnionWith(currentIterationFrontier);
                    neighbors.UnionWith(nextIterationFromtier);
                    nextIterationFromtier.ExceptWith(separator);
                    nextIterationFromtier.ExceptWith(component);

                    currentIterationFrontier.CopyFrom(nextIterationFromtier);
                }

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
                result.UnionWith(openNeighborhood[vertices[i]]);
            }
            result.ExceptWith(vertexSet);
            return result;
        }

        /// <summary>
        /// Constructs both a forward and a backward mapping between reduced vertex indices and original vertex indices.
        /// </summary>
        /// <param name="reindexationMapping">the mapping from reduced vertex indices back to original vertex indices</param>
        /// <param name="reductionMapping">the mapping from original vertex indices to reduced vertex indices</param>
        private void BuildReindexationMappings(out ReindexationMapping reindexationMapping, out Dictionary<int, int> reductionMapping)
        {
            reindexationMapping = new ReindexationMapping(vertexCount);
            
            reductionMapping = new Dictionary<int, int>();
            int counter = 0;
            int vertex = -1;
            while ((vertex = notRemovedVertices.NextElement(vertex)) != -1)
            {
                reductionMapping.Add(vertex, counter);
                reindexationMapping.Add(vertex);
                counter++;
            }
        }

        /// <summary>
        /// A class for converting the indices from a reduced graph back to the original (non-reduced) graph (i. e. to the indices they had before Graph.Reduce() has been called).
        /// This is necessary for turning the PTDs that are built using the reduced graph back into PTDs that are valid for the original (non-reduced) graph.
        /// </summary>
        public class ReindexationMapping
        {
            // a mapping from reduced vertex to original vertex
            public readonly List<int> map;

            // the vertex count that the original graph had (necessary for reconstructing the BitSets)
            public readonly int originalVertexCount;

            /// <summary>
            /// creates a new mapping from reduced vertices to original vertices
            /// </summary>
            /// <param name="originalVertexCount">the vertex count of the original graph</param>
            internal ReindexationMapping(int originalVertexCount)
            {
                map = new List<int>();
                this.originalVertexCount = originalVertexCount;
            }

            /// <summary>
            /// adds an original vertex index to the map as the next element
            /// </summary>
            /// <param name="originalVertex">the vertex that the next index has in the original graph</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Add(int originalVertex)
            {
                map.Add(originalVertex);
            }

            /// <summary>
            /// turns a set of vertices in a reduced graph into a set of vertices in the original graph 
            /// </summary>
            /// <param name="reducedBag">the bag to reindex</param>
            /// <returns>the reindexed bag</returns>
            public BitSet Reindex(BitSet reducedBag)
            {
                // re-index bag
                BitSet reindexedBag = new BitSet(originalVertexCount);
                foreach (int i in reducedBag.Elements())
                {
                    reindexedBag[map[i]] = true;
                }
                return reindexedBag;
            }

            /// <summary>
            /// creates a string that contains the reindexed indices
            /// </summary>
            /// <returns>a string that contains the reindexed indices</returns>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                // fill in elements
                for (int i = 0; i < map.Count; i++)
                {
                    sb.Append(map[i] + ",");
                }

                // remove trailing comma if there is one
                if (sb.Length > 1)
                {
                    sb.Length--;
                }

                // show a placeholder for the empty set
                if (sb.Length == 0)
                {
                    sb.Append("_");
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// resets the graph count so that new graph IDs starting from 0 are used
        /// </summary>
        public static void ResetGraphIDs()
        {
            graphCount = 0;
        }

        public static bool dumpSubgraphs = false;

        /// <summary>
        /// writes the graph to disk
        /// </summary>
        public void Dump()
        {
            if (dumpSubgraphs)
            {
                // TODO: doesn't work in all languages/cultures
                Directory.CreateDirectory(Program.date_time_string);
                
                // ".gr" format:
                //      p tw vertex_count edge_count
                //      edge_list
                using (StreamWriter sw = new StreamWriter(String.Format(Program.date_time_string + "\\{0}.gr", graphID)))
                {
                    sw.WriteLine(String.Format("p tw {0} {1}", notRemovedVertexCount, edgeCount));

                    for (int u = 0; u < vertexCount; u++)
                    {
                        if (notRemovedVertices[u])
                        {
                            for (int j = 0; j < adjacencyList[u].Count; j++)
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

                // ".tgf" format:
                //      node_list
                //      #
                //      edge_list
                using (StreamWriter sw = new StreamWriter(String.Format(Program.date_time_string + "\\{0}.tgf", graphID)))
                {
                    for (int u = 0; u < vertexCount; u++)
                    {
                        if (notRemovedVertices[u])
                        {
                            sw.WriteLine(String.Format("{0} {0}", u));
                        }
                    }
                    sw.WriteLine("#");
                    for (int u = 0; u < vertexCount; u++)
                    {
                        if (notRemovedVertices[u])
                        {
                            for (int j = 0; j < adjacencyList[u].Count; j++)
                            {
                                int v = adjacencyList[u][j];
                                if (u < v)
                                {
                                    sw.WriteLine(String.Format("{0} {1}", u, v));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
