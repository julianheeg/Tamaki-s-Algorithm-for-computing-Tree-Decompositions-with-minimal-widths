using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tamaki_Tree_Decomp.Data_Structures.BlockSieve;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// A class for representing a layered sieve. It stores ptdurs and can be queried with a ptd to return candidate ptdurs to combine it with.
    /// </summary>
    public class LayeredSieve
    {
        readonly BlockSieve[] blockSieves;  // blockSieves with increasing margins (upper bounds on vertices that can be added to the ptdurs' root bags)
        readonly int k;
        readonly List<PTD> toAdd;                                           // ptdurs that are marked for later addition (see the Add method for details)
        readonly Dictionary<PTD, Leaf> ptdurToLeafMapping;                  // maps ptdurs to the leaves that they are in
        readonly Dictionary<PTD, int> ptdurToDeferredAdditionIndexMapping;  // maps ptdurs that are marked for addition to their indices within the toAdd list.

        /// <summary>
        /// constructs a sieve
        /// </summary>
        /// <param name="k">the currently tested treewidth</param>
        /// <param name="vertexCount">the vertex count of the current graph</param>
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

            toAdd = new List<PTD>();
            ptdurToLeafMapping = new Dictionary<PTD, Leaf>();
            ptdurToDeferredAdditionIndexMapping = new Dictionary<PTD, int>();
        }

        /// <summary>
        /// adds a ptdur to the sieve
        /// </summary>
        /// <param name="ptdur">the ptdur to add</param>
        /// <returns>the leaf that the ptdur is stored in</returns>
        private Leaf Add(PTD ptdur)
        {
            int margin = k + 1 - (int)ptdur.Bag.Count();
            int sieveIndex = margin == 0 ? 0 : ((int)Math.Ceiling(Math.Log(margin, 2)) + 1);
            return blockSieves[sieveIndex].Add(ptdur);
        }

        /// <summary>
        /// Marks a ptdur for later addition to the sieve. The eventual addition needs to be initiated by calling the FlushAdd method.
        /// The reason for adding the ptdur later is that, during a query of a block sieve, a potential splitting of a node in that sieve
        /// will confuse the query method. Also in our case the later addition does not cause problems because the ptdur is always a child
        /// of the currently queried ptd and, thus, combining them will always lead to a ptdur that is not possibly usable.     
        /// </summary>
        /// <param name="ptdur">the ptdur to add</param>
        public void DeferredAdd(PTD ptdur)
        {
            ptdurToDeferredAdditionIndexMapping.Add(ptdur, toAdd.Count);
            toAdd.Add(ptdur);
        }

        /// <summary>
        /// Adds the ptdurs that have been marked for later addition to the sieve.
        /// </summary>
        public void FlushAdd()
        {
            for (int i = 0; i < toAdd.Count; i++)
            {
                Leaf leaf = Add(toAdd[i]);
                ptdurToLeafMapping.Add(toAdd[i], leaf);
            }
            toAdd.Clear();
            ptdurToDeferredAdditionIndexMapping.Clear();
        }

        /// <summary>
        /// Replaces a ptdur with another equivalent one which has a smaller root bag. The replacement is not done immediately,
        /// but needs to be initiated by the FlushAdd method.
        /// </summary>
        /// <param name="oldPtdur">the ptdur to replace</param>
        /// <param name="newPtdur">the new ptdur</param>
        public void Replace(PTD oldPtdur, PTD newPtdur)
        {
            if (ptdurToLeafMapping.TryGetValue(oldPtdur, out Leaf leaf)) 
            {
                int oldMargin = k + 1 - (int)oldPtdur.Bag.Count();
                int newMargin = k + 1 - (int)newPtdur.Bag.Count();

                int oldSieveIndex = oldMargin == 0 ? 0 : ((int)Math.Ceiling(Math.Log(oldMargin, 2)) + 1);
                int newSieveIndex = newMargin == 0 ? 0 : ((int)Math.Ceiling(Math.Log(newMargin, 2)) + 1);

                if (oldSieveIndex == newSieveIndex)
                {
                    leaf.ptdur = newPtdur;
                    ptdurToLeafMapping.Remove(oldPtdur);
                    ptdurToLeafMapping.Add(newPtdur, leaf);
                }
                else
                {
                    leaf.Remove();
                    ptdurToLeafMapping.Remove(oldPtdur);
                    leaf = Add(newPtdur);
                    ptdurToLeafMapping.Add(newPtdur, leaf);
                }
            }
            else
            {
                int index = ptdurToDeferredAdditionIndexMapping[oldPtdur];
                ptdurToDeferredAdditionIndexMapping.Add(newPtdur, index);
                toAdd[index] = newPtdur;
            }
        }

        /// <summary>
        /// queries this sieve to find all candidate ptdurs that a ptd might be able to be attached to
        /// </summary>
        /// <param name="ptd">the ptd to find candidate parents for</param>
        /// <returns>an enumerable of candidate ptdurs that the ptd might be able to be attached to</returns>
        public IEnumerable<PTD> EligiblePTDURs(PTD ptd, PTD ptdurVersionOfPTD)
        {
            yield return ptdurVersionOfPTD;
            for (int i = 0; i < blockSieves.Length; i++)
            {
                foreach(PTD ptdur in blockSieves[i].GetCandidatePTDURs(ptd))
                {
                    yield return ptdur;
                }
            }
        }
    }
}
