using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp.UnitTests
{
    [TestClass]
    class BitSetTests
    {
        /// <summary>
        /// use random data to test if the indexing works, both for setting and for getting
        /// </summary>
        [TestMethod]
        public void Indexer()
        {
            // seeded random number generator
            Random rnd = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                // define sets
                int length = rnd.Next() % 10000;
                BitSet bitSet = new BitSet(length);

                bool[] bitArray = new bool[length];

                // generate data
                for (int j = 0; j < 3 * length; j++)
                {
                    int key = rnd.Next() % length;
                    bool value = rnd.Next() % 2 == 1 ? true : false;

                    bitSet[key] = value;
                    bitArray[key] = value;
                }

                // check if both sets contain the same data
                for (int j = 0; j < length; j++)
                {
                    Assert.AreEqual<bool>(bitArray[j], bitSet[j], String.Format("Indexer test failed in iteration {0}", i));
                }
            }
        }


        /// <summary>
        /// use random data to test if a superset is recognized as a superset
        /// </summary>
        [TestMethod]
        public void IsSuperset_Superset_ReturnTrue()
        {
            // seeded random number generator
            Random rnd = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                // define sets
                int length = rnd.Next() % 10000;
                BitSet superSet = new BitSet(length);
                BitSet subSet = new BitSet(length);

                // generate data
                for (int j = 0; j < 3 * length; j++)
                {
                    int key = rnd.Next() % length;
                    bool value = rnd.Next() % 2 == 1 ? true : false;

                    superSet[key] = value;
                    subSet[key] = value;
                }

                // add data to superSet (50 test iterations without adding, 50 test iterations with adding 1 element, 50 with 2, etc. up to 20 elements)
                for (int j = 0; j * 50 < i; j++)
                {
                    int key = rnd.Next() % length;

                    superSet[key] = true;
                }

                // check if the superset is determined as a superset
                Assert.IsTrue(superSet.IsSupersetOf(subSet), String.Format("IsSuperset_Superset_ReturnTrue test failed in iteration {0}", i));
            }
        }

        /// <summary>
        /// use random data to test if a set that is not a superset is recognized as not being a superset
        /// </summary>
        [TestMethod]
        public void IsSuperset_NoSuperset_ReturnFalse()
        {
            // seeded random number generator
            Random rnd = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                // define sets
                int length = rnd.Next() % 10000;
                BitSet notSuperSet = new BitSet(length);
                BitSet notSubSet = new BitSet(length);

                // generate data

                // this index will always be set in the first set, but not in the second set, thus making the second set not a superset of the first one
                int firstOne = rnd.Next() % length;
                notSubSet[firstOne] = true;
                // generate additional data for the first set
                for (int j = 0; j < length; j++)
                {
                    int key = rnd.Next() % length;
                    notSubSet[key] = true;
                }

                // generate data for the second set
                for (int j = 0; j < length; j++)
                {
                    int key = rnd.Next() % length;
                    if (key != firstOne)
                    {
                        notSuperSet[key] = true;
                    }
                }

                // check if the second set is not a superset of the first one
                Assert.IsFalse(notSuperSet.IsSupersetOf(notSubSet), String.Format("IsSuperset_NoSuperset_ReturnFalse test failed in iteration {0}", i));
            }
        }
    }
}
