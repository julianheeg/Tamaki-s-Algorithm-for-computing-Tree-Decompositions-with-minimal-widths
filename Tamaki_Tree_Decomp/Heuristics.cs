using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    public static class Heuristics
    {
        /// <summary>
        /// an enumeration of implemented heuristics for finding small tree decompositions
        /// </summary>
        public enum Heuristic
        {
            min_degree,
            min_fill
        }

        /// <summary>
        /// finds candidate safe separators using a heuristic
        /// </summary>
        /// <param name="mode">the decomposition heuristic</param>
        /// <returns>an enumerable that lists candidate safe separators</returns>
        public static IEnumerable<(BitSet, BitSet)> HeuristicBagsAndNeighbors(Graph graph, Heuristic mode)
        {
            // only to vertexCount - 1 because the empty set is trivially a safe separator
            for (int i = 0; i < graph.vertexCount - 1; i++)
            {
                // if the remaining graph is a clique, end the iterator. The last bag is then in graph.notRemovedVertices
                if (graph.notRemovedVertexCount*(graph.notRemovedVertexCount-1)/2 == graph.edgeCount)
                {
                    yield break;
                }

                int min = FindMinCostVertex(graph.notRemovedVertices, graph, mode);

                // TODO: separator might be wrong #################################################################################################################
                // TODO: add minK parameter and stop once a separator is too big (perhaps low can be used for that, but it is usually used a bit differently)


                // TODO: why not this:
                BitSet result = new BitSet(graph.neighborSetsWithout[min]);

                /*
                // weird outlet computation, but it should work
                BitSet result = new BitSet(graph.neighborSetsWithout[min]);
                result[min] = true;
                result = graph.Neighbors(result);
                result = graph.Neighbors(result);
                result.IntersectWith(graph.neighborSetsWithout[min]);
                */

                yield return (graph.neighborSetsWith[min], result);
                // ################################################################################################################################################

                // remove min from the neighbors' adjacency lists
                graph.Remove(min);

                // make neighbors into a clique
                graph.MakeIntoClique(graph.adjacencyList[min]);
            }
        }

        /// <summary>
        /// finds candidate safe separators using a heuristic. This is Tamaki's code.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static List<BitSet> Tamaki_HeuristicVerticesAndNeighbors(Graph graph, Heuristic mode)
        {
            // code adapted from https://github.com/TCS-Meiji/PACE2017-TrackA/blob/master/tw/exact/GreedyDecomposer.java

            List<BitSet> separators = new List<BitSet>();

            // ----- lines 62 to 64 -----

            // copy fields so that we can change them locally
            Graph copy = new Graph(graph);

            List<BitSet> frontier = new List<BitSet>();
            BitSet remaining = BitSet.All(graph.vertexCount);

            // ----- lines 66 to 80 -----

            while (!remaining.IsEmpty())
            {
                // ----- lines 67 to 80 -----

                int vmin = FindMinCostVertex(remaining, copy, mode);

                // ----- line 82 -----

                List<BitSet> joined = new List<BitSet>();

                // lines 84 and 85 -----

                BitSet toBeAclique = new BitSet(graph.vertexCount);
                toBeAclique[vmin] = true;

                // ----- lines 87 to 93 -----

                foreach (BitSet s in frontier)
                {
                    if (s[vmin])
                    {
                        joined.Add(s);
                        toBeAclique.UnionWith(s);
                    }
                }

                // ----- lines 97 to 119 -----

                if (joined.Count == 0)
                {
                    toBeAclique[vmin] = true;
                }
                else if (joined.Count == 1)
                {
                    BitSet uniqueSeparator = joined[0];
                    BitSet test = new BitSet(copy.neighborSetsWithout[vmin]);
                    test.IntersectWith(remaining);
                    if (uniqueSeparator.IsSupersetOf(test))
                    {
                        uniqueSeparator[vmin] = false;
                        if (uniqueSeparator.IsEmpty())
                        {
                            separators.Remove(uniqueSeparator);

                            frontier.Remove(uniqueSeparator);
                        }
                        remaining[vmin] = false;
                        continue;
                    }
                }

                // ----- line 121 -----

                BitSet temp = new BitSet(copy.neighborSetsWithout[vmin]);
                temp.IntersectWith(remaining);
                toBeAclique.UnionWith(temp);

                // ----- line 129 -----

                copy.MakeIntoClique(toBeAclique.Elements());

                // ----- lines 131 and 132 -----
                BitSet sep = new BitSet(toBeAclique);
                sep[vmin] = false;

                // ----- lines 134 to 147 -----

                if (!sep.IsEmpty())
                {
                    BitSet separator = new BitSet(sep);
                    separators.Add(separator);

                    frontier.Add(separator);
                }

                // ----- lines 153 to 161 -----
                foreach (BitSet s in joined)
                {
                    Debug.Assert(!s.IsEmpty());

                    frontier.Remove(s);
                }
                remaining[vmin] = false;
            }

            return separators;
        }


        /// <summary>
        /// finds the vertex with the lowest cost to remove from a graph with respect to a heuristic
        /// </summary>
        /// <param name="remaining">the vertices that aren't yet removed (as opposed to ones that have been removed in a previous iteration)</param>
        /// <param name="adjacencyList">the adjacency list for the graph</param>
        /// <param name="neighborSetsWithout">the open neighborhood sets for the graph</param>
        /// <param name="mode">the decomposition heuristic</param>
        /// <returns>the vertex with the lowest cost to remove from the graph with respect to the chosen decomposition heuristic</returns>
        public static int FindMinCostVertex(BitSet remaining, Graph graph, Heuristic mode)
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
                        BitSet neighbors = new BitSet(graph.neighborSetsWithout[v]);

                        foreach (int neighbor in neighbors.Elements())
                        {
                            BitSet newEdges = new BitSet(neighbors);
                            newEdges.ExceptWith(graph.neighborSetsWithout[neighbor]);
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
                    foreach (int v in remainingVertices)
                    {
                        if (graph.adjacencyList[v].Count < min)
                        {
                            min = graph.adjacencyList[v].Count;
                            vmin = v;
                        }
                    }
                    return vmin;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
