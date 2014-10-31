using System;
using System.Threading;

class Host {

	public int Field = 42;

	public Host () {}

	~Host () {
		Console.WriteLine ("got finalizated");
		Program.resed = this;
	}
}


class Program {
		internal static Host resed;
		static int result;

		static void DoStuff ()
        {
			new Host ();
        }

		static bool CheckStuff () {
			if (resed == null)
				return false;
			result = resed.Field;
			resed = null;
			return true;
		}

		public static int Main ()
        {
			int cnt = 5;
			var t = new Thread (DoStuff);
			t.Start ();
			t.Join ();
			do {
				if (CheckStuff ())
					break;
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
				Thread.Sleep (10);
			} while (cnt-- > 0);
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			Console.WriteLine ("done with finalizers");
			return result == 42 ? 0 : 1;
		}
}
