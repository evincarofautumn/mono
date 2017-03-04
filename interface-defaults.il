.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'interface-defaults'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'interface-defaults.exe' // GUID = {E087409E-D352-442C-8DD0-FCE3049D229A}


  .class private auto ansi beforefieldinit Test
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Test::.ctor

    // method line 2
    .method public static hidebysig 
           default void Main (string[] arguments)  cil managed noinlining nooptimization 
    {
        // Method begins at RVA 0x2058
	.entrypoint
	// Code size 66 (0x42)
	.maxstack 2
	.locals init (
		class Default	V_0,
		class Overrides	V_1,
		class [mscorlib]System.Exception	V_2)
	.try { // 0
	    newobj instance void class Default::'.ctor'()
	    stloc.0 
	    newobj instance void class Overrides::'.ctor'()
	    stloc.1 
	    ldstr "*** Calling default implementation."
	    call void class [mscorlib]System.Console::WriteLine(string)
	    ldloc.0 
	    callvirt instance void class Default::Method()
	    ldstr "*** Calling overridden implementation."
	    call void class [mscorlib]System.Console::WriteLine(string)
	    ldloc.1 
	    callvirt instance void class Overrides::Method()
	    ldstr "*** Done."
	    call void class [mscorlib]System.Console::WriteLine(string)
	    leave END

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	    stloc.2 
	    ldstr "*** Method was missing: {0}"
	    ldloc.2 
	    callvirt instance string object::ToString()
	    call void class [mscorlib]System.Console::WriteLine(string, object)
	    leave END

	} // end handler 0
	END:  ret 
    } // end of method Test::Main

  } // end of class Test

  .class interface private auto ansi IHasDefault
  {

    // method line 3
    .method public virtual hidebysig newslot abstract 
           instance default void Method ()  cil managed 
    {
        // Method begins at RVA 0x20b8
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldstr "*** Default implementation called."
	IL_0005:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000a:  ret 
    } // end of method IHasDefault::Method

  } // end of class IHasDefault

  .class private auto ansi beforefieldinit Default
  	extends [mscorlib]System.Object
  	implements IHasDefault  {

    // method line 4
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20c4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Default::.ctor

  } // end of class Default

  .class private auto ansi beforefieldinit Overrides
  	extends [mscorlib]System.Object
  	implements IHasDefault  {

    // method line 5
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20cc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Overrides::.ctor

    // method line 6
    .method public final virtual hidebysig newslot 
           instance default void Method ()  cil managed 
    {
        // Method begins at RVA 0x20d4
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldstr "*** Overridden implementation called."
	IL_0005:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000a:  ret 
    } // end of method Overrides::Method

  } // end of class Overrides

