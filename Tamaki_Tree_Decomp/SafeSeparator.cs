using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    class SafeSeparator
    {
        readonly int vertexCount;
        readonly Graph graph;
        readonly List<int>[] adjacencyList;
        readonly BitSet[] neighborSetsWithout;

        BitSet separator;
        readonly List<Graph> subGraphs;
        int separatorSize;


        readonly List<int[]> reconstructionMappings;            // mapping from reduced vertex id to original vertex id, by component
        /*
        readonly List<BitSet> reconstructionBagsToAppendTo;     // a list of (subsets of) bags that the bags in the next list are appended to during reconstruction
        readonly List<BitSet> reconstructionBagsToAppend;       // bags to append to (subsets of) bags in the list above during reconstruction 
        */

        public SafeSeparator(Graph graph)
        {
            vertexCount = graph.vertexCount;
            this.graph = graph;
            adjacencyList = new List<int>[vertexCount];
            neighborSetsWithout = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacencyList[i] = new List<int>(graph.adjacencyList[i]);
                neighborSetsWithout[i] = new BitSet(graph.neighborSetsWithout[i]);
            }

            subGraphs = new List<Graph>();

            reconstructionMappings = new List<int[]>();
        }

        /// <summary>
        /// tries to separate the graph using safe separators. If successful the minK parameter is set to the maximum of minK and the separator size
        /// </summary>
        /// <param name="separatedGraphs">a list of the separated graphs, if a separator exists, else null</param>
        /// <param name="minK">the minimum tree width parameter. If a separator is found that is greater than minK, it is set to the separator size</param>
        /// <returns>true iff a separation has been performed</returns>
        public bool Separate(out List<Graph> separatedGraphs, ref int minK)
        {

            bool result = Size2Separate();
            if (result)
            {
                separatedGraphs = subGraphs;
                if (minK < separatorSize)
                {
                    minK = separatorSize;
                }
            }
            else
            {
                separatedGraphs = null;
            }
            return result;
        }

        /// <summary>
        /// Tests if the graph can be separated with a separator of size 2. If so, the graph is split and the
        /// resulting subgraphs and the reconstruction mappings are saved in the corresponding member variables
        /// TODO: Improve. This is a naive (n^3) implementation.
        /// </summary>
        /// <returns>true iff a size 2 separator exists</returns>
        public bool Size2Separate()
        {
            BitSet separator = new BitSet(vertexCount);

            // loop over every pair of vertices
            for (int u = 0; u < vertexCount; u++)
            {
                separator[u] = true;
                for (int v = u + 1; v < vertexCount; v++)
                {
                    separator[v] = true;

                    // test if they are a minimal separator
                    if (graph.IsMinimalSeparator_ReturnComponents(separator, out List<BitSet> components))
                    {
                        this.separator = separator;
                        separatorSize = 2;

                        // complete the separator into a clique
                        if (!neighborSetsWithout[u][v])
                        {
                            neighborSetsWithout[u][v] = true;
                            neighborSetsWithout[v][u] = true;
                            adjacencyList[u].Add(v);
                            adjacencyList[v].Add(u);
                        }

                        // divide the graph into subgraphs for each component
                        foreach (BitSet component in components)
                        {
                            List<int> vertices = component.Elements();
                            component.UnionWith(separator);

                            // map vertices from this graph to the new subgraph and vice versa
                            Dictionary<int, int> reductionMapping = new Dictionary<int, int>(vertices.Count + 2);
                            int[] reconstructionMapping = new int[vertices.Count + 2];
                            reconstructionMappings.Add(reconstructionMapping);
                            for (int i = 0; i < vertices.Count; i++)
                            {
                                reductionMapping[vertices[i]] = i;
                                reconstructionMapping[i] = vertices[i];
                            }
                            // don't forget the separator
                            reductionMapping[u] = vertices.Count;
                            reconstructionMapping[vertices.Count] = u;
                            reductionMapping[v] = vertices.Count + 1;
                            reconstructionMapping[vertices.Count + 1] = v;

                            // create new adjacency list
                            List<int>[] subAdjacencyList = new List<int>[vertices.Count + 2];
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
                            // also for u ...
                            subAdjacencyList[vertices.Count] = new List<int>();
                            foreach(int oldNeighbor in adjacencyList[u])
                            {
                                if (component[oldNeighbor])
                                {
                                    int newNeighbor = reductionMapping[oldNeighbor];
                                    subAdjacencyList[vertices.Count].Add(newNeighbor);
                                }
                            }
                            // ... and for v
                            subAdjacencyList[vertices.Count + 1] = new List<int>();
                            foreach (int oldNeighbor in adjacencyList[v])
                            {
                                if (component[oldNeighbor])
                                {
                                    int newNeighbor = reductionMapping[oldNeighbor];
                                    subAdjacencyList[vertices.Count + 1].Add(newNeighbor);
                                }
                            }

                            // create graph
                            subGraphs.Add(new Graph(subAdjacencyList));
                        }

                        int maxVertices = 0;
                        int maxEdges = 0;
                        foreach (Graph subgraph in subGraphs)
                        {
                            if (subgraph.vertexCount > maxVertices)
                            {
                                maxVertices = subgraph.vertexCount;
                            }
                            if (subgraph.edgeCount > maxEdges)
                            {
                                maxEdges = subgraph.edgeCount;
                            }
                        }
                        Console.WriteLine("splitted graph with {0} vertices and {1} edges into {2} smaller graphs with at most {3} vertices and {4} edges", graph.vertexCount, graph.edgeCount, subGraphs.Count, maxVertices, maxEdges);
                        return true;
                    }

                    // reset second vertex
                    separator[v] = false;
                }

                // reset first vertex
                separator[u] = false;
            }
            return false;
        }

        public IEnumerable<BitSet> CandidateSeparators()
        {
            
            foreach (BitSet candidate in HeuristicDecomposition(Heuristic.min_fill))
            {
                yield return candidate;
            }
            /*
            foreach (BitSet candidate in HeuristicDecomposition(Heuristic.min_degree))
            {
                yield return candidate;
            }
            */
        }

        enum Heuristic
        {
            min_fill,
            min_degree
        }

        private IEnumerable<BitSet> HeuristicDecomposition(Heuristic mode)
        {
            // copy fields so that we can change them locally
            List<int>[] adjacencyList = new List<int>[vertexCount];
            BitSet[] neighborSetsWithout = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacencyList[i] = new List<int>(this.adjacencyList[i]);
                neighborSetsWithout[i] = new BitSet(this.neighborSetsWithout[i]);
            }

            BitSet remaining = BitSet.All(vertexCount);


            for (int i = 0; i < vertexCount; i++)
            {
                int min = FindMinCostVertex(remaining, adjacencyList, neighborSetsWithout, mode);
                yield return neighborSetsWithout[min];
                remaining[min] = false;
                for (int j = 0; j < adjacencyList[min].Count; j++)
                {
                    int u = adjacencyList[min][j];
                    
                    // remove min from the neighbors' adjacency lists
                    neighborSetsWithout[u][min] = false;
                    adjacencyList[u].Remove(min);

                    // make neighbors into a clique
                    for (int k = j + 1; k < adjacencyList[min].Count; k++)
                    {
                        int v = adjacencyList[min][k];
                        if (!neighborSetsWithout[u][v])
                        {
                            neighborSetsWithout[u][v] = true;
                            neighborSetsWithout[v][u] = true;
                            adjacencyList[u].Add(v);
                            adjacencyList[v].Add(u);
                        }
                    }
                }
            }
        }

        private int FindMinCostVertex(BitSet remaining, List<int>[] adjacencyList, BitSet[] neighborSetsWithout, Heuristic mode)
        {
            List<int> remainingVertices = remaining.Elements();
            int min = int.MaxValue;
            int vmin = -1;
            switch (mode)
            {
                case Heuristic.min_fill:
                    foreach (int v in remainingVertices)
                    {
                        int fillEdges = 0;
                        BitSet neighbors = new BitSet(neighborSetsWithout[v]);

                        // because we maintain the graph this should be the same
                        BitSet debug = new BitSet(neighbors);
                        debug.IntersectWith(remaining);
                        Debug.Assert(neighbors.Equals(debug));

                        foreach (int neighbor in neighbors.Elements())
                        {
                            BitSet newEdges = new BitSet(neighbors);
                            newEdges.ExceptWith(neighborSetsWithout[neighbor]);
                            fillEdges += (int)newEdges.Count() - 1;
                        }

                        if (fillEdges < min)
                        {
                            min = fillEdges;
                            vmin = v;
                        }
                    }
                    return vmin;

                case Heuristic.min_degree:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }


        /// <summary>
        /// recombines tree decompositions for the subgraphs into a tree decomposition for the original graph
        /// </summary>
        /// <param name="ptds">the tree decompositions for each of the subgraphs</param>
        /// <returns>a tree decomposition for the original graph</returns>
        public PTD RecombineTreeDecompositions(PTD[] ptds)
        {
            PTD separatorNode = null;
            Stack<PTD> childrenStack = new Stack<PTD>();

            // reindex the tree decompositions
            for (int i = 0; i < ptds.Length; i++)
            {
                int[] reconstructionMapping = reconstructionMappings[i];
                childrenStack.Push(ptds[i]);

                while (childrenStack.Count > 0)
                {
                    PTD currentNode = childrenStack.Pop();

                    // reindex bag
                    BitSet reconstructedBag = new BitSet(vertexCount);
                    foreach (int j in currentNode.Bag.Elements())
                    {
                        reconstructedBag[reconstructionMapping[j]] = true;
                    }
                    currentNode.SetBag(reconstructedBag);

                    // find separator node in the first tree decomposition
                    if (i == 0 && currentNode.Bag.IsSuperset(separator))
                    {
                        separatorNode = currentNode;
                    }

                    // push children onto the stack
                    foreach (PTD childNode in currentNode.children)
                    {
                        childrenStack.Push(childNode);
                    }
                }
            }

            // reroot the other tree decompositions and append them to the first one
            for (int i = 1; i < ptds.Length; i++)
            {
                separatorNode.children.Add(ptds[i].Reroot(separator));
            }

            return ptds[0];
        }
    }
}
