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
        private const int timeout = 1800000;
        static bool size3Separate = true;

        [TestMethod, Timeout(timeout)]
        public void IsPotMaxClique_PotMax_Clique_ReturnTrue()
        {
            string testFile1 = "Test Data\\test1.gr";
            ImmutableGraph graph = new ImmutableGraph(testFile1);

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
            ImmutableGraph graph = new ImmutableGraph(testFile1);
            
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

        private void RunAndAssertCorrect(ImmutableGraph g, int treewidth)
        {
            if (!size3Separate)
            {
                SafeSeparator.size3Separate = false;
            }
            Assert.AreEqual(treewidth, Treewidth.TreeWidth(g, out PTD output));
            output.AssertValidTreeDecomposition(g);
        }

        private void AssertCorrect(ImmutableGraph g, int treewidth)
        {
            if (!size3Separate)
            {
                SafeSeparator.size3Separate = false;
            }
            Assert.IsFalse(Treewidth.IsTreeWidthAtMost(g, treewidth - 1, out PTD output));
            Assert.IsTrue(Treewidth.IsTreeWidthAtMost(g, treewidth, out output));
            output.AssertValidTreeDecomposition(g);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_SmallGraphs_ReturnTrue()
        {
            ImmutableGraph g = new ImmutableGraph("Test Data\\test1.gr");
            RunAndAssertCorrect(g, 3);

            g = new ImmutableGraph("Test Data\\s0_fuzix_clock_settime_clock_settime.gr");
            RunAndAssertCorrect(g, 2);

            g = new ImmutableGraph("Test Data\\s1_fuzix_clock_settime_clock_settime.gr");
            RunAndAssertCorrect(g, 2);

            g = new ImmutableGraph("Test Data\\empty.gr");
            RunAndAssertCorrect(g, 0);

            g = new ImmutableGraph("Test Data\\four_in_a_line.gr");
            RunAndAssertCorrect(g, 1);

            g = new ImmutableGraph("Test Data\\gr-only.gr");
            RunAndAssertCorrect(g, 1);

            g = new ImmutableGraph("Test Data\\single-vertex.gr");
            RunAndAssertCorrect(g, 0);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_MediumGraphs_ReturnTrue()
        {
            ImmutableGraph g = new ImmutableGraph("Test Data\\2016\\medium\\NauruGraph.gr");
            RunAndAssertCorrect(g, 6);

            g = new ImmutableGraph("Test Data\\2016\\medium\\FlowerSnark.gr");
            RunAndAssertCorrect(g, 6);

            g = new ImmutableGraph("Test Data\\2016\\medium\\DesarguesGraph.gr");
            RunAndAssertCorrect(g, 6);

            g = new ImmutableGraph("Test Data\\2016\\medium\\GeneralizedPetersenGraph_10_4.gr");
            RunAndAssertCorrect(g, 6);

            g = new ImmutableGraph("Test Data\\2016\\medium\\HoffmanGraph.gr");
            RunAndAssertCorrect(g, 6);

        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2016_HardGraphs_ReturnTrue()
        {
            ImmutableGraph g = new ImmutableGraph("Test Data\\2016\\hard\\ClebschGraph.gr");
            RunAndAssertCorrect(g, 8);

            g = new ImmutableGraph("Test Data\\2016\\hard\\contiki_dhcpc_handle_dhcp.gr");
            RunAndAssertCorrect(g, 6);

            g = new ImmutableGraph("Test Data\\2016\\hard\\DoubleStarSnark.gr");
            RunAndAssertCorrect(g, 6);

            g = new ImmutableGraph("Test Data\\2016\\hard\\fuzix_vfscanf_vfscanf.gr");
            RunAndAssertCorrect(g, 6);

            g = new ImmutableGraph("Test Data\\2016\\hard\\McGeeGraph.gr");
            RunAndAssertCorrect(g, 7);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_001_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex001.gr");
            AssertCorrect(g, treeWidth);
        }


        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_003_ReturnTrue()
        {
            int treeWidth = 44;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex003.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_005_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex005.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_007_ReturnTrue()
        {
            int treeWidth = 12;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex007.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_009_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex009.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_011_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex011.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_013_ReturnTrue()
        {
            int treeWidth = 29;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex013.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_015_ReturnTrue()
        {
            int treeWidth = 15;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex015.gr");
            AssertCorrect(g, treeWidth);
        }


        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_017_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex017.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_019_ReturnTrue()
        {
            int treeWidth = 11;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex019.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_021_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex021.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_023_ReturnTrue()
        {
            int treeWidth = 8;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex023.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_025_ReturnTrue()
        {
            int treeWidth = 20;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex025.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_027_ReturnTrue()
        {
            int treeWidth = 11;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex027.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_029_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex029.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_031_ReturnTrue()
        {
            int treeWidth = 8;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex031.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_033_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex033.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_035_ReturnTrue()
        {
            int treeWidth = 14;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex035.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_037_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex037.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_039_ReturnTrue()
        {
            int treeWidth = 32;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex039.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_041_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex041.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_043_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex043.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_045_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex045.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_047_ReturnTrue()
        {
            int treeWidth = 21;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex047.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_049_ReturnTrue()
        {
            int treeWidth = 13;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex049.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_051_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex051.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_053_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex053.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_055_ReturnTrue()
        {
            int treeWidth = 18;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex055.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_057_ReturnTrue()
        {
            int treeWidth = 117;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex057.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_059_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex059.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_061_ReturnTrue()
        {
            int treeWidth = 22;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex061.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_063_ReturnTrue()
        {
            int treeWidth = 34;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex063.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_065_ReturnTrue()
        {
            int treeWidth = 25;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex065.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_067_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex067.gr");
            AssertCorrect(g, treeWidth);            
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_069_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex069.gr");
            AssertCorrect(g, treeWidth);            
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_071_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex071.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_073_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex073.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_075_ReturnTrue()
        {
            int treeWidth = 8;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex075.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_077_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex077.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_079_ReturnTrue()
        {
            int treeWidth = 42;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex079.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_081_ReturnTrue()
        {
            int treeWidth = 6;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex081.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_083_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex083.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_085_ReturnTrue()
        {
            int treeWidth = 8;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex085.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_087_ReturnTrue()
        {
            int treeWidth = 47;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex087.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_089_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex089.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_091_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex091.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_093_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex093.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_095_ReturnTrue()
        {
            int treeWidth = 11;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex095.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_097_ReturnTrue()
        {
            int treeWidth = 48;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex097.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_099_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex099.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_101_ReturnTrue()
        {
            int treeWidth = 540;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex101.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_103_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex103.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_105_ReturnTrue()
        {
            int treeWidth = 540;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex105.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_107_ReturnTrue()
        {
            int treeWidth = 12;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex107.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_109_ReturnTrue()
        {
            int treeWidth = 7;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex109.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_111_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex111.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_113_ReturnTrue()
        {
            int treeWidth = 14;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex113.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_115_ReturnTrue()
        {
            int treeWidth = 908;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex115.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_117_ReturnTrue()
        {
            int treeWidth = 13;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex117.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_119_ReturnTrue()
        {
            int treeWidth = 23;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex119.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_121_ReturnTrue()
        {
            int treeWidth = 34;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex121.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_123_ReturnTrue()
        {
            int treeWidth = 35;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex123.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_125_ReturnTrue()
        {
            int treeWidth = 70;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex125.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_127_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex127.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_129_ReturnTrue()
        {
            int treeWidth = 14;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex129.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_131_ReturnTrue()
        {
            int treeWidth = 18;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex131.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_133_ReturnTrue()
        {
            int treeWidth = 11;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex133.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_135_ReturnTrue()
        {
            int treeWidth = 87;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex135.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_137_ReturnTrue()
        {
            int treeWidth = 19;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex137.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_139_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex139.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_141_ReturnTrue()
        {
            int treeWidth = 34;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex141.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_143_ReturnTrue()
        {
            int treeWidth = 35;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex143.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_145_ReturnTrue()
        {
            int treeWidth = 12;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex145.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_147_ReturnTrue()
        {
            int treeWidth = 16;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex147.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_149_ReturnTrue()
        {
            int treeWidth = 12;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex149.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_151_ReturnTrue()
        {
            int treeWidth = 12;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex151.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_153_ReturnTrue()
        {
            int treeWidth = 47;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex153.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_155_ReturnTrue()
        {
            int treeWidth = 47;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex155.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_157_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex157.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_159_ReturnTrue()
        {
            int treeWidth = 18;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex159.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_161_ReturnTrue()
        {
            int treeWidth = 12;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex161.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_163_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex163.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_165_ReturnTrue()
        {
            int treeWidth = 14;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex165.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_167_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex167.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_169_ReturnTrue()
        {
            int treeWidth = 22;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex169.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_171_ReturnTrue()
        {
            int treeWidth = 14;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex171.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_173_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex173.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_175_ReturnTrue()
        {
            int treeWidth = 17;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex175.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_177_ReturnTrue()
        {
            int treeWidth = 14;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex177.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_179_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex179.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_181_ReturnTrue()
        {
            int treeWidth = 18;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex181.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_183_ReturnTrue()
        {
            int treeWidth = 11;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex183.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_185_ReturnTrue()
        {
            int treeWidth = 14;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex185.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_187_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex187.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_189_ReturnTrue()
        {
            int treeWidth = 70;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex189.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_191_ReturnTrue()
        {
            int treeWidth = 15;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex191.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_193_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex193.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_195_ReturnTrue()
        {
            int treeWidth = 10;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex195.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_197_ReturnTrue()
        {
            int treeWidth = 15;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex197.gr");
            AssertCorrect(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_199_ReturnTrue()
        {
            int treeWidth = 9;
            ImmutableGraph g = new ImmutableGraph("Test Data\\2017\\ex199.gr");
            AssertCorrect(g, treeWidth);
        }
    }
}
