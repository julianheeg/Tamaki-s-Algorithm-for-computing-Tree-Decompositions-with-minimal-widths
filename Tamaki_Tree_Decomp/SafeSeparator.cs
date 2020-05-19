using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    class SafeSeparator
    {
        readonly int vertexCount;
        readonly Graph graph;

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
            
            if (Size2Separate() || HeuristicDecomposition())
            {
                PrintSeparation();
                separatedGraphs = subGraphs;
                if (minK < separatorSize)
                {
                    minK = separatorSize;
                }
                return true;
            }
            /*
            else if (HeuristicDecomposition())
            {
                separatedGraphs = subGraphs;
                if (minK < separatorSize)
                {
                    minK = separatorSize;
                }
                return true;
            }
            */

            separatedGraphs = null;
            return false;
        }

        #region exact safe separator search

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

                        CopyGraph(out List<int>[] adjacencyList, out BitSet[] neighborSetsWithout);

                        MakeSeparatorIntoClique(new List<int> { u, v }, adjacencyList, neighborSetsWithout);

                        // divide the graph into subgraphs for each component
                        foreach (BitSet component in components)
                        {
                            BuildSubgraph(new List<int> { u, v }, adjacencyList, component);
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

        #endregion

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
                if (IsSafeSeparator(candidateSeparator))
                {
                    separator = candidateSeparator;
                    List<int> separatorVertices = separator.Elements();
                    separatorSize = separatorVertices.Count;

                    CopyGraph(out List<int>[] adjacencyList, out BitSet[] neighborSetsWithout);

                    MakeSeparatorIntoClique(separatorVertices, adjacencyList, neighborSetsWithout);

                    foreach (Tuple<BitSet, BitSet> C_NC in graph.ComponentsAndNeighbors(separator))
                    {
                        BitSet component = C_NC.Item1;
                        BuildSubgraph(separatorVertices, adjacencyList, component);
                    }

                    return true;
                }

            }
            return false;
        }

        private static readonly int MAX_MISSINGS = 100;
        private static readonly int MAX_STEPS = 1000000;

        /// <summary>
        /// tests heuristically if a candidate separator is a safe separator. If this method returns true then the separator is guaranteed to be safe. False negatives are possible, however.
        /// </summary>
        /// <param name="candidateSeparator">the candidate separator to test</param>
        /// <returns>true iff the used heuristic gives a guarantee that the candidate is a safe separator</returns>
        public bool IsSafeSeparator(BitSet candidateSeparator)  // TODO: make private
        {
            bool isFirstComponent = true;

            // try to find a contraction of each component where the candidate separator is a labelled minor
            foreach (Tuple<BitSet, BitSet> C_NC in graph.ComponentsAndNeighbors(candidateSeparator))
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
                    if (allTest.Equals(graph.allVertices))
                    {
                        return false;
                    }
                    
                    // count missing edges
                    int missingEdges = 0;
                    foreach (int v in candidateSeparator.Elements())
                    {
                        BitSet separator = new BitSet(candidateSeparator);
                        separator.ExceptWith(graph.neighborSetsWithout[v]);
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
                BitSet rest = new BitSet(graph.allVertices);
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

                                BitSet vs1 = right1.neighborSet;
                                BitSet vs2 = right2.neighborSet;
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
                    if (!graph.neighborSetsWithout[left1][left2])
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

            // TAMAKI: missingEdges -> set index


            // ----- lines 241 to 263 -----

            List<RightNode> rightNodes = new List<RightNode>();
            BitSet neighborSet = graph.Neighbors(separator);
            neighborSet.IntersectWith(rest);
            List<int> neighbors = neighborSet.Elements();

            for (int i = 0; i < neighbors.Count; i++)
            {
                int v = neighbors[i];
                if (graph.adjacencyList[v].Length == 1)
                {
                    continue;
                }
                bool useless = true;
                for (int j = 0; j < missingEdges.Count; j++)
                {
                    Edge missingEdge = missingEdges[j];
                    if (graph.neighborSetsWithout[v][missingEdge.left1] || graph.neighborSetsWithout[v][missingEdge.left2])
                    {
                        useless = false;
                        break;
                    }
                }

                if (useless)
                {
                    continue;
                }

                RightNode rn = new RightNode(v, graph.neighborSetsWithout[v], graph.vertexCount);
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
                        int minCover = MinCover(missingEdges, rightNodes, vertexCount);
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
                if (counter > 50)
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
                back = graph.neighborSetsWithout[v];
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

        /// <summary>
        /// finds candidate safe separators using a heuristic. This is Tamaki's code.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private List<BitSet> Tamaki_CandidateSeparators(Heuristic mode)
        {
            // code adapted from https://github.com/TCS-Meiji/PACE2017-TrackA/blob/master/tw/exact/GreedyDecomposer.java

            List<BitSet> separators = new List<BitSet>();

            // ----- lines 62 to 64 -----

            // copy fields so that we can change them locally
            List<int>[] adjacencyList = new List<int>[vertexCount];
            BitSet[] neighborSetsWithout = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacencyList[i] = new List<int>(graph.adjacencyList[i]);
                neighborSetsWithout[i] = new BitSet(graph.neighborSetsWithout[i]);
            }

            List<BitSet> frontier = new List<BitSet>();
            BitSet remaining = BitSet.All(vertexCount);

            // ----- lines 66 to 80 -----

            while (!remaining.IsEmpty())
            {
                // ----- lines 67 to 80 -----

                int vmin = FindMinCostVertex(remaining, adjacencyList, neighborSetsWithout, mode);

                // ----- line 82 -----

                List<BitSet> joined = new List<BitSet>();

                // lines 84 and 85 -----

                BitSet toBeAclique = new BitSet(vertexCount);
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
                    BitSet test = new BitSet(neighborSetsWithout[vmin]);
                    test.IntersectWith(remaining);
                    if (uniqueSeparator.IsSuperset(test))
                    {
                        uniqueSeparator[vmin] = false;
                        if (uniqueSeparator.IsEmpty())
                        {
                            separators.Remove(uniqueSeparator);     // TODO: is it correct that BitSet implements the IEquatable<T> interface, causing
                                                                    // the equals() method to be called? equals() is not overridden in Tamaki's code, so this may be wrong?

                            frontier.Remove(uniqueSeparator);
                        }
                        remaining[vmin] = false;
                        continue;
                    }
                }

                // ----- line 121 -----

                BitSet temp = new BitSet(neighborSetsWithout[vmin]);
                temp.IntersectWith(remaining);
                toBeAclique.UnionWith(temp);

                // ----- line 129 -----

                MakeSeparatorIntoClique(toBeAclique.Elements(), adjacencyList, neighborSetsWithout);

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

            // TODO: CRITICAL???? set width  ###########################################################################################################################################

            return separators;
        }

        #endregion

        /// <summary>
        /// finds candidate safe separators using every implemented heuristic
        /// </summary>
        /// <returns>an enumerable that lists candidate safe separators</returns>
        public IEnumerable<BitSet> CandidateSeparators()
        {
            HashSet<BitSet> tested = new HashSet<BitSet>();

            // loop over heuristics
            foreach (Heuristic heuristic in Enum.GetValues(typeof(Heuristic)))
            {
                // return candidates one by one
                foreach (BitSet candidate in Tamaki_CandidateSeparators(heuristic))
                {
                    if (!tested.Contains(candidate))
                    {
                        yield return candidate;
                        tested.Add(candidate);
                    }
                }
            }
        }

        /// <summary>
        /// an enumeration of implemented heuristics for finding small tree decompositions
        /// </summary>
        enum Heuristic
        {
            min_fill,
            min_degree
        }

        /// <summary>
        /// finds candidate safe separators using a heuristic
        /// </summary>
        /// <param name="mode">the decomposition heuristic</param>
        /// <returns>an enumerable that lists candidate safe separators</returns>
        private IEnumerable<BitSet> CandidateSeparators(Heuristic mode)
        {
            // copy fields so that we can change them locally
            List<int>[] adjacencyList = new List<int>[vertexCount];
            BitSet[] neighborSetsWithout = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacencyList[i] = new List<int>(graph.adjacencyList[i]);
                neighborSetsWithout[i] = new BitSet(graph.neighborSetsWithout[i]);
            }

            BitSet remaining = BitSet.All(vertexCount);

            // only to vertexCount - 1 because the empty set is trivially a safe separator
            for (int i = 0; i < vertexCount - 1; i++)
            {
                int min = FindMinCostVertex(remaining, adjacencyList, neighborSetsWithout, mode);

                // TODO: separator might be wrong #################################################################################################################
                // TODO: add minK parameter and stop once a separator is too big (perhaps low can be used for that, but it is usually used a bit differently)

                //BitSet result = new BitSet(neighborSetsWithout[min]);

                
                // weird outlet computation, but it should work
                BitSet result = new BitSet(neighborSetsWithout[min]);
                result[min] = true;
                result = graph.Neighbors(result);
                result = graph.Neighbors(result);
                result.IntersectWith(neighborSetsWithout[min]);
                
                yield return result;
                // ################################################################################################################################################

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

        /// <summary>
        /// finds the vertex with the lowest cost to remove from a graph with respect to a heuristic
        /// </summary>
        /// <param name="remaining">the vertices that aren't yet removed (as opposed to ones that have been removed in a previous iteration)</param>
        /// <param name="adjacencyList">the adjacency list for the graph</param>
        /// <param name="neighborSetsWithout">the open neighborhood sets for the graph</param>
        /// <param name="mode">the decomposition heuristic</param>
        /// <returns>the vertex with the lowest cost to remove from the graph with respect to the chosen decomposition heuristic</returns>
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
                    foreach (int v in remainingVertices)
                    {
                        if (adjacencyList[v].Count < min)
                        {
                            min = adjacencyList[v].Count;
                            vmin = v;
                        }
                    }
                    return vmin;

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region recombination

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

        #endregion

        #region utility

        /// <summary>
        /// makes a copy of the graph's adjacency list and open neighborhood list
        /// </summary>
        /// <param name="adjacencyList">the adjacency list</param>
        /// <param name="neighborSetsWithout">the open neighborhoods for each vertex</param>
        private void CopyGraph(out List<int>[] adjacencyList, out BitSet[] neighborSetsWithout)
        {
            adjacencyList = new List<int>[vertexCount];
            neighborSetsWithout = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacencyList[i] = new List<int>(graph.adjacencyList[i]);
                neighborSetsWithout[i] = new BitSet(graph.neighborSetsWithout[i]);
            }
        }

        /// <summary>
        /// adds edges such that a separator becomes a clique in the graph given as an adjacency list
        /// </summary>
        /// <param name="separatorVertices">a list of vertices in the separator</param>
        /// <param name="adjacencyList">the graph given as an adjacency list</param>
        /// <param name="neighborSetsWithout">the open neighborhoods of that graph</param>
        private static void MakeSeparatorIntoClique(List<int> separatorVertices, List<int>[] adjacencyList, BitSet[] neighborSetsWithout)
        {
            for (int i = 0; i < separatorVertices.Count; i++)
            {
                int u = separatorVertices[i];
                for (int j = i + 1; j < separatorVertices.Count; j++)
                {
                    int v = separatorVertices[j];

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

        /// <summary>
        /// builds the induced subgraph consisting of the vertices in the component and the separator vertices using the given adjacency list. It is then appended to the subgraphs list.
        /// </summary>
        /// <param name="separatorVertices">a list of the vertices in the separator</param>
        /// <param name="adjacencyList">the adjacency list</param>
        /// <param name="component">the component</param>
        private void BuildSubgraph(List<int> separatorVertices, List<int>[] adjacencyList, BitSet component)
        {
            List<int> vertices = component.Elements();
            component.UnionWith(separator);

            // map vertices from this graph to the new subgraph and vice versa
            Dictionary<int, int> reductionMapping = new Dictionary<int, int>(vertices.Count + separatorSize);
            int[] reconstructionMapping = new int[vertices.Count + separatorSize];
            reconstructionMappings.Add(reconstructionMapping);
            for (int i = 0; i < vertices.Count; i++)
            {
                reductionMapping[vertices[i]] = i;
                reconstructionMapping[i] = vertices[i];
            }

            // don't forget the separator
            for (int i = 0; i < separatorSize; i++)
            {
                int u = separatorVertices[i];
                reductionMapping[u] = vertices.Count + i;
                reconstructionMapping[vertices.Count + i] = u;
            }

            // create new adjacency list
            List<int>[] subAdjacencyList = new List<int>[vertices.Count + separatorSize];
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
            for (int i = 0; i < separatorSize; i++)
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

            // create graph
            subGraphs.Add(new Graph(subAdjacencyList));
        }

        #endregion

        #region debug

        /// <summary>
        /// prints into how many subgraphs the graph has been separated. The vertex and edge counts of the graph and of the largest subgraph are also printed.
        /// </summary>
        [Conditional("DEBUG")]
        private void PrintSeparation()
        {
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
        }

        #endregion
    }
}
