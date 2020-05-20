using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tamaki_Tree_Decomp.Data_Structures
{
    public class BitSet : IEquatable<BitSet>
    {
        private int[] bytes;

        /// <summary>
        /// constructs a BitSet of the given length
        /// </summary>
        /// <param name="length">the number of entries</param>
        public BitSet(int length)
        {
            bytes = new int[(length + 31) / 32];
        }

        /// <summary>
        /// constructs a BitSet of the given length and sets the bits given by the indices
        /// </summary>
        /// <param name="length">the number of entries</param>
        /// <param name="indices">the indices of the set bits</param>
        public BitSet(int length, int[] indices)
        {
            bytes = new int[(length + 31) / 32];
            for (int i = 0; i < indices.Length; i++)
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
            bytes = new int[from.bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
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
        /// accesses the bit at position key
        /// </summary>
        /// <param name="key">the position</param>
        /// <returns>true iff the bit at that position is set</returns>
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
                bytes[key / 32] ^= (-(value ? 1 : 0) ^ bytes[key / 32]) & (1 << (key % 32));
            }
        }

        /// <summary>
        /// returns a list of the indices of the set bits
        /// </summary>
        /// <returns>that list</returns>
        public List<int> Elements()
        {
            List<int> setBits = new List<int>(); // TODO: array if capacity is known

            // iterate over bytes
            for (int i = 0; i < bytes.Length; i++)
            {
                // if byte not 0
                if (bytes[i] != 0)
                {
                    // iterate over bits
                    for (int j = 0; j < 32; j++)
                    {
                        // get bit value
                        int k = (bytes[i] & (1 << j));
                        // append index if bit not 0
                        if ((bytes[i] & (1 << j)) != 0)
                        {
                            setBits.Add(32 * i + j);
                        }
                    }
                }
            }
            return setBits;
        }
       
        /// <summary>
        /// tests if this set is a superset of the other set
        /// </summary>
        /// <param name="subset">the supposed subset</param>
        /// <returns>true, iff this set is a superset of the subset</returns>
        public bool IsSuperset(BitSet subset)
        {
            bool result = true;
            for (int i = 0; i < bytes.Length; i++)
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
            for (int i = 0; i < bytes.Length; i++)
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
            for (int i = 0; i < bytes.Length; i++)
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
            for (int i = 0; i < bytes.Length; i++)
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
            for (int i = 0; i < bytes.Length; i++)
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
            for (int i = 0; i < bytes.Length; i++)
            {
                result &= bytes[i] == 0;
            }
            return result;
        }

        /// <summary>
        /// counts the amount of items in this set
        /// </summary>
        /// <returns>the number of items in this set</returns>
        public uint Count()
        {
            // code taken from http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel
            // TODO: it probably doesn't make much sense here to check for equality to 0 since it's so fast anyways and would just introduce branches
            uint count = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                uint v = (uint) bytes[i];
                v = v - ((v >> 1) & 0x55555555);
                v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
                count += ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
            }
            return count;
        }

        static readonly int[] Mod37BitPosition = // map a bit value mod 37 to its position
            {
                32, 0, 1, 26, 2, 23, 27, 0, 3, 16, 24, 30, 28, 11, 0, 13, 4,
                7, 17, 0, 25, 22, 31, 15, 29, 10, 12, 6, 0, 21, 14, 9, 5,
                20, 8, 19, 18
            };

        public int First()
        {
            int first = 32 * bytes.Length;
            // code taken from http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 0)
                {
                    uint v = (uint) bytes[i];
                    
                    /*
                    v |= v >> 1; // first round down to one less than a power of 2 
                    v |= v >> 2;
                    v |= v >> 4;
                    v |= v >> 8;
                    v |= v >> 16;

                    first = i * 32 + MultiplyDeBruijnBitPosition[((uint)((v & -v) * 0x077CB531U)) >> 27];
                    break;
                    */

                    first = i * 32 + Mod37BitPosition[(-v & v) % 37];
                    break;
                }
            }

#if DEBUG
            // TODO: remove if assertion never fails
            int second = 32 * bytes.Length;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 0)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if (this[32 * i + j])
                        {
                            second = i * 32 + j;
                            break;
                        }
                    }
                }
                if (second != 32 * bytes.Length)
                {
                    break;
                }
            }
            Debug.Assert(first == second);
