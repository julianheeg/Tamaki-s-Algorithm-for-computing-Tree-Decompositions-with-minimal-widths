using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;
using static Tamaki_Tree_Decomp.Heuristics;

namespace Tamaki_Tree_Decomp.Safe_Separators
{
    public partial class SafeSeparator
    {
        #region heuristic safe separator search

        /// <summary>
        /// Tries to find a safe separator using heuristics. If so, the graph is split and the resulting
        /// subgraphs and the reconstruction mappings are saved in the corresponding member variables.
        /// </summary>
        /// <returns>true iff a safe separator has been found</returns>
        public bool HeuristicDecomposition()
        {
            // consider all candidate separators
            foreach (BitSet candidateSeparator in CandidateSeparators())
            {
                if (IsSafeSeparator_Heuristic(candidateSeparator))
                {
                    separator = candidateSeparator;
                    separatorType = SeparatorType.CliqueMinor;
                    return true;
                }

            }
            return false;
        }

        private static readonly int MAX_MISSINGS = 100;
        private static readonly int MAX_STEPS = 1000000;

        /// <summary>
        /// tests heuristically if a candidate separator is a safe separator. If this method returns true then the separator is guaranteed to be safe. False negatives are however possible.
        /// </summary>
        /// <param name="candidateSeparator">the candidate separator to test</param>
        /// <returns>true iff the used heuristic gives a guarantee that the candidate is a safe separator</returns>
        public bool IsSafeSeparator_Heuristic(BitSet candidateSeparator)  // TODO: make private
        {
            bool isFirstComponent = true;

            // try to find a contraction of each component where the candidate separator is a labelled minor
            foreach ((BitSet, BitSet) C_NC in graph.ComponentsAndNeighbors(candidateSeparator))
            {
                BitSet component = C_NC.Item1;

                // test two things:
                //   1. test if only one component exists. In that case we don't have a safe separator (or a separator at all, for that matter)
                //      the test ist done by testing if the union of the candidate and the component is equal to the entire vertex set
                //   2. test if the number of missing edges is larger than the max missings parameter. In that case we give up on this candidate
                // both things need to be tested only once, obviously, therefore we test it only when we examine the first component.
                if (isFirstComponent)
                {
                    isFirstComponent = false;

                    // test if there is only one component associated with the separator
                    BitSet allTest = new BitSet(candidateSeparator);
                    allTest.UnionWith(component);
                    if (allTest.Equals(graph.notRemovedVertices))
                    {
                        return false;
                    }

                    // count missing edges
                    int missingEdges = 0;
                    foreach (int v in candidateSeparator.Elements())
                    {
                        BitSet separator = new BitSet(candidateSeparator);
                        separator.ExceptWith(graph.openNeighborhood[v]);
                        missingEdges += (int)separator.Count() - 1;
                    }
                    missingEdges /= 2;

                    if (missingEdges > MAX_MISSINGS)
                    {
                        return false;
                    }
                }

                // search for the candidate separator as a clique minor on the remaining graph without the component
                BitSet sep = C_NC.Item2;
                BitSet rest = new BitSet(graph.notRemovedVertices);
                rest.ExceptWith(sep);
                rest.ExceptWith(component);
                if (!FindCliqueMinor(sep, rest))
                {
                    return false;
                }
            }

            return true;
        }

        #region Tamaki's logic

        private class Edge
        {
            public readonly int left1;
            public readonly int left2;
            public bool unAugmentable;

            internal Edge(int left1, int left2)
            {
                this.left1 = left1;
                this.left2 = left2;
            }

            /// <summary>
            /// tries to find a pair of right nodes that can cover this edge and that are still available 
            /// </summary>
            /// <param name="rightNodes">the list of right nodes</param>
            /// <param name="available">the set of available nodes</param>
            /// <param name="graph">the underlying graph</param>
            /// <param name="coveringPair">a covering pair of right nodes if one exists, else null</param>
            /// <returns>true, iff a covering pair could be found</returns>
            internal bool FindCoveringPair(List<RightNode> rightNodes, BitSet available, Graph graph, out (RightNode, RightNode) coveringPair)
            {
                foreach (RightNode right1 in rightNodes)
                {
                    if (right1.neighborSet[left1] && !right1.neighborSet[left2])
                    {
                        foreach (RightNode right2 in rightNodes)
                        {
                            if (right2.neighborSet[left2] && !right2.neighborSet[left1])
                            {
                                // ----- lines 508 to 519 -----

                                BitSet vs1 = right1.vertexSet;
                                BitSet vs2 = right2.vertexSet;
                                BitSet vs = new BitSet(vs1);

                                while (true)
                                {
                                    BitSet ns = graph.Neighbors(vs);
                                    if (ns.Intersects(vs2))
                                    {
                                        coveringPair = (right1, right2);
                                        return true;
                                    }
                                    ns.IntersectWith(available);
                                    if (ns.IsEmpty())
                                    {
                                        break;
                                    }
                                    vs.UnionWith(ns);
                                }
                            }
                        }
                    }
                }
                coveringPair = (null, null);
                return false;
            }

