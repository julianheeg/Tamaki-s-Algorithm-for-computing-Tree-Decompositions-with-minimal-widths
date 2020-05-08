using System;
using System.Collections.Generic;
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
        int low;


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
            /*
            reconstructionBagsToAppendTo = new List<BitSet>();
            reconstructionBagsToAppend = new List<BitSet>();
            */
        }

        public bool Separate(out List<Graph> separatedGraphs, ref int minK)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests if the graph can be separated with a separator of size 2. If so, the graph is split and the
        /// resulting subgraphs and the reconstruction mappings are saved in the corresponding member variables
        /// TODO: Improve. This is a naive (n^3) implementation.
        /// </summary>
        /// <returns>true iff a size 2 separator exists</returns>
        private bool Size2Separate()
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
                        low = 2;

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

                            // map vertices from this graph to the new subgraph and vice versa
                            Dictionary<int, int> reductionMapping = new Dictionary<int, int>();
                            int[] reconstructionMapping = new int[vertices.Count];
                            reconstructionMappings.Add(reconstructionMapping);
                            for (int i = 0; i < vertices.Count; i++)
                            {
                                reductionMapping[vertices[i]] = i;
                                reconstructionMapping[i] = vertices[i];
                            }

                            // create new adjacency list
                            List<int>[] subAdjacencyList = new List<int>[vertices.Count];
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

                            // create graph
                            subGraphs.Add(new Graph(subAdjacencyList));
                        }

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

        public PTD CombineTreeDecompositions(PTD[] ptds)
        {
            throw new NotImplementedException();
        }
    }
}
