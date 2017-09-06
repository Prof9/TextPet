using System;

[assembly: CLSCompliant(true)]
namespace TextPet {	
	class Program {
		public static string Version => "v1.0-alpha2";

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
		static void Main(string[] args) {
#if !DEBUG
			try {
#endif
				Console.Title = "TextPet CLI";
				Console.WriteLine("TextPet " + Version + " by Prof. 9");
				Console.WriteLine();
				
				TextPetCore core = new TextPetCore();

				new CommandLineInterface(core).Run(args);
#if !DEBUG
			} catch (Exception ex) {
				Console.WriteLine("FATAL: " + ex.Message);
			}
#else
			Console.ReadKey();
#endif
		}
	}
}
