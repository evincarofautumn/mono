using System;

public class Test
{
    static int[] glob;
	public static void Main (String [] args)
	{
        Run1 ();
	}
	static void Run1 ()
	{
		var x = new int [100];
        glob = x;
        for (int i = 0; i < 100; ++i)
            Run2 ();
	}
    static void Run2 ()
    {
        var x = new int [100];
    }
}
