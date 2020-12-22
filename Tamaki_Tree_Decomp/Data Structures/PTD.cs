using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Tamaki_Tree_Decomp.Safe_Separators;
using static Tamaki_Tree_Decomp.Data_Structures.Graph;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// a class for representing partial tree decompositions and partial tree decompositions with unfinished root
    /// </summary>
    public class PTD
    {
        public BitSet Bag { get; private set; }
        public readonly BitSet vertices;
        private BitSet possiblyUsableIgnore;
        public BitSet inlet;
        public BitSet outlet;
        public readonly List<PTD> children;
        public List<int> skipChildrenInNormalizedCheck = null;

        //TODO: perhaps maintain the sets of components associated with the outlet

#region constructors

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="ptd">the ptd to copy</param>
        public PTD(PTD ptd)
        {
            Bag = new BitSet(ptd.Bag);
            vertices = new BitSet(ptd.vertices);
            possiblyUsableIgnore = new BitSet(ptd.possiblyUsableIgnore);
            inlet = new BitSet(ptd.inlet);
            outlet = new BitSet(ptd.outlet);
            children = new List<PTD>(ptd.children);
        }

        /// <summary>
        /// constructor that explicitly sets all internal values
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="vertices"></param>
        /// <param name="outlet"></param>
        /// <param name="inlet"></param>
        /// <param name="children"></param>
        public PTD(BitSet bag, BitSet vertices, BitSet possiblyUsableIgnore, BitSet outlet, BitSet inlet, List<PTD> children)
        {
            Bag = bag;
            this.vertices = vertices;
            this.possiblyUsableIgnore = possiblyUsableIgnore;
            this.outlet = outlet;
            this.inlet = inlet;
            this.children = children;
        }

        /// <summary>
        /// constructor that explicitly sets bag, vertices and children
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="vertices"></param>
        /// <param name="children"></param>
        public PTD(BitSet bag, BitSet vertices, List<PTD> children)
        {
            // TODO: construct inlet and outlet as empty sets
            Bag = bag;
            this.vertices = vertices;
            this.children = children;
        }

        /// <summary>
        /// constructor for one node of a PTD.
        /// used in line 4
        /// </summary>
        /// <param name="bag">the set of vertices that this node contains</param>
        /// <param name="outlet">the outlet of this node</param>
        public PTD(BitSet bag, BitSet outlet)
        {
            Bag = new BitSet(bag);              // TODO: can be shared
            vertices = new BitSet(bag);         // TODO: can be shared
            possiblyUsableIgnore = new BitSet(bag.Capacity());
            this.outlet = new BitSet(outlet);   // TODO: can be shared
            inlet = new BitSet(bag);
            inlet.ExceptWith(outlet);
            children = new List<PTD>();
        }

#endregion

        /// <summary>
        /// sets the bag of this node to a new set of vertices.
        /// Only used for the purpose of reconstruction from graph reduction rules.
        /// </summary>
        /// <param name="newBag">the new bag</param>
        public void SetBag(BitSet newBag)
        {
            Bag = newBag;
        }

#region combination rules from algorithm

        /// <summary>
        /// creates a new root with the given node as a child and the child's outlet as bag
        /// </summary>
        /// <param name="Tau">the child of this node</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PTD Line9(PTD Tau)
        {
            BitSet bag = new BitSet(Tau.outlet);
            BitSet outlet = new BitSet(Tau.outlet);
            BitSet inlet = new BitSet(Tau.inlet);
            BitSet vertices = new BitSet(Tau.vertices);
            BitSet possiblyUsableIgnore = new BitSet(Tau.possiblyUsableIgnore);

            List<PTD> children = new List<PTD>();
            children.Add(Tau);
            return new PTD(bag, vertices, possiblyUsableIgnore, outlet, inlet, children);
        }

        public static bool testIfAddingOneVertexToBagFormsPMC = false;

        /// <summary>
        /// line 13 (combining a ptd and ptdur to form a new ptdur), but exit early if bag size is too big or if the ptd is not possibly usable
        /// </summary>
        /// <param name="Tau_prime">the ptdur to be combined</param>
        /// <param name="Tau">the ptd</param>
        /// <param name="graph">the underlying graph</param>
        /// <param name="k">the treewidth currently being tested</param>
        /// <param name="result">the resulting ptd, or null, if the return value is false</param>
        /// <returns>true, iff the resulting ptd is possibly usable and its bag is small enough for treewidth k</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Line13_CheckBagSize_CheckPossiblyUsable_CheckCliquish(PTD Tau_prime, PTD Tau, ImmutableGraph graph, int k, out PTD result, Graph mutableGraph)
        {
            // return early if bag would get too big
            uint futureBagSize = BitSet.CountUnion(Tau_prime.Bag, Tau.outlet);
            if (futureBagSize > k + 1)
            {
                result = null;
                return false;
            }

            BitSet bag = new BitSet(Tau_prime.Bag);
            bag.UnionWith(Tau.outlet);            

            return Line13_CheckPossiblyUsable_CheckCliquish_Helper(Tau_prime, Tau, graph, out result, bag, futureBagSize, k, mutableGraph);
        }

        public static bool neighborsFirst = true;
        public static Stopwatch articulationPointCandidatesStopwatch = new Stopwatch();
        public static Stopwatch onlyNeighborStopwatch = new Stopwatch();

        /// <summary>
        /// a method extracted from the method above due to performance reasons. Read it as if the body of the method above would just continue here.
        /// </summary>
        /// <param name="Tau_prime">the ptdur</param>
        /// <param name="Tau">the ptd</param>
        /// <param name="graph">the underlying graph</param>
        /// <param name="result">the resulting ptd, or null if the return value is false</param>
        /// <param name="bag">the bag of the ptd to be</param>
        /// <returns>true, iff the resulting ptd is possibly usable</returns>
        private static bool Line13_CheckPossiblyUsable_CheckCliquish_Helper(PTD Tau_prime, PTD Tau, ImmutableGraph graph, out PTD result, BitSet bag, uint futureBagSize, int k, Graph mutableGraph)
        {
            List<PTD> children = new List<PTD>(Tau_prime.children);
            children.Add(Tau);

            // exit early if not possibly usable
            if (!IsPossiblyUsable(children, graph))
            {
                result = null;
                return false;
            }

            // if no vertices can be added to the bag due to size and the bag is not a pmc, we can reject this ptd immediately because it is not useful
            if (futureBagSize == k + 1 && !graph.IsPotMaxClique(bag))
            {
                result = null;
                return false;
            }
            

            if (!graph.IsCliquish(bag))
            {
                result = null;
                return false;
            }

            // if only one vertex can be added, determine all the candidates that would make this bag a pmc when added. If there are none, return.
            if (testIfAddingOneVertexToBagFormsPMC && futureBagSize == k)
            {
                // if bag is pmc already, we need this ptdur. (In that case no candidate exists which could be added.)
                if (!graph.IsPotMaxClique(bag))
                {
                    bool useless = true;

                    foreach ((BitSet component, BitSet neighbor) in graph.ComponentsAndNeighbors(bag))
                    {
                        // candidates are only found in full components
                        if (neighbor.Equals(bag) && !component.Intersects(Tau_prime.vertices) && !component.Intersects(Tau.vertices))
                        {
                            if (neighborsFirst)
                            {
                                onlyNeighborStopwatch.Start();
                                // test if a vertex in the bag has exactly one neighbor in this component, and the bag plus that vertex is still cliquish
                                foreach (int v in bag.Elements())
                                {
                                    BitSet neighbors = new BitSet(graph.openNeighborhood[v]);
                                    neighbors.IntersectWith(component);
                                    if (neighbors.Count() == 1)
                                    {
                                        int candidate = neighbors.First();
                                        bag[candidate] = true;
                                        if (graph.IsCliquish(bag))  // in this case it is guaranteed that the bag is also a pmc
                                        {
                                            Debug.Assert(graph.IsPotMaxClique(bag));
                                            bag[candidate] = false;
                                            useless = false;
                                            break;
                                        }
                                        bag[candidate] = false;
                                    }
                                }
                                onlyNeighborStopwatch.Stop();

                                if (!useless)
                                {
                                    break;
                                }
                            }

                            articulationPointCandidatesStopwatch.Start();
                            // find articulation points within this component
                            foreach (int articulationPoint in SafeSeparator.ArticulationPoints(mutableGraph, component))
                            {
                                bag[articulationPoint] = true;
                                if (graph.IsPotMaxClique(bag))
                                {
                                    bag[articulationPoint] = false;
                                    useless = false;
                                    break;
                                }
                                bag[articulationPoint] = false;
                            }
                            articulationPointCandidatesStopwatch.Stop();
                            if (!useless)
                            {
                                break;
                            }

                            if (!neighborsFirst)
                            {
                                onlyNeighborStopwatch.Start();
                                // test if a vertex in the bag has exactly one neighbor in this component, and the bag plus that vertex is still cliquish
                                foreach (int v in bag.Elements())
                                {
                                    BitSet neighbors = new BitSet(graph.openNeighborhood[v]);
                                    neighbors.IntersectWith(component);
                                    if (neighbors.Count() == 1)
                                    {
                                        int candidate = neighbors.First();
                                        bag[candidate] = true;
                                        if (graph.IsCliquish(bag))  // in this case it is guaranteed that the bag is also a pmc
                                        {
                                            Debug.Assert(graph.IsPotMaxClique(bag));
                                            bag[candidate] = false;
                                            useless = false;
                                            break;
                                        }
                                        bag[candidate] = false;
                                    }
                                }
                                onlyNeighborStopwatch.Stop();

                                if (!useless)
                                {
                                    break;
                                }
                            }


                            // TODO: possibly exclude candidates that are in this ptdur's inlet? Is that correct?
                        }
                    }

                    if (useless)
                    {           
                        result = null;
                        return false;
                    }
                }
            }

            // usability is established, so we build the ptd
            BitSet vertices = new BitSet(Tau_prime.vertices);
            vertices.UnionWith(Tau.vertices);
            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            BitSet possiblyUsableIgnore = new BitSet(Tau_prime.possiblyUsableIgnore);
            possiblyUsableIgnore.UnionWith(Tau.possiblyUsableIgnore);
            result = new PTD(new BitSet(bag), vertices, possiblyUsableIgnore, outlet, inlet, children);   // TODO: don't copy bag?

            return true;
        }


        /// <summary>
        /// the case where the bag of the ptd is a subset of the inclusive neighborhood of a vertex. Returns a new ptd with that type of bag. 
        /// </summary>
        /// <param name="Tau_wiggle">the ptdur whose root is to be extended</param>
        /// <param name="vNeighbors">the (inclusive) neighborhood of the vertex</param>
        /// <param name="graph">the underlying graph</param>
        /// <returns>a ptd where the bag is the bag of the ptdur unioned with the neighborhood</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PTD Line23(PTD Tau_wiggle, BitSet vNeighbors, ImmutableGraph graph)
        {
            BitSet bag = new BitSet(vNeighbors);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);
            BitSet vertices = new BitSet(Tau_wiggle.vertices);
            vertices.UnionWith(vNeighbors);

            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            BitSet possiblyUsableIgnore = new BitSet(Tau_wiggle.possiblyUsableIgnore);
            return new PTD(bag, vertices, possiblyUsableIgnore, outlet, inlet, children);
        }

        /// <summary>
        /// the case where the bag of the ptd covers those edges incident to a vertex that are not covered in its children. Returns a new ptd with that type of bag.
        /// </summary>
        /// <param name="Tau_wiggle">the underlying ptdur</param>
        /// <param name="newRoot">the root for the resulting ptd (must be a superset of the root of the ptdur)</param>
        /// <param name="graph">the underlying graph</param>
        /// <returns>a ptd where the bag covers those edges incident to a vertex that are not covered in its children</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PTD Line29(PTD Tau_wiggle, BitSet newRoot, ImmutableGraph graph)
        {
            Debug.Assert(newRoot.IsSupersetOf(Tau_wiggle.Bag));
            BitSet bag = newRoot;
            BitSet vertices = new BitSet(Tau_wiggle.vertices);
            vertices.UnionWith(newRoot);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);

            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            BitSet possiblyUsableIgnore = new BitSet(Tau_wiggle.possiblyUsableIgnore);
            return new PTD(bag, vertices, possiblyUsableIgnore, outlet, inlet, children);
        }

