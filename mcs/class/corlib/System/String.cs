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
//
// This class contains all implementation for culture-insensitive methods.
// Culture-sensitive methods are implemented in the System.Globalization or
// Mono.Globalization namespace.
//
// Ensure that argument checks on methods don't overflow
//

using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Mono.Globalization.Unicode;

using System.Diagnostics.Contracts;

namespace System
{
	[Serializable]
	[ComVisible (true)]
	[StructLayout (LayoutKind.Sequential)]
	public sealed class String : IConvertible, ICloneable, IEnumerable, IComparable, IComparable<String>, IEquatable <String>, IEnumerable<char>
	{
		// Since length is signed, we use the sign bit to denote its encoding:
		// positive lengths denote the normal (UTF-16) encoding, while negative
		// lengths denote the compact (ASCII) encoding.
		[NonSerialized] private UInt32 tagged_length;
		[NonSerialized] internal byte start_byte;

		public static readonly String Empty = "";

		internal static readonly int LOS_limit = GetLOSLimit ();

		/* Keep in sync with MonoInternalEncoding. */
		internal static readonly int ENCODING_UTF16 = 0;
		internal static readonly int ENCODING_ASCII = 1;

		internal static bool LegacyMode {
			get {
				return false;
			}
		}

		private bool IsCompact {
			get {
				return (tagged_length & 1) != 0;
			}
		}

		public static unsafe bool Equals (string a, string b)
		{
			if ((a as object) == (b as object))
				return true;

			if (a == null || b == null)
				return false;

			int length = a.Length;

			if (length != b.Length)
				return false;

			if (a.IsCompact && b.IsCompact) {
				fixed (byte* s1_ = &a.start_byte, s2_ = &b.start_byte) {
					for (int i = 0; i < length; ++i)
						if (s1_ [i] != s2_ [i])
							return false;
					return true;
				}
				/*
				fixed (byte* s1 = &a.start_byte, s2 = &b.start_byte) {
					byte* p1 = s1;
					byte* p2 = s2;
					while (length >= 16) {
						int* i1 = (int*)p1;
						int* i2 = (int*)p2;
						if (i1[0] != i2[0] || i1[1] != i2[1] || i1[2] != i2[2] || i1[3] != i2[3])
							return false;
						p1 += 16;
						p2 += 16;
						length -= 16;
					}
					if (length >= 8) {
						int* i1 = (int*)p1;
						int* i2 = (int*)p2;
						if (i1[0] != i2[0] || i1[1] != i2[1])
							return false;
						p1 += 8;
						p2 += 8;
						length -= 8;
					}
					if (length >= 4) {
						int* i1 = (int*)p1;
						int* i2 = (int*)p2;
						if (i1[0] != i2[0])
							return false;
						p1 += 4;
						p2 += 4;
						length -= 4;
					}
					if (length >= 2) {
						short* i1 = (short*)p1;
						short* i2 = (short*)p2;
						if (i1[0] != i2[0])
							return false;
						p1 += 2;
						p2 += 2;
						length -= 2;
					}
					return length == 0 || *p1 == *p2;
				}
				*/
			} else if (a.IsCompact) {
				fixed (byte* s1 = &a.start_byte, s2_ = &b.start_byte) {
					char* s2 = (char*)s2_;
					/* TODO: Unroll. */
					for (int i = 0; i < length; ++i)
						if ((char)s1 [i] != s2 [i])
							return false;
					return true;
				}
			} else if (b.IsCompact) {
				fixed (byte* s1_ = &a.start_byte, s2 = &b.start_byte) {
					char* s1 = (char*)s1_;
					/* TODO: Unroll. */
					for (int i = 0; i < length; ++i)
						if (s1 [i] != (char)s2 [i])
							return false;
					return true;
				}
			} else {
				fixed (byte* s1_ = &a.start_byte, s2_ = &b.start_byte) {
					char* s1 = (char*)s1_;
					char* s2 = (char*)s2_;
					char* p1 = s1;
					char* p2 = s2;
					while (length >= 8) {
						int* i1 = (int*)p1;
						int* i2 = (int*)p2;
						if (i1[0] != i2[0] || i1[1] != i2[1] || i1[2] != i2[2] || i1[3] != i2[3])
							return false;
						p1 += 8;
						p2 += 8;
						length -= 8;
					}
					if (length >= 4) {
						int* i1 = (int*)p1;
						int* i2 = (int*)p2;
						if (i1[0] != i2[0] || i1[1] != i2[1])
							return false;
						p1 += 4;
						p2 += 4;
						length -= 4;
					}
					if (length >= 2) {
						int* i1 = (int*)p1;
						int* i2 = (int*)p2;
						if (i1[0] != i2[0])
							return false;
						p1 += 2;
						p2 += 2;
						length -= 2;
					}
					return length == 0 || *p1 == *p2;
				}
			}
		}

		public static bool operator == (String a, String b)
		{
			return Equals (a, b);
		}

		public static bool operator != (String a, String b)
		{
			return !Equals (a, b);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public override bool Equals (Object obj)
		{
			return Equals (this, obj as String);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public bool Equals (String value)
		{
			return Equals (this, value);
		}

		[IndexerName ("Chars")]
		public unsafe char this [int index] {
			get {
				if (index < 0 || index >= Length)
					throw new IndexOutOfRangeException ();
				if (IsCompact) {
					fixed (byte* c = &start_byte)
						return (char)c [index];
				} else {
					fixed (byte* c = &start_byte)
						return ((char*)c) [index];
				}
			}
		}

		public Object Clone ()
		{
			return this;
		}

		public TypeCode GetTypeCode ()
		{
			return TypeCode.String;
		}

		public unsafe void CopyTo (int sourceIndex, char[] destination, int destinationIndex, int count)
		{
			if (destination == null)
				throw new ArgumentNullException ("destination");
			if (sourceIndex < 0)
				throw new ArgumentOutOfRangeException ("sourceIndex", "Cannot be negative");
			if (destinationIndex < 0)
				throw new ArgumentOutOfRangeException ("destinationIndex", "Cannot be negative.");
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "Cannot be negative.");
			if (sourceIndex > Length - count)
				throw new ArgumentOutOfRangeException ("sourceIndex", "sourceIndex + count > Length");
			if (destinationIndex > destination.Length - count)
				throw new ArgumentOutOfRangeException ("destinationIndex", "destinationIndex + count > destination.Length");

			fixed (char* dest = destination)
			fixed (byte* src_ = &this.start_byte) {
				if (IsCompact) {
					for (int i = 0; i < count; ++i)
						dest [destinationIndex + i] = (char)src_ [sourceIndex + i];
				} else {
					char* src = (char*)src_;
					CharCopy (dest + destinationIndex, src + sourceIndex, count);
				}
			}
		}

		public char[] ToCharArray ()
		{
			return ToCharArray (0, Length);
		}

		public unsafe char[] ToCharArray (int startIndex, int length)
		{
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "< 0"); 
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", "< 0"); 
			if (startIndex > this.Length - length)
				throw new ArgumentOutOfRangeException ("startIndex", "Must be greater than the length of the string.");
			char[] tmp = new char [length];
			fixed (char* dest = tmp)
			fixed (byte* src_ = &this.start_byte) {
				if (IsCompact) {
					for (int i = 0; i < length; ++i)
						dest [i] = (char)src_ [startIndex + i];
				} else {
					char* src = (char*)src_;
					CharCopy (dest, src + startIndex, length);
				}
			}
			return tmp;
		}

		public String [] Split (params char [] separator)
		{
			return Split (separator, int.MaxValue, 0);
		}

		public String[] Split (char[] separator, int count)
		{
			return Split (separator, count, 0);
		}

		[ComVisible (false)]
		public String[] Split (char[] separator, StringSplitOptions options)
		{
			return Split (separator, Int32.MaxValue, options);
		}

		[ComVisible (false)]
		public String[] Split (char[] separator, int count, StringSplitOptions options)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "Count cannot be less than zero.");
			if ((options != StringSplitOptions.None) && (options != StringSplitOptions.RemoveEmptyEntries))
				throw new ArgumentException ("Illegal enum value: " + options + ".");

			if (Length == 0 && (options & StringSplitOptions.RemoveEmptyEntries) != 0)
				return EmptyArray<string>.Value;

			if (count <= 1) {
				return count == 0 ?
					EmptyArray<string>.Value :
					new String[1] { this };
			}