            internal bool IsFinallyCovered(List<RightNode> rightNodes)
            {
                foreach (RightNode rightNode in rightNodes)
                {
                    if (rightNode.FinallyCovers(this))
                    {
                        return true;
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return String.Format("({0},{1}), unaugmentable = {2}", left1 + 1, left2 + 1, unAugmentable);
            }
        }

        private class RightNode
        {
            internal BitSet vertexSet;
            internal BitSet neighborSet;
            internal int assignedTo = -1;

            public RightNode(int vertex, BitSet neighbors, int vertexCount)
            {
                vertexSet = new BitSet(vertexCount);
                vertexSet[vertex] = true;
                neighborSet = neighbors;    // TODO: copy?
            }

            public RightNode(BitSet vertexSet, BitSet neighbors)
            {
                this.vertexSet = vertexSet;
                neighborSet = neighbors;
            }

            internal bool PotentiallyCovers(Edge edge)
            {
                return assignedTo == -1 && neighborSet[edge.left1] && neighborSet[edge.left2];
            }

            internal bool FinallyCovers(Edge edge)
            {
                return assignedTo == edge.left1 && neighborSet[edge.left2] || assignedTo == edge.left2 && neighborSet[edge.left1];
            }

            public override string ToString()
            {
                return String.Format("vertices: {{{0}}}, neighbors: {{{1}}}, assigned to: {2}", vertexSet.ToString(), neighborSet.ToString(), assignedTo + 1);
            }
        }

        /// <summary>
        /// tries to contract edges withing a subgraph consisting of a separator and a vertex set in such a way that the separator becomes
        /// a clique minor. This method works heuristically, so false negatives are possible.
        /// </summary>
        /// <param name="separator">the separator</param>
        /// <param name="rest">the vertex set</param>
        /// <returns>true, iff the graph could be contracted to a a clique on the separator</returns>
        private bool FindCliqueMinor(BitSet separator, BitSet rest)
        {
            BitSet available = new BitSet(rest);
            List<int> leftNodes = separator.Elements();
            int separatorSize = leftNodes.Count;

            // build a list of the missing edges
            List<Edge> missingEdges = new List<Edge>();
            for (int i = 0; i < leftNodes.Count; i++)
            {
                int left1 = leftNodes[i];
                for (int j = i + 1; j < leftNodes.Count; j++)
                {
                    int left2 = leftNodes[j];
                    if (!graph.openNeighborhood[left1][left2])
                    {
                        missingEdges.Add(new Edge(left1, left2));
                    }
                }
            }

            // exit early if there is no missing edge
            if (missingEdges.Count == 0)
            {
                return true;
            }

            // ----- lines 241 to 263 -----

            List<RightNode> rightNodes = new List<RightNode>();
            BitSet neighborSet = graph.Neighbors(separator);
            neighborSet.IntersectWith(rest);
            List<int> neighbors = neighborSet.Elements();

            for (int i = 0; i < neighbors.Count; i++)
            {
                int v = neighbors[i];
                if (graph.adjacencyList[v].Count == 1)
                {
                    continue;
                }
                bool useless = true;
                for (int j = 0; j < missingEdges.Count; j++)
                {
                    Edge missingEdge = missingEdges[j];
                    if (graph.openNeighborhood[v][missingEdge.left1] || graph.openNeighborhood[v][missingEdge.left2])
                    {
                        useless = false;
                        break;
                    }
                }

                if (useless)
                {
                    continue;
                }

                RightNode rn = new RightNode(v, graph.openNeighborhood[v], graph.vertexCount);
                rightNodes.Add(rn);
                available[v] = false;
            }


            int steps = 0;

            // ----- lines 265 to 281 -----

            while (FindZeroCoveredEdge(missingEdges, rightNodes, out Edge zeroCoveredEdge))
            {
                if (zeroCoveredEdge.FindCoveringPair(rightNodes, available, graph, out (RightNode, RightNode) coveringPair))
                {
                    MergeRightNodes(coveringPair, rightNodes, available, graph);
                }
                else
                {
                    return false;
                }

                steps++;
                if (steps >= MAX_STEPS)
                {
                    return false;
                }
            }

            // ----- lines 283 to 302 -----

            bool moving = true;
            while (rightNodes.Count > separatorSize / 2 && moving)
            {
                steps++;
                if (steps > MAX_STEPS)
                {
                    return false;
                }

                moving = false;
                if (FindLeastCoveredEdge(missingEdges, rightNodes, out Edge leastCoveredEdge))
                {
                    if (leastCoveredEdge.FindCoveringPair(rightNodes, available, graph, out (RightNode, RightNode) coveringPair))
                    {
                        MergeRightNodes(coveringPair, rightNodes, available, graph);
                        moving = true;
                    }
                    else
                    {
                        leastCoveredEdge.unAugmentable = true;
                    }
                }
            }

            // filter out right nodes that cover no missing edge potentially
            rightNodes =
                    rightNodes.FindAll(
                        rightNode => missingEdges.Exists(
                            edge => rightNode.PotentiallyCovers(edge)
                        )
                    );

            // ----- perform final contractions (lines 340 to 400) -----

            while (missingEdges.Count > 0)
            {
                // ----- find best covering pair (lines 347 to 383) -----

                int bestPair_left = -1;
                RightNode bestPair_right = null;
                int maxMinCover = 0;
                int maxFc = 0;

                foreach (int leftNode in leftNodes)
                {
                    foreach (RightNode rightNode in rightNodes)
                    {
                        if (rightNode.assignedTo != -1 || !rightNode.neighborSet[leftNode])
                        {
                            continue;
                        }
                        steps++;
                        if (steps > MAX_STEPS)
                        {
                            return false;
                        }
                        rightNode.assignedTo = leftNode;
                        int minCover = MinCover(missingEdges, rightNodes, graph.vertexCount);
                        int fc = 0;
                        foreach (Edge edge in missingEdges)
                        {
                            if (edge.IsFinallyCovered(rightNodes))
                            {
                                fc++;
                            }
                        }
                        rightNode.assignedTo = -1;
                        if (bestPair_left == -1 && bestPair_right == null || minCover > maxMinCover)
                        {
                            maxMinCover = minCover;
                            bestPair_left = leftNode;
                            bestPair_right = rightNode;
                            maxFc = fc;
                        }
                        else if (minCover == maxMinCover && fc > maxFc)
                        {
                            bestPair_left = leftNode;
                            bestPair_right = rightNode;
                            maxFc = fc;
                        }
                    }
                }
                if (maxMinCover == 0)
                {
                    return false;
                }

                // finally assign best pair
                bestPair_right.assignedTo = bestPair_left;

                // update missing edges list
                missingEdges.RemoveAll(edge => edge.IsFinallyCovered(rightNodes));
            }

            return true;
        }

        /// <summary>
        /// finds the augmentable missing edge, which is 'potentially covered' by the least amount of right nodes
        /// </summary>
        /// <param name="missingEdges">the list of missing edges</param>
        /// <param name="rightNodes">the list of right nodes</param>
        /// <param name="leastCoveredEdge">the least covered edge, if there is at least one such edge, else null</param>
        /// <returns>true, iff such an edge exists</returns>
        private bool FindLeastCoveredEdge(List<Edge> missingEdges, List<RightNode> rightNodes, out Edge leastCoveredEdge)
        {
            int minCover = 0;
            leastCoveredEdge = null;
            foreach (Edge edge in missingEdges)
            {
                if (edge.unAugmentable)
                {
                    continue;
                }
                int nCover = 0;
                foreach (RightNode rightNode in rightNodes)
                {
                    if (rightNode.PotentiallyCovers(edge))
                    {
                        nCover++;
                    }
                }
                if (leastCoveredEdge == null || nCover < minCover)
                {
                    minCover = nCover;
                    leastCoveredEdge = edge;
                }
            }
            return leastCoveredEdge != null;
        }

        /// <summary>
        /// determines the amount of right nodes that 'potentially cover' the least 'potentially covered' missing edge
        /// </summary>
        /// <param name="missingEdges">the list of missing edges</param>
        /// <param name="rightNodes">the list of right nodes</param>
        /// <param name="vertexCount">the graph's vertex count</param>
        /// <returns></returns>
        private int MinCover(List<Edge> missingEdges, List<RightNode> rightNodes, int vertexCount)
        {
            int minCover = vertexCount;
            foreach (Edge edge in missingEdges)
            {
                if (edge.IsFinallyCovered(rightNodes))
                {
                    continue;
                }
                int nCover = 0;
                foreach (RightNode rightNode in rightNodes)
                {
                    if (rightNode.PotentiallyCovers(edge))
                    {
                        nCover++;
                    }
                }
                if (nCover < minCover)
                {
                    minCover = nCover;
                }
            }
            return minCover;
        }

        /// <summary>
        /// merges the two right nodes of a covering pair
        /// </summary>
        /// <param name="coveringPair">the covering pair</param>
        /// <param name="rightNodes">the list of right nodes</param>
        /// <param name="available">the list of available nodes</param>
        /// <param name="graph">the underlying graph</param>
        private void MergeRightNodes((RightNode, RightNode) coveringPair, List<RightNode> rightNodes, BitSet available, Graph graph)
        {
            // ----- lines 523 to 558 -----

            RightNode rn1 = coveringPair.Item1;
            RightNode rn2 = coveringPair.Item2;

            BitSet vs1 = rn1.vertexSet;
            BitSet vs2 = rn2.vertexSet;

            List<BitSet> layerList = new List<BitSet>();

            BitSet vs = new BitSet(vs1);
            int counter = 0;
            while (true)
            {
                // TODO: remove debug stuff
                counter++;
                if (counter > 5000)
                {
                    throw new Exception("Possibly infinite loop in MergeRightNodes detected.");
                }

                BitSet ns = graph.Neighbors(vs);
                if (ns.Intersects(vs2))
                {
                    break;
                }
                ns.IntersectWith(available);
                layerList.Add(ns);
                vs.UnionWith(ns);
            }

            BitSet result = new BitSet(vs1);
            result.UnionWith(vs2);

            BitSet back = graph.Neighbors(vs2);
            for (int i = layerList.Count - 1; i >= 0; i--)
            {
                BitSet ns = layerList[i];
                ns.IntersectWith(back);
                int v = ns.First();
                result[v] = true;
                available[v] = false;
                back = graph.openNeighborhood[v];
            }

            rightNodes.Remove(rn1);
            rightNodes.Remove(rn2);
            rightNodes.Add(new RightNode(result, graph.Neighbors(result)));
        }

        /// <summary>
        /// tries to find an edge that is not 'potentially covered' by any right node
        /// </summary>
        /// <param name="missingEdges">the list of edges that are still required to form the separator into a clique</param>
        /// <param name="rightNodes">the list of right nodes</param>
        /// <param name="zeroCoveredEdge">a zero covered edge if there is one, else null</param>
        /// <returns>true, iff a zero covered edge could be found</returns>
        bool FindZeroCoveredEdge(List<Edge> missingEdges, List<RightNode> rightNodes, out Edge zeroCoveredEdge)
        {
            foreach (Edge edge in missingEdges)
            {
                bool isCovered = false;
                foreach (RightNode rightNode in rightNodes)
                {
                    if (rightNode.PotentiallyCovers(edge))
                    {
                        isCovered = true;
                        break;
                    }
                }
                if (!isCovered)
                {
                    zeroCoveredEdge = edge;
                    return true;
                }
            }
            zeroCoveredEdge = null;
            return false;
        }

        #endregion

        /// <summary>
        /// finds candidate safe separators using every implemented heuristic
        /// </summary>
        /// <returns>an enumerable that lists candidate safe separators</returns>
        private IEnumerable<BitSet> CandidateSeparators()
        {
            HashSet<BitSet> tested = new HashSet<BitSet>();

            // loop over heuristics
            foreach (Heuristic heuristic in Enum.GetValues(typeof(Heuristic)))
            {
                // return candidates one by one
                foreach (BitSet candidate in Tamaki_HeuristicVerticesAndNeighbors(graph, heuristic))
                {
                    if (!tested.Contains(candidate))
                    {
                        yield return candidate;
                        tested.Add(candidate);
                    }
                }
            }
        }

        #endregion
    }
}
