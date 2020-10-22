using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;
using static Tamaki_Tree_Decomp.Data_Structures.Graph;

namespace Tamaki_Tree_Decomp.Safe_Separators
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

        public enum SeparatorType
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
        /// <param name="minK">the minimum treewidth parameter. If a separator is found that is greater than minK, it is set to the separator size</param>
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
                separatedGraphs = SeparateAtSeparator(ref minK, out _);

                return true;
            }

            reconstructionIndexationMappings = null;
            separatedGraphs = null;
            graph = null;
            return false;
        }

        /// <summary>
        /// separates the graph at the separator
        /// </summary>
        /// <param name="minK">the minimum treewidth parameter. It is set to the maximum of its previous value and the size of the separator</param>
        /// <param name="alreadyCalculatedComponent">Optionally, a component whose subgraph has an already calculated PTD.
        ///                                         (This happens when a safe separator is found during the "HasTreewidth" calculation.)</param>
        /// <param name="alreadyCalculatedComponentIndex">-1, if no alreadyCalculatedComponent is passed, else the index in the list of subgraphs of the subgraph corresponding to that component</param>
        /// <returns>A list of the new subgraphs</returns>
        private List<Graph> SeparateAtSeparator(ref int minK, out int alreadyCalculatedComponentIndex, BitSet alreadyCalculatedComponent=null)
        {
            List<Graph> separatedGraphs;
            List<int> separatorVertices = separator.Elements();
            separatorSize = separatorVertices.Count;
            graph.MakeIntoClique(separatorVertices);

            if (verbose)
            {
                Console.WriteLine("graph {0} contains a {1} as safe separator of type {2}", graph.graphID, separator.ToString(), separatorType);
            }

            separatedGraphs = graph.Separate(separator, out reconstructionIndexationMappings, out alreadyCalculatedComponentIndex, alreadyCalculatedComponent);

            if (minK < separatorSize)
            {
                minK = separatorSize;
            }

            // remove reference to the graph so that its ressources can be freed
            graph = null;
            return separatedGraphs;
        }

        /// <summary>
        /// Separates the graph at a separator that has been found after pre-processing during the runtime of the actual algorithm.
        /// (If this function is called, a PTD for exactly one component has always been found already.)
        /// </summary>
        /// <param name="separator">the separator</param>
        /// <param name="separatorType">the type of that separator</param>
        /// <param name="minK">the minimum treewidth parameter. It is set to the maximum of its previous value and the separator size</param>
        /// <param name="alreadyCalculatedComponent">The component whose subgraph has an already calculated PTD.</param>
        /// <param name="alreadyCalculatedComponentIndex">the index in the list of subgraphs of the subgraph corresponding to that component</param>
        /// <returns>a list of the new subgraphs</returns>
        public List<Graph> ApplyExternallyFoundSafeSeparator(BitSet separator, SeparatorType separatorType, ref int minK, out int alreadyCalculatedComponentIndex, BitSet alreadyCalculatedComponent)
        {
            this.separator = separator;
            this.separatorType = separatorType;
            return SeparateAtSeparator(ref minK, out alreadyCalculatedComponentIndex, alreadyCalculatedComponent);
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
                PTD child = ptds[i];
                PTD.Reroot(ref child, separator);
                separatorNode.children.Add(child);
            }

            return ptds[0];
        }

        #endregion
    }
}
