using System;

public class Test
{
	public static void Main (String [] args)
	{
        for (int i = 0; i < 10; ++i)
            Run ();
	}
	static void Run ()
	{
        var tree = MakeTree (3);
	}
    static Node MakeTree (int depth)
    {
        return depth == 0 ? null
            : new Node (MakeTree (depth - 1), MakeTree (depth - 1));
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
