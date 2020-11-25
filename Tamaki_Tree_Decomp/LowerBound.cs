using System;
using System.Collections.Generic;
using System.Linq;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp
{
    public static class LowerBound
    {

        public static int getLowerBound(Graph graph)
        {
            Graph H = new Graph(graph);
            int maxmin = 0;
            int verticesCount = H.adjacencyList.Length;

            while (verticesCount >= 2)
            {
                //Select a vertex v from H that has minimum degree in H.
                int v = 0;
                int minDegree = Int32.MaxValue;
                for (int i = 0;  i < H.adjacencyList.Length;i++)
                {
                    if (H.adjacencyList[i].Count > 0 && H.adjacencyList[i].Count < minDegree)
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
                int commonNeighbours = Int32.MaxValue;
                int w = 0;
                foreach (var neighbor in H.adjacencyList[v])
                {
                    IEnumerable<int> intersection = H.adjacencyList[neighbor].AsQueryable().Intersect(H.adjacencyList[v]);
                    if (intersection.Count() < commonNeighbours)
                    {
                        commonNeighbours = intersection.Count();
                        w = neighbor;
                    }
                    if (commonNeighbours == 0)
                    {
                        break;
                    }
                }
                
                
                //Contract the edge {v, w} in H.
                foreach (var neighbor in H.adjacencyList[v])
                {
                    H.adjacencyList[neighbor].Remove(v);
                    if(w!=neighbor){
                        if (!H.adjacencyList[neighbor].Contains(w))
                        {
                            H.adjacencyList[neighbor].Add(w);
                        }
                        if (!H.adjacencyList[w].Contains(neighbor))
                        {
                            H.adjacencyList[w].Add(neighbor);
                        }
                    }
                    
                }
                H.adjacencyList[v].Clear();
                verticesCount--;
                
                
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

            return maxmin;

        }

    }
}