using System;

[assembly: CLSCompliant(true)]
namespace TextPet {	
	class Program {
		public static string Version => "v1.0.0";
		
		static int Main(string[] args) {
			string originalConsoleTitle = Console.Title;
			int errorLevel = 0;
#if !DEBUG
			try {
#endif
				Console.Title = "TextPet CLI";
				Console.WriteLine("TextPet " + Version + " by Prof. 9");
				Console.WriteLine();
				
				TextPetCore core = new TextPetCore();

				errorLevel = new CommandLineInterface(core).Run(args);
#if !DEBUG
			} catch (Exception ex) {
				Console.WriteLine("FATAL: " + ex.Message);
				errorLevel = 2;
			}
#else
			Console.ReadKey();
#endif
			Console.Title = originalConsoleTitle;
			return errorLevel;
		}
	}
}
