using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tamaki_Tree_Decomp.Data_Structures;
using static Tamaki_Tree_Decomp.Data_Structures.Graph;

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
        int low; // TODO: heuristic for setting low is given in the paper
        Graph graph;
        ReindexationMapping reconstructionIndexationMapping;
        readonly List<(BitSet, BitSet)> reconstructionBagsToAppend;     // a list of (bag, parentbag) for reconstructing the correct tree decomposition // TODO: HashSet instead?

        readonly List<String> reconstructionBagsDebug;  // TODO: remove
        readonly int originalVertexCount;   // for debugging only

        private readonly bool verbose;

        public static bool reduce = true;   // this can be set to false in order to easily disable
                                            // the graph reduction for debugging reasons

        public GraphReduction(Graph graph, int low = 0, bool verbose = true)
        {
            this.low = low;
            this.graph = graph;
            reconstructionBagsToAppend = new List<(BitSet, BitSet)>();

            reconstructionBagsDebug = new List<string>();
            originalVertexCount = graph.vertexCount;

            this.verbose = verbose;
        }

        /// <summary>
        /// performs as many reductions as possible
        /// </summary>
        /// <param name="out_low">a lower bound for the tree width of the graph. It can be increased on basis of the reduction rules.</param>
        /// <returns>true, iff at least one reduction could be performed</returns>
        public bool Reduce(ref int out_low)
        {
            if (!reduce)
            {
                graph = null;   // remove reference to the graph so that its ressources can be freed
                return false;
            }

            bool reduced = false;
            while (SimplicialVertexRule() || AlmostSimplicialVertexRule() || BuddyRule() || CubeRule())
            {
                reduced = true;
            }

            graph.Reduce(out reconstructionIndexationMapping);

            graph = null;   // remove reference to the graph so that its ressources can be freed
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
            for (int i = 0; i < graph.vertexCount && !isReduced; i++)
            {
                // ... that is not yet removed ...
                if (graph.notRemovedVertices[i])
                {
                    // ... and check if its neighbors form a clique
                    bool neighborsFormClique = true;
                    for (int j = 0; j < graph.adjacencyList[i].Count && neighborsFormClique; j++)
                    {
                        int u = graph.adjacencyList[i][j];
                        for (int k = j + 1; k < graph.adjacencyList[i].Count; k++)
                        {
                            int v = graph.adjacencyList[i][k];
                            if (!graph.neighborSetsWithout[u][v])
                            {
                                neighborsFormClique = false;
                                break;
                            }
                        }
                    }
                    if (neighborsFormClique)
                    {
                        isReduced = true;

                        graph.Remove(i);
                        /*
                        isReduced = true;
                        removedVertices[i] = true;
                        for (int j = 0; j < adjacencyList[i].Count; j++)
                        {
                            int neighbor = adjacencyList[i][j];
                            adjacencyList[neighbor].Remove(i);
                            neighborSetsWithout[neighbor][i] = false;
                        }
                        */
                        if (graph.adjacencyList[i].Count > low)
                        {
                            low = graph.adjacencyList[i].Count;
                        }

                        // remember bag for reconstruction
                        BitSet bag = new BitSet(graph.neighborSetsWithout[i]);
                        bag[i] = true;
                        reconstructionBagsToAppend.Add((bag, new BitSet(graph.neighborSetsWithout[i])));
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
            for (int i = 0; i < graph.vertexCount && !isReduced; i++)
            {
                // ... that is not yet removed and whose degree is smaller than low ...
                if (graph.notRemovedVertices[i] && low >= graph.adjacencyList[i].Count)
                {
                    // ... and check if all of its neighbors except one form a clique
                    bool neighborsFormAlmostClique = true;
                    int edgeUNotIn = -1;
                    int edgeVNotIn = -1;
                    int vertexNotIn = -1;
                    for (int j = 0; j < graph.adjacencyList[i].Count && neighborsFormAlmostClique; j++)
                    {
                        int u = graph.adjacencyList[i][j];
                        for (int k = j + 1; k < graph.adjacencyList[i].Count; k++)
                        {
                            int v = graph.adjacencyList[i][k];
                            if (!graph.neighborSetsWithout[u][v])
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
                    // remove i and make all of its neighbors a clique
                    if (neighborsFormAlmostClique)
                    {
                        isReduced = true;

                        graph.Remove(i);
                        graph.MakeIntoClique(graph.adjacencyList[i]);
                        
                        // remember bag for reconstruction
                        BitSet bag = new BitSet(graph.neighborSetsWithout[i]);
                        bag[i] = true;
                        reconstructionBagsToAppend.Add((bag, new BitSet(graph.neighborSetsWithout[i])));
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
            for (int i = 0; i < graph.vertexCount && !isReduced; i++)
            {
                // ... that is not yet removed and has three neighbors ...
                if (graph.notRemovedVertices[i] && graph.adjacencyList[i].Count == 3)
                {
                    List<int> neighbors = graph.adjacencyList[i];
                    // ... test if a neighbor's ...
                    for (int j = 0; j < 3; j++)
                    {
                        int neighbor = neighbors[j];
                        // ... neighbor ...
                        for (int k = 0; k < graph.adjacencyList[neighbor].Count; k++)
                        {
                            // ... is a buddy
                            int buddy = graph.adjacencyList[neighbor][k];
                            if (buddy != i && graph.neighborSetsWithout[i].Equals(graph.neighborSetsWithout[buddy]))
                            {
                                isReduced = true;

                                graph.Remove(i);
                                graph.Remove(buddy);

                                graph.MakeIntoClique(neighbors);                                

                                // remember bags for reconstruction
                                BitSet bag1 = new BitSet(graph.neighborSetsWith[i]);                                // TODO: all of them don't need to be copied (?)
                                reconstructionBagsToAppend.Add((bag1, new BitSet(graph.neighborSetsWithout[i])));   
                                BitSet bag2 = new BitSet(graph.neighborSetsWith[buddy]);
                                reconstructionBagsToAppend.Add((bag2, new BitSet(graph.neighborSetsWithout[buddy])));
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
            for (int i = 0; i < graph.vertexCount && !isReduced; i++)
            {
                if (graph.notRemovedVertices[i] && graph.adjacencyList[i].Count == 3)
                {
                    List<int> neighbors = graph.adjacencyList[i];
                    if (graph.adjacencyList[neighbors[0]].Count == 3 && graph.adjacencyList[neighbors[1]].Count == 3 && graph.adjacencyList[neighbors[2]].Count == 3)
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
                                int neighborsNeighbor = graph.adjacencyList[neighbors[j]][k];
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

                            graph.MakeIntoClique(uvw);

                            // remember bags for reconstruction
                            BitSet appendTo = new BitSet(graph.vertexCount);
                            appendTo[uvw[0]] = true;
                            appendTo[uvw[1]] = true;
                            appendTo[uvw[2]] = true;
                            BitSet append_center = new BitSet(appendTo);
                            append_center[i] = true;
                            reconstructionBagsToAppend.Add((append_center, appendTo));
                            reconstructionBagsDebug.Add("cube i");

                            for (int j = 0; j < 3; j++)
                            {
                                BitSet append_corner = new BitSet(graph.neighborSetsWith[neighbors[j]]);
                                reconstructionBagsToAppend.Add((append_corner, append_center));
                                reconstructionBagsDebug.Add("cube neighbor " + j);
                            }

                            graph.Remove(i);
                            graph.Remove(neighbors[0]);
                            graph.Remove(neighbors[1]);
                            graph.Remove(neighbors[2]);
                        }
                    }
                }
            }

            return isReduced;
        }
        
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

            td.Reindex(reconstructionIndexationMapping);

            if (td == null || td.Bag.IsEmpty()) // TODO: correct?
            {
                int lastIndex = reconstructionBagsToAppend.Count - 1;
                td = new PTD(reconstructionBagsToAppend[lastIndex].Item1);
                reconstructionBagsToAppend.RemoveAt(lastIndex);
            }

            // initialize a stack of nodes
            Stack<PTD> nodeStack = new Stack<PTD>();
            nodeStack.Push(td);
            List<PTD> reconstructedNodes = new List<PTD>();

            // put all bags in the tree into a list
            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();                
                reconstructedNodes.Add(currentNode);

                // push children onto stack
                for (int i = 0; i < currentNode.children.Count; i++)
                {
                    nodeStack.Push(currentNode.children[i]);
                }
            }


            BitSet debug1 = originalVertexCount == 363? new BitSet(originalVertexCount, new int[] { 51, 105, 166, 168 }) : null;

            // add the bags with the vertices that have been removed during reduction
            while (reconstructionBagsToAppend.Count > 0)
            {
                bool hasChanged = false;
                for (int i = 0; i < reconstructionBagsToAppend.Count; i++)
                {
                    (BitSet child, BitSet parent) = reconstructionBagsToAppend[i];
                    foreach(PTD node in reconstructedNodes)     // TODO: bad loop for searching parent bags,
                                                                //       could use a better data structure
                    {
                        if (node.Bag.IsSupersetOf(parent))
                        {
                            if (originalVertexCount == 363 && (child.IsSupersetOf(debug1) || node.Bag.IsSupersetOf(debug1)))
                            {
                                ;
                            }

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

                // debug stuff. TODO: remove eventually
                if (!hasChanged)
                {
                    ;
                    throw new Exception("tree decomposition could not be rebuilt");
                }
            }

            // --- make tree decomposition canonical again ---

            Dictionary<PTD, PTD> parents = new Dictionary<PTD, PTD> { [td] = null };

            // remove the nodes where the child is a subset of a node (this occurs
            //because the simplicial vertex rule is also applied to vertices in cliques)
            nodeStack.Push(td);
            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();

                // consider all children
                for (int i = 0; i < currentNode.children.Count; i++)
                {
                    PTD child = currentNode.children[i];
                    // if child is a subset, remove the child and adopt grandchildren
                    if (currentNode.Bag.IsSupersetOf(child.Bag))
                    {
                        currentNode.children.RemoveAt(i);
                        i--;
                        currentNode.children.AddRange(child.children);
                    }
                    // else push the child onto the stack
                    else
                    {
                        nodeStack.Push(child);
                        parents[child] = currentNode;
                    }
                }
            }

            // remove the nodes where a node is the subset of its child (this occurs
            // because the simplicial vertex rule is also applied to vertices in cliques)

            // get nodes in post order
            Stack<PTD> postOrderHelperStack = new Stack<PTD>();
            postOrderHelperStack.Push(td);
            while (postOrderHelperStack.Count > 0)
            {
                PTD currentNode = postOrderHelperStack.Pop();
                for (int i = 0; i < currentNode.children.Count; i++)
                {
                    postOrderHelperStack.Push(currentNode.children[i]);
                }
                nodeStack.Push(currentNode);
            }

            // remove the nodes where a node is the subset of its child
            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();
                for (int i = 0; i < currentNode.children.Count; i++)
                {

                    PTD child = currentNode.children[i];
                    if (child.Bag.IsSupersetOf(currentNode.Bag))
                    {
                        PTD parent = parents[currentNode];
                        if (parent != null)     // if current node is not the root, attach child to its grandparent
                        {
                            // replace the parent in the grandparent's children list
                            parents[child] = parent;
                            int index = parent.children.IndexOf(currentNode);
                            parent.children[index] = child;

                            // add former parent's children to this node
                            currentNode.children.RemoveAt(i);
                            child.children.AddRange(currentNode.children);
                        }
                        else                    // if current node is the root, make the child the root instead
                        {
                            parents[child] = null;
                            td.children.RemoveAt(i);
                            child.children.AddRange(td.children);
                            td = child;
                        }
                        break;
                    }
                }
            }

            CheckVertexCover(td);
        }

        [Conditional("DEBUG")]
        private void CheckVertexCover(PTD td)
        {
            BitSet covered = new BitSet(originalVertexCount);

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

            Debug.Assert(covered.Equals(BitSet.All(originalVertexCount)));
        }
    }
}
