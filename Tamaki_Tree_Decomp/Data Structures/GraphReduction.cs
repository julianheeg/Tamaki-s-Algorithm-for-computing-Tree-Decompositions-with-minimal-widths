using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    public class GraphReduction
    {
        readonly int vertexCount;
        readonly List<int>[] adjacencyList;
        readonly BitSet[] neighborSetsWithout;
        readonly BitSet removedVertices;
        int low = 0; // TODO: heuristic for setting low is given in the paper
        readonly Dictionary<int, int> reconstructionMapping;    // mapping from reduced vertex id to original vertex id
        readonly List<BitSet> reconstructionBagsToAppendTo;     // a list of (subsets of) bags that the bags in the next list are appended to during reconstruction
        readonly List<BitSet> reconstructionBagsToAppend;       // bags to append to (subsets of) bags in the list above during reconstruction 

        public GraphReduction(Graph graph)
        {
            vertexCount = graph.vertexCount;
            adjacencyList = new List<int>[vertexCount];
            neighborSetsWithout = new BitSet[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacencyList[i] = new List<int>(graph.adjacencyList[i]);
                neighborSetsWithout[i] = new BitSet(graph.neighborSetsWithout[i]);
            }
            removedVertices = new BitSet(vertexCount);
            reconstructionMapping = new Dictionary<int, int>();
            reconstructionBagsToAppendTo = new List<BitSet>();
            reconstructionBagsToAppend = new List<BitSet>();
        }

        /// <summary>
        /// reconstructs a graph after reduction
        /// </summary>
        /// <returns>the reduced graph</returns>
        public Graph ToGraph()
        {
            // build a mapping from old graph vertices to new graph vertices and vice versa
            Dictionary<int, int> ReductionMapping = new Dictionary<int, int>();
            int counter = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                if (!removedVertices[i])
                {
                    ReductionMapping.Add(i, counter);
                    reconstructionMapping.Add(counter, i);
                    counter++;
                }
            }

            // use that mapping to construct an adjacency list for this graph
            List<int>[] reducedAdjacencyList = new List<int>[ReductionMapping.Count];
            for (int i = 0; i < vertexCount; i++)
            {
                if (!removedVertices[i])
                {
                    List<int> currentVertexAdjacencies = new List<int>(adjacencyList[i].Count);
                    for (int j = 0; j < adjacencyList[i].Count; j++)
                    {
                        int neighbor = adjacencyList[i][j];
                        currentVertexAdjacencies.Add(ReductionMapping[neighbor]);
                    }
                    reducedAdjacencyList[ReductionMapping[i]] = currentVertexAdjacencies;
                }
            }

            // TODO: can save a small bit of memory by deleting the old adjacency list and neighbor bit sets 

            return new Graph(reducedAdjacencyList);
        }

        /// <summary>
        /// tries to apply the simplicial vertex rule to the graph once
        /// TODO: perhaps pass an additional vertex where to start and "wrap" the first loop around. Might be more efficient
        /// </summary>
        /// <returns>true iff a reduction could be performed</returns>
        public bool SimplicialVertexRule()
        {
            bool isReduced = false;

            // loop over each vertex ...
            for (int i = 0; i < vertexCount && !isReduced; i++)
            {
                // ... that is not yet removed ...
                if (!removedVertices[i])
                {
                    // ... and check if its neighbors form a clique
                    bool neighborsFormClique = true;
                    for (int j = 0; j < adjacencyList[i].Count && neighborsFormClique; j++)
                    {
                        int u = adjacencyList[i][j];
                        for (int k = j + 1; k < adjacencyList[i].Count; k++)
                        {
                            int v = adjacencyList[i][k];
                            if (!neighborSetsWithout[u][v])
                            {
                                neighborsFormClique = false;
                                break;
                            }
                        }
                    }
                    if (neighborsFormClique)
                    {
                        isReduced = true;
                        removedVertices[i] = true;
                        for (int j = 0; j < adjacencyList[i].Count; j++)
                        {
                            int neighbor = adjacencyList[i][j];
                            adjacencyList[neighbor].Remove(i);
                            neighborSetsWithout[neighbor][i] = false;
                        }
                        if (adjacencyList[i].Count > low)
                        {
                            low = adjacencyList[i].Count;
                        }

                        // remember bag for reconstruction
                        BitSet bag = new BitSet(neighborSetsWithout[i]);
                        bag[i] = true;
                        reconstructionBagsToAppendTo.Add(neighborSetsWithout[i]);
                        reconstructionBagsToAppend.Add(bag);
                    }
                }
            }
            return isReduced;
        }

        /// <summary>
        /// tries to apply the almost simplicial vertex rule to the graph once
        /// TODO: perhaps pass an additional vertex where to start and "wrap" the first loop around. Might be more efficient
        /// </summary>
        /// <returns>true iff a reduction could be performed</returns>
        public bool AlmostSimplicialVertexRule()
        {
            bool isReduced = false;

            // loop over each vertex ...
            for (int i = 0; i < vertexCount && !isReduced; i++)
            {
                // ... that is not yet removed and whose degree is smaller than low ...
                if (!removedVertices[i] && low >= adjacencyList[i].Count)
                {
                    // ... and check if all of its neighbors except one form a clique
                    bool neighborsFormAlmostClique = true;
                    int edgeUNotIn = -1;
                    int edgeVNotIn = -1;
                    int vertexNotIn = -1;
                    for (int j = 0; j < adjacencyList[i].Count && neighborsFormAlmostClique; j++)
                    {
                        int u = adjacencyList[i][j];
                        for (int k = j + 1; k < adjacencyList[i].Count; k++)
                        {
                            int v = adjacencyList[i][k];
                            if (!neighborSetsWithout[u][v])
                            {
                                if (vertexNotIn == u || vertexNotIn == v)
                                {
                                    continue;
                                }
                                if (edgeUNotIn == -1)
                                {
                                    edgeUNotIn = u;
                                    edgeVNotIn = v;
                                    continue;
                                }
                                if (vertexNotIn == -1 && (edgeUNotIn == u || edgeVNotIn == u))
                                {
                                    vertexNotIn = u;
                                    continue;
                                }
                                if (vertexNotIn == -1 && (edgeUNotIn == v || edgeVNotIn == v))
                                {
                                    vertexNotIn = v;
                                    continue;
                                }

                                neighborsFormAlmostClique = false;
                                break;
                            }
                        }
                    }
                    if (neighborsFormAlmostClique)
                    {
                        isReduced = true;
                        removedVertices[i] = true;
                        for (int j = 0; j < adjacencyList[i].Count; j++)
                        {
                            int u = adjacencyList[i][j];
                            adjacencyList[u].Remove(i);
                            neighborSetsWithout[u][i] = false;
                            for (int k = j + 1; k < adjacencyList[i].Count; k++)
                            {
                                int v = adjacencyList[i][k];
                                if (!neighborSetsWithout[u][v])
                                {
                                    adjacencyList[u].Add(v);
                                    neighborSetsWithout[u][v] = true;
                                    adjacencyList[v].Add(u);
                                    neighborSetsWithout[v][u] = true;
                                }
                            }
                        }

                        // remember bag for reconstruction
                        BitSet bag = new BitSet(neighborSetsWithout[i]);
                        bag[i] = true;
                        reconstructionBagsToAppendTo.Add(neighborSetsWithout[i]);
                        reconstructionBagsToAppend.Add(bag);
                    }
                }
            }

            return isReduced;
        }

        /// <summary>
        /// tries to apply the buddy rule to the graph once
        /// TODO: perhaps pass an additional vertex where to start and "wrap" the first loop around. Might be more efficient
        /// </summary>
        /// <returns>true iff a reduction could be performed</returns>
        public bool BuddyRule()
        {
            // the rule can only be applied if low >= 3
            if (low < 3)
            {
                return false;
            }

            bool isReduced = false;

            // for each vertex i ...
            for (int i = 0; i < vertexCount && !isReduced; i++)
            {
                // ... that is not yet removed and has three neighbors ...
                if (!removedVertices[i] && adjacencyList[i].Count == 3)
                {
                    List<int> neighbors = adjacencyList[i];
                    // ... test if a neighbor's ...
                    for (int j = 0; j < 3; j++)
                    {
                        int neighbor = neighbors[j];
                        // ... neighbor ...
                        for (int k = 0; k < adjacencyList[neighbor].Count; k++)
                        {
                            // ... is a buddy
                            int buddy = adjacencyList[neighbor][k];
                            if (buddy != i && neighborSetsWithout[i].Equals(neighborSetsWithout[buddy]))
                            {
                                isReduced = true;

                                // if so, remove i and buddy from the graph ...
                                removedVertices[i] = true;
                                removedVertices[buddy] = true;

                                for (int l = 0; l < 3; l++)
                                {
                                    int u = neighbors[l];
                                    adjacencyList[u].Remove(i);
                                    adjacencyList[u].Remove(buddy);
                                    neighborSetsWithout[u][i] = false;
                                    neighborSetsWithout[u][buddy] = false;

                                    // ... and turn the neighbors into a clique
                                    for (int m = l + 1; m < 3; m++)
                                    {
                                        int v = neighbors[m];
                                        if (!neighborSetsWithout[u][v])
                                        {
                                            adjacencyList[u].Add(v);
                                            adjacencyList[v].Add(u);
                                            neighborSetsWithout[u][v] = true;
                                            neighborSetsWithout[v][u] = true;
                                        }
                                    }
                                }

                                // remember bags for reconstruction
                                BitSet bag1 = new BitSet(neighborSetsWithout[i]);
                                bag1[i] = true;
                                reconstructionBagsToAppendTo.Add(neighborSetsWithout[i]);
                                reconstructionBagsToAppend.Add(bag1);
                                BitSet bag2 = new BitSet(neighborSetsWithout[buddy]);
                                bag2[buddy] = true;
                                reconstructionBagsToAppendTo.Add(neighborSetsWithout[buddy]);
                                reconstructionBagsToAppend.Add(bag2);
                                break;
                            }
                        }
                        if (isReduced)
                        {
                            break;
                        }
                    }
                }
            }
            return isReduced;
        }

        public void RebuildTreeDecomposition(PTD ptd)
        {
            // return if there is no bag to append
            if (reconstructionBagsToAppend.Count == 0)
            {
                return;
            }

            Stack<PTD> nodeStack = new Stack<PTD>();
            List<PTD> reconstructedNodes = new List<PTD>();
            nodeStack.Push(ptd);

            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();
                BitSet reducedBag = currentNode.Bag;

                // reconstruct bag
                BitSet reconstructedBag = new BitSet(vertexCount);
                foreach (int i in reducedBag.Elements())
                {
                    reconstructedBag[reconstructionMapping[i]] = true;
                }
                currentNode.SetBag(reconstructedBag);
                
                // push children onto stack
                for (int i = 0; i < currentNode.children.Count; i++)
                {
                    nodeStack.Push(currentNode.children[i]);
                }

                // reconstruct remaining child nodes of the current node and add them to a second list (we need to handle those differently because their bags don't need to be reconstructed)
                for (int i = 0; i < reconstructionBagsToAppendTo.Count; i++)
                {
                    if (reconstructedBag.IsSuperset(reconstructionBagsToAppendTo[i]))
                    {
                        PTD child = new PTD(reconstructionBagsToAppend[i]);
                        currentNode.children.Add(child);
                        reconstructedNodes.Add(child);

                        reconstructionBagsToAppendTo.RemoveAt(i);
                        reconstructionBagsToAppend.RemoveAt(i);
                        i--;
                    }
                }
            }

            // reconstruct children of reconstructed children
            for (int i = 0; i < reconstructedNodes.Count; i++)
            {
                PTD currentNode = reconstructedNodes[i];
                for (int j = 0; j < reconstructionBagsToAppendTo.Count; j++)
                {
                    if (currentNode.Bag.IsSuperset(reconstructionBagsToAppendTo[j]))
                    {
                        PTD child = new PTD(reconstructionBagsToAppend[j]);
                        currentNode.children.Add(child);
                        reconstructedNodes.Add(child);

                        reconstructionBagsToAppendTo.RemoveAt(j);
                        reconstructionBagsToAppend.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

    }
}
