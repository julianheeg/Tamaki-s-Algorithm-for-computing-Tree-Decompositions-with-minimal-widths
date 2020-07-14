#define statistics

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// a class for representing an immutable graph, i. e. a graph that an algorithm can run on. It is not suited for preprocessing. Use the regular Graph class for that.
    /// </summary>
    public class ImmutableGraph
    {
        public readonly int vertexCount;
        public readonly int edgeCount;

        public readonly int[][] adjacencyList;
        public readonly BitSet[] neighborSetsWithout;   // contains N(v)
        public readonly BitSet[] neighborSetsWith;      // contains N[v]
        public readonly BitSet allVertices;             // used for easy testing if a ptd covers all vertices
        
        public readonly int graphID;

        public static bool verbose = true;
        
        #region constructors

        /// <summary>
        /// constructs a graph from an adjacency list
        /// </summary>
        /// <param name="graph">the adjacecy list for this graph</param>
        public ImmutableGraph(Graph graph)
        {
            vertexCount = graph.vertexCount;
            adjacencyList = new int[vertexCount][];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacencyList[i] = graph.adjacencyList[i].ToArray();
                edgeCount += adjacencyList[i].Length;
            }
            edgeCount /= 2;

            // fill neighbor sets
            neighborSetsWithout = graph.neighborSetsWithout;
            neighborSetsWith = graph.neighborSetsWith;

            allVertices = BitSet.All(vertexCount);

            graphID = graph.graphID;
        }

        #endregion


        #region neighbors, components, cliquish, pmc, etc.

        /*
         * TODO: depth first search can be done using bit operations and it may be faster that way
         *       This is implemented in some of the functions here already, but not in all
         * 
         *  for every vertex v that is not in the separator{
         *      BitSet component = new BitSet(neighborSetsWith[v]);
         *      component.ExceptWith(separator);
         *      
         *      BitSet tempNeighbors = new BitSet(neighborSetsWithout[v]);
         *      tempNeighbors.ExceptWith(separator);
         *      while (tempNeighbors != 0){
         *          List nnnnnnn = tempNeighbors.Elements()
         *          tempNeighbors = empty;
         *          for every vertex u in nnnnnnn {
         *              tempNeighbors.unionWith(neighborSetsWithout[u];
         *              component.UnionWith(u); // or neighborSetsWith[u];
         *          }
         *          component.exceptWith(separator);
         *          tempNeighbors.exceptWith(separator);
         *      }
         *  }
         *  
         * */
        

        /// <summary>
        /// determines the components associated with a separator and also which of the vertices in the separator
        /// are neighbors of vertices in those components (which are a subset of the separator)
        /// </summary>
        /// <param name="separator">the separator</param>
        /// <returns>an enumerable consisting of tuples of i) component C and ii) neighbors N(C)</returns>
        public IEnumerable<(BitSet, BitSet)> ComponentsAndNeighbors(BitSet separator)
        {
            BitSet unvisited = new BitSet(separator);
            unvisited.Flip(vertexCount);

            // find components as long as they exist
            int startingVertex = -1;
            while ((startingVertex = unvisited.NextElement(startingVertex, isConsumed: true)) != -1)
            {
                BitSet currentIterationFrontier = new BitSet(vertexCount);
                currentIterationFrontier[startingVertex] = true;
                BitSet nextIterationFromtier = new BitSet(vertexCount);
                BitSet component = new BitSet(vertexCount);
                BitSet neighbors = Neighbors(component);

                while (!currentIterationFrontier.IsEmpty())
                {
                    nextIterationFromtier.Clear();

                    int redElement = -1;
                    while ((redElement = currentIterationFrontier.NextElement(redElement, isConsumed: false)) != -1)
                    {
                        nextIterationFromtier.UnionWith(neighborSetsWithout[redElement]);
                    }

                    component.UnionWith(currentIterationFrontier);
                    neighbors.UnionWith(nextIterationFromtier);
                    nextIterationFromtier.ExceptWith(separator);
                    nextIterationFromtier.ExceptWith(component);

                    currentIterationFrontier.CopyFrom(nextIterationFromtier);
                }

                // debug.Add((new BitSet(purple), new BitSet(neighbors)));
                unvisited.ExceptWith(component);
                neighbors.IntersectWith(separator);
                Debug.Assert(neighbors.Equals(Neighbors(component)));
                yield return (component, neighbors);
            }
        }

        /// <summary>
        /// determines the neighbor set of a set of vertices
        /// </summary>
        /// <param name="vertexSet">the set of vertices</param>
        /// <returns>the neighbor set of that set of vertices</returns>
        public BitSet Neighbors(BitSet vertexSet)
        {
            BitSet result = new BitSet(vertexCount);
            List<int> vertices = vertexSet.Elements();
            for (int i = 0; i < vertices.Count; i++)
            {
                result.UnionWith(neighborSetsWithout[vertices[i]]);
            }
            result.ExceptWith(vertexSet);
            return result;
        }

        /// <summary>
        /// tests if a set of vertices is a minimal separator
        /// </summary>
        /// <param name="separator">the set of vertices to test</param>
        /// <returns>true iff separator is a minimal separator</returns>
        public bool IsMinimalSeparator(BitSet separator)
        {
            int fullComponents = 0;
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(separator))
            {
                if (C_NC.Item2.Equals(separator))
                {
                    fullComponents++;
                    if (fullComponents == 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// tests whether a set of vertices is a minimal separator and if so, computes the components associated with it
        /// </summary>
        /// <param name="separator">the set of vertices</param>
        /// <param name="components">a list of the components associated with the set of vertices</param>
        /// <returns>true iff separator is a minimal separator</returns>
        public bool IsMinimalSeparator_ReturnComponents(BitSet separator, out List<BitSet> components)
        {
            components = new List<BitSet>();
            int fullComponents = 0;
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(separator))
            {
                components.Add(C_NC.Item1);
                if (C_NC.Item2.Equals(separator))
                {
                    fullComponents++;
                }
            }
            if (fullComponents >= 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// determines if a given vertex set is cliquish
        /// </summary>
        /// <param name="K">the vertex set</param>
        /// <returns>true, iff the vertex set is cliquish</returns>
        public bool IsCliquish(BitSet K)
        {
            // ------ 1.------

            List<BitSet> componentNeighborsInK = new List<BitSet>();
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(K))
            {
                BitSet N_C = C_NC.Item2;
                componentNeighborsInK.Add(C_NC.Item2);
            }
            List<int> KBits = K.Elements();

            // ------ 2.------
            // check if K is cliquish

            for (int i = 0; i < KBits.Count; i++)
            {
                int u = KBits[i];
                for (int j = i + 1; j < KBits.Count; j++)   // check only u and v with u < v
                {
                    int v = KBits[j];

                    // if a.) doesn't hold ...
                    if (!neighborSetsWithout[u][v])
                    {
                        bool b_satisfied = false;

                        // ... check if there is a component such that b.) doesn't hold ...
                        for (int k = 0; k < componentNeighborsInK.Count; k++)
                        {
                            if (componentNeighborsInK[k][u] && componentNeighborsInK[k][v])
                            {
                                b_satisfied = true;
                                break;
                            }
                        }
                        // ... if neither a.) nor b.) hold, K is not a potential maximal clique
                        if (!b_satisfied)
                        {
                            return false;
                        }
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// checks if K is a potential maximal clique and if so, computes the boundary vertices of K
        /// </summary>
        /// <param name="K">the vertex set to test</param>
        /// <param name="boundary">the boundary vertices of K if K is a potentially maximal clique</param>
        /// <returns>true and a bitset of the boundary vertices, iff K is a potential maximal clique. If K ist not a potential maximal clique, null is returned for the boundary vertices</returns>
        // TODO: cache lists, stack, sets, etc.
        public bool IsPotMaxClique(BitSet K, out BitSet boundary)
        {
            /*
             *  This method operates by the following principle:
             *  K is potential maximal clique <=>
             *      1. G\K has no full-components associated with K
             *      and
             *      2. K is cliquish
             * 
             *  For 1. we need to separate G into components C associated with K and check if N(C) is a proper subset of K,
             *      or in this case equivalently, if N(C) != K
             *  
             *  For 2. we need to check for each pair of vertices u,v in K if either of these conditions hold:
             *      a.) u,v are neighbors
             *      or
             *      b.) there exists C such that u,v are in N(C)
             * 
             *  During 1., while we determine the components, we save N(C) for each component so that we can use it in 2. later
             * 
             */


            // ------ 1.------

            List<BitSet> componentNeighborsInK = new List<BitSet>();
            foreach ((BitSet, BitSet) C_NC in ComponentsAndNeighbors(K))
            {
                BitSet N_C = C_NC.Item2;
                if (N_C.Equals(K))
                {
                    boundary = null;
                    return false;
                }
                componentNeighborsInK.Add(C_NC.Item2);
            }
            List<int> KBits = K.Elements();

            // ------ 2.------
            // check if K is cliquish

            for (int i = 0; i < KBits.Count; i++)
            {
                int u = KBits[i];
                for (int j = i + 1; j < KBits.Count; j++)   // check only u and v with u < v
                {
                    int v = KBits[j];

                    // if a.) doesn't hold ...
                    if (!neighborSetsWithout[u][v])
                    {
                        bool b_satisfied = false;

                        // ... check if there is a component such that b.) doesn't hold ...
                        for (int k = 0; k < componentNeighborsInK.Count; k++)
                        {
                            if (componentNeighborsInK[k][u] && componentNeighborsInK[k][v])
                            {
                                b_satisfied = true;
                                break;
                            }
                        }
                        // ... if neither a.) nor b.) hold, K is not a potential maximal clique
                        if (!b_satisfied)
                        {
                            boundary = null;
                            return false;
                        }
                    }

                }
            }

            boundary = new BitSet(vertexCount);
            for (int i = 0; i < componentNeighborsInK.Count; i++)
            {
                boundary.UnionWith(componentNeighborsInK[i]);
            }
            return true;
        }

        #endregion

        /// <summary>
        /// computes the outlet of the union of a ptd with a component
        /// </summary>
        /// <param name="ptd">the ptd</param>
        /// <param name="component">the component</param>
        /// <returns>the vertices in the outlet of ptd that are adjacent to vertices that are neither contained in the ptd nor in the component</returns>
        public BitSet UnionOutlet(PTD ptd, BitSet component)
        {
            
            BitSet unionOutlet = new BitSet(vertexCount);
            BitSet unionVertices = new BitSet(ptd.vertices);
            unionVertices.UnionWith(component);

            List<int> outletVertices = ptd.outlet.Elements();
            for (int i = 0; i < outletVertices.Count; i++)
            {
                int u = outletVertices[i];
                for (int j = 0; j < adjacencyList[u].Length; j++)
                {
                    int v = adjacencyList[u][j];
                    if (!unionVertices[v])
                    {
                        unionOutlet[u] = true;
                        break;
                    }
                }
            }
            return unionOutlet;
        }

        /// <summary>
        /// determines the outlet of a PTD with a given bag as the root node and a given vertex set
        /// </summary>
        /// <param name="bag">the PTD's root bag</param>
        /// <param name="vertices">the PTD's vertex set</param>
        /// <returns>the outlet of the PTD</returns>
        public BitSet Outlet(BitSet bag, BitSet vertices)
        {
            /*
            // create neighbor set
            List<int> elements = bag.Elements();
            BitSet bitSet = new BitSet(vertexCount);
            for (int i = 0; i < elements.Count; i++)
            {
                bitSet.UnionWith(neighborSetsWithout[elements[i]]);
            }
            bitSet.ExceptWith(vertices);

            // create outlet
            elements = bitSet.Elements();
            bitSet.Clear();
            for (int i = 0; i < elements.Count; i++)
            {
                bitSet.UnionWith(neighborSetsWithout[elements[i]]);
            }
            bitSet.IntersectWith(vertices);
            return bitSet;
            */
            
            // create neighbor set            
            BitSet neighbors = new BitSet(vertexCount);
            int pos = -1;
            while((pos = bag.NextElement(pos, isConsumed: false)) != -1)
            {
                neighbors.UnionWith(neighborSetsWithout[pos]);
            }
            neighbors.ExceptWith(vertices);

            // create outlet
            BitSet outlet = new BitSet(vertexCount);
            pos = -1;

            while ((pos = neighbors.NextElement(pos, isConsumed: false)) != -1)
            {
                outlet.UnionWith(neighborSetsWithout[pos]);
            }
            outlet.IntersectWith(vertices);

            return outlet;
            
        }
    }
}