#endregion



        /// <summary>
        /// tests whether this and the other PTD are PTDs that contain the same vertices within them
        /// </summary>
        /// <param name="other">the other PTD</param>
        /// <returns>true iff the PTDs contain the same vertices</returns>
        public bool Equivalent(PTD other)
        {
            return inlet.Equals(other.inlet);
        }

        private static int currentGraphID = -1;
        private static bool changed = false;

        /// <summary>
        /// tests whether this PTD is possibly usable
        /// </summary>
        /// <returns>true iff the PTD is possibly usable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPossiblyUsable(List<PTD> children, ImmutableGraph graph)
        {
            if (currentGraphID != graph.graphID)
            {
                currentGraphID = graph.graphID;
                changed = true;
            }
            bool someIntersectionContainsElements = false;
            for (int i = 0; i < children.Count; i++)
            {
                for (int j = i + 1; j < children.Count; j++)
                {
                    /*
                    BitSet verticesIgnoreIntersection = new BitSet(children[i].possiblyUsableIgnore);
                    verticesIgnoreIntersection.IntersectWith(children[j].possiblyUsableIgnore);
                    BitSet childrenInletsIntersection = new BitSet(children[i].inlet);
                    childrenInletsIntersection.IntersectWith(children[j].inlet);

                    Debug.Assert(childrenInletsIntersection.IsSupersetOf(verticesIgnoreIntersection));
                    if (!childrenInletsIntersection.Equals(verticesIgnoreIntersection))
                    {
                        return false;
                    }
                    */
                    if (children[i].inlet.IsSupersetOf(children[j].inlet) || children[j].inlet.IsSupersetOf(children[i].inlet))
                    {
                        return false;
                    }

                    BitSet verticesIgnoreIntersection = new BitSet(children[i].possiblyUsableIgnore);
                    verticesIgnoreIntersection.IntersectWith(children[j].possiblyUsableIgnore);
                    BitSet verticesIgnoreUnion = new BitSet(children[i].possiblyUsableIgnore);
                    verticesIgnoreUnion.UnionWith(children[j].possiblyUsableIgnore);
                    BitSet childrenInletsIntersection = new BitSet(children[i].inlet);
                    childrenInletsIntersection.IntersectWith(children[j].inlet);
                    
                    /*
                    if (verticesIgnoreUnion.IsSupersetOf(childrenInletsIntersection) != verticesIgnoreIntersection.Equals(childrenInletsIntersection))
                    {
                        bool union = verticesIgnoreUnion.IsSupersetOf(childrenInletsIntersection);
                        if (!union)
                        {
                            ;
                        }
                    }
                    */
                    
                    
                    if (!verticesIgnoreUnion.IsSupersetOf(childrenInletsIntersection))
                    {
                        return false;
                    }
                    /*                    
                    if (!childrenInletsIntersection.Equals(verticesIgnoreIntersection))
                    {
                        return false;
                    }
                    */


                    BitSet verticesIntersection = new BitSet(children[i].vertices);
                    verticesIntersection.IntersectWith(children[j].vertices);
                    verticesIntersection.ExceptWith(verticesIgnoreIntersection);
                    if (!children[i].outlet.IsSupersetOf(verticesIntersection) || !children[j].outlet.IsSupersetOf(verticesIntersection))
                    {
                        return false;
                    }

                    if (verticesIgnoreUnion.Count() > 0)
                    {
                        someIntersectionContainsElements = true;
                    }

                }
            }

#if DEBUG
            // Debug. TODO: remove
            if (someIntersectionContainsElements)
            {
                BitSet debugBag = new BitSet(children[0].outlet);
                for (int i = 1; i < children.Count; i++)
                {
                    debugBag.UnionWith(children[i].outlet);
                }
                //BitSet debugVertices = new BitSet(children[i].vertices);
                //debugVertices.UnionWith(children[j].vertices);
                //BitSet outlet = graph.Outlet(debugBag, debugVertices);

                PTD debugPTDUR = new PTD(debugBag);
                for (int i = 0; i < children.Count; i++)
                {
                    debugPTDUR.children.Add(children[i]);
                }

                PTD deepcopy = DeepCopy(debugPTDUR);

                deepcopy.RemoveDuplicateBags();
                deepcopy.AssertConsistency(graph.vertexCount, fullConsistencyCheck: true);
            }
#endif


            return true;
        }

        private static PTD DeepCopy(PTD original)
        {
            PTD result = new PTD(original.Bag);
            for (int i = 0; i < original.children.Count; i++)
            {
                result.children.Add(DeepCopy(original.children[i]));
            }
            return result;
        }

        /// <summary>
        /// tests if this PTD is an incoming PTD
        /// </summary>
        /// <returns>true iff the PTD is incoming</returns>
        public bool IsIncoming(ImmutableGraph graph)
        {
            BitSet rest = vertices.Complement();
            return inlet.First() < rest.First();
        }

        /// <summary>
        /// tests if this PTD is normalized
        /// </summary>
        /// <returns>true iff the PTD is normalized</returns>
        public bool IsNormalized()
        {
            for (int i = 0; i < children.Count; i++)
            {
                Debug.Assert(!children[i].outlet.IsEmpty());
                if ((skipChildrenInNormalizedCheck == null || !skipChildrenInNormalizedCheck.Contains(i)) && outlet.IsSupersetOf(children[i].outlet))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// adds a child whose normalized check is disabled
        /// </summary>
        /// <param name="child">the child to add</param>
        /// <param name="graph">the underlying graph</param>
        public void AddChildNotNormalized(PTD child, ImmutableGraph graph)
        {
            if (skipChildrenInNormalizedCheck == null)
            {
                skipChildrenInNormalizedCheck = new List<int>();
            }
            skipChildrenInNormalizedCheck.Add(children.Count);
            children.Add(child);

            vertices.UnionWith(child.vertices);
            possiblyUsableIgnore.UnionWith(child.inlet);
            outlet = graph.Outlet(Bag, vertices);
            inlet.CopyFrom(vertices);
            inlet.ExceptWith(outlet);
        }

        public void AddInletToPossiblyUsableIgnore(BitSet otherInlet)
        {
            possiblyUsableIgnore.UnionWith(otherInlet);
        }

        /// <summary>
        /// removes duplicate bags from this tree decomposition. These may be added incorrectly if Treewidth.moreThan2ComponentsOptimization is enabled.
        /// </summary>
        public void RemoveDuplicateBags()
        {
            HashSet<BitSet> bagSet = new HashSet<BitSet> { Bag };
            Stack<PTD> nodeStack = new Stack<PTD>();
            nodeStack.Push(this);

            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();
                for (int i = currentNode.children.Count - 1; i >= 0; i--)
                {
                    PTD currentChild = currentNode.children[i];
                    if (bagSet.Contains(currentChild.Bag))
                    {
                        currentNode.children.RemoveAt(i);
                    }
                    else
                    {
                        bagSet.Add(currentChild.Bag);
                        nodeStack.Push(currentChild);
                    }
                }

            }
        }

        /// <summary>
        /// Constructs a tree decomposition with a different root. The root will be a superset of the rootSet parameter.
        /// If rootSet is a safe separator between the graph underlying this tree decomposition and another graph, this
        /// method is helpful to reconstruct a tree decomposition of the original graph.
        /// </summary>
        /// <param name="rootSet">a subset of the future root</param>
        /// <returns>a rerooted version of this tree decomposition where rootSet is a subset of the new root</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reroot(ref PTD ptd, BitSet rootSet)
        {
            if (ptd.Bag.IsSupersetOf(rootSet))
            {
                return;
            }

            Dictionary<PTD, PTD> parents = new Dictionary<PTD, PTD>();
            parents[ptd] = null;

            // find root node (its bag is a superset of the root set)
            PTD rootNode = null;
            Stack<PTD> nodeStack = new Stack<PTD>();
            nodeStack.Push(ptd);
            PTD currentNode;

            while(nodeStack.Count > 0)
            {
                currentNode = nodeStack.Pop();

                for (int i = 0; i < currentNode.children.Count; i++)
                {
                    PTD child = currentNode.children[i];

                    parents[child] = currentNode;

                    if (child.Bag.IsSupersetOf(rootSet))
                    {
                        rootNode = child;
                        break;
                    }

                    nodeStack.Push(child);
                }
            }

            Debug.Assert(rootNode != null);

            // reroot (swap parent-child relationship between all nodes that lie between the former root and the future root)
            currentNode = rootNode;
            PTD formerParentNode;
            while ((formerParentNode = parents[currentNode]) != null)
            {
                currentNode.children.Add(formerParentNode);
                formerParentNode.children.Remove(currentNode);
                currentNode = formerParentNode;
            }

            ptd = rootNode;
        }

        /// <summary>
        /// reindexes the vertices of this ptd that is a ptd of a reduced graph, so that they correctly represent the same vertices in the non-reduced graph.
        /// </summary>
        /// <param name="reindexationMapping">the mapping from the vertex indices in the current ptd to their original vertex indices within the original graph.</param>
        public void Reindex(ReindexationMapping reindexationMapping)
        {
            // initialize a stack of nodes
            Stack<PTD> nodeStack = new Stack<PTD>();

            nodeStack.Push(this);

            // re-index all bags with the vertices they had before reduction
            while (nodeStack.Count > 0)
            {
                PTD currentNode = nodeStack.Pop();
                BitSet reducedBag = currentNode.Bag;

                BitSet reconstructedBag = reindexationMapping.Reindex(reducedBag);
                currentNode.SetBag(reconstructedBag);

                // push children onto stack
                for (int i = 0; i < currentNode.children.Count; i++)
                {
                    nodeStack.Push(currentNode.children[i]);
                }
            }
        }

#region import from file

        /// <summary>
        /// imports a PTD from a .td file
        /// </summary>
        /// <param name="filepath">the path to that file</param>
        public static PTD ImportPTD(string filepath)
        {
            PTD[] nodesList = null;
            bool[] isChild = null;
            try
            {
                using (StreamReader sr = new StreamReader(filepath))
                {
                    int vertexCount = -1;
                    while (!sr.EndOfStream)
                    {
                        String line = sr.ReadLine();
                        if (line.StartsWith("c"))
                        {
                            continue;
                        }
                        else
                        {
                            String[] tokens = line.Split(' ');
                            if (tokens[0] == "s")
                            {
                                int nodesCount = Convert.ToInt32(tokens[2]);
                                nodesList = new PTD[nodesCount];
                                isChild = new bool[nodesCount];
                                vertexCount = Convert.ToInt32(tokens[4]);
                                // everything else not really relevant for our purposes here
                                continue;
                            }
                            else if (tokens[0] == "b")
                            {
                                int nodePosition = Convert.ToInt32(tokens[1]) - 1;
                                BitSet bag = new BitSet(vertexCount);
                                for (int i = 2; i < tokens.Length; i++)
                                {
                                    bag[Convert.ToInt32(tokens[i]) - 1] = true;
                                }
                                nodesList[nodePosition] = new PTD(bag);
                            }
                            else
                            {
                                int from = Convert.ToInt32(tokens[0]) - 1;
                                int to = Convert.ToInt32(tokens[1]) - 1;
                                nodesList[from].children.Add(nodesList[to]);
                                isChild[to] = true;
                            }
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            // find root
            for (int i = 0; i < nodesList.Length; i++)
            {
                if (!isChild[i])
                {
                    return nodesList[i];
                }
            }
            throw new Exception("The imported tree decomposition is not a tree");
        }

        /// <summary>
        /// constructor for one node of a PTD.
        /// USE ONLY FOR IMPORTING FROM FILE OR CONSTRUCTION OF A PTD WHOSE CHILDREN AND INLET/OUTLET DON'T MATTER!
        /// variable bag is shared, not copied
        /// </summary>
        /// <param name="bag">the bag of this ptd node</param>
        public PTD(BitSet bag)
        {
            this.Bag = bag;
            vertices = null;
            outlet = null;
            inlet = null;
            children = new List<PTD>();
        }

#endregion

#region printing

        /// <summary>
        /// prints the PTD to console
        /// </summary>
        public void Print()
        {
            Print_rec(" ", true);
        }

        /// <summary>
        /// recursive helper method for the Print function
        /// </summary>
        /// <param name="indent"></param>
        /// <param name="last"></param>
        public void Print_rec(string indent, bool last)
        {
            // code taken from https://stackoverflow.com/a/1649223
            Console.Write(indent);
            if (last)
            {
                Console.Write("\\- ");
                indent += "  ";
            }
            else
            {
                Console.Write("|- ");
                indent += "| ";
            }
            Bag.Print_NoBrackets();

            for (int i = 0; i < children.Count; i++)
                children[i].Print_rec(indent, i == children.Count - 1);
        }

        public override string ToString()
        {
            return Bag.ToString();
        }

#endregion printing


#region debug

        /// <summary>
        /// asserts that this ptd is consistent
        /// </summary>
        /// <param name="vertexCount">the number of vertices in the underlying graph</param>
        [Conditional("DEBUG")]
        internal void AssertConsistency(int vertexCount, bool fullConsistencyCheck=true)
        {
            // create a list of all bags
            List<BitSet> bagsList = new List<BitSet>();
            List<int> parentBags = new List<int>();
            Stack<PTD> childrenStack = new Stack<PTD>();
            Stack<int> parentStack = new Stack<int>();
            childrenStack.Push(this);
            parentStack.Push(-1);
            while (childrenStack.Count > 0)
            {
                PTD current = childrenStack.Pop();
                int parent = parentStack.Pop();

                bagsList.Add(current.Bag);
                parentBags.Add(parent);
                foreach (PTD child in current.children)
                {
                    childrenStack.Push(child);
                    parentStack.Push(bagsList.Count - 1);
                }
            }

            // check consistency
            for (int i = 0; i < vertexCount; i++)
            {
                if (!fullConsistencyCheck && possiblyUsableIgnore[i])
                {
                    continue;
                }

                /*
                 *  key insight: all bags containing i form a subtree.
                 *  Therefore, in order for the tree decomposition to be consistent, there must be only one root for all subtrees containing i 
                 */
                HashSet<int> ancestors = new HashSet<int>();
                for (int j = 0; j < bagsList.Count; j++)
                {
                    if (bagsList[j][i] == true)
                    {
                        int currentAncestor = j;
                        int parentBag = parentBags[j];
                        while (parentBag != -1 && bagsList[parentBag][i])
                        {
                            currentAncestor = parentBag;
                            parentBag = parentBags[currentAncestor];
                            ;
                        }
                        ancestors.Add(currentAncestor);
                        if (ancestors.Count == 2)
                        {
                            Print();
                            Trace.Fail(String.Format("The printed ptd is not consistent. There are at least two subtrees containing vertex {0}.", i.ToString()));
                        }
                    }
                }
            }
        }


        /// <summary>
        /// asserts that this ptd is actually a tree decomposition for the given graph
        /// </summary>
        /// <param name="graph">the graph that this ptd is supposed to be a tree decomposition of</param>
        public void AssertValidTreeDecomposition(ImmutableGraph graph)
        {
            // create a list of all bags
            List<BitSet> bagsList = new List<BitSet>();
            List<int> parentBags = new List<int>();
            Stack<PTD> childrenStack = new Stack<PTD>();
            Stack<int> parentStack = new Stack<int>();
            childrenStack.Push(this);
            parentStack.Push(-1);
            while (childrenStack.Count > 0)
            {
                PTD current = childrenStack.Pop();
                int parent = parentStack.Pop();

                bagsList.Add(current.Bag);
                parentBags.Add(parent);
                foreach (PTD child in current.children)
                {
                    childrenStack.Push(child);
                    parentStack.Push(bagsList.Count - 1);
                }
            }

            // check vertex cover
            for (int i = 0; i < graph.vertexCount; i++)
            {
                bool isCovered = false;
                foreach (BitSet bag in bagsList)
                {
                    if (bag[i])
                    {
                        isCovered = true;
                        break;
                    }
                }
                if (!isCovered)
                {
                    Print();
                    Trace.Fail(String.Format("The printed ptd for graph {0} does not cover all of the graph's vertices. Vertex {1} is not covered.", graph.graphID, i));
                }
            }

            // check edge cover
            for (int u = 0; u < graph.vertexCount; u++)
            {
                foreach (int v in graph.adjacencyList[u])
                {
                    bool isCovered = false;
                    foreach (BitSet bag in bagsList)
                    {
                        if (bag[u] && bag[v])
                        {
                            isCovered = true;
                            break;
                        }
                    }
                    if (!isCovered)
                    {
                        Print();
                        Trace.Fail(String.Format("The printed ptd for graph {0} does not cover all of the graph's edges. Edge ({1},{2}) is not covered.", graph.graphID, u, v));
                    }
                }
            }

            // check consistency
            for (int i = 0; i < graph.vertexCount; i++)
            {
                //if (possiblyUsableIgnore[i])
                //{
                //    continue;
                //}

                /*
                 *  key insight: all bags containing i form a subtree.
                 *  Therefore, in order for the tree decomposition to be consistent, there must be only one root for all subtrees containing i 
                 */
                HashSet<int> ancestors = new HashSet<int>();
                for (int j = 0; j < bagsList.Count; j++)
                {
                    if (bagsList[j][i] == true)
                    {
                        int currentAncestor = j;
                        int parentBag = parentBags[j];
                        while (parentBag != -1 && bagsList[parentBag][i])
                        {
                            currentAncestor = parentBag;
                            parentBag = parentBags[currentAncestor];
                            ;
                        }
                        ancestors.Add(currentAncestor);
                        if (ancestors.Count == 2)
                        {
                            Print();
                            Trace.Fail(String.Format("The printed ptd for graph {0} is not consistent. There are at least two subtrees containing vertex {1}.", graph.graphID, i));
                        }
                    }
                }
            }
        }

#endregion
    }
}
