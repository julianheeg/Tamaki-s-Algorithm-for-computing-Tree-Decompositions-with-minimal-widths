using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tamaki_Tree_Decomp.Data_Structures.BlockSieve;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    public class LayeredSieve
    {
        readonly BlockSieve[] blockSieves;
        readonly int k;

        public LayeredSieve(int k, int vertexCount)
        {
            this.k = k;

            int sieveCount = (int)Math.Ceiling(Math.Log(k, 2)) + 2;
            blockSieves = new BlockSieve[sieveCount];

            blockSieves[0] = new BlockSieve(0, vertexCount);
            int margin = 1;
            for (int i = 1; i < sieveCount; i++)
            {
                blockSieves[i] = new BlockSieve(margin, vertexCount);
                margin *= 2;
            }
        }

        /// <summary>
        /// adds a ptdur to the sieve
        /// </summary>
        /// <param name="ptdur">the ptdur to add</param>
        /// <returns>the leaf that the ptdur is stored in</returns>
        internal Leaf Add(PTD ptdur)
        {
            // DEBUG
            BitSet verticesWithoutPUI = new BitSet(ptdur.vertices);
            verticesWithoutPUI.ExceptWith(ptdur.possiblyUsableIgnoreComponentsUnion);
            if (k == 69 && verticesWithoutPUI.Equals(new BitSet(161, new int[] { 9, 14, 15, 17, 18, 20, 21, 26, 30, 33, 36, 40, 42, 45, 48, 49, 57, 59, 60, 61, 63, 71, 72, 73, 77, 80, 81, 82, 83, 86, 90, 91, 93, 94, 95, 96, 98, 100, 102, 105, 109, 116, 119, 130, 131, 133, 136, 139, 140, 141, 144, 149, 150 })))
            {
                ;
            }

            int margin = k + 1 - (int)ptdur.Bag.Count();
            int sieveIndex = margin == 0 ? 0 : ((int)Math.Ceiling(Math.Log(margin, 2)) + 1);
            return blockSieves[sieveIndex].Add(ptdur);
        }

        /// <summary>
        /// queries this sieve to find all candidate ptdurs that a ptd might be able to be attached to
        /// </summary>
        /// <param name="ptd">the ptd to find candidate parents for</param>
        /// <returns>an enumerable of candidate ptdurs that the ptd might be able to be attached to</returns>
        public IEnumerable<PTD> EligiblePTDURs(PTD ptd)
        {
            for (int i = blockSieves.Length - 1; i >= 0; i--)
            {
                foreach(PTD ptdur in blockSieves[i].GetCandidatePTDURs(ptd))
                {
                    yield return ptdur;
                }
            }
            /*
            IEnumerable<PTD> iterator = blockSieves[blockSieves.Length - 1].GetCandidatePTDURs(ptd);
            for (int i = blockSieves.Length - 2; i >= 0; i--)
            {
                iterator.Concat(blockSieves[i].GetCandidatePTDURs(ptd));
            }
            return iterator;
            */
        }
    }
}
