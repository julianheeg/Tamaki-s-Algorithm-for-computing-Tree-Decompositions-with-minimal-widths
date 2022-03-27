# Tamaki-s-Algorithm-for-computing-Tree-Decompositions-with-minimal-widths

# Description

This project is an implementation of the reinterpretation of [Tamaki's algorithm for calculating treewidths](https://drops.dagstuhl.de/opus/volltexte/2017/7880/) presented in our paper [On Tamaki’s Algorithm to Compute Treewidths](https://drops.dagstuhl.de/opus/frontdoor.php?source_opus=13781).
Given an input graph, it calculates the treewidth and outputs a minimal tree decomposition (also called junction trees or clique trees). They are useful as an intermediate step in a number of graph applications. Besides well-known NP-hard graph problems like Clique, Travelling Salesman, and Hamiltonian Circuit, these include probabilistic inference algorithms on Bayesian networks or Markov random fields and matrix decomposition.
Tamaki's implementation can be found [here](https://github.com/TCS-Meiji/PACE2017-TrackA).

# Input and output

The program expects the path to a .gr file as a command line argument.  It outputs the tree decomposition as a .td file to the command line. The file formats are described in detail on the [website for the PACE 2017 challenge](https://pacechallenge.org/2017/treewidth/) for which Tamaki's originally constructed his algorithm.
