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
        var tree = new Node (
            new Node (new Node (null, null), new Node (null, null)),
            new Node (new Node (null, null), new Node (null, null))
        );
	}
}

class Node
{
    Node left, right;
    public Node (Node l, Node r)
    {
        left = l;
        right = r;
    }
}
