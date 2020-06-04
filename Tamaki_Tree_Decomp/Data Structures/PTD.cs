using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    public class PTD
    {
        public BitSet Bag { get; private set; }
        public readonly BitSet vertices;
        public readonly BitSet inlet;
        public readonly BitSet outlet;
        public readonly List<PTD> children;

        //TODO: perhaps maintain the sets of components associated with the outel

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="ptd">the ptd to copy</param>
        public PTD(PTD ptd)
        {
            Bag = new BitSet(ptd.Bag);
            vertices = new BitSet(ptd.vertices);
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
        public PTD(BitSet bag, BitSet vertices, BitSet outlet, BitSet inlet, List<PTD> children)
        {
            Bag = bag;
            this.vertices = vertices;
            this.outlet = outlet;
            this.inlet = inlet;
            this.children = children;
        }

        /// <summary>
        /// constructor for one node of a PTD. variables are shared
        /// used in line 4
        /// </summary>
        /// <param name="bag">the set of vertices that this node contains</param>
        /// <param name="outlet">the outlet of this node</param>
        public PTD(BitSet bag, BitSet outlet)
        {
            Bag = new BitSet(bag);              // TODO: can be shared
            vertices = new BitSet(bag);         // TODO: can be shared
            this.outlet = new BitSet(outlet);   // TODO: can be shared
            inlet = new BitSet(bag);
            inlet.ExceptWith(outlet);
            children = new List<PTD>();

            AssertVerticesCorrect();
        }

        /// <summary>
        /// sets the bag of this node to a new set of vertices. Only for reconstruction purposes
        /// </summary>
        /// <param name="newBag">the new bag</param>
        public void SetBag(BitSet newBag)
        {
            Bag = newBag;
        }
        
        /// <summary>
        /// creates a new root with the given node as a child and the child's outlet as bag
        /// </summary>
        /// <param name="Tau">the child of this node</param>
        public static PTD Line9(PTD Tau)
        {
            BitSet bag = new BitSet(Tau.outlet);        // TODO: not copy? would be probably wrong since the child's outlet is used in line 9 and the bag of this root can change
            BitSet outlet = new BitSet(Tau.outlet);     // TODO: not copy? the outlet doesn't really change, does it?
            BitSet inlet = new BitSet(Tau.inlet);       // TODO: not copy? same here...
            BitSet vertices = new BitSet(Tau.vertices); // TODO: not copy

            List<PTD> children = new List<PTD>();
            children.Add(Tau);
            PTD result = new PTD(bag, vertices, outlet, inlet, children);
            result.AssertVerticesCorrect();

            return result;
        }

        // line 13
        public static PTD Line13(PTD Tau_prime, PTD Tau, Graph graph)
        {
            Debug.Assert(!Tau_prime.inlet.Equals(Tau.inlet));
            BitSet bag = new BitSet(Tau_prime.Bag);
            bag.UnionWith(Tau.outlet);

            List<PTD> children = new List<PTD>(Tau_prime.children);
            children.Add(Tau);
            BitSet vertices = new BitSet(Tau_prime.vertices);
            vertices.UnionWith(Tau.vertices);

            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            PTD result = new PTD(bag, vertices, outlet, inlet, children);

            return result;
        }

        // line 13, but exit early if bag size is too big
        public static bool Line13_CheckBagSize(PTD Tau_prime, PTD Tau, Graph graph, int k, out PTD result)
        {
            Debug.Assert(!Tau_prime.inlet.Equals(Tau.inlet));
            BitSet bag = new BitSet(Tau_prime.Bag);
            bag.UnionWith(Tau.outlet);
            if (bag.Count() > k + 1)
            {
                result = null;
                return false;
            }

            List<PTD> children = new List<PTD>(Tau_prime.children);
            children.Add(Tau);
            BitSet vertices = new BitSet(Tau_prime.vertices);
            vertices.UnionWith(Tau.vertices);

            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            result = new PTD(bag, vertices, outlet, inlet, children);

            return true;
        }

        // line 13, but exit early if bag size is too big
        public static bool Line13_CheckBagSize_CheckPossiblyUsable(PTD Tau_prime, PTD Tau, Graph graph, int k, out PTD result)
        {
            Debug.Assert(!Tau_prime.inlet.Equals(Tau.inlet));
            BitSet bag = new BitSet(Tau_prime.Bag);
            bag.UnionWith(Tau.outlet);

            // exit early if bag is too big
            if (bag.Count() > k + 1)
            {
                result = null;
                return false;
            }

            List<PTD> children = new List<PTD>(Tau_prime.children);
            children.Add(Tau);

            // exit early if not possibly usable
            for (int i = 0; i < children.Count; i++)
            {
                for (int j = i + 1; j < children.Count; j++)
                {
                    if (!children[i].inlet.IsDisjoint(children[j].inlet))
                    {
                        result = null;
                        return false;
                    }

                    BitSet verticesIntersection = new BitSet(children[i].vertices);
                    verticesIntersection.IntersectWith(children[j].vertices);
                    if (!children[i].outlet.IsSuperset(verticesIntersection) || !children[j].outlet.IsSuperset(verticesIntersection))
                    {
                        result = null;
                        return false;
                    }
                }
            }

            BitSet vertices = new BitSet(Tau_prime.vertices);
            vertices.UnionWith(Tau.vertices);

            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            result = new PTD(bag, vertices, outlet, inlet, children);

            return true;
        }

        // line 23
        public static PTD Line23(PTD Tau_wiggle, BitSet vNeighbors, Graph graph)
        {
            BitSet bag = new BitSet(vNeighbors);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);
            BitSet vertices = new BitSet(Tau_wiggle.vertices);
            vertices.UnionWith(vNeighbors);

            /*
            // TODO: correct calculation of inlet and outlet? perhaps not
            BitSet inlet = new BitSet(Tau_wiggle.inlet);
            BitSet outlet = new BitSet(bag);
            PTD result = new PTD(bag, vertices, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
            result.AssertVerticesCorrect();
            */

            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            PTD result = new PTD(bag, vertices, outlet, inlet, children);

            return result;
        }

        // line 29
        public static PTD Line29(PTD Tau_wiggle, BitSet newRoot, Graph graph)
        {
            Debug.Assert(newRoot.IsSuperset(Tau_wiggle.Bag));
            BitSet bag = newRoot;
            BitSet vertices = new BitSet(Tau_wiggle.vertices);
            vertices.UnionWith(newRoot);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);

            /*
            BitSet outlet = new BitSet(Tau_wiggle.outlet);
            BitSet inlet = new BitSet(Tau_wiggle.inlet);
            PTD result = new PTD(bag, vertices, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
            result.AssertVerticesCorrect();
            */

            BitSet outlet = graph.Outlet(bag, vertices);
            BitSet inlet = new BitSet(vertices);
            inlet.ExceptWith(outlet);
            PTD result = new PTD(bag, vertices, outlet, inlet, children);

            return result;
        }

        /// <summary>
        /// tests whether this and the other PTD are PTDs that contain the same vertices within them
        /// </summary>
        /// <param name="other">the other PTD</param>
        /// <returns>true iff the PTDs contain the same vertices</returns>
        public bool Equivalent(PTD other)
        {
            return inlet.Equals(other.inlet);
        }

        /// <summary>
        /// tests whether this PTD is possibly usable
        /// </summary>
        /// <returns>true iff the PTD is possibly usable</returns>
        public bool IsPossiblyUsable()
        {
            for (int i = 0; i < children.Count; i++)
            {
                Debug.Assert(children[i].inlet.IsDisjoint(outlet));

                for (int j = i + 1; j < children.Count; j++)
                {
                    if (!children[i].inlet.IsDisjoint(children[j].inlet))
                    {
                        return false;
                    }
                    
                    BitSet verticesIntersection = new BitSet(children[i].vertices);
                    verticesIntersection.IntersectWith(children[j].vertices);
                    if (!children[i].outlet.IsSuperset(verticesIntersection) || !children[j].outlet.IsSuperset(verticesIntersection))
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// tests if this PTD is an incoming PTD
        /// </summary>
        /// <returns>true iff the PTD is incoming</returns>
        public bool IsIncoming_Daniela(Graph graph)
        {
            BitSet rest = vertices.Complement();
            return inlet.First() < rest.First();
        }

        public bool IsNormalized_Daniela(PTD child)
        {
            return !outlet.IsSuperset(child.outlet);
        }

        /// <summary>
        /// tests if this PTD is an incoming PTD
        /// </summary>
        /// <returns>true iff the PTD is incoming</returns>
        public bool IsIncoming(Graph graph)
        {
            /*
            foreach ((BitSet, BitSet) C_NC in graph.ComponentsAndNeighbors(vertices))
            {
                if (!graph.UnionOutlet(this, C_NC.Item1).IsSuperset(C_NC.Item2))
                {
                    if (C_NC.Item1.First() > inlet.First())
                    {
                        return false;
                    }
                }
            }
            return true;
            */
            
            foreach ((BitSet, BitSet) C_NC in graph.ComponentsAndNeighbors(vertices))
            {
                if (!graph.UnionOutlet(this, C_NC.Item1).IsSuperset(C_NC.Item2))
                {
                    if (C_NC.Item1.First() > inlet.First())
                    {
                        return true;
                    }
                }
            }
            return false;           
            
        }


        [Conditional("DEBUG")]
        private void AssertVerticesCorrect()
        {
            Debug.Assert(inlet.IsDisjoint(outlet));

            BitSet union = new BitSet(inlet);
            union.UnionWith(outlet);
            Debug.Assert(vertices.Equals(union));
        }

        /// <summary>
        /// Constructs a tree decomposition with a different root. The root will be a superset of the rootSet parameter.
        /// If rootSet is a safe separator between the graph underlying this tree decomposition and another graph, this
        /// method is helpful to reconstruct a tree decomposition of the original graph.
        /// </summary>
        /// <param name="rootSet">a subset of the future root</param>
        /// <returns>a rerooted version of this tree decomposition where rootSet is a subset of the new root</returns>
        public PTD Reroot(BitSet rootSet)
        {
            if (Bag.IsSuperset(rootSet))
            {
                return this;
            }

            // ---- 1 ----
            // build undirected tree as an adjacency list
            Dictionary<BitSet, List<BitSet>> adjacencyList = new Dictionary<BitSet, List<BitSet>>();
            Stack<PTD> childrenStack = new Stack<PTD>();
            adjacencyList[Bag] = new List<BitSet>();
            childrenStack.Push(this);
            // while we're at it, might as well already create the visited array
            Dictionary<BitSet, bool> visited = new Dictionary<BitSet, bool> { [Bag] = false };
            bool rootFound = false;

            while (childrenStack.Count > 0)
            {
                PTD currentNode = childrenStack.Pop();
                visited[currentNode.Bag] = false;

                // find a bag that can act as a root
                if (!rootFound && currentNode.Bag.IsSuperset(rootSet))
                {
                    rootFound = true;
                    rootSet = currentNode.Bag;
                }
                
                // add children to the adjacency list
                foreach (PTD child in currentNode.children)
                {
                    adjacencyList[child.Bag] = new List<BitSet>();
                    adjacencyList[child.Bag].Add(currentNode.Bag);
                    adjacencyList[currentNode.Bag].Add(child.Bag);
                    childrenStack.Push(child);
                }
            }

            // ---- 2 ----
            // rebuild tree with different root
            PTD rootNode = new PTD(rootSet);
            childrenStack.Push(rootNode);

            do
            {
                PTD currentNode = childrenStack.Pop();
                visited[currentNode.Bag] = true;
                foreach (BitSet adjacentNode in adjacencyList[currentNode.Bag])
                {
                    if (!visited[adjacentNode])
                    {
                        PTD childNode = new PTD(adjacentNode);
                        currentNode.children.Add(childNode);
                        childrenStack.Push(childNode);
                    }
                }
            }
            while (childrenStack.Count > 0);

            return rootNode;
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
        /// USE ONLY FOR IMPORTING FROM FILE!
        /// variable bag is shared
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
    }
}
