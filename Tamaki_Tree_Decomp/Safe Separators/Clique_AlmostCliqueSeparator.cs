using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp.Safe_Separators
{
    public partial class SafeSeparator
    {
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
            while (almostCliqueVertex < graph.vertexCount)
            {
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
                    if (!F.Contains((y, x)) && !graph.openNeighborhood[x][y])
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
                    BitSet intersection = new BitSet(H_prime.openNeighborhood[x]);   // TODO: reuse to avoid garbage
                    for (int j = 0; j < S.Count; j++)
                    {
                        intersection.IntersectWith(graph.closedNeighborhood[S[j]]);  // TODO: one could implement a test for an early exit
                    }
                    if (intersection.Equals(H_prime.openNeighborhood[x]))
                    {
                        separator = H_prime.openNeighborhood[x];
                        cliqueSeparators++;
                        return true;
                    }
                }

                // remove x from H'
                H_prime.Remove(x);
            }

            return false;
        }
    }
}
