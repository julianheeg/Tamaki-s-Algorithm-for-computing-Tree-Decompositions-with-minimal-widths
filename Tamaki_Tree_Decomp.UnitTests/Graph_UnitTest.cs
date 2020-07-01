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
        // test timeout in milliseconds
        private const int timeout = 60000;

        [TestMethod, Timeout(timeout)]
        public void IsPotMaxClique_PotMax_Clique_ReturnTrue()
        {
            string testFile1 = "Test Data\\test1.gr";
            Graph graph = new Graph(testFile1);

            BitSet a = new BitSet(6, new int[] { 0, 1, 2 });
            BitSet b = new BitSet(6, new int[] { 1, 2, 3, 4 });
            BitSet c = new BitSet(6, new int[] { 4, 5 });

            Assert.IsTrue(graph.IsPotMaxClique(a, out _));
            Assert.IsTrue(graph.IsPotMaxClique(b, out _));
            Assert.IsTrue(graph.IsPotMaxClique(c, out _));
        }

        [TestMethod, Timeout(timeout)]
        public void IsPotMaxClique_NotPotMaxClique_ReturnFalse()
        {
            string testFile1 = "Test Data\\test1.gr";
            Graph graph = new Graph(testFile1);
            
            BitSet j = new BitSet(6, new int[] { 0 });
            BitSet k = new BitSet(6, new int[] { 0, 1 });
            BitSet l = new BitSet(6, new int[] { 0, 4 });
            BitSet m = new BitSet(6, new int[] { 0, 5 });
            BitSet n = new BitSet(6, new int[] { 1, 5 });
            BitSet o = new BitSet(6, new int[] { 2, 5 });
            BitSet p = new BitSet(6, new int[] { 3, 5 });
            BitSet q = new BitSet(6, new int[] { 1, 2 });
            BitSet r = new BitSet(6, new int[] { 0, 3 });

            Assert.IsFalse(graph.IsPotMaxClique(j, out _));
            Assert.IsFalse(graph.IsPotMaxClique(k, out _));
            Assert.IsFalse(graph.IsPotMaxClique(l, out _));
            Assert.IsFalse(graph.IsPotMaxClique(m, out _));
            Assert.IsFalse(graph.IsPotMaxClique(n, out _));
            Assert.IsFalse(graph.IsPotMaxClique(o, out _));
            Assert.IsFalse(graph.IsPotMaxClique(p, out _));
            Assert.IsFalse(graph.IsPotMaxClique(q, out _));
            Assert.IsFalse(graph.IsPotMaxClique(r, out _));
        }

        [TestMethod, Timeout(timeout)]
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

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_MediumGraphs_ReturnTrue()
        {
            Graph g = new Graph("Test Data\\2016\\medium\\NauruGraph.gr");
            Assert.AreEqual(6, g.TreeWidth(out PTD output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
            
            g = new Graph("Test Data\\2016\\medium\\FlowerSnark.gr");
            output = null;
            Assert.AreEqual(6, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\2016\\medium\\DesarguesGraph.gr");
            output = null;
            Assert.AreEqual(6, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\2016\\medium\\GeneralizedPetersenGraph_10_4.gr");
            output = null;
            Assert.AreEqual(6, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\2016\\medium\\HoffmanGraph.gr");
            output = null;
            Assert.AreEqual(6, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
            
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2016_HardGraphs_ReturnTrue()
        {
            Graph g = new Graph("Test Data\\2016\\hard\\ClebschGraph.gr");
            Assert.AreEqual(8, g.TreeWidth(out PTD output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\2016\\hard\\contiki_dhcpc_handle_dhcp.gr");
            output = null;
            Assert.AreEqual(6, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\2016\\hard\\DoubleStarSnark.gr");
            output = null;
            Assert.AreEqual(6, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\2016\\hard\\fuzix_vfscanf_vfscanf.gr");
            output = null;
            Assert.AreEqual(6, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));

            g = new Graph("Test Data\\2016\\hard\\McGeeGraph.gr");
            output = null;
            Assert.AreEqual(7, g.TreeWidth(out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_001_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex001.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }


        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_003_ReturnTrue()
        {
            int treeWidth = 44;
            Graph g = new Graph("Test Data\\2017\\ex003.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_005_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex005.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_007_ReturnTrue()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex007.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_009_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex009.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_011_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex011.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_013_ReturnTrue()
        {
            int treeWidth = 29;
            Graph g = new Graph("Test Data\\2017\\ex013.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_015_ReturnTrue()
        {
            int treeWidth = 15;
            Graph g = new Graph("Test Data\\2017\\ex015.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }


        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_017_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex017.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_019_ReturnTrue()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex019.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_021_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex021.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_023_ReturnTrue()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex023.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_025_ReturnTrue()
        {
            int treeWidth = 20;
            Graph g = new Graph("Test Data\\2017\\ex025.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_027_ReturnTrue()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex027.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_029_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex029.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_031_ReturnTrue()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex031.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_033_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex033.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_035_ReturnTrue()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex035.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_037_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex037.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_039_ReturnTrue()
        {
            int treeWidth = 32;
            Graph g = new Graph("Test Data\\2017\\ex039.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_041_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex041.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_043_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex043.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_045_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex045.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_047_ReturnTrue()
        {
            int treeWidth = 21;
            Graph g = new Graph("Test Data\\2017\\ex047.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_049_ReturnTrue()
        {
            int treeWidth = 13;
            Graph g = new Graph("Test Data\\2017\\ex049.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_051_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex051.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_053_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex053.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_055_ReturnTrue()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex055.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_057_ReturnTrue()
        {
            int treeWidth = 117;
            Graph g = new Graph("Test Data\\2017\\ex057.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_059_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex059.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_061_ReturnTrue()
        {
            int treeWidth = 22;
            Graph g = new Graph("Test Data\\2017\\ex061.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_063_ReturnTrue()
        {
            int treeWidth = 34;
            Graph g = new Graph("Test Data\\2017\\ex063.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_065_ReturnTrue()
        {
            int treeWidth = 25;
            Graph g = new Graph("Test Data\\2017\\ex065.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_067_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex067.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_069_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex069.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_071_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex071.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_073_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex073.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_075_ReturnTrue()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex075.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_077_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex077.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_079_ReturnTrue()
        {
            int treeWidth = 42;
            Graph g = new Graph("Test Data\\2017\\ex079.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_081_ReturnTrue()
        {
            int treeWidth = 6;
            Graph g = new Graph("Test Data\\2017\\ex081.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_083_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex083.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_085_ReturnTrue()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex085.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_087_ReturnTrue()
        {
            int treeWidth = 47;
            Graph g = new Graph("Test Data\\2017\\ex087.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_089_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex089.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_091_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex091.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_093_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex093.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_095_ReturnTrue()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex095.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_097_ReturnTrue()
        {
            int treeWidth = 48;
            Graph g = new Graph("Test Data\\2017\\ex097.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_099_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex099.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_101_ReturnTrue()
        {
            int treeWidth = 540;
            Graph g = new Graph("Test Data\\2017\\ex101.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_103_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex103.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_105_ReturnTrue()
        {
            int treeWidth = 540;
            Graph g = new Graph("Test Data\\2017\\ex105.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_107_ReturnTrue()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex107.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_109_ReturnTrue()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex109.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_111_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex111.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_113_ReturnTrue()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex113.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_115_ReturnTrue()
        {
            int treeWidth = 908;
            Graph g = new Graph("Test Data\\2017\\ex115.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_117_ReturnTrue()
        {
            int treeWidth = 13;
            Graph g = new Graph("Test Data\\2017\\ex117.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_119_ReturnTrue()
        {
            int treeWidth = 23;
            Graph g = new Graph("Test Data\\2017\\ex119.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_121_ReturnTrue()
        {
            int treeWidth = 34;
            Graph g = new Graph("Test Data\\2017\\ex121.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_123_ReturnTrue()
        {
            int treeWidth = 35;
            Graph g = new Graph("Test Data\\2017\\ex123.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_125_ReturnTrue()
        {
            int treeWidth = 70;
            Graph g = new Graph("Test Data\\2017\\ex125.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_127_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex127.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_129_ReturnTrue()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex129.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_131_ReturnTrue()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex131.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_133_ReturnTrue()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex133.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_135_ReturnTrue()
        {
            int treeWidth = 87;
            Graph g = new Graph("Test Data\\2017\\ex135.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_137_ReturnTrue()
        {
            int treeWidth = 19;
            Graph g = new Graph("Test Data\\2017\\ex137.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_139_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex139.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_141_ReturnTrue()
        {
            int treeWidth = 34;
            Graph g = new Graph("Test Data\\2017\\ex141.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_143_ReturnTrue()
        {
            int treeWidth = 35;
            Graph g = new Graph("Test Data\\2017\\ex143.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_145_ReturnTrue()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex145.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_147_ReturnTrue()
        {
            int treeWidth = 16;
            Graph g = new Graph("Test Data\\2017\\ex147.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_149_ReturnTrue()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex149.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_151_ReturnTrue()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex151.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_153_ReturnTrue()
        {
            int treeWidth = 47;
            Graph g = new Graph("Test Data\\2017\\ex153.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_155_ReturnTrue()
        {
            int treeWidth = 47;
            Graph g = new Graph("Test Data\\2017\\ex155.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_157_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex157.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_159_ReturnTrue()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex159.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_161_ReturnTrue()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex161.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_163_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex163.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_165_ReturnTrue()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex165.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_167_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex167.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_169_ReturnTrue()
        {
            int treeWidth = 22;
            Graph g = new Graph("Test Data\\2017\\ex169.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_171_ReturnTrue()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex171.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_173_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex173.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_175_ReturnTrue()
        {
            int treeWidth = 17;
            Graph g = new Graph("Test Data\\2017\\ex175.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_177_ReturnTrue()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex177.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_179_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex179.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_181_ReturnTrue()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex181.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_183_ReturnTrue()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex183.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_185_ReturnTrue()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex185.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_187_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex187.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_189_ReturnTrue()
        {
            int treeWidth = 70;
            Graph g = new Graph("Test Data\\2017\\ex189.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_191_ReturnTrue()
        {
            int treeWidth = 15;
            Graph g = new Graph("Test Data\\2017\\ex191.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_193_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex193.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_195_ReturnTrue()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex195.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_197_ReturnTrue()
        {
            int treeWidth = 15;
            Graph g = new Graph("Test Data\\2017\\ex197.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_199_ReturnTrue()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex199.gr");
            Assert.IsFalse(g.IsTreeWidthAtMost(treeWidth - 1, out PTD output));
            Assert.IsTrue(g.IsTreeWidthAtMost(treeWidth, out output));
            Assert.IsTrue(g.IsValidTreeDecomposition(output));
        }
    }
}
