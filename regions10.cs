using System;

public class Test
{
	public static void Main (String [] args)
	{
        for (int i = 0; i < 1000; ++i)
            Run1 ();
	}
	static void Run1 ()
	{
        var list = new Node (new Node (new Node (null)));
	}
}

class Node
{
    Node next;
    public Node (Node n)
    {
        next = n;
    }
}