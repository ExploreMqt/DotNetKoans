using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace AutoKoanRunner
{
	class Program
	{
		private static readonly string CSharpKoanSource = @"..\..\..\CSharp";
		private static readonly string CSharpKoansAssembly = @"..\..\..\CSharp\bin\debug\csharp.dll";

		private static readonly string VBNetKoanSource = @"..\..\..\VBNet";
		private static readonly string VBNetKoansAssembly = @"..\..\..\VBNet\bin\debug\VBNet.dll";

		private static readonly string koansRunner = @"..\..\..\KoanRunner\bin\debug\koanrunner.exe";
		private static DateTime lastChange;
        private static void Main(string[] args)
		{
			if (Directory.Exists(CSharpKoanSource) == false)
			{
				Console.WriteLine("The CSharp Koans were not where we expect them to be.");
				return;
			}
			if (Directory.Exists(VBNetKoanSource) == false)
			{
				Console.WriteLine("The VB.Net Koans were not where we expect them to be.");
				return;
			}
			FileSystemWatcher CSharpWatcher = new FileSystemWatcher(CSharpKoanSource, "*.cs");
			FileSystemWatcher VBNetWatcher = new FileSystemWatcher(VBNetKoanSource, "*.vb");
			try
			{
				CSharpWatcher.Changed += StartRunner;
				CSharpWatcher.NotifyFilter = NotifyFilters.LastWrite;
				CSharpWatcher.EnableRaisingEvents = true;
				
				VBNetWatcher.Changed += StartRunner;
				VBNetWatcher.NotifyFilter = NotifyFilters.LastWrite;
				VBNetWatcher.EnableRaisingEvents = true;

				lastChange = DateTime.MinValue;

				StartRunner(null, null);//Auto run the first time

				Console.WriteLine("When you save a Koan, we'll check your work.");
				Console.WriteLine("Press a key to exit...");
				Console.ReadKey();
				
				CSharpWatcher.Changed -= StartRunner;
				VBNetWatcher.Changed -= StartRunner;
			}
			finally
			{
				if (CSharpWatcher != null)
					CSharpWatcher.Dispose();
				if (VBNetWatcher != null)
					VBNetWatcher.Dispose();
			}
		}
		private static void StartRunner(object sender, FileSystemEventArgs e)
		{
			if (e != null)
			{
				DateTime timestamp = File.GetLastWriteTime(e.FullPath);
				if (lastChange.ToString() == timestamp.ToString())// Use string version to eliminate second save by VS a fraction of a second later
					return;
				lastChange = timestamp;
			}
			BuildProject("CSharp");
			BuildProject("VBNet");
			RunKoans(koansRunner, CSharpKoansAssembly, "CSharp");
			RunKoans(koansRunner, VBNetKoansAssembly, "VBNet");
		}
		private static bool BuildProject(string Project)
		{
			Console.WriteLine("Building...");
			using (Process build = new Process())
			{
				build.StartInfo.FileName = "devenv";
				build.StartInfo.Arguments = String.Format(@"/build Debug /project {0} ..\..\..\DotNetKoans.sln", Project);
				build.StartInfo.CreateNoWindow = true;
				build.Start();
				build.WaitForExit();
			}
			return false;
		}
		private static void RunKoans(string koansRunner, string koansAssembly, string projectName)
		{
			if (File.Exists(koansAssembly))
			{
				Console.WriteLine("Checking Koans...");
				using (Process launch = new Process())
				{
					launch.StartInfo.FileName = koansRunner;
					launch.StartInfo.Arguments = koansAssembly;
					launch.StartInfo.RedirectStandardOutput = true;
					launch.StartInfo.UseShellExecute = false;
					launch.Start();
					string output = launch.StandardOutput.ReadToEnd();
					launch.WaitForExit();
					EchoResult(output, projectName);
				}
			}
			File.Delete(koansAssembly);
		}
		private static void EchoResult(string output, string koanProjectName)
		{
			string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			const string kExpanded = "has expanded your";
			const string kDamaged = "has damaged your";
			Array.ForEach(lines, line =>
			{
				string partialNamespace = String.Format(".{0}.", koanProjectName);
				if (line.Contains(kExpanded))
				{
					PrintTestLine(line, ConsoleColor.Green, kExpanded, partialNamespace);
				}
				else if (line.Contains(kDamaged))
				{
					PrintTestLine(line, ConsoleColor.Red, kDamaged, partialNamespace);
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.White;
					Console.WriteLine(line);
				}
			});
		}
		private static void PrintTestLine(string line, ConsoleColor accent, string action, string KoanAssembly)
		{
			int testStart = line.IndexOf(KoanAssembly) + KoanAssembly.Length;
			int testEnd = line.IndexOf(action, testStart);
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(line.Substring(0, testStart));
			Console.ForegroundColor = accent;
			Console.Write(line.Substring(testStart, testEnd - testStart));
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(line.Substring(testEnd));
		}
	}
}
