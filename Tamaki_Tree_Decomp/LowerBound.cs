using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    public static class LowerBound
    {
        public static bool calculateLowerBound = true;
        public static Stopwatch stopWatch = new Stopwatch();

        public static int getLowerBound(Graph graph)
        {
            if (!calculateLowerBound)
            {
                return 0;
            }
            stopWatch.Start();
            Graph H = new Graph(graph);
            int maxmin = 0;
            int verticesCount = H.vertexCount;

            while (H.notRemovedVertexCount >= 2)
            {
                //Select a vertex v from H that has minimum degree in H.
                int v = 0;
                int minDegree = Int32.MaxValue;
                for (int i = 0;  i < H.vertexCount; i++)
                {
                    if (H.notRemovedVertices[i] && H.adjacencyList[i].Count < minDegree)
                    {
                        minDegree = H.adjacencyList[i].Count;
                        v = i;
                    }
                }

                //maxmin = max(maxmin,dH(v)).
                if (minDegree > maxmin)
                {
                    maxmin = minDegree;
                }

                //MMD+


                //Select a neighbour w of v. {A specific strategy can be used here.}
                int commonNeighborCount = int.MaxValue;
                int w = 0;
                foreach (int neighbor in H.adjacencyList[v])
                {
                    BitSet commonNeighbors = new BitSet(H.openNeighborhood[v]);
                    commonNeighbors.IntersectWith(H.openNeighborhood[neighbor]);
                    int currentCommonNeighborCount = (int)commonNeighbors.Count();
                    if (currentCommonNeighborCount < commonNeighborCount)
                    {
                        commonNeighborCount = currentCommonNeighborCount;
                        w = neighbor;
                    }
                    if (commonNeighborCount == 0)
                    {
                        break;
                    }
                }


                //Contract the edge {v, w} in H.
                H.Contract(w, v);
                
                //MMD
                
                //Remove v and its incident edges from H.
                /*
                foreach (var neighbor in H.adjacencyList[v])
                {
                    H.adjacencyList[neighbor].Remove(v);
                }
                H.adjacencyList[v].Clear();
                verticesCount--;
                */
            }

            stopWatch.Stop();
            return maxmin;

        }

    }
}