#endif
            return first;
        }

        static uint currentByte = 0;
        static int currentPos = -1;
        public int NextElement(int pos)
        {
            for (int i = pos / 32; i < bytes.Length; i++)
            {
                if (pos == -1 || currentPos / 32 < i)
                {
                    currentByte = (uint) bytes[i];
                }
                if (currentByte != 0)
                {
                    /*
                    v |= v >> 1; // first round down to one less than a power of 2 
                    v |= v >> 2;
                    v |= v >> 4;
                    v |= v >> 8;
                    v |= v >> 16;

                    first = i * 32 + MultiplyDeBruijnBitPosition[((uint)((v & -v) * 0x077CB531U)) >> 27];
                    break;
                    */
                    int first = i * 32 + Mod37BitPosition[(-currentByte & currentByte) % 37];
                    currentByte &= ~(1U << first);
                    currentPos = first;
                    return first;
                }
            }
            return -1;
        }
       

        /// <summary>
        /// determines wether this and the other set are disjoint
        /// </summary>
        /// <param name="other">the other set</param>
        /// <returns>true iff both sets are disjoint</returns>
        public bool IsDisjoint(BitSet other)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                if (((bytes[i]) & (other.bytes[i])) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// constructs a bit set that is the compliment of this one (bits after the maximum size are set)
        /// </summary>
        /// <returns>a complimentary bit set</returns>
        public BitSet Complement()
        {
            BitSet complement = new BitSet(this);
            for (int i = 0; i < bytes.Length; i++)
            {
                complement.bytes[i] = ~complement.bytes[i];
            }
            return complement;
        }

        /// <summary>
        /// tests if this set intersects the other set, i.e. there are bits in this set that are also set in the other set
        /// </summary>
        /// <param name="vs2">the other set</param>
        /// <returns>true, iff an element exists that is in both sets</returns>
        public bool Intersects(BitSet vs2)
        {
            return IsDisjoint(vs2) && !IsEmpty() && !vs2.IsEmpty();
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

        /*
        /// <summary>
        /// tests if the union of both bit sets contains all elements
        /// </summary>
        /// <param name="first">the first bit set</param>
        /// <param name="second">the second bit set</param>
        /// <param name="length">the length of the bit sets</param>
        /// <returns></returns>
        public static bool UnionContainsAll(BitSet first, BitSet second, int length)
        {
            for (int i = 0; i < length / 32; i++)
            {
                if ((first.bytes[i] | second.bytes[i]) != -1)
                {
                    return false;
                }
            }
            if ((first.bytes[(length - 1) / 32] | second.bytes[(length - 1) / 32]) != ~(-1 << (length % 32)))
            {
                // Console.WriteLine("UnionContainsAll method: union is {0}, 32-modulus is {1}. Is it correct to say false?", Convert.ToString((first.bytes[(length - 1) / 32] | second.bytes[(length - 1) / 32]), 2), length % 32);
                return false;
            }
            return true;
        }
        */

        /*
        /// <summary>
        /// adds the vertices in the array to the set
        /// </summary>
        /// <param name="indices">the vertices to add</param>
        public void Add(int[] indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                this[indices[i]] = true;
            }
        }
        */

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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            // fill in elements
            for (int i = 0; i < bytes.Length * 32; i++)
            {
                if (this[i])
                {
                    sb.Append((i+1) + ",");
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

        /// <summary>
        /// assigns a hash code to this bit set.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // TODO: cache hash code?
            int hashCode = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                hashCode ^= bytes[i];
            }
            return hashCode;
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
    }
}
