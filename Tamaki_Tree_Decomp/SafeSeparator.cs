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

            if (TestNotConnected() || FindSize1Separator() || FindSize2Separator() || HeuristicDecomposition() || FindSize3Separator() || FindCliqueSeparator() || FindAlmostCliqueSeparator())
            {
                if (verbose)
                {
                    Console.WriteLine("graph {0} contains a {1} as safe separator of type {2}", graph.graphID, separator.ToString(), separatorType);
                }
                List<int> separatorVertices = separator.Elements();
                separatorSize = separatorVertices.Count;    // TODO: correct? should be...
                graph.MakeIntoClique(separatorVertices);

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
            return false;
        }

        #region exact safe separator search

        // TODO: check if graph is connected, otherwise split immediately

        /// <summary>
        /// lists all articulation points of the graph
        /// This method can also be used to test for separators of size n by passing a set of n-1 vertices as
        /// ignoredVertices. and combining the result with the ignoredVertices.
        /// Code adapated from https://slideplayer.com/slide/12811727/
        /// </summary>
        /// <param name="ignoredVertices">a list of vertices to treat as non-existent</param>
        /// <returns>an iterator over all articulation points</returns>
        public IEnumerable<int> ArticulationPoints(List<int> ignoredVertices = null)
        {
            if (graph.vertexCount == 0)
            {
                yield break;
            }

            ignoredVertices = ignoredVertices ?? new List<int>();   // create an empty list if the given one is null in order to prevent NullReferenceExceptions

            int start = 0;
            while (ignoredVertices.Contains(start))
            {
                start++;
            }
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
                if (count[rootNeighbor] == int.MaxValue && !ignoredVertices.Contains(rootNeighbor))
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
                                if (neighbor != fromNode && !ignoredVertices.Contains(neighbor))
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
            foreach (int a in ArticulationPoints())
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
            List<int> ignoredVertices = new List<int>(1) { -1 };

            // loop over every pair of vertices
            for (int u = 0; u < graph.vertexCount; u++)
            {
                ignoredVertices[0] = u;
                foreach (int a in ArticulationPoints(ignoredVertices))
                {
                    separator = new BitSet(graph.vertexCount);
                    separator[u] = true;
                    separator[a] = true;
                    separatorType = SeparatorType.Size2;
                    return true;
                }
            }
            return false;
        }

        [ThreadStatic]
        public static Stopwatch size3SeparationStopwatch;
        [ThreadStatic]
        public static int size3separators = 0;

        /// <summary>
        /// Tests if the graph can be separated with a safe separator of size 3. If so, the separator is saved in the "separator" member variable
        /// </summary>
        /// <returns>true iff a size 2 separator exists</returns>
        public bool FindSize3Separator()
        {
            if (!size3Separate || graph.vertexCount < 3)
            {
                return false;
            }
            if (size3SeparationStopwatch == null)
            {
                size3SeparationStopwatch = new Stopwatch();
            }
            size3SeparationStopwatch.Start();
            List<int> ignoredVertices = new List<int>(2) { -1, -1 };

            // loop over every pair of vertices
            for (int u = 0; u < graph.vertexCount; u++)
            {
                ignoredVertices[0] = u;
                for (int v = u + 1; v < graph.vertexCount; v++)
                {
                    ignoredVertices[1] = v;
                    foreach (int a in ArticulationPoints(ignoredVertices))
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
                }
            }
            size3SeparationStopwatch.Stop();
            return false;
        }

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
            if (cliqueSeparatorStopwatch == null)
            {
                cliqueSeparatorStopwatch = new Stopwatch();
            }
            cliqueSeparatorStopwatch.Start();

            MCS_M_Plus(out int[] alpha, out Graph H, out BitSet X);

            //Atoms(alpha, H, X, out List<Graph> atoms, out List<BitSet> minSeps, out List<BitSet> cliqueSeps, ignoredVertex);
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
                H.Insert(u, v);
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