			return SplitByCharacters (separator, count, options != 0);
		}

		[ComVisible (false)]
		public String[] Split (string[] separator, StringSplitOptions options)
		{
			return Split (separator, Int32.MaxValue, options);
		}

		[ComVisible (false)]
		public String[] Split (string[] separator, int count, StringSplitOptions options)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "Count cannot be less than zero.");
			if ((options != StringSplitOptions.None) && (options != StringSplitOptions.RemoveEmptyEntries))
				throw new ArgumentException ("Illegal enum value: " + options + ".");

			if (count <= 1) {
				return count == 0 ?
					EmptyArray<string>.Value :
					new String[1] { this };
			}

			bool removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;

			if (separator == null || separator.Length == 0)
				return SplitByCharacters (null, count, removeEmpty);

			if (Length == 0 && removeEmpty)
				return EmptyArray<string>.Value;

			List<String> arr = new List<String> ();

			int pos = 0;
			int matchCount = 0;
			while (pos < this.Length) {
				int matchIndex = -1;
				int matchPos = Int32.MaxValue;

				// Find the first position where any of the separators matches
				for (int i = 0; i < separator.Length; ++i) {
					string sep = separator [i];
					if (sep == null || sep.Length == 0)
						continue;

					int match = IndexOfOrdinalUnchecked (sep, pos, Length - pos);
					if (match >= 0 && match < matchPos) {
						matchIndex = i;
						matchPos = match;
					}
				}

				if (matchIndex == -1)
					break;

				if (!(matchPos == pos && removeEmpty)) {
					if (arr.Count == count - 1)
						break;
					arr.Add (this.Substring (pos, matchPos - pos));
				}

				pos = matchPos + separator [matchIndex].Length;

				matchCount ++;
			}

			if (matchCount == 0)
				return new String [] { this };

			// string contained only separators
			if (removeEmpty && matchCount != 0 && pos == this.Length && arr.Count == 0)
				return EmptyArray<string>.Value;

			if (!(removeEmpty && pos == this.Length))
				arr.Add (this.Substring (pos));

			return arr.ToArray ();
		}

		// .NET 2.0 compatibility only

		unsafe string[] SplitByCharacters (char[] sep, int count, bool removeEmpty)
		{
			if (IsCompact)
				throw new NotImplementedException ();

			int[] split_points = null;
			int total_points = 0;
			--count;

			if (sep == null || sep.Length == 0) {
				fixed (byte* src_ = &this.start_byte) {
					char* src = (char*)src_;
					char* src_ptr = src;
					int len = Length;

					while (len > 0) {
						if (char.IsWhiteSpace (*src_ptr++)) {
							if (split_points == null) {
								split_points = new int[8];
							} else if (split_points.Length == total_points) {
								Array.Resize (ref split_points, split_points.Length * 2);
							}

							split_points[total_points++] = Length - len;
							if (total_points == count && !removeEmpty)
								break;
						}
						--len;
					}
				}
			} else {
				fixed (byte* src_ = &this.start_byte) {
					char* src = (char*)src_;
					fixed (char* sep_src = sep) {
						char* src_ptr = src;
						char* sep_ptr_end = sep_src + sep.Length;
						int len = Length;
						while (len > 0) {
							char* sep_ptr = sep_src;
							do {
								if (*sep_ptr++ == *src_ptr) {
									if (split_points == null) {
										split_points = new int[8];
									} else if (split_points.Length == total_points) {
										Array.Resize (ref split_points, split_points.Length * 2);
									}

									split_points[total_points++] = Length - len;
									if (total_points == count && !removeEmpty)
										len = 0;

									break;
								}
							} while (sep_ptr != sep_ptr_end);

							++src_ptr;
							--len;
						}
					}
				}
			}

			if (total_points == 0)
				return new string[] { this };

			var res = new string[Math.Min (total_points, count) + 1];
			int prev_index = 0;
			int i = 0;
			if (!removeEmpty) {
				for (; i < total_points; ++i) {
					var start = split_points[i];
					res[i] = SubstringUnchecked (prev_index, start - prev_index);
					prev_index = start + 1;
				}

				res[i] = SubstringUnchecked (prev_index, Length - prev_index);
			} else {
				int used = 0;
				int length;
				for (; i < total_points; ++i) {
					var start = split_points[i];
					length = start - prev_index;
					if (length != 0) {
						if (used == count)
							break;

						res[used++] = SubstringUnchecked (prev_index, length);
					}

					prev_index = start + 1;
				}

				length = Length - prev_index;
				if (length != 0)
					res[used++] = SubstringUnchecked (prev_index, length);

				if (used != res.Length)
					Array.Resize (ref res, used);
			}

			return res;
		}

		public String Substring (int startIndex)
		{
			if (startIndex == 0)
				return this;
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex");

			return SubstringUnchecked (startIndex, this.Length - startIndex);
		}

		public String Substring (int startIndex, int length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", "Cannot be negative.");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative.");
			if (startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot exceed length of string.");
			if (startIndex > this.Length - length)
				throw new ArgumentOutOfRangeException ("length", "startIndex + length cannot exceed length of string.");
			if (startIndex == 0 && length == this.Length)
				return this;

			return SubstringUnchecked (startIndex, length);
		}

		// This method is used by StringBuilder.ToString() and is expected to
		// always create a new string object (or return String.Empty). 
		internal unsafe String SubstringUnchecked (int startIndex, int length)
		{
			if (length == 0)
				return Empty;

			string tmp = InternalAllocateStr (length, IsCompact ? ENCODING_ASCII : ENCODING_UTF16);
			fixed (byte* dest_ = &tmp.start_byte)
			fixed (byte* src_ = &this.start_byte) {
				if (IsCompact) {
					memcpy (dest_, src_ + startIndex, length);
				} else {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest, src + startIndex, length);
				}
			}
			return tmp;
		}

		public String Trim ()
		{
			if (Length == 0) 
				return Empty;
			int start = FindNotWhiteSpace (0, Length, 1);

			if (start == Length)
				return Empty;

			int end = FindNotWhiteSpace (Length - 1, start, -1);

			int newLength = end - start + 1;
			if (newLength == Length)
				return this;

			return SubstringUnchecked (start, newLength);
		}

		public String Trim (params char[] trimChars)
		{
			if (trimChars == null || trimChars.Length == 0)
				return Trim ();

			if (Length == 0) 
				return Empty;
			int start = FindNotInTable (0, Length, 1, trimChars);

			if (start == Length)
				return Empty;

			int end = FindNotInTable (Length - 1, start, -1, trimChars);

			int newLength = end - start + 1;
			if (newLength == Length)
				return this;

			return SubstringUnchecked (start, newLength);
		}

		public String TrimStart (params char[] trimChars)
		{
			if (Length == 0) 
				return Empty;
			int start;
			if (trimChars == null || trimChars.Length == 0)
				start = FindNotWhiteSpace (0, Length, 1);
			else
				start = FindNotInTable (0, Length, 1, trimChars);

			if (start == 0)
				return this;

			return SubstringUnchecked (start, Length - start);
		}

		public String TrimEnd (params char[] trimChars)
		{
			if (Length == 0) 
				return Empty;
			int end;
			if (trimChars == null || trimChars.Length == 0)
				end = FindNotWhiteSpace (Length - 1, -1, -1);
			else
				end = FindNotInTable (Length - 1, -1, -1, trimChars);

			end++;
			if (end == Length)
				return this;

			return SubstringUnchecked (0, end);
		}

		unsafe int FindNotWhiteSpace (int pos, int target, int change)
		{
			fixed (byte* src_ = &this.start_byte) {
				if (IsCompact) {
					while (pos != target) {
						if (!char.IsWhiteSpace ((char)src_ [pos]))
							return pos;
						pos += change;
					}
				} else {
					char* src = (char*)src_;
					while (pos != target) {
						if (!char.IsWhiteSpace (src [pos]))
							return pos;
						pos += change;
					}
				}
			}
			return pos;
		}

		private unsafe int FindNotInTable (int pos, int target, int change, char[] table)
		{
			fixed (char* tablePtr = table)
			fixed (byte* thisPtr_ = &this.start_byte) {
				if (IsCompact) {
					while (pos != target) {
						char c = (char)thisPtr_ [pos];
						int x = 0;
						while (x < table.Length) {
							if (c == tablePtr [x])
								break;
							++x;
						}
						if (x == table.Length)
							return pos;
						pos += change;
					}
				} else {
					char* thisPtr = (char*)thisPtr_;
					while (pos != target) {
						char c = thisPtr [pos];
						int x = 0;
						while (x < table.Length) {
							if (c == tablePtr [x])
								break;
							++x;
						}
						if (x == table.Length)
							return pos;
						pos += change;
					}
				}
			}
			return pos;
		}

		public static int Compare (String strA, String strB)
		{
			return CultureInfo.CurrentCulture.CompareInfo.Compare (strA, strB, CompareOptions.None);
		}

		public static int Compare (String strA, String strB, bool ignoreCase)
		{
			return CultureInfo.CurrentCulture.CompareInfo.Compare (strA, strB, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
		}

		public static int Compare (String strA, String strB, bool ignoreCase, CultureInfo culture)
		{
			if (culture == null)
				throw new ArgumentNullException ("culture");

			return culture.CompareInfo.Compare (strA, strB, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
		}

		public static int Compare (String strA, int indexA, String strB, int indexB, int length)
		{
			return Compare (strA, indexA, strB, indexB, length, false, CultureInfo.CurrentCulture);
		}

		public static int Compare (String strA, int indexA, String strB, int indexB, int length, bool ignoreCase)
		{
			return Compare (strA, indexA, strB, indexB, length, ignoreCase, CultureInfo.CurrentCulture);
		}
		
		public static int Compare (String strA, int indexA, String strB, int indexB, int length, bool ignoreCase, CultureInfo culture)
		{
			if (culture == null)
				throw new ArgumentNullException ("culture");

			if ((indexA > strA.Length) || (indexB > strB.Length) || (indexA < 0) || (indexB < 0) || (length < 0))
				throw new ArgumentOutOfRangeException ();

			if (length == 0)
				return 0;
			
			if (strA == null) {
				if (strB == null) {
					return 0;
				} else {
					return -1;
				}
			}
			else if (strB == null) {
				return 1;
			}

			CompareOptions compopts;

			if (ignoreCase)
				compopts = CompareOptions.IgnoreCase;
			else
				compopts = CompareOptions.None;

			// Need to cap the requested length to the
			// length of the string, because
			// CompareInfo.Compare will insist that length
			// <= (string.Length - offset)

			int len1 = length;
			int len2 = length;
			
			if (length > (strA.Length - indexA)) {
				len1 = strA.Length - indexA;
			}

			if (length > (strB.Length - indexB)) {
				len2 = strB.Length - indexB;
			}

			// ENHANCE: Might call internal_compare_switch directly instead of doing all checks twice
			return culture.CompareInfo.Compare (strA, indexA, len1, strB, indexB, len2, compopts);
		}

		public static int Compare (string strA, string strB, StringComparison comparisonType)
		{
			switch (comparisonType) {
			case StringComparison.CurrentCulture:
				return Compare (strA, strB, false, CultureInfo.CurrentCulture);
			case StringComparison.CurrentCultureIgnoreCase:
				return Compare (strA, strB, true, CultureInfo.CurrentCulture);
			case StringComparison.InvariantCulture:
				return Compare (strA, strB, false, CultureInfo.InvariantCulture);
			case StringComparison.InvariantCultureIgnoreCase:
				return Compare (strA, strB, true, CultureInfo.InvariantCulture);
			case StringComparison.Ordinal:
				return CompareOrdinalUnchecked (strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
			case StringComparison.OrdinalIgnoreCase:
				return CompareOrdinalCaseInsensitiveUnchecked (strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
			default:
				string msg = Locale.GetText ("Invalid value '{0}' for StringComparison", comparisonType);
				throw new ArgumentException (msg, "comparisonType");
			}
		}

		public static int Compare (string strA, int indexA, string strB, int indexB, int length, StringComparison comparisonType)
		{
			switch (comparisonType) {
			case StringComparison.CurrentCulture:
				return Compare (strA, indexA, strB, indexB, length, false, CultureInfo.CurrentCulture);
			case StringComparison.CurrentCultureIgnoreCase:
				return Compare (strA, indexA, strB, indexB, length, true, CultureInfo.CurrentCulture);
			case StringComparison.InvariantCulture:
				return Compare (strA, indexA, strB, indexB, length, false, CultureInfo.InvariantCulture);
			case StringComparison.InvariantCultureIgnoreCase:
				return Compare (strA, indexA, strB, indexB, length, true, CultureInfo.InvariantCulture);
			case StringComparison.Ordinal:
				return CompareOrdinal (strA, indexA, strB, indexB, length);
			case StringComparison.OrdinalIgnoreCase:
				return CompareOrdinalCaseInsensitive (strA, indexA, strB, indexB, length);
			default:
				string msg = Locale.GetText ("Invalid value '{0}' for StringComparison", comparisonType);
				throw new ArgumentException (msg, "comparisonType");
			}
		}

		public static bool Equals (string a, string b, StringComparison comparisonType)
		{
			return Compare (a, b, comparisonType) == 0;
		}

		public bool Equals (string value, StringComparison comparisonType)
		{
			return Compare (value, this, comparisonType) == 0;
		}

		public static int Compare (string strA, string strB, CultureInfo culture, CompareOptions options)
		{
			if (culture == null)
				throw new ArgumentNullException ("culture");

			return culture.CompareInfo.Compare (strA, strB, options);
		}

		public static int Compare (string strA, int indexA, string strB, int indexB, int length, CultureInfo culture, CompareOptions options)
		{
			if (culture == null)
				throw new ArgumentNullException ("culture");

			int len1 = length;
			int len2 = length;
			
			if (length > (strA.Length - indexA))
				len1 = strA.Length - indexA;

			if (length > (strB.Length - indexB))
				len2 = strB.Length - indexB;

			return culture.CompareInfo.Compare (strA, indexA, len1, strB, indexB, len2, options);
		}

		public int CompareTo (Object value)
		{
			if (value == null)
				return 1;

			if (!(value is String))
				throw new ArgumentException ();

			return String.Compare (this, (String) value);
		}

		public int CompareTo (String strB)
		{
			if (strB == null)
				return 1;

			return Compare (this, strB);
		}

		public static int CompareOrdinal (String strA, String strB)
		{
			return CompareOrdinalUnchecked (strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
		}

		public static int CompareOrdinal (String strA, int indexA, String strB, int indexB, int length)
		{
			if (strA != null && strB != null)
			{
				if (indexA > strA.Length || indexA < 0)
					throw new ArgumentOutOfRangeException ("indexA");
				if (indexB > strB.Length || indexB < 0)
					throw new ArgumentOutOfRangeException ("indexB");
				if (length < 0)
					throw new ArgumentOutOfRangeException ("length");
			}

			return CompareOrdinalUnchecked (strA, indexA, length, strB, indexB, length);
		}

		internal static int CompareOrdinalCaseInsensitive (String strA, int indexA, String strB, int indexB, int length)
		{
			if (strA != null && strB != null)
			{
				if (indexA > strA.Length || indexA < 0)
					throw new ArgumentOutOfRangeException ("indexA");
				if (indexB > strB.Length || indexB < 0)
					throw new ArgumentOutOfRangeException ("indexB");
				if (length < 0)
					throw new ArgumentOutOfRangeException ("length");
			}

			return CompareOrdinalCaseInsensitiveUnchecked (strA, indexA, length, strB, indexB, length);
		}

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

			if (strA.IsCompact && strB.IsCompact) {
				fixed (byte* aptr_ = &strA.start_byte, bptr_ = &strB.start_byte) {
					byte* ap = aptr_ + indexA;
					byte* end = ap + Math.Min (lengthA, lengthB);
					byte* bp = bptr_ + indexB;
					while (ap < end) {
						if (*ap != *bp)
							return *ap - *bp;
						++ap;
						++bp;
					}
					return lengthA - lengthB;
				}
			} else if (strA.IsCompact) {
				fixed (byte* aptr_ = &strA.start_byte, bptr_ = &strB.start_byte) {
					char* bptr = (char*)bptr_;
					byte* ap = aptr_ + indexA;
					byte* end = ap + Math.Min (lengthA, lengthB);
					char* bp = bptr + indexB;
					while (ap < end) {
						if ((char)*ap != *bp)
							return (char)*ap - *bp;
						++ap;
						++bp;
					}
					return lengthA - lengthB;
				}
			} else if (strB.IsCompact) {
				fixed (byte* aptr_ = &strA.start_byte, bptr_ = &strB.start_byte) {
					char* aptr = (char*)aptr_;
					char* ap = aptr + indexA;
					char* end = ap + Math.Min (lengthA, lengthB);
					byte* bp = bptr_ + indexB;
					while (ap < end) {
						if (*ap != (char)*bp)
							return *ap - (char)*bp;
						++ap;
						++bp;
					}
					return lengthA - lengthB;
				}
			} else {
				fixed (byte* aptr_ = &strA.start_byte, bptr_ = &strB.start_byte) {
					char* aptr = (char*)aptr_;
					char* bptr = (char*)bptr_;
					char* ap = aptr + indexA;
					char* end = ap + Math.Min (lengthA, lengthB);
					char* bp = bptr + indexB;
					while (ap < end) {
						if (*ap != *bp)
							return *ap - *bp;
						++ap;
						++bp;
					}
					return lengthA - lengthB;
				}
			}
		}

		//
		// Fastest method for internal case insensitive comparison
		//
		internal static int CompareOrdinalCaseInsensitiveUnchecked (string strA, string strB)
		{
			return CompareOrdinalCaseInsensitiveUnchecked (strA, 0, int.MaxValue, strB, 0, int.MaxValue);
		}

		internal static unsafe int CompareOrdinalCaseInsensitiveUnchecked (String strA, int indexA, int lenA, String strB, int indexB, int lenB)
		{
			if (strA.IsCompact || strB.IsCompact)
				throw new NotImplementedException ();
			// Same as above, but checks versus uppercase characters
			if (strA == null) {
				return strB == null ? 0 : -1;
			}
			if (strB == null) {
				return 1;
			}
			int lengthA = Math.Min (lenA, strA.Length - indexA);
			int lengthB = Math.Min (lenB, strB.Length - indexB);

			if (lengthA == lengthB && Object.ReferenceEquals (strA, strB))
				return 0;

			fixed (byte* aptr_ = &strA.start_byte, bptr_ = &strB.start_byte) {
				char* aptr = (char*)aptr_;
				char* bptr = (char*)bptr_;
				char* ap = aptr + indexA;
				char* end = ap + Math.Min (lengthA, lengthB);
				char* bp = bptr + indexB;
				while (ap < end) {
					if (*ap != *bp) {
						char c1 = Char.ToUpperInvariant (*ap);
						char c2 = Char.ToUpperInvariant (*bp);
						if (c1 != c2)
							return c1 - c2;
					}
					ap++;
					bp++;
				}
				return lengthA - lengthB;
			}
		}

		public bool EndsWith (String value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			return CultureInfo.CurrentCulture.CompareInfo.IsSuffix (this, value, CompareOptions.None);
		}

		public bool EndsWith (String value, bool ignoreCase, CultureInfo culture)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (culture == null)
				culture = CultureInfo.CurrentCulture;

			return culture.CompareInfo.IsSuffix (this, value,
				ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
		}

		// Following methods are culture-insensitive
		public int IndexOfAny (char [] anyOf)
		{
			if (anyOf == null)
				throw new ArgumentNullException ();
			if (this.Length == 0)
				return -1;

			return IndexOfAnyUnchecked (anyOf, 0, this.Length);
		}

		public int IndexOfAny (char [] anyOf, int startIndex)
		{
			if (anyOf == null)
				throw new ArgumentNullException ();
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ();

			return IndexOfAnyUnchecked (anyOf, startIndex, this.Length - startIndex);
		}

		public int IndexOfAny (char [] anyOf, int startIndex, int count)
		{
			if (anyOf == null)
				throw new ArgumentNullException ();
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ();
			if (count < 0 || startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "Count cannot be negative, and startIndex + count must be less than length of the string.");

			return IndexOfAnyUnchecked (anyOf, startIndex, count);
		}

		private unsafe int IndexOfAnyUnchecked (char[] anyOf, int startIndex, int count)
		{
			if (IsCompact)
				throw new NotImplementedException ();

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

				fixed (byte* start_ = &start_byte) {
					char* start = (char*)start_;
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


		public int IndexOf (string value, StringComparison comparisonType)
		{
			return IndexOf (value, 0, this.Length, comparisonType);
		}

		public int IndexOf (string value, int startIndex, StringComparison comparisonType)
		{
			return IndexOf (value, startIndex, this.Length - startIndex, comparisonType);
		}

		public int IndexOf (string value, int startIndex, int count, StringComparison comparisonType)
		{
			switch (comparisonType) {
			case StringComparison.CurrentCulture:
				return CultureInfo.CurrentCulture.CompareInfo.IndexOf (this, value, startIndex, count, CompareOptions.None);
			case StringComparison.CurrentCultureIgnoreCase:
				return CultureInfo.CurrentCulture.CompareInfo.IndexOf (this, value, startIndex, count, CompareOptions.IgnoreCase);
			case StringComparison.InvariantCulture:
				return CultureInfo.InvariantCulture.CompareInfo.IndexOf (this, value, startIndex, count, CompareOptions.None);
			case StringComparison.InvariantCultureIgnoreCase:
				return CultureInfo.InvariantCulture.CompareInfo.IndexOf (this, value, startIndex, count, CompareOptions.IgnoreCase);
			case StringComparison.Ordinal:
				return IndexOfOrdinal (value, startIndex, count, CompareOptions.Ordinal);
			case StringComparison.OrdinalIgnoreCase:
				return IndexOfOrdinal (value, startIndex, count, CompareOptions.OrdinalIgnoreCase);
			default:
				string msg = Locale.GetText ("Invalid value '{0}' for StringComparison", comparisonType);
				throw new ArgumentException (msg, "comparisonType");
			}
		}

		internal int IndexOfOrdinal (string value, int startIndex, int count, CompareOptions options)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex");
			if (count < 0 || (this.Length - startIndex) < count)
				throw new ArgumentOutOfRangeException ("count");

			if (options == CompareOptions.Ordinal)
				return IndexOfOrdinalUnchecked (value, startIndex, count);
			return IndexOfOrdinalIgnoreCaseUnchecked (value, startIndex, count);
		}

		internal unsafe int IndexOfOrdinalUnchecked (string value)
		{
			return IndexOfOrdinalUnchecked (value, 0, Length);
		}

		internal unsafe int IndexOfOrdinalUnchecked (string value, int startIndex, int count)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			int valueLen = value.Length;
			if (count < valueLen)
				return -1;

			if (valueLen <= 1) {
				if (valueLen == 1)
					return IndexOfUnchecked (value[0], startIndex, count);
				return startIndex;
			}

			fixed (byte* thisptr_ = &this.start_byte, valueptr_ = &value.start_byte) {
				char* thisptr = (char*)thisptr_;
				char* valueptr = (char*)valueptr_;
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

		internal unsafe int IndexOfOrdinalIgnoreCaseUnchecked (string value, int startIndex, int count)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			int valueLen = value.Length;
			if (count < valueLen)
				return -1;

			if (valueLen == 0)
				return startIndex;

			fixed (byte* thisptr_ = &this.start_byte, valueptr_ = &value.start_byte) {
				char* thisptr = (char*)thisptr_;
				char* valueptr = (char*)valueptr_;
				char* ap = thisptr + startIndex;
				char* thisEnd = ap + count - valueLen + 1;
				while (ap != thisEnd) {
					for (int i = 0; i < valueLen; i++) {
						if (Char.ToUpperInvariant (ap[i]) != Char.ToUpperInvariant (valueptr[i]))
							goto NextVal;
					}
					return (int)(ap - thisptr);
					NextVal:
					ap++;
				}
			}
			return -1;
		}

		public int LastIndexOf (string value, StringComparison comparisonType)
		{
			if (this.Length == 0)
				return value.Length == 0 ? 0 : -1;
			else
				return LastIndexOf (value, this.Length - 1, this.Length, comparisonType);
		}

		public int LastIndexOf (string value, int startIndex, StringComparison comparisonType)
		{
			return LastIndexOf (value, startIndex, startIndex + 1, comparisonType);
		}

		public int LastIndexOf (string value, int startIndex, int count, StringComparison comparisonType)
		{
			switch (comparisonType) {
			case StringComparison.CurrentCulture:
				return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf (this, value, startIndex, count, CompareOptions.None);
			case StringComparison.CurrentCultureIgnoreCase:
				return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf (this, value, startIndex, count, CompareOptions.IgnoreCase);
			case StringComparison.InvariantCulture:
				return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf (this, value, startIndex, count, CompareOptions.None);
			case StringComparison.InvariantCultureIgnoreCase:
				return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf (this, value, startIndex, count, CompareOptions.IgnoreCase);
			case StringComparison.Ordinal:
				return LastIndexOfOrdinal (value, startIndex, count, CompareOptions.Ordinal);
			case StringComparison.OrdinalIgnoreCase:
				return LastIndexOfOrdinal (value, startIndex, count, CompareOptions.OrdinalIgnoreCase);
			default:
				string msg = Locale.GetText ("Invalid value '{0}' for StringComparison", comparisonType);
				throw new ArgumentException (msg, "comparisonType");
			}
		}

		internal int LastIndexOfOrdinal (string value, int startIndex, int count, CompareOptions options)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (this.Length == 0)
				return value.Length == 0 ? 0 : -1;
			if (value.Length == 0)
				return Math.Min (this.Length - 1, startIndex);
			if (startIndex < 0 || startIndex > Length)
				throw new ArgumentOutOfRangeException ("startIndex");
			if (count < 0 || (startIndex < count - 1))
				throw new ArgumentOutOfRangeException ("count");

			if (options == CompareOptions.Ordinal)
				return LastIndexOfOrdinalUnchecked (value, startIndex, count);
			return LastIndexOfOrdinalIgnoreCaseUnchecked (value, startIndex, count);
		}

		internal unsafe int LastIndexOfOrdinalUnchecked (string value, int startIndex, int count)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			int valueLen = value.Length;
			if (count < valueLen)
				return -1;

			if (valueLen <= 1) {
				if (valueLen == 1)
					return LastIndexOfUnchecked (value[0], startIndex, count);
				return startIndex;
			}

			fixed (byte* thisptr_ = &this.start_byte, valueptr_ = &value.start_byte) {
				char* thisptr = (char*)thisptr_;
				char* valueptr = (char*)valueptr_;
				char* ap = thisptr + startIndex - valueLen + 1;
				char* thisEnd = ap - count + valueLen - 1;
				while (ap != thisEnd) {
					if (*ap == *valueptr) {
						for (int i = 1; i < valueLen; i++) {
							if (ap[i] != valueptr[i])
								goto NextVal;
						}
						return (int)(ap - thisptr);
					}
					NextVal:
					ap--;
				}
			}
			return -1;
		}

		internal unsafe int LastIndexOfOrdinalIgnoreCaseUnchecked (string value, int startIndex, int count)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			int valueLen = value.Length;
			if (count < valueLen)
				return -1;

			if (valueLen == 0)
				return startIndex;

			fixed (byte* thisptr_ = &this.start_byte, valueptr_ = &value.start_byte) {
				char* thisptr = (char*)thisptr_;
				char* valueptr = (char*)valueptr_;
				char* ap = thisptr + startIndex - valueLen + 1;
				char* thisEnd = ap - count + valueLen - 1;
				while (ap != thisEnd) {
					for (int i = 0; i < valueLen; i++) {
						if (Char.ToUpperInvariant (ap[i]) != Char.ToUpperInvariant (valueptr[i]))
							goto NextVal;
					}
					return (int)(ap - thisptr);
					NextVal:
					ap--;
				}
			}
			return -1;
		}

		// Following methods are culture-insensitive
		public int IndexOf (char value)
		{
			if (this.Length == 0)
				return -1;

			return IndexOfUnchecked (value, 0, this.Length);
		}

		public int IndexOf (char value, int startIndex)
		{
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "< 0");
			if (startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "startIndex > this.length");

			if ((startIndex == 0 && this.Length == 0) || (startIndex == this.Length))
				return -1;

			return IndexOfUnchecked (value, startIndex, this.Length - startIndex);
		}

		public int IndexOf (char value, int startIndex, int count)
		{
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative and must be< 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "< 0");
			if (startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "startIndex + count > this.length");

			if ((startIndex == 0 && this.Length == 0) || (startIndex == this.Length) || (count == 0))
				return -1;

			return IndexOfUnchecked (value, startIndex, count);
		}

		internal unsafe int IndexOfUnchecked (char value, int startIndex, int count)
		{
			// It helps JIT compiler to optimize comparison
			int value_32 = (int)value;

			fixed (byte* start_ = &start_byte) {
				if (IsCompact) {
					/* TODO: Unroll. */
					for (int i = 0; i < count; ++i)
						if ((char)start_ [startIndex + i] == value)
							return startIndex + i;
					return -1;
				} else {
					char* start = (char*)start_;
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

		internal unsafe int IndexOfOrdinalIgnoreCase (char value, int startIndex, int count)
		{
			if (Length == 0)
				return -1;
			int end = startIndex + count;
			char c = Char.ToUpperInvariant (value);
			fixed (byte* s_ = &start_byte) {
				if (IsCompact) {
					for (int i = startIndex; i < end; ++i)
						if (Char.ToUpperInvariant ((char)s_ [i]) == c)
							return i;
				} else {
					char* s = (char*)s_;
					for (int i = startIndex; i < end; ++i)
						if (Char.ToUpperInvariant (s [i]) == c)
							return i;
				}
			}
			return -1;
		}

		// Following methods are culture-sensitive
		public int IndexOf (String value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (value.Length == 0)
				return 0;
			if (this.Length == 0)
				return -1;
			return CultureInfo.CurrentCulture.CompareInfo.IndexOf (this, value, 0, this.Length, CompareOptions.None);
		}

		public int IndexOf (String value, int startIndex)
		{
			return IndexOf (value, startIndex, this.Length - startIndex);
		}

		public int IndexOf (String value, int startIndex, int count)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative, and should not exceed length of string.");
			if (count < 0 || startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "Cannot be negative, and should point to location in string.");

			if (value.Length == 0)
				return startIndex;

			if (startIndex == 0 && this.Length == 0)
				return -1;

			if (count == 0)
				return -1;

			return CultureInfo.CurrentCulture.CompareInfo.IndexOf (this, value, startIndex, count);
		}

		// Following methods are culture-insensitive
		public int LastIndexOfAny (char [] anyOf)
		{
			if (anyOf == null)
				throw new ArgumentNullException ();
			if (this.Length == 0)
				return -1;

			return LastIndexOfAnyUnchecked (anyOf, this.Length - 1, this.Length);
		}

		public int LastIndexOfAny (char [] anyOf, int startIndex)
		{
			if (anyOf == null)
				throw new ArgumentNullException ();
			if (this.Length == 0)
				return -1;

			if (startIndex < 0 || startIndex >= this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative, and should be less than length of string.");

			if (this.Length == 0)
				return -1;

			return LastIndexOfAnyUnchecked (anyOf, startIndex, startIndex + 1);
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

			fixed (byte* start_ = &this.start_byte)
			fixed (char* testStart_ = anyOf) {
				char* test;
				char* testStart = (char*)testStart_;
				char* testEnd = testStart + anyOf.Length;
				if (IsCompact) {
					byte* ptr = start_ + startIndex;
					byte* ptrEnd = ptr - count;
					while (ptr != ptrEnd) {
						test = testStart;
						while (test != testEnd) {
							if (*test == (char)*ptr)
								return (int)(ptr - start_);
							test++;
						}
						--ptr;
					}
					return -1;
				} else {
					char* start = (char*)start_;
					char* ptr = start + startIndex;
					char* ptrEnd = ptr - count;
					while (ptr != ptrEnd) {
						test = testStart;
						while (test != testEnd) {
							if (*test == *ptr)
								return (int)(ptr - start);
							test++;
						}
						--ptr;
					}
					return -1;
				}
			}
		}

		// Following methods are culture-insensitive
		public int LastIndexOf (char value)
		{
			if (this.Length == 0)
				return -1;
			
			return LastIndexOfUnchecked (value, this.Length - 1, this.Length);
		}

		public int LastIndexOf (char value, int startIndex)
		{
			return LastIndexOf (value, startIndex, startIndex + 1);
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
			if (IsCompact)
				throw new NotImplementedException ();

			// It helps JIT compiler to optimize comparison
			int value_32 = (int)value;

			fixed (byte* start_ = &start_byte) {
				char* start = (char*)start_;
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

		internal unsafe int LastIndexOfOrdinalIgnoreCase (char value, int startIndex, int count)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			if (this.Length == 0)
				return -1;
			int end = startIndex - count;
			char c = Char.ToUpperInvariant (value);
			fixed (byte* s_ = &start_byte) {
				char* s = (char*)s_;
				for (int i = startIndex; i > end; i--)
					if (Char.ToUpperInvariant (s [i]) == c)
						return i;
			}
			return -1;
		}

		// Following methods are culture-sensitive
		public int LastIndexOf (String value)
		{
			return LastIndexOf (value, this.Length - 1, this.Length);
		}

		public int LastIndexOf (String value, int startIndex)
		{
			int max = startIndex;
			if (max < this.Length)
				max++;
			return LastIndexOf (value, startIndex, max);
		}

		public int LastIndexOf (String value, int startIndex, int count)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			if (this.Length == 0)
				return value.Length == 0 ? 0 : -1;
			// -1 > startIndex > for string (0 > startIndex >= for char)
			if ((startIndex < -1) || (startIndex > this.Length))
				throw new ArgumentOutOfRangeException ("startIndex", "< 0 || > this.Length");
			if ((count < 0) || (count > this.Length))
				throw new ArgumentOutOfRangeException ("count", "< 0 || > this.Length");
			if (startIndex - count + 1 < 0)
				throw new ArgumentOutOfRangeException ("startIndex - count + 1 < 0");

			if (value.Length == 0)
				return Math.Min (this.Length - 1, startIndex);

			if (startIndex == 0 && this.Length == 0)
				return -1;

			// This check is needed to match undocumented MS behaviour
			if (this.Length == 0 && value.Length > 0)
				return -1;

			if (count == 0)
				return -1;

			if (startIndex == this.Length)
				startIndex--;
			return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf (this, value, startIndex, count);
		}

		public bool Contains (String value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			return IndexOfOrdinalUnchecked (value, 0, Length) != -1;
		}

		public static bool IsNullOrEmpty (String value)
		{
			return (value == null) || (value.Length == 0);
		}

		public string Normalize ()
		{
			return Normalization.Normalize (this, 0);
		}

		public string Normalize (NormalizationForm normalizationForm)
		{
			switch (normalizationForm) {
			default:
				return Normalization.Normalize (this, 0);
			case NormalizationForm.FormD:
				return Normalization.Normalize (this, 1);
			case NormalizationForm.FormKC:
				return Normalization.Normalize (this, 2);
			case NormalizationForm.FormKD:
				return Normalization.Normalize (this, 3);
			}
		}

		public bool IsNormalized ()
		{
			return Normalization.IsNormalized (this, 0);
		}

		public bool IsNormalized (NormalizationForm normalizationForm)
		{
			switch (normalizationForm) {
			default:
				return Normalization.IsNormalized (this, 0);
			case NormalizationForm.FormD:
				return Normalization.IsNormalized (this, 1);
			case NormalizationForm.FormKC:
				return Normalization.IsNormalized (this, 2);
			case NormalizationForm.FormKD:
				return Normalization.IsNormalized (this, 3);
			}
		}

		public string Remove (int startIndex)
		{
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "StartIndex can not be less than zero");
			if (startIndex >= this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "StartIndex must be less than the length of the string");

			return Remove (startIndex, this.Length - startIndex);
		}

		public String PadLeft (int totalWidth)
		{
			return PadLeft (totalWidth, ' ');
		}

		public unsafe String PadLeft (int totalWidth, char paddingChar)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			//LAMESPEC: MSDN Doc says this is reversed for RtL languages, but this seems to be untrue

			if (totalWidth < 0)
				throw new ArgumentOutOfRangeException ("totalWidth", "< 0");

			if (totalWidth < this.Length)
				return this;
			if (totalWidth == 0)
				return Empty;

			String tmp = InternalAllocateStr (totalWidth, ENCODING_UTF16);

			fixed (byte* dest_ = &tmp.start_byte, src_ = &this.start_byte) {
				char* dest = (char*)dest_;
				char* src = (char*)src_;
				char* padPos = dest;
				char* padTo;
				try {
					padTo = checked (dest + (totalWidth - this.Length));
				} catch (OverflowException) {
					throw new OutOfMemoryException ();
				}

				while (padPos != padTo)
					*padPos++ = paddingChar;

				CharCopy (padTo, src, this.Length);
			}
			return tmp;
		}

		public String PadRight (int totalWidth)
		{
			return PadRight (totalWidth, ' ');
		}

		public unsafe String PadRight (int totalWidth, char paddingChar)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			//LAMESPEC: MSDN Doc says this is reversed for RtL languages, but this seems to be untrue

			if (totalWidth < 0)
				throw new ArgumentOutOfRangeException ("totalWidth", "< 0");

			if (totalWidth < this.Length)
				return this;
			if (totalWidth == 0)
				return Empty;

			String tmp = InternalAllocateStr (totalWidth, ENCODING_UTF16);

			fixed (byte* dest_ = &tmp.start_byte, src_ = &this.start_byte) {
				char* dest = (char*)dest_;
				char* src = (char*)src_;
				CharCopy (dest, src, this.Length);

				try {
					char* padPos = checked (dest + this.Length);
					char* padTo = checked (dest + totalWidth);
					while (padPos != padTo)
						*padPos++ = paddingChar;
				} catch (OverflowException) {
					throw new OutOfMemoryException ();
				}
			}
			return tmp;
		}

		public bool StartsWith (String value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			return CultureInfo.CurrentCulture.CompareInfo.IsPrefix (this, value, CompareOptions.None);
		}

		[ComVisible (false)]
		public bool StartsWith (string value, StringComparison comparisonType)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			switch (comparisonType) {
			case StringComparison.CurrentCulture:
				return CultureInfo.CurrentCulture.CompareInfo.IsPrefix (this, value, CompareOptions.None);
			case StringComparison.CurrentCultureIgnoreCase:
				return CultureInfo.CurrentCulture.CompareInfo.IsPrefix (this, value, CompareOptions.IgnoreCase);
			case StringComparison.InvariantCulture:
				return CultureInfo.InvariantCulture.CompareInfo.IsPrefix (this, value, CompareOptions.None);
			case StringComparison.InvariantCultureIgnoreCase:
				return CultureInfo.InvariantCulture.CompareInfo.IsPrefix (this, value, CompareOptions.IgnoreCase);
			case StringComparison.Ordinal:
				return StartsWithOrdinalUnchecked (value);
			case StringComparison.OrdinalIgnoreCase:
				return StartsWithOrdinalCaseInsensitiveUnchecked (value);
			default:
				string msg = Locale.GetText ("Invalid value '{0}' for StringComparison", comparisonType);
				throw new ArgumentException (msg, "comparisonType");
			}
		}

		internal bool StartsWithOrdinalUnchecked (string value)
		{
			return this.Length >= value.Length && CompareOrdinalUnchecked (this, 0, value.Length, value, 0, value.Length) == 0;
		}

		internal bool StartsWithOrdinalCaseInsensitiveUnchecked (string value)
		{
			return this.Length >= value.Length && CompareOrdinalCaseInsensitiveUnchecked (this, 0, value.Length, value, 0, value.Length) == 0;
		}

		[ComVisible (false)]
		public bool EndsWith (string value, StringComparison comparisonType)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			switch (comparisonType) {
			case StringComparison.CurrentCulture:
				return CultureInfo.CurrentCulture.CompareInfo.IsSuffix (this, value, CompareOptions.None);
			case StringComparison.CurrentCultureIgnoreCase:
				return CultureInfo.CurrentCulture.CompareInfo.IsSuffix (this, value, CompareOptions.IgnoreCase);
			case StringComparison.InvariantCulture:
				return CultureInfo.InvariantCulture.CompareInfo.IsSuffix (this, value, CompareOptions.None);
			case StringComparison.InvariantCultureIgnoreCase:
				return CultureInfo.InvariantCulture.CompareInfo.IsSuffix (this, value, CompareOptions.IgnoreCase);
			case StringComparison.Ordinal:
				return CultureInfo.InvariantCulture.CompareInfo.IsSuffix (this, value, CompareOptions.Ordinal);
			case StringComparison.OrdinalIgnoreCase:
				return CultureInfo.InvariantCulture.CompareInfo.IsSuffix (this, value, CompareOptions.OrdinalIgnoreCase);
			default:
				string msg = Locale.GetText ("Invalid value '{0}' for StringComparison", comparisonType);
				throw new ArgumentException (msg, "comparisonType");
			}
		}

		public bool StartsWith (String value, bool ignoreCase, CultureInfo culture)
		{
			if (culture == null)
				culture = CultureInfo.CurrentCulture;
			
			return culture.CompareInfo.IsPrefix (this, value, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
		}

		// Following method is culture-insensitive
		public unsafe String Replace (char oldChar, char newChar)
		{
			if (IsCompact)
				throw new NotImplementedException ();

			if (this.Length == 0 || oldChar == newChar)
				return this;

			int start_pos = IndexOfUnchecked (oldChar, 0, this.Length);
			if (start_pos == -1)
				return this;

			if (start_pos < 4)
				start_pos = 0;

			string tmp = InternalAllocateStr (this.Length, ENCODING_UTF16);
			fixed (byte* dest_ = &tmp.start_byte, src_ = &start_byte) {
				char* dest = (char*)dest_;
				char* src = (char*)src_;

				if (start_pos != 0)
					CharCopy (dest, src, start_pos);

				char* end_ptr = dest + this.Length;
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

		// culture-insensitive using ordinal search (See testcase StringTest.ReplaceStringCultureTests)
		public String Replace (String oldValue, String newValue)
		{
			// LAMESPEC: According to MSDN the following method is culture-sensitive but this seems to be incorrect
			// LAMESPEC: Result is undefined if result length is longer than maximum string length

			if (oldValue == null)
				throw new ArgumentNullException ("oldValue");

			if (oldValue.Length == 0)
				throw new ArgumentException ("oldValue is the empty string.");

			if (this.Length == 0)
				return this;
			
			if (newValue == null)
				newValue = Empty;

			return ReplaceUnchecked (oldValue, newValue);
		}

		private unsafe String ReplaceUnchecked (String oldValue, String newValue)
		{
			if (IsCompact)
				throw new NotImplementedException ();

			if (oldValue.Length > this.Length)
				return this;
			if (oldValue.Length == 1 && newValue.Length == 1) {
				return Replace (oldValue[0], newValue[0]);
				// ENHANCE: It would be possible to special case oldValue.length == newValue.length
				// because the length of the result would be this.length and length calculation unneccesary
			}

			const int maxValue = 200; // Allocate 800 byte maximum
			int* dat = stackalloc int[maxValue];
			fixed (byte* source_ = &this.start_byte, replace_ = &newValue.start_byte) {
				char* source = (char*)source_;
				char* replace = (char*)replace_;
				int i = 0, count = 0;
				while (i < this.Length) {
					int found = IndexOfOrdinalUnchecked (oldValue, i, this.Length - i);
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
					return this;
				int nlen = 0;
				checked {
					try {
						nlen = this.Length + ((newValue.Length - oldValue.Length) * count);
					} catch (OverflowException) {
						throw new OutOfMemoryException ();
					}
				}
				String tmp = InternalAllocateStr (nlen, ENCODING_UTF16);

				int curPos = 0, lastReadPos = 0;
				fixed (byte* dest_ = &tmp.start_byte) {
					char* dest = (char*)dest_;
					for (int j = 0; j < count; j++) {
						int precopy = dat[j] - lastReadPos;
						CharCopy (dest + curPos, source + lastReadPos, precopy);
						curPos += precopy;
						lastReadPos = dat[j] + oldValue.Length;
						CharCopy (dest + curPos, replace, newValue.Length);
						curPos += newValue.Length;
					}
					CharCopy (dest + curPos, source + lastReadPos, this.Length - lastReadPos);
				}
				return tmp;
			}
		}

		private String ReplaceFallback (String oldValue, String newValue, int testedCount)
		{
			if (IsCompact)
				throw new NotImplementedException ();

			int lengthEstimate = this.Length + ((newValue.Length - oldValue.Length) * testedCount);
			StringBuilder sb = new StringBuilder (lengthEstimate);
			for (int i = 0; i < this.Length;) {
				int found = IndexOfOrdinalUnchecked (oldValue, i, this.Length - i);
				if (found < 0) {
					sb.Append (SubstringUnchecked (i, this.Length - i));
					break;
				}
				sb.Append (SubstringUnchecked (i, found - i));
				sb.Append (newValue);
				i = found + oldValue.Length;
			}
			return sb.ToString ();

		}

		public unsafe String Remove (int startIndex, int count)
		{
			if (IsCompact)
				throw new NotImplementedException ();
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative.");
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "Cannot be negative.");
			if (startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "startIndex + count > this.length");

			String tmp = InternalAllocateStr (this.Length - count, ENCODING_UTF16);

			fixed (byte* dst_ = &tmp.start_byte, src_ = &this.start_byte) {
				char* dst = (char*)dst_;
				char* src = (char*)src_;
				CharCopy (dst, src, startIndex);
				int skip = startIndex + count;
				dst += startIndex;
				CharCopy (dst, src + skip, this.Length - skip);
			}
			return tmp;
		}

		public String ToLower ()
		{
			return ToLower (CultureInfo.CurrentCulture);
		}

		public String ToLower (CultureInfo culture)
		{
			if (culture == null)
				throw new ArgumentNullException ("culture");

			if (culture.LCID == 0x007F) // Invariant
				return ToLowerInvariant ();

			return culture.TextInfo.ToLower (this);
		}

		public unsafe String ToLowerInvariant ()
		{
			if (this.Length == 0)
				return Empty;

			/* It's safe to use the compact encoding if the source string is
			 * compact because the invariant culture will only produce ASCII
			 * output given ASCII input.
			 */
			string tmp = InternalAllocateStr (this.Length, IsCompact ? ENCODING_ASCII : ENCODING_UTF16);
			fixed (byte* dest_ = &tmp.start_byte)
			fixed (byte* source_ = &start_byte) {
				if (IsCompact) {
					for (int i = 0; i < Length; ++i)
						dest_ [i] = (byte)Char.ToLowerInvariant ((char)source_ [i]);
				} else {
					char* dest = (char*)dest_;
					char* source = (char*)source_;
					char* destPtr = (char*)dest;
					char* sourcePtr = (char*)source;

					for (int n = 0; n < this.Length; n++) {
						*destPtr = Char.ToLowerInvariant (*sourcePtr);
						sourcePtr++;
						destPtr++;
					}
				}
			}
			return tmp;
		}

		public String ToUpper ()
		{
			return ToUpper (CultureInfo.CurrentCulture);
		}

		public String ToUpper (CultureInfo culture)
		{
			if (culture == null)
				throw new ArgumentNullException ("culture");

			if (culture.LCID == 0x007F) // Invariant
				return ToUpperInvariant ();

			return culture.TextInfo.ToUpper (this);
		}

		public unsafe String ToUpperInvariant ()
		{
			if (IsCompact)
				throw new NotImplementedException ();

			if (this.Length == 0)
				return Empty;

			string tmp = InternalAllocateStr (this.Length, ENCODING_UTF16);
			fixed (byte* dest_ = &tmp.start_byte)
			fixed (byte* source_ = &start_byte) {
				char* destPtr = (char*)dest_;
				char* sourcePtr = (char*)source_;

				for (int n = 0; n < this.Length; n++) {
					*destPtr = Char.ToUpperInvariant (*sourcePtr);
					sourcePtr++;
					destPtr++;
				}
			}
			return tmp;
		}

		public override String ToString ()
		{
			return this;
		}

		public String ToString (IFormatProvider provider)
		{
			return this;
		}

		public static String Format (String format, Object arg0)
		{
			return Format (null, format, new Object[] {arg0});
		}

		public static String Format (String format, Object arg0, Object arg1)
		{
			return Format (null, format, new Object[] {arg0, arg1});
		}

		public static String Format (String format, Object arg0, Object arg1, Object arg2)
		{
			return Format (null, format, new Object[] {arg0, arg1, arg2});
		}

		public static string Format (string format, params object[] args)
		{
			return Format (null, format, args);
		}
	
		public static string Format (IFormatProvider provider, string format, params object[] args)
		{
			StringBuilder b = FormatHelper (null, provider, format, args);
			return b.ToString ();
		}
		
		internal static StringBuilder FormatHelper (StringBuilder result, IFormatProvider provider, string format, params object[] args)
		{
			if (format == null)
				throw new ArgumentNullException ("format");
			if (args == null)
				throw new ArgumentNullException ("args");

			if (result == null) {
				/* Try to approximate the size of result to avoid reallocations */
				int i, len;

				len = 0;
				for (i = 0; i < args.Length; ++i) {
					string s = args [i] as string;
					if (s != null)
						len += s.Length;
					else
						break;
				}
				if (i == args.Length)
					result = new StringBuilder (len + format.Length);
				else
					result = new StringBuilder ();
			}

			int ptr = 0;
			int start = ptr;
			var formatter = provider != null ? provider.GetFormat (typeof (ICustomFormatter)) as ICustomFormatter : null;

			while (ptr < format.Length) {
				char c = format[ptr ++];

				if (c == '{') {
					result.Append (format, start, ptr - start - 1);

					// check for escaped open bracket

					if (format[ptr] == '{') {
						start = ptr ++;
						continue;
					}

					// parse specifier
				
					int n, width;
					bool left_align;
					string arg_format;

					ParseFormatSpecifier (format, ref ptr, out n, out width, out left_align, out arg_format);
					if (n >= args.Length)
						throw new FormatException ("Index (zero based) must be greater than or equal to zero and less than the size of the argument list.");

					// format argument

					object arg = args[n];

					string str;
					if (arg == null)
						str = Empty;
					else if (formatter != null)
						str = formatter.Format (arg_format, arg, provider);
					else
						str = null;

					if (str == null) {
						if (arg is IFormattable)
							str = ((IFormattable)arg).ToString (arg_format, provider);
						else
							str = arg.ToString () ?? Empty;
					}

					// pad formatted string and append to result
					if (width > str.Length) {
						const char padchar = ' ';
						int padlen = width - str.Length;

						if (left_align) {
							result.Append (str);
							result.Append (padchar, padlen);
						}
						else {
							result.Append (padchar, padlen);
							result.Append (str);
						}
					} else {
						result.Append (str);
					}

					start = ptr;
				}
				else if (c == '}' && ptr < format.Length && format[ptr] == '}') {
					result.Append (format, start, ptr - start - 1);
					start = ptr ++;
				}
				else if (c == '}') {
					throw new FormatException ("Input string was not in a correct format.");
				}
			}

			if (start < format.Length)
				result.Append (format, start, format.Length - start);

			return result;
		}

		public unsafe static String Copy (String str)
		{
			if (str == null)
				throw new ArgumentNullException ("str");

			if (str.IsCompact)
				throw new NotImplementedException ();

			int length = str.Length;

			String tmp = InternalAllocateStr (length, ENCODING_UTF16);
			if (length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest, src, length);
				}
			}
			return tmp;
		}

		public static String Concat (Object arg0)
		{
			if (arg0 == null)
				return Empty;

			return arg0.ToString ();
		}

		public static String Concat (Object arg0, Object arg1)
		{
			return Concat ((arg0 != null) ? arg0.ToString () : null, (arg1 != null) ? arg1.ToString () : null);
		}

		public static String Concat (Object arg0, Object arg1, Object arg2)
		{
			string s1, s2, s3;
			if (arg0 == null)
				s1 = Empty;
			else
				s1 = arg0.ToString ();

			if (arg1 == null)
				s2 = Empty;
			else
				s2 = arg1.ToString ();

			if (arg2 == null)
				s3 = Empty;
			else
				s3 = arg2.ToString ();

			return Concat (s1, s2, s3);
		}

		[CLSCompliant(false)]
		public static String Concat (Object arg0, Object arg1, Object arg2,
					     Object arg3, __arglist)
		{
			string s1, s2, s3, s4;

			if (arg0 == null)
				s1 = Empty;
			else
				s1 = arg0.ToString ();

			if (arg1 == null)
				s2 = Empty;
			else
				s2 = arg1.ToString ();

			if (arg2 == null)
				s3 = Empty;
			else
				s3 = arg2.ToString ();

			ArgIterator iter = new ArgIterator (__arglist);
			int argCount = iter.GetRemainingCount();

			StringBuilder sb = new StringBuilder ();
			if (arg3 != null)
				sb.Append (arg3.ToString ());

			for (int i = 0; i < argCount; i++) {
				TypedReference typedRef = iter.GetNextArg ();
				sb.Append (TypedReference.ToObject (typedRef));
			}

			s4 = sb.ToString ();

			return Concat (s1, s2, s3, s4);			
		}

		public unsafe static String Concat (String str0, String str1)
		{
			if (str0 == null || str0.Length == 0) {
				if (str1 == null || str1.Length == 0)
					return Empty;
				return str1;
			}

			if (str1 == null || str1.Length == 0)
				return str0; 

			int nlen = str0.Length + str1.Length;
			if (nlen < 0)
				throw new OutOfMemoryException ();

			String tmp;
			if (str0.IsCompact && str1.IsCompact) {
				tmp = InternalAllocateStr (nlen, ENCODING_ASCII);
				fixed (byte* dest_ = &tmp.start_byte) {
					fixed (byte* src_ = &str0.start_byte)
						for (int i = 0; i < str0.Length; ++i)
							dest_ [i] = src_ [i];
					fixed (byte* src_ = &str1.start_byte)
						for (int i = 0; i < str1.Length; ++i)
							dest_ [str0.Length + i] = src_ [i];
				}
			} else if (str0.IsCompact) {
				tmp = InternalAllocateStr (nlen, ENCODING_UTF16);
				fixed (byte* dest_ = &tmp.start_byte) {
					char* dest = (char*)dest_;
					fixed (byte* src_ = &str0.start_byte)
						for (int i = 0; i < str0.Length; ++i)
							dest [i] = (char)src_ [i];
					fixed (byte* src_ = &str1.start_byte) {
						char* src = (char*)src_;
						CharCopy (dest + str0.Length, src, str1.Length);
					}
				}
			} else if (str1.IsCompact) {
				throw new NotImplementedException ();
			} else {
				tmp = InternalAllocateStr (nlen, ENCODING_UTF16);
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str0.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest, src, str0.Length);
				}
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str1.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest + str0.Length, src, str1.Length);
				}
			}
			return tmp;
		}

		public unsafe static String Concat (String str0, String str1, String str2)
		{
			if (str0 == null || str0.Length == 0){
				if (str1 == null || str1.Length == 0){
					if (str2 == null || str2.Length == 0)
						return Empty;
					return str2;
				} else {
					if (str2 == null || str2.Length == 0)
						return str1;
				}
				str0 = Empty;
			} else {
				if (str1 == null || str1.Length == 0){
					if (str2 == null || str2.Length == 0)
						return str0;
					else
						str1 = Empty;
				} else {
					if (str2 == null || str2.Length == 0)
						str2 = Empty;
				}
			}

			int nlen = str0.Length + str1.Length;
			if (nlen < 0)
				throw new OutOfMemoryException ();
			nlen += str2.Length;
			if (nlen < 0)
				throw new OutOfMemoryException ();
			String tmp = InternalAllocateStr (nlen, ENCODING_UTF16);

			if (str0.IsCompact || str1.IsCompact || str2.IsCompact)
				throw new NotImplementedException ();

			if (str0.Length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str0.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest, src, str0.Length);
				}
			}
			if (str1.Length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str1.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest + str0.Length, src, str1.Length);
				}
			}
			if (str2.Length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str2.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest + str0.Length + str1.Length, src, str2.Length);
				}
			}

			return tmp;
		}

		public unsafe static String Concat (String str0, String str1, String str2, String str3)
		{
			if (str0 == null && str1 == null && str2 == null && str3 == null)
				return Empty;

			if (str0 == null)
				str0 = Empty;
			if (str1 == null)
				str1 = Empty;
			if (str2 == null)
				str2 = Empty;
			if (str3 == null)
				str3 = Empty;

			if (str0.IsCompact || str1.IsCompact || str2.IsCompact || str3.IsCompact)
				throw new NotImplementedException ();

			int nlen = str0.Length + str1.Length;
			if (nlen < 0)
				throw new OutOfMemoryException ();
			nlen += str2.Length;
			if (nlen < 0)
				throw new OutOfMemoryException ();
			nlen += str3.Length;
			if (nlen < 0)
				throw new OutOfMemoryException ();
			String tmp = InternalAllocateStr (str0.Length + str1.Length + str2.Length + str3.Length, ENCODING_UTF16);

			if (str0.Length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str0.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest, src, str0.Length);
				}
			}
			if (str1.Length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str1.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest + str0.Length, src, str1.Length);
				}
			}
			if (str2.Length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str2.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest + str0.Length + str1.Length, src, str2.Length);
				}
			}
			if (str3.Length != 0) {
				fixed (byte* dest_ = &tmp.start_byte, src_ = &str3.start_byte) {
					char* dest = (char*)dest_;
					char* src = (char*)src_;
					CharCopy (dest + str0.Length + str1.Length + str2.Length, src, str3.Length);
				}
			}

			return tmp;
		}

		public static String Concat (params Object[] args)
		{
			if (args == null)
				throw new ArgumentNullException ("args");

			int argLen = args.Length;
			if (argLen == 0)
				return Empty;

			string [] strings = new string [argLen];
			int len = 0;
			for (int i = 0; i < argLen; i++) {
				if (args[i] != null) {
					strings[i] = args[i].ToString ();
					len += strings[i].Length;
					if (len < 0)
						throw new OutOfMemoryException ();
				}
			}

			return ConcatInternal (strings, len);
		}

		public static String Concat (params String[] values)
		{
			if (values == null)
				throw new ArgumentNullException ("values");

			int len = 0;
			for (int i = 0; i < values.Length; i++) {
				String s = values[i];
				if (s != null)
					len += s.Length;
					if (len < 0)
						throw new OutOfMemoryException ();
			}

			return ConcatInternal (values, len);
		}

		private static unsafe String ConcatInternal (String[] values, int length)
		{
			if (length == 0)
				return Empty;
			if (length < 0)
				throw new OutOfMemoryException ();

			String tmp = InternalAllocateStr (length, ENCODING_UTF16);

			fixed (byte* dest_ = &tmp.start_byte) {
				char* dest = (char*)dest_;
				int pos = 0;
				for (int i = 0; i < values.Length; i++) {
					String source = values[i];
					if (source != null) {
						if (source.IsCompact)
							throw new NotImplementedException ();
						fixed (byte* src_ = &source.start_byte) {
							char* src = (char*)src_;
							CharCopy (dest + pos, src, source.Length);
						}
						pos += source.Length;
					}
				}
			}
			return tmp;
		}

		public unsafe String Insert (int startIndex, String value)
		{
			if (IsCompact)
				throw new NotImplementedException ();

			if (value == null)
				throw new ArgumentNullException ("value");

			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative and must be less than or equal to length of string.");

			if (value.Length == 0)
				return this;
			if (this.Length == 0)
				return value;

			int nlen = this.Length + value.Length;
			if (nlen < 0)
				throw new OutOfMemoryException ();

			String tmp = InternalAllocateStr (nlen, ENCODING_UTF16);

			fixed (byte* dst_ = &tmp.start_byte, src_ = &this.start_byte, val_ = &value.start_byte) {
				char* dst = (char*)dst_;
				char* src = (char*)src_;
				char* val = (char*)val_;
				CharCopy (dst, src, startIndex);
				dst += startIndex;
				CharCopy (dst, val, value.Length);
				dst += value.Length;
				CharCopy (dst, src + startIndex, this.Length - startIndex);
			}
			return tmp;
		}

		public static string Intern (string str)
		{
			if (str == null)
				throw new ArgumentNullException ("str");

			return InternalIntern (str);
		}

		public static string IsInterned (string str)
		{
			if (str == null)
				throw new ArgumentNullException ("str");

			return InternalIsInterned (str);
		}
	
		public static string Join (string separator, params string [] value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (separator == null)
				separator = Empty;

			return JoinUnchecked (separator, value, 0, value.Length);
		}

		public static string Join (string separator, string[] value, int startIndex, int count)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "< 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "< 0");
			if (startIndex > value.Length - count)
				throw new ArgumentOutOfRangeException ("startIndex", "startIndex + count > value.Length");

			if (startIndex == value.Length)
				return Empty;
			if (separator == null)
				separator = Empty;

			return JoinUnchecked (separator, value, startIndex, count);
		}

		private static unsafe string JoinUnchecked (string separator, string[] value, int startIndex, int count)
		{
			// Unchecked parameters
			// startIndex, count must be >= 0; startIndex + count must be <= value.Length
			// separator and value must not be null

			int length = 0;
			int maxIndex = startIndex + count;
			// Precount the number of characters that the resulting string will have
			for (int i = startIndex; i < maxIndex; i++) {
				String s = value[i];
				if (s != null)
					length += s.Length;
			}
			length += separator.Length * (count - 1);
			if (length <= 0)
				return Empty;

			String tmp = InternalAllocateStr (length, ENCODING_UTF16);

			maxIndex--;
			fixed (byte* dest_ = &tmp.start_byte, sepsrc_ = &separator.start_byte) {
				char* dest = (char*)dest_;
				char* sepsrc = (char*)sepsrc_;
				// Copy each string from value except the last one and add a separator for each
				int pos = 0;
				for (int i = startIndex; i < maxIndex; i++) {
					String source = value[i];
					if (source != null) {
						if (source.Length > 0) {
							fixed (byte* src_ = &source.start_byte) {
								char* src = (char*)src_;
								CharCopy (dest + pos, src, source.Length);
							}
							pos += source.Length;
						}
					}
					if (separator.Length > 0) {
						CharCopy (dest + pos, sepsrc, separator.Length);
						pos += separator.Length;
					}
				}
				// Append last string that does not get an additional separator
				String sourceLast = value[maxIndex];
				if (sourceLast != null) {
					if (sourceLast.Length > 0) {
						fixed (byte* src_ = &sourceLast.start_byte) {
							char* src = (char*)src_;
							CharCopy (dest + pos, src, sourceLast.Length);
						}

					}
				}
			}
			return tmp;
		}

		bool IConvertible.ToBoolean (IFormatProvider provider)
		{
			return Convert.ToBoolean (this, provider);
		}

		byte IConvertible.ToByte (IFormatProvider provider)
		{
			return Convert.ToByte (this, provider);
		}

		char IConvertible.ToChar (IFormatProvider provider)
		{
			return Convert.ToChar (this, provider);
		}

		DateTime IConvertible.ToDateTime (IFormatProvider provider)
		{
			return Convert.ToDateTime (this, provider);
		}

		decimal IConvertible.ToDecimal (IFormatProvider provider)
		{
			return Convert.ToDecimal (this, provider);
		}

		double IConvertible.ToDouble (IFormatProvider provider)
		{
			return Convert.ToDouble (this, provider);
		}

		short IConvertible.ToInt16 (IFormatProvider provider)
		{
			return Convert.ToInt16 (this, provider);
		}

		int IConvertible.ToInt32 (IFormatProvider provider)
		{
			return Convert.ToInt32 (this, provider);
		}

		long IConvertible.ToInt64 (IFormatProvider provider)
		{
			return Convert.ToInt64 (this, provider);
		}

		sbyte IConvertible.ToSByte (IFormatProvider provider)
		{
			return Convert.ToSByte (this, provider);
		}

		float IConvertible.ToSingle (IFormatProvider provider)
		{
			return Convert.ToSingle (this, provider);
		}

		object IConvertible.ToType (Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType ((IConvertible)this, type, provider);
		}

		ushort IConvertible.ToUInt16 (IFormatProvider provider)
		{
			return Convert.ToUInt16 (this, provider);
		}

		uint IConvertible.ToUInt32 (IFormatProvider provider)
		{
			return Convert.ToUInt32 (this, provider);
		}

		ulong IConvertible.ToUInt64 (IFormatProvider provider)
		{
			return Convert.ToUInt64 (this, provider);
		}

		public int Length {
			get {
				return (int)(tagged_length >> 1);
			}
		}

		public CharEnumerator GetEnumerator ()
		{
			return new CharEnumerator (this);
		}

		IEnumerator<char> IEnumerable<char>.GetEnumerator ()
		{
			return new CharEnumerator (this);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return new CharEnumerator (this);
		}

		private static void ParseFormatSpecifier (string str, ref int ptr, out int n, out int width,
		                                          out bool left_align, out string format)
		{
			int max = str.Length;
			
			// parses format specifier of form:
			//   N,[\ +[-]M][:F]}
			//
			// where:
			// N = argument number (non-negative integer)
			
			n = ParseDecimal (str, ref ptr);
			if (n < 0)
				throw new FormatException ("Input string was not in a correct format.");
			
			// M = width (non-negative integer)
			
			if (ptr < max && str[ptr] == ',') {
				// White space between ',' and number or sign.
				++ptr;
				while (ptr < max && Char.IsWhiteSpace (str [ptr]))
					++ptr;
				int start = ptr;
				
				format = str.Substring (start, ptr - start);
				
				left_align = (ptr < max && str [ptr] == '-');
				if (left_align)
					++ ptr;
				
				width = ParseDecimal (str, ref ptr);
				if (width < 0)
					throw new FormatException ("Input string was not in a correct format.");
			}
			else {
				width = 0;
				left_align = false;
				format = Empty;
			}
			
			// F = argument format (string)
			
			if (ptr < max && str[ptr] == ':') {
				int start = ++ ptr;
				while (ptr < max) {
					if (str [ptr] == '}') {
						if (ptr + 1 < max && str [ptr + 1] == '}') {
							++ptr;
							format += str.Substring (start, ptr - start);
							++ptr;
							start = ptr;
							continue;
						}

						break;
					}

					++ptr;
				}

				format += str.Substring (start, ptr - start);
			}
			else
				format = null;
			
			if ((ptr >= max) || str[ptr ++] != '}')
				throw new FormatException ("Input string was not in a correct format.");
		}

		private static int ParseDecimal (string str, ref int ptr)
		{
			int p = ptr;
			int n = 0;
			int max = str.Length;
			
			while (p < max) {
				char c = str[p];
				if (c < '0' || '9' < c)
					break;

				n = n * 10 + c - '0';
				++ p;
			}

			if (p == ptr || p == max)
				return -1;

			ptr = p;
			return n;
		}

		internal unsafe void InternalSetChar (int idx, char val)
		{
			if (IsCompact)
				throw new NotImplementedException ();

			if ((uint) idx >= (uint) Length)
				throw new ArgumentOutOfRangeException ("idx");

			fixed (byte* pStr_ = &start_byte) {
				char* pStr = (char*)pStr_;
				pStr [idx] = val;
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern void InternalSetLength (int newLength);

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		// When modifying it, GetCaseInsensitiveHashCode() should be modified as well.
		public unsafe override int GetHashCode ()
		{
			uint h = 0;
			int length = this.Length;
			fixed (byte* c_ = &this.start_byte) {
				if (IsCompact) {
					byte* cc = c_;
					byte* end = cc + length;
					while (cc < end) {
						h = (h << 5) - h + (char)*cc;
						++cc;
					}
				} else {
					char* c = (char*)c_;
					char* cc = c;
					char* end = cc + length;
					while (cc < end) {
						h = (h << 5) - h + *cc;
						++cc;
					}
				}
			}
			return (int)h;
		}

		[ComVisible(false)]
		public static string Concat (IEnumerable<string> values)
		{
			if (values == null)
				throw new ArgumentNullException ("values");

			var stringList = new List<string> ();
			int len = 0;
			foreach (var v in values){
				if (v == null)
					continue;
				len += v.Length;
				if (len < 0)
					throw new OutOfMemoryException ();
				stringList.Add (v);
			}
			return ConcatInternal (stringList.ToArray (), len);
		}

		[ComVisibleAttribute(false)]
		public static string Concat<T> (IEnumerable<T> values)
		{
			if (values == null)
				throw new ArgumentNullException ("values");

			var stringList = new List<string> ();
			int len = 0;
			foreach (var v in values){
				string sr = v.ToString ();
				len += sr.Length;
				if (len < 0)
					throw new OutOfMemoryException ();
				stringList.Add (sr);
			}
			return ConcatInternal (stringList.ToArray (), len);
		}

		[ComVisibleAttribute(false)]
		public static string Join (string separator, IEnumerable<string> values)
		{
			if (separator == null)
				return Concat (values);
			
			if (values == null)
				throw new ArgumentNullException ("values");
			
			var stringList = new List<string> (values);

			return JoinUnchecked (separator, stringList.ToArray (), 0, stringList.Count);
		}

		[ComVisibleAttribute(false)]
		public static string Join (string separator, params object [] values)
		{
			if (separator == null)
				return Concat (values);
			
			if (values == null)
				throw new ArgumentNullException ("values");

			var strCopy = new string [values.Length];
			int i = 0;
			foreach (var v in values)
				strCopy [i++] = v.ToString ();

			return JoinUnchecked (separator, strCopy, 0, strCopy.Length);
		}
		
		[ComVisible (false)]
		public static string Join<T> (string separator, IEnumerable<T> values)
		{
			if (separator == null)
				return Concat<T> (values);
				
			if (values == null)
				throw new ArgumentNullException ("values");
			
			var stringList = values as IList<T> ?? new List<T> (values);
			var strCopy = new string [stringList.Count];
			int i = 0;
			foreach (var v in stringList)
				strCopy [i++] = v.ToString ();

			return JoinUnchecked (separator, strCopy, 0, strCopy.Length);
		}

		public static bool IsNullOrWhiteSpace (string value)
		{
			if ((value == null) || (value.Length == 0))
				return true;
			foreach (char c in value)
				if (!Char.IsWhiteSpace (c))
					return false;
			return true;
		}

		internal unsafe int GetCaseInsensitiveHashCode ()
		{
			uint h = 0;
			fixed (byte* c_ = &this.start_byte) {
				if (IsCompact) {
					Console.WriteLine ('X');
					throw new NotImplementedException ();
					byte* cc = c_;
					byte* end = cc + this.Length - 1;
					while (cc < end) {
						h = (h << 5) - h + Char.ToUpperInvariant ((char)cc [0]);
						h = (h << 5) - h + Char.ToUpperInvariant ((char)cc [1]);
						cc += 2;
					}
					++end;
					if (cc < end)
						h = (h << 5) - h + Char.ToUpperInvariant ((char)cc [0]);
				} else {
					char* c = (char*)c_;
					char* cc = c;
					char* end = cc + this.Length - 1;
					while (cc < end) {
						h = (h << 5) - h + Char.ToUpperInvariant (cc [0]);
						h = (h << 5) - h + Char.ToUpperInvariant (cc [1]);
						cc += 2;
					}
					++end;
					if (cc < end)
						h = (h << 5) - h + Char.ToUpperInvariant (cc [0]);
				}
			}
			return (int)h;
		}

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

		private unsafe String CreateString (sbyte* value, int startIndex, int length)
		{
			return CreateString (value, startIndex, length, null);
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

		unsafe string CreateString (char *value)
		{
			if (value == null)
				return Empty;
			char *p = value;
			int i = 0;
			while (*p != 0) {
				++i;
				++p;
			}
			string result = InternalAllocateStr (i, ENCODING_UTF16);

			if (i != 0) {
				fixed (byte *dest_ = &result.start_byte) {
					char* dest = (char*)dest_;
					CharCopy (dest, value, i);
				}
			}
			return result;
		}

		unsafe string CreateString (char *value, int startIndex, int length)
		{
			if (length == 0)
				return Empty;
			if (value == null)
				throw new ArgumentNullException ("value");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex");
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length");

			string result = InternalAllocateStr (length, ENCODING_UTF16);

			fixed (byte* dest_ = &result.start_byte) {
				char* dest = (char*)dest_;
				CharCopy (dest, value + startIndex, length);
			}
			return result;
		}

		unsafe string CreateString (char [] val, int startIndex, int length)
		{
			if (val == null)
				throw new ArgumentNullException ("value");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative.");
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", "Cannot be negative.");
			if (startIndex > val.Length - length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative, and should be less than length of string.");
			if (length == 0)
				return Empty;

			string result = InternalAllocateStr (length, ENCODING_UTF16);

			fixed (byte* dest_ = &result.start_byte)
			fixed (char* src = val) {
				char* dest = (char*)dest_;
				CharCopy (dest, src + startIndex, length);
			}
			return result;
		}

		unsafe string CreateString (char [] val)
		{
			if (val == null || val.Length == 0)
				return Empty;
			string result = InternalAllocateStr (val.Length, ENCODING_UTF16);

			fixed (byte* dest_ = &result.start_byte)
			fixed (char* src = val) {
				char* dest = (char*)dest_;
				CharCopy (dest, src, val.Length);
			}
			return result;
		}

		unsafe string CreateString (char c, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count");
			if (count == 0)
				return Empty;
			string result = InternalAllocateStr (count, ENCODING_UTF16);
			fixed (byte* dest_ = &result.start_byte) {
				char* dest = (char*)dest_;
				char* p = dest;
				char* end = p + count;
				while (p < end) {
					*p = c;
					p++;
				}
			}
			return result;
		}

		/* helpers used by the runtime as well as above or eslewhere in corlib */
		internal static unsafe void memset (byte *dest, int val, int len)
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

		static unsafe void memcpy4 (byte *dest, byte *src, int size) {
			/*while (size >= 32) {
				// using long is better than int and slower than double
				// FIXME: enable this only on correct alignment or on platforms
				// that can tolerate unaligned reads/writes of doubles
				((double*)dest) [0] = ((double*)src) [0];
				((double*)dest) [1] = ((double*)src) [1];
				((double*)dest) [2] = ((double*)src) [2];
				((double*)dest) [3] = ((double*)src) [3];
				dest += 32;
				src += 32;
				size -= 32;
			}*/
			while (size >= 16) {
				((int*)dest) [0] = ((int*)src) [0];
				((int*)dest) [1] = ((int*)src) [1];
				((int*)dest) [2] = ((int*)src) [2];
				((int*)dest) [3] = ((int*)src) [3];
				dest += 16;
				src += 16;
				size -= 16;
			}
			while (size >= 4) {
				((int*)dest) [0] = ((int*)src) [0];
				dest += 4;
				src += 4;
				size -= 4;
			}
			while (size > 0) {
				((byte*)dest) [0] = ((byte*)src) [0];
				dest += 1;
				src += 1;
				--size;
			}
		}
		static unsafe void memcpy2 (byte *dest, byte *src, int size) {
			while (size >= 8) {
				((short*)dest) [0] = ((short*)src) [0];
				((short*)dest) [1] = ((short*)src) [1];
				((short*)dest) [2] = ((short*)src) [2];
				((short*)dest) [3] = ((short*)src) [3];
				dest += 8;
				src += 8;
				size -= 8;
			}
			while (size >= 2) {
				((short*)dest) [0] = ((short*)src) [0];
				dest += 2;
				src += 2;
				size -= 2;
			}
			if (size > 0)
				((byte*)dest) [0] = ((byte*)src) [0];
		}
		static unsafe void memcpy1 (byte *dest, byte *src, int size) {
			while (size >= 8) {
				((byte*)dest) [0] = ((byte*)src) [0];
				((byte*)dest) [1] = ((byte*)src) [1];
				((byte*)dest) [2] = ((byte*)src) [2];
				((byte*)dest) [3] = ((byte*)src) [3];
				((byte*)dest) [4] = ((byte*)src) [4];
				((byte*)dest) [5] = ((byte*)src) [5];
				((byte*)dest) [6] = ((byte*)src) [6];
				((byte*)dest) [7] = ((byte*)src) [7];
				dest += 8;
				src += 8;
				size -= 8;
			}
			while (size >= 2) {
				((byte*)dest) [0] = ((byte*)src) [0];
				((byte*)dest) [1] = ((byte*)src) [1];
				dest += 2;
				src += 2;
				size -= 2;
			}
			if (size > 0)
				((byte*)dest) [0] = ((byte*)src) [0];
		}

		internal static unsafe void memcpy (byte *dest, byte *src, int size) {
			// FIXME: if pointers are not aligned, try to align them
			// so a faster routine can be used. Handle the case where
			// the pointers can't be reduced to have the same alignment
			// (just ignore the issue on x86?)
			if ((((int)dest | (int)src) & 3) != 0) {
				if (((int)dest & 1) != 0 && ((int)src & 1) != 0 && size >= 1) {
					dest [0] = src [0];
					++dest;
					++src;
					--size;
				}
				if (((int)dest & 2) != 0 && ((int)src & 2) != 0 && size >= 2) {
					((short*)dest) [0] = ((short*)src) [0];
					dest += 2;
					src += 2;
					size -= 2;
				}
				if ((((int)dest | (int)src) & 1) != 0) {
					memcpy1 (dest, src, size);
					return;
				}
				if ((((int)dest | (int)src) & 2) != 0) {
					memcpy2 (dest, src, size);
					return;
				}
			}
			memcpy4 (dest, src, size);
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
					memcpy2 ((byte*)dest, (byte*)src, count * 2);
					return;
				}
			}
			memcpy4 ((byte*)dest, (byte*)src, count * 2);
		}

		internal static unsafe void CharCopyReverse (char *dest, char *src, int count)
		{
			dest += count;
			src += count;
			for (int i = count; i > 0; i--) {
				dest--;
				src--;
				*dest = *src;
			}	
		}

		internal static unsafe void CharCopy (String target, int targetIndex, String source, int sourceIndex, int count)
		{
			fixed (byte* dest_ = &target.start_byte, src_ = &source.start_byte) {
				char* dest = (char*)dest_;
				char* src = (char*)src_;
				CharCopy (dest + targetIndex, src + sourceIndex, count);
			}
		}

		internal static unsafe void CharCopy (String target, int targetIndex, Char[] source, int sourceIndex, int count)
		{
			fixed (byte* dest_ = &target.start_byte)
			fixed (char* src = source) {
				char* dest = (char*)dest_;
				CharCopy (dest + targetIndex, src + sourceIndex, count);
			}
		}

		// Use this method if you cannot block copy from left to right (e.g. because you are coping within the same string)
		internal static unsafe void CharCopyReverse (String target, int targetIndex, String source, int sourceIndex, int count)
		{
			fixed (byte* dest_ = &target.start_byte, src_ = &source.start_byte) {
				char* dest = (char*)dest_;
				char* src = (char*)src_;
				CharCopyReverse (dest + targetIndex, src + sourceIndex, count);
			}
		}

		/* This is called from Convert. */
		internal static String FastAllocateString (int length)
		{
			return InternalAllocateStr (length, ENCODING_UTF16);
		}

		[CLSCompliant (false), MethodImplAttribute (MethodImplOptions.InternalCall)]
		unsafe public extern String (char *value);

		[CLSCompliant (false), MethodImplAttribute (MethodImplOptions.InternalCall)]
		unsafe public extern String (char *value, int startIndex, int length);

		[CLSCompliant (false), MethodImplAttribute (MethodImplOptions.InternalCall)]
		unsafe public extern String (sbyte *value);

		[CLSCompliant (false), MethodImplAttribute (MethodImplOptions.InternalCall)]
		unsafe public extern String (sbyte *value, int startIndex, int length);

		[CLSCompliant (false), MethodImplAttribute (MethodImplOptions.InternalCall)]
		unsafe public extern String (sbyte *value, int startIndex, int length, Encoding enc);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern String (char [] value, int startIndex, int length);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern String (char [] value);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern String (char c, int count);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static String InternalAllocateStr (int length, int encoding);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string InternalIntern (string str);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string InternalIsInterned (string str);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static int GetLOSLimit ();

#region "from referencesource" // and actually we replaced some parts.

        // Helper for encodings so they can talk to our buffer directly
        // stringLength must be the exact size we'll expect
        [System.Security.SecurityCritical]  // auto-generated
        unsafe static internal String CreateStringFromEncoding(
            byte* bytes, int byteLength, Encoding encoding)
        {
            Contract.Requires(bytes != null);
            Contract.Requires(byteLength >= 0);

            // Get our string length
            int stringLength = encoding.GetCharCount(bytes, byteLength, null);
            Contract.Assert(stringLength >= 0, "stringLength >= 0");
            
            // They gave us an empty string if they needed one
            // 0 bytelength might be possible if there's something in an encoder
            if (stringLength == 0)
                return String.Empty;
            
            String s = InternalAllocateStr (stringLength, ENCODING_UTF16);
            fixed(byte* pTempBytes = &s.start_byte)
            {
				char* pTempChars = (char*)pTempBytes;
                int doubleCheck = encoding.GetChars(bytes, byteLength, pTempChars, stringLength, null);
                Contract.Assert(stringLength == doubleCheck, 
                    "Expected encoding.GetChars to return same length as encoding.GetCharCount");
            }

            return s;
        }

		// our own implementation for CLR icall.
		unsafe internal static int nativeCompareOrdinalIgnoreCaseWC (string name, sbyte *strBBytes)
		{
			for (int i = 0; i < name.Length; i++) {
				sbyte b = *(strBBytes + i);
				if (b < 0)
					throw new ArgumentException ();
				int ret = char.ToUpper ((char) b) - char.ToUpper (name [i]);
				if (ret != 0)
					return ret;
			}
			return 0;
		}
#endregion
	}
}
