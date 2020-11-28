using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tamaki_Tree_Decomp.Data_Structures;

[assembly: Parallelize(Workers = 3, Scope = ExecutionScope.MethodLevel)]

namespace Tamaki_Tree_Decomp.UnitTests
{
    [TestClass]
    public class Graph_UnitTest
    {
        [TestMethod]
        public void IsPotMaxClique_PotMaxClique()
        {
            string testFile1 = "..\\..\\Test Data\\test1.gr";
            Graph g = new Graph(testFile1);
            ImmutableGraph graph = new ImmutableGraph(g);

            BitSet a = new BitSet(6, new int[] { 0, 1, 2 });
            BitSet b = new BitSet(6, new int[] { 1, 2, 3, 4 });
            BitSet c = new BitSet(6, new int[] { 4, 5 });

            Assert.IsTrue(graph.IsPotMaxClique(a));
            Assert.IsTrue(graph.IsPotMaxClique(b));
            Assert.IsTrue(graph.IsPotMaxClique(c));
        }

        [TestMethod]
        public void IsPotMaxClique_NotPotMaxClique()
        {
            string testFile1 = "..\\..\\Test Data\\test1.gr";
            Graph g = new Graph(testFile1);
            ImmutableGraph graph = new ImmutableGraph(g);

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
    }
}
