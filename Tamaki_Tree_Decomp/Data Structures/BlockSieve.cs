using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// A class for representing a block sieve. It stores ptdurs with a given upper bound on vertices that can be added to their root bag
    /// and can be queried with a ptd to return candidate ptdurs to combine it with. It is implemented as a modified trie.
    /// 
    /// Each node specifies an interval of vertices. Each edge to a child corresponds to a different set restricted to that interval.
    /// 
    /// Ptdurs are added to the sieve as follows: starting at startNode, test if the vertex set of the ptdur restricted to the interval
    /// specified by the current node corresponds to any of the edges. If so, descend to that node and continue. Otherwise, add a new node
    /// whose interval starts with the next vertex of the current interval and ends at the vertex count (if that interval is non-empty), and
    /// add a leaf containing the ptdur to add.
    /// 
    /// Queries for candidate ptdurs for a given ptd are done as follows: starting at startNode, descend all edges that do not intersect the
    /// inlet of the ptd on the interval specified by the current node. All ptdurs in leaves reached this way are candidates. Further pruning
    /// is achieved by adding up the amount of vertices in the outlet of the ptd (restricted to the union of intervals seen so far) that are
    /// not also present in the edges followed so far. These vertices correspond to those that would need to be added to any ptdur that is
    /// located further down the search tree. Thus, if their amount exceeds the margin at some point during the traversal, the corresponding
    /// subtree can also be excluded and the edge can be skipped. 
    /// </summary>
    class BlockSieve
    {
        readonly int margin;    // the amount of vertices that can maximally be added to the bags of the ptdurs stored in this sieve 
        readonly int vertexCount;
        private readonly InnerNode startNode;

        public static int maxChildrenPerNode = 32;  // if an InnerNode has more children than this number, it will be split

        /// <summary>
        /// constructs a block sieve with a specific margin.
        /// </summary>
        /// <param name="margin">the margin</param>
        /// <param name="vertexCount">the vertex count of the currently processed graph</param>
        public BlockSieve(int margin, int vertexCount)
        {
            this.margin = margin;
            startNode = new InnerNode(0, vertexCount, null);
            this.vertexCount = vertexCount;
        }

        /// <summary>
        /// adds a ptdur to this sieve
        /// </summary>
        /// <param name="ptdur">the ptdur to add</param>
        /// <param name="forceAddition">forces the addition of the ptdur without testing if a ptdur already exists that is 'better' in terms of optional components</param>
        /// <returns>the leaf that this ptdur is stored in</returns>
        public Leaf Add(PTD ptdur, bool forceAddition=false)
        {
            SieveNode currentNode = startNode;
            BitSet verticesWithoutPUI = new BitSet(ptdur.vertices);
            verticesWithoutPUI.ExceptWith(ptdur.possiblyUsableIgnoreComponentsUnion);

            // traverse the tree
            do
            {
                InnerNode currentInnerNode = currentNode as InnerNode;

                // if it exists, then find the child with the matching interval BitSet and mark it as the current node 
                bool currentNodeHasMatchingChild = false;
                for (int i = 0; i < currentInnerNode.children.Count; i++)
                {
                    (BitSet childSet, SieveNode sieveNode) = currentInnerNode.children[i];
                    if (verticesWithoutPUI.EqualsOnInterval(childSet, currentInnerNode.intervalFrom, currentInnerNode.intervalTo))
                    {
                        currentNode = sieveNode;
                        currentNodeHasMatchingChild = true;
                        break;
                    }
                }
                // if such a child does not exist, make one
                if (!currentNodeHasMatchingChild)
                {
                    // add new inner node that covers the rest of the interval if the current node doesn't cover it fully
                    if (currentInnerNode.intervalTo < vertexCount)
                    {
                        InnerNode newNode = new InnerNode(currentInnerNode.intervalTo, vertexCount, currentInnerNode);
                        BitSet intervalBitSet = verticesWithoutPUI;
                        currentInnerNode.AddChild(intervalBitSet, newNode);
                        currentInnerNode = newNode;
                    }

                    Debug.Assert(currentInnerNode.intervalTo == vertexCount);

                    // add leaf
                    Leaf newLeaf = new Leaf(ptdur, currentInnerNode);
                    currentInnerNode.AddChild(verticesWithoutPUI, newLeaf);
                    currentNode = newLeaf;

                    return newLeaf;

                }
            }
            while (!currentNode.isLeaf);


            // test if a ptdur exists in this leaf that is 'better' than the one to add or vice versa
            Leaf currentNodeAsLeaf = currentNode as Leaf;
            if (!forceAddition)
            {
                for (int i = 0; i < currentNodeAsLeaf.ptdurs.Count; i++)
                {
                    PTD currentPtdur = currentNodeAsLeaf.ptdurs[i];
                    // if the ptdur to add is better, replace the old one
                    if (ptdur.vertices.IsSupersetOf(currentPtdur.vertices))
                    {
                        currentNodeAsLeaf.ptdurs[i] = ptdur;
                        return currentNodeAsLeaf;
                    }
                    // if the current one is better, reject the old one
                    else if (currentPtdur.vertices.IsSupersetOf(ptdur.vertices))
                    {
                        return currentNodeAsLeaf;
                    }

                }
            }

            // if not, add the ptdur to the leaf
            currentNodeAsLeaf.ptdurs.Add(ptdur);
            return currentNodeAsLeaf;
        }

        /// <summary>
        /// queries this sieve to find all candidate ptdurs that a ptd might be able to be attached to
        /// </summary>
        /// <param name="ptd">the ptd to find candidate parents for</param>
        /// <returns>an enumerable of candidate ptdurs that the ptd might be able to be attached to</returns>
        public IEnumerable<PTD> GetCandidatePTDURs(PTD ptd)
        {
            BitSet inletWithoutPUI = new BitSet(ptd.inlet);
            inletWithoutPUI.ExceptWith(ptd.possiblyUsableIgnoreComponentsUnion);

            Stack<(InnerNode, int)> nodeStack = new Stack<(InnerNode, int)>();
            nodeStack.Push((startNode, 0));

            // perform depth-first search
            while(nodeStack.Count > 0)
            {
                // get the current node and the current i-value for that node
                (InnerNode currentNode, int currentI) = nodeStack.Pop();

                // process all children of current node
                for (int j = 0; j < currentNode.children.Count; j++)
                {
                    (BitSet intervalBitSet, SieveNode child) = currentNode.children[j];
                    // test if child is a candidate vertex-wise
                    if (inletWithoutPUI.IsDisjointOnInterval(intervalBitSet, currentNode.intervalFrom, currentNode.intervalTo))
                    {
                        // test if margin is large enough. If not, prune this child by doing nothing
                        int nextI = currentI + (int)ptd.outlet.CountOnIntervalExcept(intervalBitSet, currentNode.intervalFrom, currentNode.intervalTo);
                        if (nextI <= margin)
                        {
                            // return ptdur if the child is a leaf
                            if (child.isLeaf)
                            {
                                Leaf leaf = child as Leaf;
                                Debug.Assert(child != null);
                                for (int k = 0; k < leaf.ptdurs.Count; k++)
                                {
                                    yield return leaf.ptdurs[k];
                                }
                            }
                            // put the child on the stack if it is not a leaf
                            else
                            {
                                InnerNode nextNode = child as InnerNode;
                                Debug.Assert(nextNode != null);
                                nodeStack.Push((nextNode, nextI));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// an abstract class for representing a sieve node. Derived classes are InnerNode and Leaf.
        /// </summary>
        public abstract class SieveNode
        {
            public bool isLeaf;
            public InnerNode parent;
        }

        /// <summary>
        /// a class for representing an inner node
        /// </summary>
        public class InnerNode : SieveNode
        {
            public readonly int intervalFrom;   // inclusive
            public int intervalTo;              // exclusive
            public List<(BitSet intervalBitSet, SieveNode)> children;

            /// <summary>
            /// constructs an inner node that corresponds to a specific interval
            /// </summary>
            /// <param name="intervalFrom">the lower bound of the interval (inlcusive)</param>
            /// <param name="intervalTo">the upper bound of the interval (exclusive)</param>
            /// <param name="parent">the parent of this node within the sieve</param>
            public InnerNode(int intervalFrom, int intervalTo, InnerNode parent)
            {
                isLeaf = false;
                this.intervalFrom = intervalFrom;
                this.intervalTo = intervalTo;
                this.parent = parent;
                children = new List<(BitSet, SieveNode)>();
            }

            /// <summary>
            /// adds a child node to this node, and splits the node if it has more children than 'BlockSieve.maxChildrenPerNode'
            /// </summary>
            /// <param name="intervalBitSet">the vertex set of the child on this interval</param>
            /// <param name="child">the child node</param>
            public void AddChild(BitSet intervalBitSet, SieveNode child)
            {
                children.Add((intervalBitSet, child));

                child.parent = this;

                if (children.Count > maxChildrenPerNode)
                {
                    Split();
                }
            }

            /// <summary>
            /// splits this node into two layers which cover smaller intervals than the old node
            /// </summary>
            public void Split()
            {
                int splitVertex = GetSplitVertex();

                int childrenIntervalFrom = splitVertex;
                int childrenIntervalTo = intervalTo;
                intervalTo = splitVertex;

                // create new children list
                List<(BitSet, SieveNode)> newChildrenList = new List<(BitSet, SieveNode)>();
                // loop over all former children
                for (int i = 0; i < children.Count; i++)
                {
                    (BitSet currentChildIntervalBitSet, SieveNode currentChildNode) = children[i];
                    // find correct new child to attach the former child to
                    bool newChildExists = false;
                    for (int j = 0; j < newChildrenList.Count; j++)
                    {
                        (BitSet currentNewChildIntervalBitSet, SieveNode currentNewChildNode) = newChildrenList[j];
                        if (currentChildIntervalBitSet.EqualsOnInterval(currentNewChildIntervalBitSet, intervalFrom, intervalTo))
                        {
                            ((InnerNode)currentNewChildNode).AddChild(currentChildIntervalBitSet, currentChildNode);
                            currentChildNode.parent = (InnerNode)currentNewChildNode;
                            Debug.Assert(currentChildNode.parent == currentNewChildNode);
                            newChildExists = true;
                            break;
                        }
                    }
                    // if no such new child exists, create it
                    if (!newChildExists)
                    {
                        InnerNode newNode = new InnerNode(childrenIntervalFrom, childrenIntervalTo, this);
                        newNode.AddChild(currentChildIntervalBitSet, currentChildNode);
                        //currentChildNode.parent = newNode;
                        Debug.Assert(currentChildNode.parent == newNode);
                        newChildrenList.Add((currentChildIntervalBitSet, newNode));
                    }
                }

                // replace the old children list by the new one
                children = newChildrenList;
            }

            public enum SplitHeuristic { intervalCenter, powerOfTwo };
            public static SplitHeuristic splitHeuristic = SplitHeuristic.powerOfTwo;

            /// <summary>
            /// calculates a vertex at which this node should be split (split to the left of that vertex)
            /// </summary>
            /// <returns>a vertex at which to split this node</returns>
            private int GetSplitVertex()
            {
                Debug.Assert(intervalFrom < intervalTo + 1);
                switch (splitHeuristic)
                {
                    case SplitHeuristic.intervalCenter: // splits at the center of the interval
                        return (intervalFrom + intervalTo) / 2;
                    case SplitHeuristic.powerOfTwo:     // splits at (the highest power of two smaller than the interval size) added to the smaller bound of the interval
                        int intervalSize = intervalTo - intervalFrom;
                        int log = (int)Math.Log(intervalSize - 1);
                        int offset = 1 << log;
                        return intervalFrom + offset;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            /// removes a leaf from this node
            /// </summary>
            /// <param name="leaf">the leaf to remove</param>
            public void RemoveLeaf(Leaf leaf)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].Item2 == leaf)
                    {
                        Debug.Assert(children[i].Item2.isLeaf);
                        children.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// a class for representing a leaf
        /// </summary>
        public class Leaf : SieveNode
        {
            public List<PTD> ptdurs;

            /// <summary>
            /// constructs a leaf initialized with a given ptdur and a reference to the leaf's parent within the sieve
            /// </summary>
            /// <param name="ptdur">the initial ptdur at this leaf</param>
            /// <param name="parent">the leaf's parent</param>
            public Leaf(PTD ptdur, InnerNode parent)
            {
                isLeaf = true;
                ptdurs = new List<PTD>() { ptdur };
                this.parent = parent;
            }

            /// <summary>
            /// removes a ptdur from this leaf. If no ptdurs remain, this leaf is detached from its parent.
            /// </summary>
            /// <param name="ptdur">the ptdur to delete</param>
            public void Remove(PTD ptdur)
            {
                bool removed = ptdurs.Remove(ptdur);
                if (ptdurs.Count == 0)
                {
                    parent.RemoveLeaf(this);
                }
            }
        }
    }
}
