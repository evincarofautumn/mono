//
// System.String.cs
//
// Authors:
//   Patrik Torstensson
//   Jeffrey Stedfast (fejj@ximian.com)
//   Dan Lewis (dihlewis@yahoo.co.uk)
//   Sebastien Pouliot  <sebastien@ximian.com>
//   Marek Safar (marek.safar@seznam.cz)
//   Andreas Nahr (Classdevelopment@A-SoftTech.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004-2005 Novell (http://www.novell.com)
// Copyright (c) 2012 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics.Contracts;

namespace System
{
	partial class String
	{
        public int Length {
            get { return (int)(m_taggedStringLength >> 1); }
        }

        internal bool IsCompact {
            get { return (m_taggedStringLength & 1) != 0; }
        }

#if false

        // Joins an array of strings together as one string with a separator between each original string.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static String Join(String separator, String[] value, int startIndex, int count) {
            //Range check the array
            if (value == null)
                throw new ArgumentNullException("value");

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

            if (startIndex > value.Length - count)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            //Treat null as empty string.
            if (separator == null) {
                separator = String.Empty;
            }

            //If count is 0, that skews a whole bunch of the calculations below, so just special case that.
            if (count == 0) {
                return String.Empty;
            }
            
            int jointLength = 0;
            //Figure out the total length of the strings in value
            int endIndex = startIndex + count - 1;
            for (int stringToJoinIndex = startIndex; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
                if (value[stringToJoinIndex] != null) {
                    jointLength += value[stringToJoinIndex].Length;
                }
            }
            
            //Add enough room for the separator.
            jointLength += (count - 1) * separator.Length;

            // Note that we may not catch all overflows with this check (since we could have wrapped around the 4gb range any number of times
            // and landed back in the positive range.) The input array might be modifed from other threads, 
            // so we have to do an overflow check before each append below anyway. Those overflows will get caught down there.
            if ((jointLength < 0) || ((jointLength + 1) < 0) ) {
                throw new OutOfMemoryException();
            }

            //If this is an empty string, just return.
            if (jointLength == 0) {
                return String.Empty;
            }

            string jointString = FastAllocateString( jointLength );
			/* ASSUMES FastAllocateString returns a UTF-16 buffer. */
            fixed (byte* pointerToJointStringByte = &jointString.m_firstByte) {
				char* pointerToJointString = (char*)pointerToJointStringByte;
                UnSafeCharBuffer charBuffer = new UnSafeCharBuffer( pointerToJointString, jointLength);                
                
                // Append the first string first and then append each following string prefixed by the separator.
                charBuffer.AppendString( value[startIndex] );
                for (int stringToJoinIndex = startIndex + 1; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
                    charBuffer.AppendString( separator );
                    charBuffer.AppendString( value[stringToJoinIndex] );
                }
                Contract.Assert(*(pointerToJointString + charBuffer.Length) == '\0', "String must be null-terminated!");
            }

            return jointString;
        }

		private unsafe static int CompareOrdinalIgnoreCaseHelper(String strA, String strB) {
			throw new NotImplementedException ("CompareOrdinalIgnoreCaseHelper");
		}

		internal unsafe static string SmallCharToUpper(string strIn) {
			throw new NotImplementedException ("SmallCharToUpper");
		}

