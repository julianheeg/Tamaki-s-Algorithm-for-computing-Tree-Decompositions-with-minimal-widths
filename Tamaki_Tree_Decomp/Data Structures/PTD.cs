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
            // TODO: possibly CRITICAL: update inlet, outlet, vertices, etc.
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

            BitSet inlet = new BitSet(Tau_prime.inlet);
            inlet.UnionWith(Tau.inlet);
            BitSet outlet = new BitSet(Tau_prime.outlet);
            BitSet vertices = new BitSet(Tau_prime.vertices);
            vertices.UnionWith(Tau.vertices);
            PTD result = new PTD(bag, vertices, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
            result.AssertVerticesCorrect();
            return result;
        }

        // line 23
        public static PTD Line23(PTD Tau_wiggle, BitSet vNeighbors, Graph graph)
        {
            BitSet bag = new BitSet(vNeighbors);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);

            // TODO: correct calculation of inlet and outlet? perhaps not
            BitSet inlet = new BitSet(Tau_wiggle.inlet);
            BitSet outlet = new BitSet(bag);
            BitSet vertices = new BitSet(Tau_wiggle.vertices);
            vertices.UnionWith(vNeighbors);

            PTD result = new PTD(bag, vertices, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
            result.AssertVerticesCorrect();
            return result;
        }

        // line 29
        public static PTD Line29(PTD Tau_wiggle, BitSet newRoot, Graph graph)
        {
            Debug.Assert(newRoot.IsSuperset(Tau_wiggle.Bag));
            BitSet bag = newRoot;
          
            BitSet outlet = new BitSet(Tau_wiggle.outlet);
            BitSet inlet = new BitSet(Tau_wiggle.inlet);
            BitSet vertices = new BitSet(Tau_wiggle.vertices);
            vertices.UnionWith(newRoot);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);

            PTD result = new PTD(bag, vertices, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
            result.AssertVerticesCorrect();
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
                // TODO: check correct?
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
        public bool IsIncoming(Graph graph)
        {
            foreach(Tuple<BitSet, BitSet> C_NC in graph.ComponentsAndNeighbors(Bag))
            {
                if (outlet.IsSuperset(C_NC.Item2) && !C_NC.Item2.Equals(outlet) && C_NC.Item2.First() < inlet.First())
                {
                    return false;
                }
            }
            return true;
        }


        private void AssertVerticesCorrect()
        {
            Debug.Assert(inlet.IsDisjoint(outlet));

            BitSet union = new BitSet(inlet);
            union.UnionWith(outlet);
            Debug.Assert(vertices.Equals(union));
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
