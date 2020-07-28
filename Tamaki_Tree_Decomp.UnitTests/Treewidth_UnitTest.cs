using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tamaki_Tree_Decomp.Data_Structures;

namespace Tamaki_Tree_Decomp.UnitTests
{
    [TestClass]
    public class Treewidth_UnitTest
    {
        // test timeout in milliseconds
        private const int timeout = 1800000;

        /// <summary>
        /// asserts that the treewidth that the algorithm finds is the graph's actual treewidth
        /// </summary>
        /// <param name="g">the graph to test</param>
        /// <param name="treewidth">the graph's actual tree width</param>
        private void RunCompletelyAndAssertCorrectTreewidth(Graph g, int treewidth)
        {
            ImmutableGraph h = new ImmutableGraph(g);   // copy for check if treewidth is correct
            Assert.AreEqual(treewidth, Treewidth.TreeWidth(g, out PTD output));

            output.AssertValidTreeDecomposition(h);
        }

        /// <summary>
        /// asserts that the treewidth that the algorithm finds is the graph's actual treewidth.
        /// Does not run the entire algorithm, but only the two necessary tests for "treewidth - 1" (false) and "treewidth" (true)
        /// </summary>
        /// <param name="g">the graph to test</param>
        /// <param name="treewidth">the graph's actual tree width</param>
        private void AssertCorrectTreewidth(Graph g, int treewidth)
        {
            Graph g2 = new Graph(g);  // copy for second call
            ImmutableGraph h = new ImmutableGraph(g);   // copy for check if treewidth is correct
            Assert.IsFalse(Treewidth.IsTreeWidthAtMost(g, treewidth - 1, out PTD output));
            Assert.IsTrue(Treewidth.IsTreeWidthAtMost(g2, treewidth, out output));
            output.AssertValidTreeDecomposition(h);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_SmallGraphs()
        {
            Graph g = new Graph("Test Data\\test1.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 3);

            g = new Graph("Test Data\\s0_fuzix_clock_settime_clock_settime.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 2);

            g = new Graph("Test Data\\s1_fuzix_clock_settime_clock_settime.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 2);

            g = new Graph("Test Data\\empty.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 0);

            g = new Graph("Test Data\\four_in_a_line.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 1);

            g = new Graph("Test Data\\gr-only.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 1);

            g = new Graph("Test Data\\single-vertex.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 0);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_MediumGraphs()
        {
            Graph g = new Graph("Test Data\\2016\\medium\\NauruGraph.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

            g = new Graph("Test Data\\2016\\medium\\FlowerSnark.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

            g = new Graph("Test Data\\2016\\medium\\DesarguesGraph.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

            g = new Graph("Test Data\\2016\\medium\\GeneralizedPetersenGraph_10_4.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

            g = new Graph("Test Data\\2016\\medium\\HoffmanGraph.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2016_HardGraphs()
        {
            Graph g = new Graph("Test Data\\2016\\hard\\ClebschGraph.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 8);

            g = new Graph("Test Data\\2016\\hard\\contiki_dhcpc_handle_dhcp.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

            g = new Graph("Test Data\\2016\\hard\\DoubleStarSnark.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

            g = new Graph("Test Data\\2016\\hard\\fuzix_vfscanf_vfscanf.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 6);

            g = new Graph("Test Data\\2016\\hard\\McGeeGraph.gr");
            RunCompletelyAndAssertCorrectTreewidth(g, 7);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_001()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex001.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }


        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_003()
        {
            int treeWidth = 44;
            Graph g = new Graph("Test Data\\2017\\ex003.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_005()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex005.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_007()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex007.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_009()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex009.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_011()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex011.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_013()
        {
            int treeWidth = 29;
            Graph g = new Graph("Test Data\\2017\\ex013.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_015()
        {
            int treeWidth = 15;
            Graph g = new Graph("Test Data\\2017\\ex015.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }


        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_017()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex017.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_019()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex019.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_021()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex021.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_023()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex023.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_025()
        {
            int treeWidth = 20;
            Graph g = new Graph("Test Data\\2017\\ex025.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_027()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex027.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_029()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex029.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_031()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex031.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_033()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex033.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_035()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex035.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_037()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex037.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_039()
        {
            int treeWidth = 32;
            Graph g = new Graph("Test Data\\2017\\ex039.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_041()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex041.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_043()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex043.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_045()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex045.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_047()
        {
            int treeWidth = 21;
            Graph g = new Graph("Test Data\\2017\\ex047.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_049()
        {
            int treeWidth = 13;
            Graph g = new Graph("Test Data\\2017\\ex049.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_051()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex051.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_053()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex053.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_055()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex055.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_057()
        {
            int treeWidth = 117;
            Graph g = new Graph("Test Data\\2017\\ex057.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_059()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex059.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_061()
        {
            int treeWidth = 22;
            Graph g = new Graph("Test Data\\2017\\ex061.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_063()
        {
            int treeWidth = 34;
            Graph g = new Graph("Test Data\\2017\\ex063.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_065()
        {
            int treeWidth = 25;
            Graph g = new Graph("Test Data\\2017\\ex065.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_067()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex067.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_069()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex069.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_071()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex071.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_073()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex073.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_075()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex075.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_077()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex077.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_079()
        {
            int treeWidth = 42;
            Graph g = new Graph("Test Data\\2017\\ex079.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_081()
        {
            int treeWidth = 6;
            Graph g = new Graph("Test Data\\2017\\ex081.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_083()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex083.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_085()
        {
            int treeWidth = 8;
            Graph g = new Graph("Test Data\\2017\\ex085.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_087()
        {
            int treeWidth = 47;
            Graph g = new Graph("Test Data\\2017\\ex087.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_089()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex089.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_091()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex091.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_093()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex093.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_095()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex095.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_097()
        {
            int treeWidth = 48;
            Graph g = new Graph("Test Data\\2017\\ex097.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_099()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex099.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_101()
        {
            int treeWidth = 540;
            Graph g = new Graph("Test Data\\2017\\ex101.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_103()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex103.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_105()
        {
            int treeWidth = 540;
            Graph g = new Graph("Test Data\\2017\\ex105.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_107()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex107.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_109()
        {
            int treeWidth = 7;
            Graph g = new Graph("Test Data\\2017\\ex109.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_111()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex111.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_113()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex113.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_115()
        {
            int treeWidth = 908;
            Graph g = new Graph("Test Data\\2017\\ex115.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_117()
        {
            int treeWidth = 13;
            Graph g = new Graph("Test Data\\2017\\ex117.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_119()
        {
            int treeWidth = 23;
            Graph g = new Graph("Test Data\\2017\\ex119.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_121()
        {
            int treeWidth = 34;
            Graph g = new Graph("Test Data\\2017\\ex121.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_123()
        {
            int treeWidth = 35;
            Graph g = new Graph("Test Data\\2017\\ex123.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_125()
        {
            int treeWidth = 70;
            Graph g = new Graph("Test Data\\2017\\ex125.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_127()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex127.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_129()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex129.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_131()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex131.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_133()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex133.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_135()
        {
            int treeWidth = 87;
            Graph g = new Graph("Test Data\\2017\\ex135.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_137()
        {
            int treeWidth = 19;
            Graph g = new Graph("Test Data\\2017\\ex137.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_139()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex139.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_141()
        {
            int treeWidth = 34;
            Graph g = new Graph("Test Data\\2017\\ex141.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_143()
        {
            int treeWidth = 35;
            Graph g = new Graph("Test Data\\2017\\ex143.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_145()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex145.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_147()
        {
            int treeWidth = 16;
            Graph g = new Graph("Test Data\\2017\\ex147.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_149()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex149.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_151()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex151.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_153()
        {
            int treeWidth = 47;
            Graph g = new Graph("Test Data\\2017\\ex153.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_155()
        {
            int treeWidth = 47;
            Graph g = new Graph("Test Data\\2017\\ex155.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_157()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex157.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_159()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex159.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_161()
        {
            int treeWidth = 12;
            Graph g = new Graph("Test Data\\2017\\ex161.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_163()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex163.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_165()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex165.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_167()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex167.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_169()
        {
            int treeWidth = 22;
            Graph g = new Graph("Test Data\\2017\\ex169.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_171()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex171.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_173()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex173.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_175()
        {
            int treeWidth = 17;
            Graph g = new Graph("Test Data\\2017\\ex175.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_177()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex177.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_179()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex179.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_181()
        {
            int treeWidth = 18;
            Graph g = new Graph("Test Data\\2017\\ex181.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_183()
        {
            int treeWidth = 11;
            Graph g = new Graph("Test Data\\2017\\ex183.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_185()
        {
            int treeWidth = 14;
            Graph g = new Graph("Test Data\\2017\\ex185.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_187()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex187.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_189()
        {
            int treeWidth = 70;
            Graph g = new Graph("Test Data\\2017\\ex189.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_191()
        {
            int treeWidth = 15;
            Graph g = new Graph("Test Data\\2017\\ex191.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_193()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex193.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_195()
        {
            int treeWidth = 10;
            Graph g = new Graph("Test Data\\2017\\ex195.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_197()
        {
            int treeWidth = 15;
            Graph g = new Graph("Test Data\\2017\\ex197.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }

        [TestMethod, Timeout(timeout)]
        public void TreeWidth_2017_199()
        {
            int treeWidth = 9;
            Graph g = new Graph("Test Data\\2017\\ex199.gr");
            AssertCorrectTreewidth(g, treeWidth);
        }
    }
}
