using System;

public class Test
{
	public static void Main (String [] args)
	{
        int[] y = null;
		Run1 (ref y);
	}
	static void Run1 (ref int[] y)
	{
		var x = new int [100];
        y = x;
        Run2 ();
	}
    static void Run2 ()
    {
        var x = new int [100];
    }
}