		private unsafe static bool EqualsHelper(String strA, String strB) {
            Contract.Requires(strA != null);
            Contract.Requires(strB != null);
            Contract.Requires(strA.Length == strB.Length);

			if (strA.IsCompact && strB.IsCompact) {
				throw new NotImplementedException ("EqualsHelper");
			} else if (strA.IsCompact) {
				throw new NotImplementedException ("EqualsHelper");
			} else if (strB.IsCompact) {
				throw new NotImplementedException ("EqualsHelper");
			} else {
				int length = strA.Length;

				fixed (byte* apByte = &strA.m_firstByte, bpByte = &strB.m_firstByte)
				{
					char* ap = (char*)apByte;
					char* bp = (char*)bpByte;
					char* a = ap;
					char* b = bp;

					// unroll the loop
#if AMD64
					// for AMD64 bit platform we unroll by 12 and
					// check 3 qword at a time. This is less code
					// than the 32 bit case and is shorter
					// pathlength

					while (length >= 12)
					{
						if (*(long*)a     != *(long*)b) return false;
						if (*(long*)(a+4) != *(long*)(b+4)) return false;
						if (*(long*)(a+8) != *(long*)(b+8)) return false;
						a += 12; b += 12; length -= 12;
					}
#else
					while (length >= 10)
					{
						if (*(int*)a != *(int*)b) return false;
						if (*(int*)(a+2) != *(int*)(b+2)) return false;
						if (*(int*)(a+4) != *(int*)(b+4)) return false;
						if (*(int*)(a+6) != *(int*)(b+6)) return false;
						if (*(int*)(a+8) != *(int*)(b+8)) return false;
						a += 10; b += 10; length -= 10;
					}
#endif

					// This depends on the fact that the String objects are
					// always zero terminated and that the terminating zero is not included
					// in the length. For odd string sizes, the last compare will include
					// the zero terminator.
					while (length > 0) 
					{
						if (*(int*)a != *(int*)b) break;
						a += 2; b += 2; length -= 2;
					}

					return (length <= 0);
				}

			}
		}

		private unsafe static int CompareOrdinalHelper(String strA, String strB) {
			throw new NotImplementedException ("CompareOrdinalHelper");
		}

		unsafe public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count > Length - sourceIndex)
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            if (destinationIndex > destination.Length - count || destinationIndex < 0)
                throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            Contract.EndContractBlock();

            // Note: fixed does not like empty arrays
            if (count > 0) {
				if (IsCompact) {
					throw new NotImplementedException ("CopyTo");
				} else {
					fixed (byte* srcByte = &this.m_firstByte)
						fixed (char* dest = destination)
                        	wstrcpy(dest + destinationIndex, (char*)srcByte + sourceIndex, count);
				}
            }
		}

		unsafe public char[] ToCharArray() {
			int length = Length;
			char[] chars = new char [length];
			if (length > 0) {
				if (IsCompact) {
					throw new NotImplementedException ("ToCharArray");
				} else {
					fixed (char* dest = chars)
					fixed (byte* srcByte = &this.m_firstByte) {
						char* src = (char*)srcByte;
						CharCopy (dest, src, length);
					}
				}
			}
			return chars;
		}

		unsafe public char[] ToCharArray(int startIndex, int length) {
			throw new NotImplementedException ("ToCharArray");
		}

		private unsafe int MakeSeparatorList(char[] separator, ref int[] sepList) {
			throw new NotImplementedException ("MakeSeparatorList");
		}

		private unsafe int MakeSeparatorList(String[] separators, ref int[] sepList, ref int[] lengthList) {
			throw new NotImplementedException ("MakeSeparatorList");
		}

		unsafe string InternalSubString(int startIndex, int length) {
            Contract.Assert( startIndex >= 0 && startIndex <= this.Length, "StartIndex is out of range!");
            Contract.Assert( length >= 0 && startIndex <= this.Length - length, "length is out of range!");            
            String result = FastAllocateString(length);
			/* ASSUMES FastAllocateString returns a UTF-16 buffer. */
            fixed (byte* destByte = &result.m_firstByte) {
				char* dest = (char*)destByte;
				if (IsCompact) {
					throw new NotImplementedException ("InternalSubString");
				} else {
					fixed (byte* src = &this.m_firstByte)
						CharCopy (dest, (char*)src + startIndex, length);
				}
			}
            return result;
		}

		unsafe static internal String CreateStringFromEncoding(byte* bytes, int byteLength, Encoding encoding) {
			throw new NotImplementedException ("CreateStringFromEncoding");
		}

		unsafe private static void FillStringChecked(String dest, int destPos, String src) {
			throw new NotImplementedException ("FillStringChecked");
		}

		public static int Compare(String strA, String strB, StringComparison comparisonType)  {
			throw new NotImplementedException ("Compare");
		}

		public static int CompareOrdinal(String strA, String strB) {
			throw new NotImplementedException ("CompareOrdinal");
		}

		public String Insert(int startIndex, String value) {
			throw new NotImplementedException ("Insert");
		}

		public String Remove(int startIndex, int count) {
			throw new NotImplementedException ("Remove");
		}

		unsafe public static String Copy(String str) {
			throw new NotImplementedException ("Copy");
		}

		internal unsafe static void InternalCopy(String src, IntPtr dest,int len) {
			throw new NotImplementedException ("InternalCopy");
		}

