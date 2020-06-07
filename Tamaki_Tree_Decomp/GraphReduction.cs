using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    /// <summary>
    /// A class for simplifying a graph according to the rules in
    ///     H. L. Bodlaender, A. M. C. A. Koster, F. van den Eijkhof, and L. C. van der Gaag. Preprocessing for triangulation of probabilistic networks.
    ///     In J. Breese and D. Koller, editors, Proceedings of the 17th Conference on Uncertainty in Artificial Intelligence, pages 32–39,
    ///     San Francisco, 2001. Morgan Kaufmann.
    /// </summary>
    public class GraphReduction
    {
        readonly int vertexCount;
        readonly List<int>[] adjacencyList;
        readonly BitSet[] neighborSetsWithout;
        readonly BitSet removedVertices;
        int low; // TODO: heuristic for setting low is given in the paper
        readonly Dictionary<int, int> reconstructionMapping;    // mapping from reduced vertex id to original vertex id
        readonly List<(BitSet, BitSet)> reconstructionBagsToAppend;     // a list of (bag, parentbag) for reconstructing the correct tree decomposition // TODO: HashSet instead?

        readonly List<String> reconstructionBagsDebug;  // TODO: remove

        public static bool reduce = true;

        public GraphReduction(Graph graph, int low = 0)
        {
            this.low = low;
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
            reconstructionBagsToAppend = new List<(BitSet, BitSet)>();

            reconstructionBagsDebug = new List<string>();
        }

        /// <summary>
        /// reconstructs a graph after reduction
        /// </summary>
        /// <returns>the reduced graph</returns>
        public Graph ToGraph()
        {
            // build a mapping from old graph vertices to new graph vertices and vice versa
            Dictionary<int, int> reductionMapping = new Dictionary<int, int>();
            int counter = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                if (!removedVertices[i])
                {
                    reductionMapping.Add(i, counter);
                    reconstructionMapping.Add(counter, i);
                    counter++;
                }
            }

            int edgeCount = 0;
            // use that mapping to construct an adjacency list for this graph
            List<int>[] reducedAdjacencyList = new List<int>[reductionMapping.Count];
            for (int i = 0; i < vertexCount; i++)
            {
                if (!removedVertices[i])
                {
                    List<int> currentVertexAdjacencies = new List<int>(adjacencyList[i].Count);
                    for (int j = 0; j < adjacencyList[i].Count; j++)
                    {
                        int neighbor = adjacencyList[i][j];
                        currentVertexAdjacencies.Add(reductionMapping[neighbor]);
                    }
                    reducedAdjacencyList[reductionMapping[i]] = currentVertexAdjacencies;
                    edgeCount += currentVertexAdjacencies.Count;
                }
            }

            // TODO: can save a small bit of memory by deleting the old adjacency list and neighbor bit sets 

            edgeCount /= 2;
            Console.WriteLine("Graph reduced to {0} nodes and {1} edges", reducedAdjacencyList.Length, edgeCount);
            return new Graph(reducedAdjacencyList);
        }

        public bool Reduce(ref int out_low)
        {
            if (!reduce)
            {
                return false;
            }

            bool reduced = false;
            while (SimplicialVertexRule() || AlmostSimplicialVertexRule() || BuddyRule() || CubeRule())
            {
                reduced = true;
            }

            out_low = low;
            return reduced;
        }

        /// <summary>
        /// tries to apply the simplicial vertex rule to the graph once
        /// TODO: perhaps pass an additional vertex parameter where to start and "wrap" the first loop around. Might be more efficient
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
                    if (i == 145)   // TODO: remove
                    {
                        ;
                    }

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
                        reconstructionBagsToAppend.Add((bag, new BitSet(neighborSetsWithout[i])));
                        reconstructionBagsDebug.Add("simplicial");
                    }
                }
            }
            return isReduced;
        }

        /// <summary>
        /// tries to apply the almost simplicial vertex rule to the graph once
        /// TODO: perhaps pass an additional vertex parameter where to start and "wrap" the first loop around. Might be more efficient
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
                        reconstructionBagsToAppend.Add((bag, new BitSet(neighborSetsWithout[i])));
                        reconstructionBagsDebug.Add("almost simplicial");
                    }
                }
            }

            return isReduced;
        }

        /// <summary>
        /// tries to apply the buddy rule to the graph once
        /// TODO: perhaps pass an additional vertex parameter where to start and "wrap" the first loop around. Might be more efficient
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
                                reconstructionBagsToAppend.Add((bag1, new BitSet(neighborSetsWithout[i])));
                                BitSet bag2 = new BitSet(neighborSetsWithout[buddy]);
                                bag2[buddy] = true;
                                reconstructionBagsToAppend.Add((bag2, new BitSet(neighborSetsWithout[buddy])));
                                reconstructionBagsDebug.Add("buddy 1");
                                reconstructionBagsDebug.Add("buddy 2");
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

        /// <summary>
        /// tries to apply the buddy rule to the graph once
        /// TODO: perhaps pass an additional vertex parameter where to start and "wrap" the first loop around. Might be more efficient
        /// </summary>
        /// <returns>true iff a reduction could be performed</returns>
        public bool CubeRule()
        {
            bool isReduced = false;

            // for each vertex i ...
            for (int i = 0; i < vertexCount && !isReduced; i++)
            {
                if (!removedVertices[i] && adjacencyList[i].Count == 3)
                {
                    List<int> neighbors = adjacencyList[i];
                    if (adjacencyList[neighbors[0]].Count == 3 && adjacencyList[neighbors[1]].Count == 3 && adjacencyList[neighbors[2]].Count == 3)
                    {
                        /*
                         *  idea: the conditions for the cube rule are fulfilled, iff the neighbors of the three neighbors are neighbors of
                         *  exactly two of those three neighbors (except i which is a three times neighbor).
                         *  So we use a dictionary to count for each u,v,w how many times it is the neighbor of a,b or c.
                         *  If always two times, then the cube rule can be applied
                         */
                        Dictionary<int, int> neighborsNeighborsCount = new Dictionary<int, int>(3);
                        for (int j = 0; j < 3; j++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                int neighborsNeighbor = adjacencyList[neighbors[j]][k];
                                if (neighborsNeighbor != i)
                                {
                                    if (neighborsNeighborsCount.ContainsKey(neighborsNeighbor))
                                    {
                                        neighborsNeighborsCount[neighborsNeighbor]++;
                                    }
                                    else
                                    {
                                        neighborsNeighborsCount.Add(neighborsNeighbor, 1);
                                    }
                                }
                            }
                        }
                        bool isCube = true;
                        List<int> uvw = new List<int>();
                        foreach (KeyValuePair<int, int> keyvalue in neighborsNeighborsCount)
                        {
                            if (keyvalue.Value != 2)
                            {
                                isCube = false;
                                break;
                            }
                            else
                            {
                                uvw.Add(keyvalue.Key);
                            }
                        }
                        if (isCube)
                        {
                            Debug.Assert(uvw.Count == 3);
                            isReduced = true;

                            // update low
                            if (low < 3)
                            {
                                low = 3;
                            }

                            // alter graph
                            removedVertices[i] = true;
                            removedVertices[neighbors[0]] = true;
                            removedVertices[neighbors[1]] = true;
                            removedVertices[neighbors[2]] = true;
                            foreach (int node in uvw)
                            {
                                adjacencyList[node].Remove(neighbors[0]);
                                adjacencyList[node].Remove(neighbors[1]);
                                adjacencyList[node].Remove(neighbors[2]);
                                neighborSetsWithout[node][neighbors[0]] = false;
                                neighborSetsWithout[node][neighbors[1]] = false;
                                neighborSetsWithout[node][neighbors[2]] = false;
                                for (int j = 0; j < 3; j++)
                                {
                                    if (node != uvw[j] && !adjacencyList[node].Contains(uvw[j]))
                                    {
                                        adjacencyList[node].Add(uvw[j]);
                                        neighborSetsWithout[node][j] = true;
                                    }
                                }
                            }

                            // remember bags for reconstruction
                            BitSet appendTo = new BitSet(vertexCount);
                            appendTo[uvw[0]] = true;
                            appendTo[uvw[1]] = true;
                            appendTo[uvw[2]] = true;
                            BitSet append = new BitSet(appendTo);
                            append[i] = true;
                            reconstructionBagsToAppend.Add((append, appendTo));
                            reconstructionBagsDebug.Add("cube i");

                            for (int j = 0; j < 3; j++)
                            {
                                appendTo = new BitSet(neighborSetsWithout[neighbors[j]]);
                                appendTo[neighbors[j]] = true;
                                reconstructionBagsToAppend.Add((appendTo, append));
                                reconstructionBagsDebug.Add("cube neighbor " + j);
                            }
                        }
                    }
                }
            }

            return isReduced;
        }

        /*
        /// <summary>
        /// turns a tree decomposition of the reduced graph into a tree decomposition of the original input graph
        /// </summary>
        /// <param name="td">the tree decomposition of the reduced graph</param>
        public void RebuildTreeDecomposition(ref PTD td)
        {
            // return if there is no bag to append
            if (reconstructionBagsToAppend.Count == 0)
            {
                return;
            }

            Stack<PTD> nodeStack = new Stack<PTD>();
            List<PTD> reconstructedNodes = new List<PTD>();
            if (td == null)
            {
                int lastIndex = reconstructionBagsToAppend.Count - 1;
                td = new PTD(reconstructionBagsToAppend[lastIndex]);
                reconstructedNodes.Add(td);
                reconstructionBagsToAppendTo.RemoveAt(lastIndex);
                reconstructionBagsToAppend.RemoveAt(lastIndex);
            }
            else
            {
                nodeStack.Push(td);
            }

            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();
                BitSet reducedBag = currentNode.Bag;

                // reindex bag
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

            Debug.Assert(reconstructionBagsToAppend.Count == 0);

            CheckVertexCover(td);
        }
        */

        /// <summary>
        /// turns a tree decomposition of the reduced graph into a tree decomposition of the original input graph
        /// </summary>
        /// <param name="td">the tree decomposition of the reduced graph</param>
        public void RebuildTreeDecomposition(ref PTD td)
        {
            // return if there is no bag to append
            if (reconstructionBagsToAppend.Count == 0)
            {
                return;
            }

            Stack<PTD> nodeStack = new Stack<PTD>();
            List<PTD> reconstructedNodes = new List<PTD>();
            if (td == null)
            {
                int lastIndex = reconstructionBagsToAppend.Count - 1;
                td = new PTD(reconstructionBagsToAppend[lastIndex].Item1);
                reconstructedNodes.Add(td);
                reconstructionBagsToAppend.RemoveAt(lastIndex);
            }
            else
            {
                nodeStack.Push(td);
            }

            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();
                BitSet reducedBag = currentNode.Bag;

                // reindex bag
                BitSet reconstructedBag = new BitSet(vertexCount);
                foreach (int i in reducedBag.Elements())
                {
                    reconstructedBag[reconstructionMapping[i]] = true;
                }
                currentNode.SetBag(reconstructedBag);

                reconstructedNodes.Add(currentNode);

                // push children onto stack
                for (int i = 0; i < currentNode.children.Count; i++)
                {
                    nodeStack.Push(currentNode.children[i]);
                }
            }

            while (reconstructionBagsToAppend.Count > 0)
            {
                bool hasChanged = false;
                for (int i = 0; i < reconstructionBagsToAppend.Count; i++)
                {
                    (BitSet child, BitSet parent) = reconstructionBagsToAppend[i];
                    foreach(PTD node in reconstructedNodes)
                    {
                        if (node.Bag.IsSuperset(parent))
                        {
                            PTD childNode = new PTD(child);
                            node.children.Add(childNode);
                            reconstructedNodes.Add(childNode);
                            reconstructionBagsToAppend.RemoveAt(i);
                            reconstructionBagsDebug.RemoveAt(i);
                            i--;
                            hasChanged = true;
                            break;
                        }
                    }
                }
                if (!hasChanged)
                {
                    ;
                    throw new Exception("tree decomposition could not be rebuilt");
                }
            }

            CheckVertexCover(td);
        }

        [Conditional("DEBUG")]
        private void CheckVertexCover(PTD td)
        {
            BitSet covered = new BitSet(vertexCount);

            Stack<PTD> nodeStack = new Stack<PTD>();
            nodeStack.Push(td);

            while(nodeStack.Count > 0)
            {
                PTD current = nodeStack.Pop();
                covered.UnionWith(current.Bag);
                foreach(PTD child in current.children)
                {
                    nodeStack.Push(child);
                }
            }

            if (!covered.Equals(BitSet.All(vertexCount)))
            {
                ;
            }

            Debug.Assert(covered.Equals(BitSet.All(vertexCount)));
        }
        

    }
}
