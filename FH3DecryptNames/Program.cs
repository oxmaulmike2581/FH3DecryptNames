using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FH3DecryptNames
{
	public class Program
	{
		private static bool ProcessZipsOnly = false;
		private static string StartPath = "";

		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: fh3decryptnames.exe path_to_dir [-zip]\n");
				Console.WriteLine("\"-zip\" is an optional switch to process only zip archives (unpack, decrypt all files, and pack them back) using 7-zip.");
				Console.WriteLine("path_to_dir - the input path, for example: \"C:\\FH3\"\n");
				Console.WriteLine("Notes:");
				Console.WriteLine("* if the \"media\" folder is present inside input path, it will be renamed.");
				Console.WriteLine("* this tool was written and tested on windows 11 22h2 and forza horizon 3 codex release.\n");
				Console.WriteLine("(c) 2022 Mike Oxmaul.\n\n");
				Console.WriteLine("Press any key to exit.");
				return;
			}
			Console.WriteLine("Started.");
			StartPath = args[0];

			// set the flag
			if ((args.Length > 1) && (args[1] == "-zip"))
			{
				ProcessZipsOnly = true;
			}

			if (ProcessZipsOnly)
			{
				// get all entries and then process them
				IEnumerable<string> zips = Directory.GetFiles(StartPath, "*.zip", SearchOption.AllDirectories);
				foreach (string zip in zips)
				{
					// create a folder where the archive will be unpacked
					string pathToUnpack = Path.Combine(Path.GetDirectoryName(zip), $"z_{Path.GetFileNameWithoutExtension(zip)}");
					pathToUnpack.Evaluate();

					// unpack it
					Launch7z(zip, pathToUnpack, unzip: true);

					// decrypt all names
					IEnumerable<string> files = Directory.GetFiles(pathToUnpack, "*", SearchOption.AllDirectories);
					ProcessFiles(files, pathToUnpack, false, true);

					// delete original
					File.Delete(zip);

					// delete empty directories
					IEnumerable<string> dirs = Directory.GetDirectories(pathToUnpack, "*", SearchOption.TopDirectoryOnly);
					foreach (string dir in dirs)
					{
						if (DirectoryIsEmpty(dir))
						{
							Directory.Delete(dir, true);
						}
					}

					// pack it
					Launch7z(zip, pathToUnpack, Path.GetDirectoryName(zip), false);

					// remove folders with unpacked content
					Directory.Delete(pathToUnpack, true);
				}
			}
			else
			{
				if (Directory.Exists(StartPath))
				{
					// check if we have "media" folder - rename it and work inside it
					string mediaDirEnc = Path.Combine(StartPath, "gq6$l");
					//string mediaDirDec = Path.Combine(StartPath, "media");
					if (Directory.Exists(mediaDirEnc))
					{
						//Directory.Move(mediaDirEnc, mediaDirDec);
						StartPath = mediaDirEnc;
					}

					// get all entries and then process them
					IEnumerable<string> paths = Directory.GetFiles(StartPath, "*", SearchOption.AllDirectories);
					ProcessFiles(paths);
				}
				else
				{
					Console.WriteLine("Input directory does not exist or access denied.");
					return;
				}
			}
			
			Console.WriteLine("Finished.");
		}

		static void ProcessFiles(IEnumerable<string> files, string customStartPath = null, bool onlyLastPart = false, bool deleteOriginal = false)
		{
			foreach (string path in files)
			{
				// remove unneccessary part of path from entry
				string shortened;
				if (!string.IsNullOrEmpty(customStartPath))
				{
					shortened = path.Replace(customStartPath + "\\", "");
				}
				else
				{
					shortened = path.Replace(StartPath + "\\", "");
				}

				// process our string
				string decoded = ProcessString(shortened, onlyLastPart);

				// compile a new path
				string newPath;
				if (!string.IsNullOrEmpty(customStartPath))
				{
					newPath = customStartPath.Replace("gq6$l", "media") + "\\" + decoded;
				}
				else
				{
					newPath = StartPath.Replace("gq6$l", "media") + "\\" + decoded;
				}

				// read source file
				Console.WriteLine($"Reading \"{path}\"");

				// evaluate the path
				newPath.Evaluate();

				// write source file's contents to the destination
				Console.WriteLine($"Writing \"{newPath}\"");

				// determine if source path is point to the file or directory
				// https://stackoverflow.com/a/1395226
				FileAttributes attr = File.GetAttributes(path);
				if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
				{
					Directory.CreateDirectory(newPath);
					Directory.Move(path, newPath);
					if (deleteOriginal)
					{
						Directory.Delete(path, true);
					}
				}
				else
				{
					File.Copy(path, newPath);
					if (deleteOriginal)
					{
						File.Delete(path);
					}
				}

				// print empty string as separator
				Console.WriteLine("");
			}
		}

		static string ProcessString(string src, bool onlyLastPart = false)
		{
			// split our entry
			string[] parts = src.Split('\\');

			// initialize a new string
			StringBuilder sb = new StringBuilder();

			// for all parts
			for (int i = 0; i < parts.Length; i++)
			{
				if (onlyLastPart)
				{
					if (i == parts.Length - 1)
					{
						parts[i] = parts[i].DecryptFH();
					}
				}
				else
				{
					// decrypt
					parts[i] = parts[i].DecryptFH();
				}

				// add to new string
				sb.Append(parts[i]);

				// if we process a non-last part - add a directory separator
				if (i != parts.Length - 1)
				{
					sb.Append("\\");
				}
			}

			return sb.ToString();
		}

		// https://stackoverflow.com/a/8595185
		static bool DirectoryIsEmpty(string path)
		{
			int fileCount = Directory.GetFiles(path).Length;
			if (fileCount > 0)
			{
				return false;
			}

			string[] dirs = Directory.GetDirectories(path);
			foreach (string dir in dirs)
			{
				if (!DirectoryIsEmpty(dir))
				{
					return false;
				}
			}

			return true;
		}

		static void Launch7z(string archive, string dir, string outdir = null, bool unzip = true)
		{
			if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.exe")))
			{
				Console.WriteLine("7z.exe is not found in app directory. Can't continue.");
				Environment.Exit(0);
			}

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = "7z.exe",
				UseShellExecute = false,
				CreateNoWindow = false
			};

			if (unzip)
			{
				psi.Arguments = $"x -o\"{dir}\" {archive}";
			}
			else
			{
				psi.Arguments = $"a -tzip -o\"{outdir}\" {archive} \"{dir}\\*\"";
			}

			Process p = new Process
			{
				StartInfo = psi
			};

			p.Start();
			p.WaitForExit();

			if (p.HasExited)
			{
				return;
			}
		}
	}

	public static class Extensions
	{
		public static string DecryptFH(this string str)
		{
			// https://forum.xentax.com/viewtopic.php?t=21916
			Dictionary<char, char> chars = new Dictionary<char, char>()
			{
				{ 'l', 'a' },
				{ '`', 'b' },
				{ '^', 'c' },
				{ '6', 'd' },
				{ 'q', 'e' },
				{ 'v', 'f' },
				{ '{', 'g' },
				{ '@', 'h' },
				{ '$', 'i' },
				{ '7', 'j' },
				{ 's', 'k' },
				{ 'b', 'l' },
				{ 'g', 'm' },
				{ '8', 'n' },
				{ 'h', 'o' },
				{ 'u', 'p' },
				{ 'f', 'q' },
				{ '4', 'r' },
				{ '~', 's' },
				{ '1', 't' },
				{ '=', 'u' },
				{ '\'', 'v' },
				{ 'm', 'w' },
				{ ']', 'x' },
				{ '!', 'y' },
				{ ',', 'z' },
				{ 'y', '_' },
				{ '_', '-' },
				{ '[', '0' },
				{ '0', '1' },
				{ 'w', '2' },
				{ 'k', '3' },
				{ '(', '4' },
				{ '2', '5' },
				{ 'j', '6' },
				{ '}', '7' },
				{ ';', '8' },
				{ '+', '9' }
			};

			return string.Join("", str.ToCharArray().Select(i => chars.ContainsKey(i) ? chars[i] : i));
		}

		public static void Evaluate(this string path)
		{
			string folder = Path.GetDirectoryName(path);

			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
		}
	}
}
