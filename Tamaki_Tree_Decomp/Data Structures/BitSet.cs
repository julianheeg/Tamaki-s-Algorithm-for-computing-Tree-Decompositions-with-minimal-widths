using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    /// <summary>
    /// a class for representing sets whose elements are "small" non-negative integers
    /// </summary>
    public class BitSet : IEquatable<BitSet>
    {
        private uint[] bytes;

        #region constructors and copy functions

        /// <summary>
        /// constructs a BitSet of the given length
        /// </summary>
        /// <param name="length">the number of entries</param>
        public BitSet(int length)
        {
            bytes = new uint[(length + 31) / 32];
        }

        /// <summary>
        /// constructs a BitSet of the given length and sets the bits given by the indices
        /// </summary>
        /// <param name="length">the number of entries</param>
        /// <param name="indices">the indices of the set bits</param>
        public BitSet(int length, int[] indices)
        {
            bytes = new uint[(length + 31) / 32];
            for (int i = 0; i < indices.Length; i++)
            {
                this[indices[i]] = true;
            }
        }

        /// <summary>
        /// constructs a BitSet of the given length and sets the bits given by the indices
        /// </summary>
        /// <param name="length">the number of entries</param>
        /// <param name="indices">the indices of the set bits</param>
        public BitSet(int length, List<int> indices)
        {
            bytes = new uint[(length + 31) / 32];
            for (int i = 0; i < indices.Count; i++)
            {
                this[indices[i]] = true;
            }
        }

        /// <summary>
        /// constructs a BitSet that is a copy of another BitSet
        /// </summary>
        /// <param name="from">the bit set from where to copy</param>
        public BitSet(BitSet from)
        {
            bytes = new uint[from.bytes.Length];
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                bytes[i] = from.bytes[i];
            }
        }

        /// <summary>
        /// overwrites the contents of this bit set with the contents of another bit set.
        /// You can use this if there is an already allocated BitSet that you can reuse, in order to reduce garbage.
        /// </summary>
        /// <param name="from">the bit set to copy</param>
        public void CopyFrom(BitSet from)
        {
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                bytes[i] = from.bytes[i];
            }
        }

        /// <summary>
        /// makes a bit set where all elements are set
        /// </summary>
        /// <param name="length">the length of the bit set</param>
        /// <returns>a bit set that contains all elements</returns>
        public static BitSet All(int length)
        {
            BitSet bitset = new BitSet(length);
            for (int i = 0; i < length; i++)
            {
                bitset[i] = true;
            }
            return bitset;
        }

        /// <summary>
        /// removes all elements from this set
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 0;
            }
        }

        #endregion


        /// <summary>
        /// accesses the bit at position key
        /// </summary>
        /// <param name="key">the position</param>
        /// <returns>true, iff the bit at that position is set</returns>
        public bool this[int key]
        {
            // unsafe because no check for index out of range is being made
            get
            {
                return (bytes[key / 32] & (1 << (key % 32))) != 0;
            }
            // unsafe because no check for index out of range is being made
            set
            {
                // adapted from https://stackoverflow.com/a/47990

                bytes[key / 32] ^= ((value ? 0xFFFFFFFF : 0U) ^ bytes[key / 32]) & (1U << (key % 32));
            }
        }

        /// <summary>
        /// counts the amount of items in this set
        /// </summary>
        /// <returns>the number of items in this set</returns>
        public uint Count()
        {              
            // code taken from http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel
            // it probably doesn't make much sense here to check for equality to 0 since it's so fast anyways and would just introduce branches
            uint count = 0;
            int length = bytes.Length;
            for (int i = 0; i < length; i++)
            {
                uint v = bytes[i];

                v = v - ((v >> 1) & 0x55555555);
                v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
                count += ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
            }
            return count;            
        }


        //TODO: does not seem to be significantly faster
        /// <summary>
        /// checks if the number of elements is smaller than a given value
        /// </summary>
        /// <param name="k">the number of elements to test against</param>
        /// <returns>true, iff there are less elements than the given number</returns>
        public bool IsCountSmallerThan(int k)
        {
            // code taken from http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel
            // it probably doesn't make much sense here to check for equality to 0 since it's so fast anyways and would just introduce branches
            uint count = 0;
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                uint v = bytes[i];
                v = v - ((v >> 1) & 0x55555555);
                v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
                count += ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
                if (count >= k)
                {
                    return false;
                }
            }
            return true;
        }

        // look in the method below for explanation
        static readonly int[] Mod37BitPosition = // map a bit value mod 37 to its position
            {
                32, 0, 1, 26, 2, 23, 27, 0, 3, 16, 24, 30, 28, 11, 0, 13, 4,
                7, 17, 0, 25, 22, 31, 15, 29, 10, 12, 6, 0, 21, 14, 9, 5,
                20, 8, 19, 18
            };

        /// <summary>
        /// returns the position of the first set bit
        /// </summary>
        /// <returns>the position of the first set bit, if there is one, and -1 otherwise</returns>
        public int First()
        {
            int length = bytes.Length;
            int first = length * 32;
            // code taken from http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn
            for (int i = 0; i < length; i++)
            {
                if (bytes[i] != 0)
                {
                    uint v = bytes[i];

                    first = i * 32 + Mod37BitPosition[(-v & v) % 37];
                    break;
                }
            }

            return first;
        }

        // needed for the method below
        [ThreadStatic] static uint currentByte = 0;
        [ThreadStatic] static int currentPos = -1;

        /// <summary>
        /// returns the next element starting from a given element. This method is slightly faster than using the Elements function.
        /// when iterating it can be used as follows:
        ///     int pos = -1;
        ///     while((pos = NextElement(pos, true/false)) != -1) { ... }
        /// </summary>
        /// <param name="pos">the starting element index</param>
        /// <param name="isConsumed">pass true if elements are taken out of this set during an iteration. pass false otherwise</param>
        /// <returns>the position of the next set element if there is one, and -1 otherwise</returns>
        public int NextElement(int pos, bool isConsumed = false)
        {
            for (int i = pos / 32; i < bytes.Length; i++)
            {
                if (pos == -1 || currentPos / 32 < i || isConsumed)
                {
                    currentByte = bytes[i];
                }
                if (currentByte != 0)
                {
                    int first = i * 32 + Mod37BitPosition[(-currentByte & currentByte) % 37];
                    currentByte &= ~(1U << first);
                    currentPos = first;
                    return first;
                }
            }
            return -1;
        }

        /// <summary>
        /// returns a list of the indices of the set bits. The NextElement method is slightly faster, but not as intuitive to use. 
        /// </summary>
        /// <returns>that list</returns>
        public List<int> Elements()
        {
            List<int> setBits = new List<int>();

            // iterate over bytes
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                // if byte not 0
                if (bytes[i] != 0)
                {
                    // iterate over bits
                    for (int j = 0; j < 32; j++)
                    {
                        // append index if bit not 0
                        if ((bytes[i] & (1U << j)) != 0)
                        {
                            setBits.Add(32 * i + j);
                        }
                    }
                }
            }
            return setBits;
        }

        #region set operations

        /// <summary>
        /// tests if this set is a superset of the other set
        /// </summary>
        /// <param name="subset">the supposed subset</param>
        /// <returns>true, iff this set is a superset of the subset</returns>
        public bool IsSupersetOf(BitSet subset)
        {
            bool result = true;
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                result &= (bytes[i] | subset.bytes[i]) == bytes[i];
            }
            return result;
        }

        /// <summary>
        /// tests if this set is equal to the other set
        /// </summary>
        /// <param name="other">the other set</param>
        /// <returns>true iff both sets contain the same elements</returns>
        public bool Equals(BitSet other)
        {
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                if ((bytes[i]) != (other.bytes[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// alters this bit set so that it represents the union of this bit set with the other bit set
        /// </summary>
        /// <param name="other">the bit set to union with</param>
        public void UnionWith(BitSet other)
        {
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                bytes[i] |= other.bytes[i];
            }
        }

        /// <summary>
        /// alters this bit set so that it represents the intersection of this bit set with the other bit set
        /// </summary>
        /// <param name="other">the bit set to intersect with</param>
        public void IntersectWith(BitSet other)
        {
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                bytes[i] &= other.bytes[i];
            }
        }

        /// <summary>
        /// removes the bits in other from this bit set
        /// </summary>
        /// <param name="other">the elements to remove</param>
        public void ExceptWith(BitSet other)
        {
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                bytes[i] &= ~other.bytes[i];
            }
        }

        /// <summary>
        /// tests if this set is empty
        /// </summary>
        /// <returns>true, iff this set is empty</returns>
        public bool IsEmpty()
        {
            bool result = true;
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                result &= bytes[i] == 0;
            }
            return result;
        }       

        /// <summary>
        /// determines wether this and the other set are disjoint
        /// </summary>
        /// <param name="other">the other set</param>
        /// <returns>true iff both sets are disjoint</returns>
        public bool IsDisjoint(BitSet other)
        {
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                if (((bytes[i]) & (other.bytes[i])) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// constructs a bit set that is the compliment of this one (bits after the maximum size are set). This method is possibly obsolete.
        /// </summary>
        /// <returns>a complimentary bit set</returns>
        public BitSet Complement()
        {
            BitSet complement = new BitSet(this);
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                complement.bytes[i] = ~complement.bytes[i];
            }
            return complement;
        }

        /// <summary>
        /// flips the first "length" bits. Can be used as an in place complement function.
        /// </summary>
        /// <param name="length">the amount of bits to flip</param>
        public void Flip(int length)
        {
            // flip all bits
            int length1 = bytes.Length; // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length1; i++)
            {
                bytes[i] = ~bytes[i];
            }
            // truncate after length
            uint mask = 0xFFFFFFFF >> (32 - length % 32);
            int lastIndex = bytes.Length - 1;
            bytes[lastIndex] &= mask;
        }

        /// <summary>
        /// tests if this set intersects the other set, i.e. there are bits in this set that are also set in the other set
        /// </summary>
        /// <param name="vs2">the other set</param>
        /// <returns>true, iff an element exists that is in both sets</returns>
        public bool Intersects(BitSet vs2)
        {
            return !IsDisjoint(vs2) && !IsEmpty() && !vs2.IsEmpty();
        }

        #endregion

        #region printing

        /// <summary>
        /// prints the set to the console
        /// </summary>
        public void Print()
        {
            StringBuilder sb = new StringBuilder();

            // opening bracket
            sb.Append("{");
            
            // fill in elements
            for (int i = 0; i < bytes.Length * 32; i++)
            {
                if (this[i])
                {
                    sb.Append((i + 1) + ",");
                }
            }

            // remove trailing comma if there is one
            if (sb.Length > 1)
            {
                sb.Length--;
            }

            // closing bracket
            sb.Append("}");
            
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// prints the set to the console, but without surrounding curly brackets. The empty set is printed as "_"
        /// </summary>
        public void Print_NoBrackets()
        {
            StringBuilder sb = new StringBuilder();

            // fill in elements
            for (int i = 0; i < bytes.Length * 32; i++)
            {
                if (this[i])
                {
                    sb.Append((i + 1) + ",");
                }
            }

            // remove trailing comma if there is one
            if (sb.Length > 1)
            {
                sb.Length--;
            }

            // show a placeholder for the empty set
            if (sb.Length == 0)
            {
                sb.Append("_");
            }

            Console.WriteLine(sb.ToString());
        }

        public static bool plusOneInString = false;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            // fill in elements
            for (int i = 0; i < bytes.Length * 32; i++)
            {
                if (this[i])
                {
                    sb.Append((i + (plusOneInString ? 1 : 0)) + ",");
                }
            }

            // remove trailing comma if there is one
            if (sb.Length > 1)
            {
                sb.Length--;
            }

            // show a placeholder for the empty set
            if (sb.Length == 0)
            {
                sb.Append("_");
            }

            return sb.ToString();
        }

        #endregion

        #region technical stuff needed for inclusion in hash based data structures

        /// <summary>
        /// assigns a hash code to this bit set.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // TODO: cache hash code?
            uint hashCode = 0;
            int length = bytes.Length;  // extracting the length seems to prevent repeated lookups of the array length
            for (int i = 0; i < length; i++)
            {
                hashCode ^= bytes[i];
            }
            return unchecked((int)hashCode);
        }

        /// <summary>
        /// checks for equality (override from object class for hash set)
        /// </summary>
        /// <param name="obj">the other object</param>
        /// <returns>true iff both objects contain the same items</returns>
        public override bool Equals(object obj)
        {
            BitSet other = obj as BitSet;
            Debug.Assert(other != null);
            return this.Equals(other);
        }

        #endregion
    }
}
