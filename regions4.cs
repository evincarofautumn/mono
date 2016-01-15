using System;

public class Test
{
	public static void Main (String [] args)
	{
		Run1 ();
	}
	static void Run1 ()
	{
		var x = new int [100];
        var y = new Junk ();
        y.field = x;
	}
}

class Junk {
    public int[] field;
}
