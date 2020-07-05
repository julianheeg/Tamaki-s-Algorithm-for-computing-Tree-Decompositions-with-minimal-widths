using System;
using System.Collections.Generic;
using System.IO;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// a class for representing a mutable graph, i. e. a graph that is suited for preprocessing.
    /// Such a graph is not suited to run expensive algorithms on. Use the ImmutableGraph class for that.
    /// </summary>
    class MutableGraph
    {
        public readonly List<int>[] adjacencyList;
        public readonly BitSet[] neighborSetsWithout;   // contains N(v)
        public readonly BitSet[] neighborSetsWith;      // contains N[v]
        private readonly BitSet removedVertices;
        int vertexCount;
        int edgeCount;

        public static bool useSetInsteadOfList = false;

        public MutableGraph(string filepath)
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
                                edgeCount = Convert.ToInt32(tokens[3]);

                                adjacencyList = new List<int>[vertexCount];
                                for (int i = 0; i < vertexCount; i++)
                                {
                                    adjacencyList[i] = new List<int>();
                                }
                                neighborSetsWithout = new BitSet[vertexCount];
                                for (int i = 0; i < vertexCount; i++)
                                {
                                    neighborSetsWithout[i] = new BitSet(vertexCount);
                                }
                            }
                            else
                            {
                                int u = Convert.ToInt32(tokens[0]) - 1;
                                int v = Convert.ToInt32(tokens[1]) - 1;

                                if (!neighborSetsWithout[u][v])
                                {
                                    adjacencyList[u].Add(v);
                                    adjacencyList[v].Add(u);
                                    neighborSetsWithout[u][v] = true;
                                    neighborSetsWithout[v][u] = true;
                                }
                            }
                        }
                    }
                }

                neighborSetsWith = new BitSet[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    neighborSetsWith[i] = new BitSet(neighborSetsWithout[i]);
                    neighborSetsWith[i][i] = true;
                }

                //graphID = graphCount;
                //graphCount++;
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        public MutableGraph(List<int> adjacencyList, BitSet component, BitSet separator)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// copy constructor. Constructs a deep copy of the given graph
        /// </summary>
        /// <param name="graph">the graph to copy from</param>
        public MutableGraph(MutableGraph graph)
        {
            vertexCount = graph.vertexCount;
            edgeCount = graph.edgeCount;
            removedVertices = new BitSet(graph.removedVertices);
            adjacencyList = new List<int>[vertexCount];
            neighborSetsWithout = new BitSet[vertexCount];
            neighborSetsWith = new BitSet[vertexCount];
            for (int i = 0; i < graph.adjacencyList.Length; i++)
            {
                adjacencyList[i] = new List<int>(graph.adjacencyList[i]);
                neighborSetsWithout[i] = new BitSet(graph.neighborSetsWithout[i]);
                neighborSetsWith[i] = new BitSet(graph.neighborSetsWith[i]);
            }
        }

        public List<MutableGraph> Separate(BitSet separator)
        {
            throw new NotImplementedException();
        }

        public void Remove(int vertex)
        {
            throw new NotImplementedException();
        }
    }
}
