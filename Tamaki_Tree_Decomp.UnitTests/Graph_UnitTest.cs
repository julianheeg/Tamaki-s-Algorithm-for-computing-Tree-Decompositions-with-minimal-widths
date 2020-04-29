using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp.UnitTests
{
    [TestClass]
    public class Graph_UnitTest
    {
        [TestMethod]
        public void IsPotMaxClique_PotMax_Clique_ReturnTrue()
        {
            string testFile1 = "Test Data\\test1.gr";
            Graph graph = new Graph(testFile1);


            // hash set
            HashSet<int> a = new HashSet<int> { 0, 1, 2 };
            HashSet<int> b = new HashSet<int> { 1, 2, 3, 4 };
            HashSet<int> c = new HashSet<int> { 4, 5 };

            Assert.IsTrue(graph.IsPotMaxClique(a));
            Assert.IsTrue(graph.IsPotMaxClique(b));
            Assert.IsTrue(graph.IsPotMaxClique(c));


            // bit set
            BitSet d = new BitSet(6, new int[] { 0, 1, 2 });
            BitSet e = new BitSet(6, new int[] { 1, 2, 3, 4 });
            BitSet f = new BitSet(6, new int[] { 4, 5});

            Assert.IsTrue(graph.IsPotMaxClique(d));
            Assert.IsTrue(graph.IsPotMaxClique(e));
            Assert.IsTrue(graph.IsPotMaxClique(f));
        }

        [TestMethod]
        public void IsPotMaxClique_NotPotMaxClique_ReturnFalse()
        {
            string testFile1 = "Test Data\\test1.gr";
            Graph graph = new Graph(testFile1);

            HashSet<int> a = new HashSet<int> { 0 };
            HashSet<int> b = new HashSet<int> { 0, 1 };
            HashSet<int> c = new HashSet<int> { 0, 4 };
            HashSet<int> d = new HashSet<int> { 0, 5 };
            HashSet<int> e = new HashSet<int> { 1, 5 };
            HashSet<int> f = new HashSet<int> { 2, 5 };
            HashSet<int> g = new HashSet<int> { 3, 5 };
            HashSet<int> h = new HashSet<int> { 1, 2 };
            HashSet<int> i = new HashSet<int> { 0, 3 };

            Assert.IsFalse(graph.IsPotMaxClique(a));
            Assert.IsFalse(graph.IsPotMaxClique(b));
            Assert.IsFalse(graph.IsPotMaxClique(c));
            Assert.IsFalse(graph.IsPotMaxClique(d));
            Assert.IsFalse(graph.IsPotMaxClique(e));
            Assert.IsFalse(graph.IsPotMaxClique(f));
            Assert.IsFalse(graph.IsPotMaxClique(g));
            Assert.IsFalse(graph.IsPotMaxClique(h));
            Assert.IsFalse(graph.IsPotMaxClique(i));

            
            BitSet j = new BitSet(6, new int[] { 0 });
            BitSet k = new BitSet(6, new int[] { 0, 1 });
            BitSet l = new BitSet(6, new int[] { 0, 4 });
            BitSet m = new BitSet(6, new int[] { 0, 5 });
            BitSet n = new BitSet(6, new int[] { 1, 5 });
            BitSet o = new BitSet(6, new int[] { 2, 5 });
            BitSet p = new BitSet(6, new int[] { 3, 5 });
            BitSet q = new BitSet(6, new int[] { 1, 2 });
            BitSet r = new BitSet(6, new int[] { 0, 3 });

            Assert.IsFalse(graph.IsPotMaxClique(j));
            Assert.IsFalse(graph.IsPotMaxClique(k));
            Assert.IsFalse(graph.IsPotMaxClique(l));
            Assert.IsFalse(graph.IsPotMaxClique(m));
            Assert.IsFalse(graph.IsPotMaxClique(n));
            Assert.IsFalse(graph.IsPotMaxClique(o));
            Assert.IsFalse(graph.IsPotMaxClique(p));
            Assert.IsFalse(graph.IsPotMaxClique(q));
            Assert.IsFalse(graph.IsPotMaxClique(r));
        }

        [TestMethod]
        public void TreeWidth_SmallGraphs_ReturnTrue()
        {
            Graph g = new Graph("Test Data\\test1.gr");
            Assert.AreEqual(3, g.TreeWidth(out PTD output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\s0_fuzix_clock_settime_clock_settime.gr");
            output = null;
            Assert.AreEqual(2, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\s1_fuzix_clock_settime_clock_settime.gr");
            output = null;
            Assert.AreEqual(2, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            
            g = new Graph("Test Data\\empty.gr");
            output = null;
            Assert.AreEqual(0, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\four_in_a_line.gr");
            output = null;
            Assert.AreEqual(1, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\gr-only.gr");
            output = null;
            Assert.AreEqual(1, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\single-vertex.gr");
            output = null;
            Assert.AreEqual(0, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }
    }
}
