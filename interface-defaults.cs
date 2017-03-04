using System;
using System.Runtime.CompilerServices;

class Test
{
    [MethodImpl (MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static void Main (String [] arguments)
    {
        try {
            var a = new Default ();
            var b = new Overrides ();
            // Console.WriteLine ("*** Calling default implementation.");
            // a.Method ();
            Console.WriteLine ("*** Calling overridden implementation.");
            b.Method ();
            Console.WriteLine ("*** Done.");
        } catch (Exception e) {
            Console.WriteLine ("*** Method was missing: {0}", e.ToString ());
        }
    }
}

interface IHasDefault
{
    void Method ()
    {
        Console.WriteLine ("*** Default implementation called.");
    }
}

class Default : IHasDefault
{
}

class Overrides : IHasDefault
{
    public void Method ()
    {
        Console.WriteLine ("*** Overridden implementation called.");
    }
}
