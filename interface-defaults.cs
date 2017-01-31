using System;

class Test
{
    public static void Main (String [] arguments)
    {
        var a = new Default ();
        var b = new Overrides ();
        a.Method ();
        b.Method ();
    }
}

interface IHasDefault
{
    void Method ()
    {
        Console.WriteLine ("Default implementation called.");
    }
}

class Default : IHasDefault
{
}

class Overrides : IHasDefault
{
    public void Method ()
    {
        Console.WriteLine ("Overridden implementation called.");
    }
}
