using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    class BlockSieve
    {
        readonly int margin;
        readonly int vertexCount;
        private readonly InnerNode startNode;

        public static int maxChildrenPerNode = 32;
        private readonly List<InnerNode> nodesWithTooManyChildren;

        public BlockSieve(int margin, int vertexCount)
        {
            this.margin = margin;
            nodesWithTooManyChildren = new List<InnerNode>();
            startNode = new InnerNode(0, vertexCount, null, nodesWithTooManyChildren);
            this.vertexCount = vertexCount;
            // TODO: can it happen that the start node is split?
        }

        /// <summary>
        /// adds a ptdur to this sieve
        /// </summary>
        /// <param name="ptdur">the ptdur to add</param>
        /// <returns>the leaf that this ptdur is stored in</returns>
        public Leaf Add(PTD ptdur)
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
                        InnerNode newNode = new InnerNode(currentInnerNode.intervalTo, vertexCount, currentInnerNode, nodesWithTooManyChildren);
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


            // a ptdur with the same vertices (except PUI) exists already
            Leaf currentNodeAsLeaf = currentNode as Leaf;
            BitSet copy = new BitSet(currentNodeAsLeaf.ptdur.vertices);
            copy.ExceptWith(currentNodeAsLeaf.ptdur.possiblyUsableIgnoreComponentsUnion);
            if (verticesWithoutPUI.Equals(copy))
            {
                if (ptdur.vertices.IsSupersetOf(currentNodeAsLeaf.ptdur.vertices))
                {
                    currentNodeAsLeaf.ptdur = ptdur;
                    return currentNodeAsLeaf;
                }
                else
                {
                    if (currentNodeAsLeaf.ptdur.vertices.IsSupersetOf(ptdur.vertices))
                    {
                        return currentNodeAsLeaf;
                    }
                    else
                    {
                        // TODO: keep all ptdurs in the leaf using a list
                        throw new Exception();
                    }
                }
                return currentNodeAsLeaf;
            }
            else
            {
                // if this is executed there is something wrong
                throw new Exception();
            }
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
                                yield return leaf.ptdur;
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
        /// splits all nodes that have too many children
        /// </summary>
        public void SplitNodesWithTooManyChildren()
        {
            for (int i = 0; i < nodesWithTooManyChildren.Count; i++)
            {
                nodesWithTooManyChildren[i].Split();
            }
        }

        // TODO: make struct?
        public abstract class SieveNode
        {
            public bool isLeaf;
            public InnerNode parent;
        }

        public class InnerNode : SieveNode
        {
            public readonly int intervalFrom;   // inclusive
            public int intervalTo;              // exclusive
            public List<(BitSet intervalBitSet, SieveNode)> children;
            private readonly List<InnerNode> nodesWithTooManyChildren;

            public InnerNode(int intervalFrom, int intervalTo, InnerNode parent, List<InnerNode> nodesWithTooManyChildren)
            {
                isLeaf = false;
                this.intervalFrom = intervalFrom;
                this.intervalTo = intervalTo;
                this.parent = parent;
                children = new List<(BitSet, SieveNode)>();
            }

            /// <summary>
            /// adds a child node to this node
            /// </summary>
            /// <param name="intervalBitSet">the vertex set of the child on this interval</param>
            /// <param name="child">the child node</param>
            public void AddChild(BitSet intervalBitSet, SieveNode child)
            {
                children.Add((intervalBitSet, child));

                child.parent = this;

                // TODO: not entirely correct. One of the new nodes might have too man children after the split ->> fix: either dfs and split all nodes with too many children or use better split heuristic
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
                            if (currentChildNode.parent != currentNewChildNode)
                            {
                                ;
                            }
                            Debug.Assert(currentChildNode.parent == currentNewChildNode);
                            newChildExists = true;
                            break;
                        }
                    }
                    // if no such new child exists, create it
                    if (!newChildExists)
                    {
                        InnerNode newNode = new InnerNode(childrenIntervalFrom, childrenIntervalTo, this, nodesWithTooManyChildren);
                        newNode.AddChild(currentChildIntervalBitSet, currentChildNode);
                        //currentChildNode.parent = newNode;
                        Debug.Assert(currentChildNode.parent == newNode);
                        newChildrenList.Add((currentChildIntervalBitSet, newNode));
                    }
                }

                // replace the old children list by the new one
                children = newChildrenList;
            }

            public enum SplitHeuristic { intervalCenter };
            public static SplitHeuristic splitHeuristic = SplitHeuristic.intervalCenter;

            /// <summary>
            /// calculates a vertex at which this node should be split (split to the left of that vertex)
            /// </summary>
            /// <returns>a vertex at which to split this node</returns>
            private int GetSplitVertex()
            {
                Debug.Assert(intervalFrom < intervalTo + 1);
                switch (splitHeuristic)
                {
                    case SplitHeuristic.intervalCenter:
                        return (intervalFrom + intervalTo) / 2;
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
                BitSet verticesWithoutPUI = new BitSet(leaf.ptdur.vertices);
                verticesWithoutPUI.ExceptWith(leaf.ptdur.possiblyUsableIgnoreComponentsUnion);

                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].intervalBitSet.EqualsOnInterval(verticesWithoutPUI, intervalFrom, intervalTo))
                    {
                        children.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public class Leaf : SieveNode
        {
            public PTD ptdur;

            public Leaf(PTD ptdur, InnerNode parent)
            {
                isLeaf = true;
                this.ptdur = ptdur;
                this.parent = parent;
            }

            /// <summary>
            /// removes this leaf from the sieve
            /// </summary>
            public void Delete()
            {
                parent.RemoveLeaf(this);
            }
        }
    }
}
