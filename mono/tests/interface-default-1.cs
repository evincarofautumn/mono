using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    class DefaultImplementationAttribute : Attribute
    {
        public DefaultImplementationAttribute(string[] defaultImplementations)
        {
            DefaultImplementations = defaultImplementations;
        }
        public string[] DefaultImplementations { get; }
    }
}

namespace DefaultInterfacesBasic
{
    [DefaultImplementationAttribute(new string[] { "DefaultImplementation"})]
    interface IInterfaceWithDefault
    {
        string DefaultMethod();
    }
    // MOVE THIS TYPE TO BE NESTED IN IInterfaceWithDefault
    static class DefaultImplementation
    {
        static string DefaultMethod(IInterfaceWithDefault actualThis)
        {
            return "DefaultImplementation";
        }
    }
    class ClassThatExplicitlyImplementsInterface : IInterfaceWithDefault
    {
        public string DefaultMethod()
        {
            return "NotDefaultImplementation";
        }
    }
    class ClassThatDoesnotImplementMethod : IInterfaceWithDefault
    {
        // DELETE THIS METHOD
        public string DefaultMethod()
        {
            return "NotDefaultImplementation";
        }
    }
    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static string CallDefaultMethod(IInterfaceWithDefault iface)
        {
            return iface.DefaultMethod();
        }
        static int Main(string[] args)
        {
            if (!"NotDefaultImplementation".Equals(CallDefaultMethod(new ClassThatExplicitlyImplementsInterface())))
                return 1;
            if (!"DefaultImplementation".Equals(CallDefaultMethod(new ClassThatDoesnotImplementMethod())))
                return 2;
            return 0;
        }
    }
}
