using System;
using System.IO;
using System.Reflection;

class Test
{
	public static void Main (string [] args)
	{
		// String s = "/Users/jonathanpurdy/Projects/mono/crappyjunk.exe";
		/*
		DirectorySeparatorChar = MonoIO.DirectorySeparatorChar;
		VolumeSeparatorChar = MonoIO.VolumeSeparatorChar;
		AltDirectorySeparatorChar = MonoIO.AltDirectorySeparatorChar;
		PathSeparator = MonoIO.PathSeparator;
		InvalidPathChars = new char [1] { '\x00' };
		DirectorySeparatorStr = DirectorySeparatorChar.ToString ();
		PathSeparatorChars = new char [] {
			DirectorySeparatorChar,
			AltDirectorySeparatorChar,
			VolumeSeparatorChar
		};
		*/
		DirectorySeparatorChar = '/';
		VolumeSeparatorChar = '/';
		AltDirectorySeparatorChar = '/';
		PathSeparator = ':';
		InvalidPathChars = new char [1] { '\x00' };
		DirectorySeparatorStr = DirectorySeparatorChar.ToString ();
		PathSeparatorChars = new char [] {
			DirectorySeparatorChar,
			AltDirectorySeparatorChar,
			VolumeSeparatorChar
		};

		String s = Assembly.GetEntryAssembly ().Location;
		Console.WriteLine (s.IsCompact);
		Console.WriteLine (s);
		String d = GetDirectoryName (s);
		Console.WriteLine (d.IsCompact);
		Console.WriteLine (d);
		for (int i = 0; i < d.Length; ++i)
			Console.Write (d [i]);
		Console.Write ('\n');
		Console.WriteLine (Path.GetDirectoryName (s));
	}

	public static char PathSeparator;
	public static char DirectorySeparatorChar;
	public static char AltDirectorySeparatorChar;
	public static string DirectorySeparatorStr;
	public static char[] PathSeparatorChars;
	public static char VolumeSeparatorChar;
	public static char[] InvalidPathChars;

	public static string GetDirectoryName (string path)
	{
		// LAMESPEC: For empty string MS docs say both
		// return null AND throw exception.  Seems .NET throws.
		if (path == String.Empty)
			throw new ArgumentException("Invalid path");

		if (path == null || GetPathRoot (path) == path)
			return null;

		if (path.Trim ().Length == 0)
			throw new ArgumentException ("Argument string consists of whitespace characters only.");

		if (path.IndexOfAny (InvalidPathChars) > -1)
			throw new ArgumentException ("Path contains invalid characters");

		int nLast = path.LastIndexOfAny (PathSeparatorChars);
		if (nLast == 0)
			nLast++;

		if (nLast > 0) {
			string ret = path.Substring (0, nLast);
			int l = ret.Length;

			if (l >= 2 && DirectorySeparatorChar == '\\' && ret [l - 1] == VolumeSeparatorChar)
				return ret + DirectorySeparatorChar;
			else if (l == 1 && DirectorySeparatorChar == '\\' && path.Length >= 2 && path [nLast] == VolumeSeparatorChar)
				return ret + VolumeSeparatorChar;
			else {
				//
				// Important: do not use CanonicalizePath here, use
				// the custom CleanPath here, as this should not
				// return absolute paths
				//
				return CleanPath (ret);
			}
		}

		return String.Empty;
	}

	internal static string CleanPath (string s)
	{
		int l = s.Length;
		int sub = 0;
		int start = 0;

		// Host prefix?
		char s0 = s [0];
		if (l > 2 && s0 == '\\' && s [1] == '\\'){
			start = 2;
		}

		// We are only left with root
		if (l == 1 && (s0 == DirectorySeparatorChar || s0 == AltDirectorySeparatorChar))
			return s;

		// Cleanup
		for (int i = start; i < l; i++){
			char c = s [i];
				
			if (c != DirectorySeparatorChar && c != AltDirectorySeparatorChar)
				continue;
			if (i+1 == l)
				sub++;
			else {
				c = s [i + 1];
				if (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar)
					sub++;
			}
		}

		if (sub == 0)
			return s;

		char [] copy = new char [l-sub];
		if (start != 0){
			copy [0] = '\\';
			copy [1] = '\\';
		}
		for (int i = start, j = start; i < l && j < copy.Length; i++){
			char c = s [i];

			if (c != DirectorySeparatorChar && c != AltDirectorySeparatorChar){
				copy [j++] = c;
				continue;
			}

			// For non-trailing cases.
			if (j+1 != copy.Length){
				copy [j++] = DirectorySeparatorChar;
				for (;i < l-1; i++){
					c = s [i+1];
					if (c != DirectorySeparatorChar && c != AltDirectorySeparatorChar)
						break;
				}
			}
		}
		return new String (copy);
	}

	public static string GetPathRoot (string path)
	{
		if (path == null)
			return null;

		if (path.Trim ().Length == 0)
			throw new ArgumentException ("The specified path is not of a legal form.");

		if (!IsPathRooted (path))
			return String.Empty;
			
		if (DirectorySeparatorChar == '/') {
			// UNIX
			return IsDirectorySeparator (path [0]) ? DirectorySeparatorStr : String.Empty;
		} else {
			// Windows
			int len = 2;

			if (path.Length == 1 && IsDirectorySeparator (path [0]))
				return DirectorySeparatorStr;
			else if (path.Length < 2)
				return String.Empty;

			if (IsDirectorySeparator (path [0]) && IsDirectorySeparator (path[1])) {
				// UNC: \\server or \\server\share
				// Get server
				while (len < path.Length && !IsDirectorySeparator (path [len])) len++;

				// Get share
				if (len < path.Length) {
					len++;
					while (len < path.Length && !IsDirectorySeparator (path [len])) len++;
				}

				return DirectorySeparatorStr +
					DirectorySeparatorStr +
					path.Substring (2, len - 2).Replace (AltDirectorySeparatorChar, DirectorySeparatorChar);
			} else if (IsDirectorySeparator (path [0])) {
				// path starts with '\' or '/'
				return DirectorySeparatorStr;
			} else if (path[1] == VolumeSeparatorChar) {
				// C:\folder
				if (path.Length >= 3 && (IsDirectorySeparator (path [2]))) len++;
			} else
				  return Directory.GetCurrentDirectory ().Substring (0, 2);// + path.Substring (0, len);
			return path.Substring (0, len);
		}
	}

	internal static bool IsDirectorySeparator (char c) {
		return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
	}

	public static bool IsPathRooted (string path)
	{
		if (path == null || path.Length == 0)
			return false;

		if (path.IndexOfAny (InvalidPathChars) != -1)
			throw new ArgumentException ("Illegal characters in path.");

		char c = path [0];
		return (c == DirectorySeparatorChar 	||
				c == AltDirectorySeparatorChar 	||
				(true && path.Length > 1 && path [1] == VolumeSeparatorChar));
	}

}