#endif

		internal static unsafe int CompareOrdinalUnchecked (String strA, int indexA, int lenA, String strB, int indexB, int lenB)
		{
			if (strA == null) {
				return strB == null ? 0 : -1;
			}
			if (strB == null) {
				return 1;
			}
			int lengthA = Math.Min (lenA, strA.Length - indexA);
			int lengthB = Math.Min (lenB, strB.Length - indexB);

			if (lengthA == lengthB && indexA == indexB && Object.ReferenceEquals (strA, strB))
				return 0;

			fixed (char* aptr = strA, bptr = strB) {
				char* ap = aptr + indexA;
				char* end = ap + Math.Min (lengthA, lengthB);
				char* bp = bptr + indexB;
				while (ap < end) {
					if (*ap != *bp)
						return *ap - *bp;
					ap++;
					bp++;
				}
				return lengthA - lengthB;
			}
		}

		public int IndexOf (char value, int startIndex, int count)
		{
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative and must be< 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "< 0");
			if (startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "startIndex + count > this.Length");

			if ((startIndex == 0 && this.Length == 0) || (startIndex == this.Length) || (count == 0))
				return -1;

			return IndexOfUnchecked (value, startIndex, count);
		}

		internal unsafe int IndexOfUnchecked (char value, int startIndex, int count)
		{
			// It helps JIT compiler to optimize comparison
			int value_32 = (int)value;

			if (IsCompact) {
				throw new NotImplementedException ();
				fixed (byte* startByte = &m_firstByte) {
					/* FIXME: Unroll. */
					for (int i = startIndex; i < startIndex + count; ++i)
						if ((char)startByte [i] == value)
							return i;
					return -1;
				}
			} else {
				fixed (byte* startByte = &m_firstByte) {
					char* start = (char*)startByte;
					char* ptr = start + startIndex;
					char* end_ptr = ptr + (count >> 3 << 3);

					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - start);
						if (ptr[1] == value_32)
							return (int)(ptr - start + 1);
						if (ptr[2] == value_32)
							return (int)(ptr - start + 2);
						if (ptr[3] == value_32)
							return (int)(ptr - start + 3);
						if (ptr[4] == value_32)
							return (int)(ptr - start + 4);
						if (ptr[5] == value_32)
							return (int)(ptr - start + 5);
						if (ptr[6] == value_32)
							return (int)(ptr - start + 6);
						if (ptr[7] == value_32)
							return (int)(ptr - start + 7);

						ptr += 8;
					}

					end_ptr += count & 0x07;
					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - start);

						ptr++;
					}
					return -1;
				}
			}
		}

		internal unsafe int IndexOfUnchecked (string value, int startIndex, int count)
		{
			int valueLen = value.Length;
			if (count < valueLen)
				return -1;

			if (valueLen <= 1) {
				if (valueLen == 1)
					return IndexOfUnchecked (value[0], startIndex, count);
				return startIndex;
			}

			/* FIXME: Avoid ToCharArray. */
			fixed (char* thisptr = ToCharArray (), valueptr = value.ToCharArray ()) {
				char* ap = thisptr + startIndex;
				char* thisEnd = ap + count - valueLen + 1;
				while (ap != thisEnd) {
					if (*ap == *valueptr) {
						for (int i = 1; i < valueLen; i++) {
							if (ap[i] != valueptr[i])
								goto NextVal;
						}
						return (int)(ap - thisptr);
					}
					NextVal:
					ap++;
				}
			}
			return -1;
		}

		public int IndexOfAny (char [] anyOf, int startIndex, int count)
		{
			if (anyOf == null)
				throw new ArgumentNullException ();
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ();
			if (count < 0 || startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "Count cannot be negative, and startIndex + count must be less than Length of the string.");

			return IndexOfAnyUnchecked (anyOf, startIndex, count);
		}		

		unsafe int IndexOfAnyUnchecked (char[] anyOf, int startIndex, int count)
		{
			if (anyOf.Length == 0)
				return -1;

			if (anyOf.Length == 1)
				return IndexOfUnchecked (anyOf[0], startIndex, count);

			fixed (char* any = anyOf) {
				int highest = *any;
				int lowest = *any;

				char* end_any_ptr = any + anyOf.Length;
				char* any_ptr = any;
				while (++any_ptr != end_any_ptr) {
					if (*any_ptr > highest) {
						highest = *any_ptr;
						continue;
					}

					if (*any_ptr < lowest)
						lowest = *any_ptr;
				}

				/* FIXME: Avoid ToCharArray. */
				fixed (char* start = ToCharArray ()) {
					char* ptr = start + startIndex;
					char* end_ptr = ptr + count;

					while (ptr != end_ptr) {
						if (*ptr > highest || *ptr < lowest) {
							ptr++;
							continue;
						}

						if (*ptr == *any)
							return (int)(ptr - start);

						any_ptr = any;
						while (++any_ptr != end_any_ptr) {
							if (*ptr == *any_ptr)
								return (int)(ptr - start);
						}

						ptr++;
					}
				}
			}
			return -1;
		}

		public int LastIndexOf (char value, int startIndex, int count)
		{
			if (this.Length == 0)
				return -1;
 
			// >= for char (> for string)
			if ((startIndex < 0) || (startIndex >= this.Length))
				throw new ArgumentOutOfRangeException ("startIndex", "< 0 || >= this.Length");
			if ((count < 0) || (count > this.Length))
				throw new ArgumentOutOfRangeException ("count", "< 0 || > this.Length");
			if (startIndex - count + 1 < 0)
				throw new ArgumentOutOfRangeException ("startIndex - count + 1 < 0");

			return LastIndexOfUnchecked (value, startIndex, count);
		}

		internal unsafe int LastIndexOfUnchecked (char value, int startIndex, int count)
		{
			// It helps JIT compiler to optimize comparison
			int value_32 = (int)value;

			/* FIXME: Avoid ToCharArray. */
			fixed (char* start = ToCharArray ()) {
				char* ptr = start + startIndex;
				char* end_ptr = ptr - (count >> 3 << 3);

				while (ptr != end_ptr) {
					if (*ptr == value_32)
						return (int)(ptr - start);
					if (ptr[-1] == value_32)
						return (int)(ptr - start) - 1;
					if (ptr[-2] == value_32)
						return (int)(ptr - start) - 2;
					if (ptr[-3] == value_32)
						return (int)(ptr - start) - 3;
					if (ptr[-4] == value_32)
						return (int)(ptr - start) - 4;
					if (ptr[-5] == value_32)
						return (int)(ptr - start) - 5;
					if (ptr[-6] == value_32)
						return (int)(ptr - start) - 6;
					if (ptr[-7] == value_32)
						return (int)(ptr - start) - 7;

					ptr -= 8;
				}

				end_ptr -= count & 0x07;
				while (ptr != end_ptr) {
					if (*ptr == value_32)
						return (int)(ptr - start);

					ptr--;
				}
				return -1;
			}
		}

		public int LastIndexOfAny (char [] anyOf, int startIndex, int count)
		{
			if (anyOf == null) 
				throw new ArgumentNullException ();
			if (this.Length == 0)
				return -1;

			if ((startIndex < 0) || (startIndex >= this.Length))
				throw new ArgumentOutOfRangeException ("startIndex", "< 0 || > this.Length");
			if ((count < 0) || (count > this.Length))
				throw new ArgumentOutOfRangeException ("count", "< 0 || > this.Length");
			if (startIndex - count + 1 < 0)
				throw new ArgumentOutOfRangeException ("startIndex - count + 1 < 0");

			if (this.Length == 0)
				return -1;

			return LastIndexOfAnyUnchecked (anyOf, startIndex, count);
		}

		private unsafe int LastIndexOfAnyUnchecked (char [] anyOf, int startIndex, int count)
		{
			if (anyOf.Length == 1)
				return LastIndexOfUnchecked (anyOf[0], startIndex, count);

			/* FIXME: Avoid ToCharArray. */
			fixed (char* start = ToCharArray (), testStart = anyOf) {
				char* ptr = start + startIndex;
				char* ptrEnd = ptr - count;
				char* test;
				char* testEnd = testStart + anyOf.Length;

				while (ptr != ptrEnd) {
					test = testStart;
					while (test != testEnd) {
						if (*test == *ptr)
							return (int)(ptr - start);
						test++;
					}
					ptr--;
				}
				return -1;
			}
		}

		internal static int nativeCompareOrdinalEx (String strA, int indexA, String strB, int indexB, int count)
		{
			//
			// .net does following checks in unmanaged land only which is quite
			// wrong as it's not always necessary and argument names don't match
			// but we are compatible
			//
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

			if (indexA < 0 || indexA > strA.Length)
				throw new ArgumentOutOfRangeException("indexA", Environment.GetResourceString("ArgumentOutOfRange_Index"));

			if (indexB < 0 || indexB > strB.Length)
				throw new ArgumentOutOfRangeException("indexB", Environment.GetResourceString("ArgumentOutOfRange_Index"));

			return CompareOrdinalUnchecked (strA, indexA, count, strB, indexB, count);
        }

		unsafe String ReplaceInternal (char oldChar, char newChar)
		{
#if !BOOTSTRAP_BASIC			
			if (this.Length == 0 || oldChar == newChar)
				return this;
#endif
			int start_pos = IndexOfUnchecked (oldChar, 0, this.Length);
#if !BOOTSTRAP_BASIC
			if (start_pos == -1)
				return this;
#endif
			if (start_pos < 4)
				start_pos = 0;

			string tmp = FastAllocateString (Length);
			fixed (char* src = ToCharArray ())
			/* ASSUMES FastAllocateString returns a UTF-16 buffer. */
			fixed (byte* destByte = &tmp.m_firstByte) {
				char* dest = (char*)destByte;
				if (start_pos != 0)
					CharCopy (dest, src, start_pos);

				char* end_ptr = dest + Length;
				char* dest_ptr = dest + start_pos;
				char* src_ptr = src + start_pos;

				while (dest_ptr != end_ptr) {
					if (*src_ptr == oldChar)
						*dest_ptr = newChar;
					else
						*dest_ptr = *src_ptr;

					++src_ptr;
					++dest_ptr;
				}
			}
			return tmp;
		}

		internal String ReplaceInternal (String oldValue, String newValue)
		{
			// LAMESPEC: According to MSDN the following method is culture-sensitive but this seems to be incorrect
			// LAMESPEC: Result is undefined if result Length is longer than maximum string Length

			if (oldValue == null)
				throw new ArgumentNullException ("oldValue");

			if (oldValue.Length == 0)
				throw new ArgumentException ("oldValue is the empty string.");

			if (this.Length == 0)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
				return this;
#endif
			if (newValue == null)
				newValue = Empty;

			return ReplaceUnchecked (oldValue, newValue);
		}

		private unsafe String ReplaceUnchecked (String oldValue, String newValue)
		{
			if (oldValue.Length > Length)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
				return this;
#endif

			if (oldValue.Length == 1 && newValue.Length == 1) {
				return Replace (oldValue[0], newValue[0]);
				// ENHANCE: It would be possible to special case oldValue.Length == newValue.Length
				// because the Length of the result would be this.Length and Length calculation unneccesary
			}

			const int maxValue = 200; // Allocate 800 byte maximum
			int* dat = stackalloc int[maxValue];
			/* FIXME: Avoid ToCharArray. */
			fixed (char* source = ToCharArray (), replace = newValue.ToCharArray ()) {
				int i = 0, count = 0;
				while (i < Length) {
					int found = IndexOfUnchecked (oldValue, i, Length - i);
					if (found < 0)
						break;
					else {
						if (count < maxValue)
							dat[count++] = found;
						else
							return ReplaceFallback (oldValue, newValue, maxValue);
					}
					i = found + oldValue.Length;
				}
				if (count == 0)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
				return this;
#endif
				int nlen = 0;
				checked {
					try {
						nlen = this.Length + ((newValue.Length - oldValue.Length) * count);
					} catch (OverflowException) {
						throw new OutOfMemoryException ();
					}
				}
				String tmp = FastAllocateString (nlen);

				int curPos = 0, lastReadPos = 0;
				fixed (char* dest = tmp) {
					for (int j = 0; j < count; j++) {
						int precopy = dat[j] - lastReadPos;
						CharCopy (dest + curPos, source + lastReadPos, precopy);
						curPos += precopy;
						lastReadPos = dat[j] + oldValue.Length;
						CharCopy (dest + curPos, replace, newValue.Length);
						curPos += newValue.Length;
					}
					CharCopy (dest + curPos, source + lastReadPos, Length - lastReadPos);
				}
				return tmp;
			}
		}

		private String ReplaceFallback (String oldValue, String newValue, int testedCount)
		{
			int lengthEstimate = this.Length + ((newValue.Length - oldValue.Length) * testedCount);
			StringBuilder sb = new StringBuilder (lengthEstimate);
			for (int i = 0; i < Length;) {
				int found = IndexOfUnchecked (oldValue, i, Length - i);
				if (found < 0) {
					sb.Append (InternalSubString (i, Length - i));
					break;
				}
				sb.Append (InternalSubString (i, found - i));
				sb.Append (newValue);
				i = found + oldValue.Length;
			}
			return sb.ToString ();

		}

		unsafe String PadHelper (int totalWidth, char paddingChar, bool isRightPadded)
		{
			if (totalWidth < 0)
				throw new ArgumentOutOfRangeException ("totalWidth", "Non-negative number required");
			if (totalWidth <= Length)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else			
				return this;
#endif
			string result = FastAllocateString (totalWidth);

			/* FIXME: Avoid ToCharArray. */
			fixed (char* src = ToCharArray ())
			/* ASSUMES FastAllocateString returns a UTF-16 buffer. */
			fixed (byte *destByte = result) {
				char* dest = (char*)destByte;
				if (isRightPadded) {
					CharCopy (dest, src, Length);
					char *end = dest + totalWidth;
					char *p = dest + Length;
					while (p < end) {
						*p++ = paddingChar;
					}
	 			} else {
					char *p = dest;
					char *end = p + totalWidth - Length;
					while (p < end) {
						*p++ = paddingChar;
					}
					CharCopy (p, src, Length);
				}
			}

			return result;
		}

		internal bool StartsWithOrdinalUnchecked (String value)
		{
#if BOOTSTRAP_BASIC
			throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
			return Length >= value.Length && CompareOrdinalUnchecked (this, 0, value.Length, value, 0, value.Length) == 0;
#endif
		}

		internal unsafe bool IsAscii ()
		{
			if (IsCompact)
				return true;
			fixed (byte* srcByte = &m_firstByte) {
				char* src = (char*)srcByte;
				char* end_ptr = src + Length;
				char* str_ptr = src;

				while (str_ptr != end_ptr) {
					if (*str_ptr >= 0x80)
						return false;

					++str_ptr;
				}
			}

			return true;
		}

		internal bool IsFastSort ()
		{
			return false;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string InternalIsInterned (string str);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string InternalIntern (string str);

		internal static unsafe void CharCopy (char *dest, char *src, int count) {
			// Same rules as for memcpy, but with the premise that 
			// chars can only be aligned to even addresses if their
			// enclosing types are correctly aligned
			if ((((int)(byte*)dest | (int)(byte*)src) & 3) != 0) {
				if (((int)(byte*)dest & 2) != 0 && ((int)(byte*)src & 2) != 0 && count > 0) {
					((short*)dest) [0] = ((short*)src) [0];
					dest++;
					src++;
					count--;
				}
				if ((((int)(byte*)dest | (int)(byte*)src) & 2) != 0) {
					Buffer.memcpy2 ((byte*)dest, (byte*)src, count * 2);
					return;
				}
			}
			Buffer.memcpy4 ((byte*)dest, (byte*)src, count * 2);
		}

		#region Runtime method-to-ir dependencies

		/* helpers used by the runtime as well as above or eslewhere in corlib */
		static unsafe void memset (byte *dest, int val, int len)
		{
			if (len < 8) {
				while (len != 0) {
					*dest = (byte)val;
					++dest;
					--len;
				}
				return;
			}
			if (val != 0) {
				val = val | (val << 8);
				val = val | (val << 16);
			}
			// align to 4
			int rest = (int)dest & 3;
			if (rest != 0) {
				rest = 4 - rest;
				len -= rest;
				do {
					*dest = (byte)val;
					++dest;
					--rest;
				} while (rest != 0);
			}
			while (len >= 16) {
				((int*)dest) [0] = val;
				((int*)dest) [1] = val;
				((int*)dest) [2] = val;
				((int*)dest) [3] = val;
				dest += 16;
				len -= 16;
			}
			while (len >= 4) {
				((int*)dest) [0] = val;
				dest += 4;
				len -= 4;
			}
			// tail bytes
			while (len > 0) {
				*dest = (byte)val;
				dest++;
				len--;
			}
		}

		static unsafe void memcpy (byte *dest, byte *src, int size)
		{
			Buffer.Memcpy (dest, src, size);
		}

		/* Used by the runtime */
		internal static unsafe void bzero (byte *dest, int len) {
			memset (dest, 0, len);
		}

		internal static unsafe void bzero_aligned_1 (byte *dest, int len) {
			((byte*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_2 (byte *dest, int len) {
			((short*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_4 (byte *dest, int len) {
			((int*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_8 (byte *dest, int len) {
			((long*)dest) [0] = 0;
		}

		internal static unsafe void memcpy_aligned_1 (byte *dest, byte *src, int size) {
			((byte*)dest) [0] = ((byte*)src) [0];
		}

		internal static unsafe void memcpy_aligned_2 (byte *dest, byte *src, int size) {
			((short*)dest) [0] = ((short*)src) [0];
		}

		internal static unsafe void memcpy_aligned_4 (byte *dest, byte *src, int size) {
			((int*)dest) [0] = ((int*)src) [0];
		}

		internal static unsafe void memcpy_aligned_8 (byte *dest, byte *src, int size) {
			((long*)dest) [0] = ((long*)src) [0];
		}

		#endregion

		// Certain constructors are redirected to CreateString methods with
		// matching argument list. The this pointer should not be used.

		private unsafe String CreateString (sbyte* value)
		{
			if (value == null)
				return Empty;

			byte* bytes = (byte*) value;
			int length = 0;

			try {
				while (bytes++ [0] != 0)
					length++;
			} catch (NullReferenceException) {
				throw new ArgumentOutOfRangeException ("ptr", "Value does not refer to a valid string.");
			}

			return CreateString (value, 0, length, null);
		}

		unsafe String CreateString (sbyte* value, int startIndex, int length)
		{
			return CreateString (value, startIndex, length, null);
		}

		unsafe string CreateString (char *value)
		{
			return CtorCharPtr (value);
		}

		unsafe string CreateString (char *value, int startIndex, int length)
		{
			return CtorCharPtrStartLength (value, startIndex, length);
		}

		string CreateString (char [] val, int startIndex, int length)
		{
			return CtorCharArrayStartLength (val, startIndex, length);
		}

		string CreateString (char [] val)
		{
			return CtorCharArray (val);
		}

		unsafe string CreateString (char c, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count");
			if (count == 0)
				return Empty;
			string result = FastAllocateString (count);
			/* ASSUMES FastAllocateString returns a UTF-16 buffer. */
			fixed (byte* destByte = &result.m_firstByte) {
				char* dest = (char*)destByte;
				char *p = dest;
				char *end = p + count;
				while (p < end) {
					*p = c;
					p++;
				}
			}
			return result;
		}

		private unsafe String CreateString (sbyte* value, int startIndex, int length, Encoding enc)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", "Non-negative number required.");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "Non-negative number required.");
			if (value + startIndex < value)
				throw new ArgumentOutOfRangeException ("startIndex", "Value, startIndex and length do not refer to a valid string.");

			if (enc == null) {
				if (value == null)
					throw new ArgumentNullException ("value");
				if (length == 0)
					return Empty;

				enc = Encoding.Default;
			}

			byte [] bytes = new byte [length];

			if (length != 0)
				fixed (byte* bytePtr = bytes)
					try {
						if (value == null)
							throw new ArgumentOutOfRangeException ("ptr", "Value, startIndex and length do not refer to a valid string.");
						memcpy (bytePtr, (byte*) (value + startIndex), length);
					} catch (NullReferenceException) {
						throw new ArgumentOutOfRangeException ("ptr", "Value, startIndex and length do not refer to a valid string.");
					}

			// GetString () is called even when length == 0
			return enc.GetString (bytes);
		}
	}
}