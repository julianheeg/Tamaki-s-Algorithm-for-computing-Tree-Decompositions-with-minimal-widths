using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    public class PTD
    {
        public readonly BitSet bag;
        public readonly BitSet inlet;
        public readonly BitSet outlet;
        public readonly List<PTD> children;

        public PTD(BitSet bag, BitSet outlet, BitSet inlet, List<PTD> children)
        {
            this.bag = bag;
            this.outlet = outlet;
            this.inlet = inlet;
            this.children = children;
        }

        /// <summary>
        /// constructor for one node of a PTD. variables are shared
        /// used in line 3
        /// </summary>
        /// <param name="bag">the set of vertices that this node contains</param>
        /// <param name="outlet">the outlet of this node</param>
        public PTD(BitSet bag, BitSet outlet)
        {
            this.bag = bag;
            this.outlet = outlet;
            inlet = new BitSet(bag);
            inlet.ExceptWith(outlet);
            children = new List<PTD>();
        }
        
        /// <summary>
        /// creates a new root with the given node as a child and the child's outlet as bag
        /// </summary>
        /// <param name="Tau">the child of this node</param>
        public static PTD Line5(PTD Tau)
        {
            BitSet bag = new BitSet(Tau.outlet);    // TODO: not copy? would be probably wrong since the child's outlet is used in line 9 and the bag of this root can change
            BitSet outlet = bag;                    // TODO: not copy? the outlet doesn't really change, does it?
            BitSet inlet = Tau.inlet;               // TODO: not copy? same here...

            List<PTD> children = new List<PTD>();
            children.Add(Tau);
            return new PTD(bag, outlet, inlet, children);
        }

        // line 9
        public static PTD Line9(PTD Tau_prime, PTD Tau, Graph graph)
        {
            BitSet bag = new BitSet(Tau_prime.bag);
            bag.UnionWith(Tau.outlet);
            List<PTD> children = new List<PTD>(Tau_prime.children);
            children.Add(Tau);

            BitSet inlet = new BitSet(Tau_prime.inlet);
            BitSet outlet = new BitSet(bag);
            PTD result = new PTD(bag, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
            return result;
        }

        // line 16
        public static PTD Line16(PTD Tau_wiggle, BitSet vNeighbors, Graph graph)
        {
            BitSet bag = new BitSet(vNeighbors);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);

            // TODO: correct calculation of inlet and outlet? perhaps not
            BitSet inlet = new BitSet(Tau_wiggle.inlet);
            BitSet outlet = new BitSet(bag);

            PTD result = new PTD(bag, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
            return result;
        }

        // line 19
        public static PTD Line19(PTD Tau_wiggle, BitSet vNeighborsWithoutInlet, Graph graph)
        {
            BitSet bag = vNeighborsWithoutInlet;
            bag.UnionWith(Tau_wiggle.bag);
            BitSet outlet = new BitSet(Tau_wiggle.outlet);
            BitSet inlet = new BitSet(Tau_wiggle.inlet);
            List<PTD> children = new List<PTD>(Tau_wiggle.children);

            PTD result = new PTD(bag, outlet, inlet, children);
            graph.RecalculateInletAndOutlet(result);
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
        /// tests whether this PTD is possibly usable in a tree decomposition with width k
        /// </summary>
        /// <param name="k">the maximum width of the tree decomposition</param>
        /// <returns>true iff the PTD is possibly usable</returns>
        public bool IsPossiblyUsable(int k)
        {
            if (bag.Count() > k + 1)
            {
                return false;
            }
            for (int i = 0; i < children.Count; i++)
            {
                for (int j = i + 1; j < children.Count; j++)
                {
                    // TODO: comparison might be wrong due to false construction of 
                    if (!children[i].inlet.IsDisjoint(children[j].inlet))
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
        /// <returns>true iff it is incoming</returns>
        public bool IsIncoming_UniqueRoot()
        {
            BitSet restVertices = inlet.Complement();
            restVertices.ExceptWith(outlet);
            return inlet.First() < restVertices.First();
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
            this.bag = bag;
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
            bag.Print_NoBrackets();

            for (int i = 0; i < children.Count; i++)
                children[i].Print_rec(indent, i == children.Count - 1);
        }

        public override string ToString()
        {
            return bag.ToString();
        }

        #endregion printing
    }
}
