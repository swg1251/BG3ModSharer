using System.Xml.Linq;

namespace BG3ModSharer
{
	internal class Program
	{
		static void Main(string[]? args)
		{
			// find game and appdata folders
			var appDataFolderPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Larian Studios",
				"Baldur's Gate 3");

			var searchStart = Path.Combine("Z:", "home", "deck", ".steam", "steam", "steamapps", "common", "Baldurs Gate 3");
			if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
			{
				searchStart = args[0];
			}

			// start searching in the deck path - windows should figure it out via search
			var gameFolderPath = GetGameFolder(searchStart);
			if (string.IsNullOrEmpty(gameFolderPath))
			{
				return;
			}

			if (!Directory.Exists(appDataFolderPath))
			{
				Console.WriteLine("App data folder not found. Press any key to quit.");
				Console.ReadKey();
				return;
			}

			Console.WriteLine("\nWelcome!");
			Console.WriteLine($"Found Game folder: {gameFolderPath}");
			Console.WriteLine($"Found AppData folder: {appDataFolderPath}");
			Console.WriteLine();
			Console.WriteLine("Press 'i' for install, 'u' for uninstall, or 'q' to quit.");
			Console.WriteLine("Note that uninstalling will remove *all* mods, not just those included here.");
			Console.WriteLine("You will have a chance to confirm before performing any actions.\n");
			
			var input = Console.ReadKey().KeyChar;
			Console.WriteLine("\n");

			var modsGameFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Mods", "Game");
			var modsAppDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Mods", "AppData");

			switch (input)
			{
				case 'i':
					Install(gameFolderPath, appDataFolderPath, modsGameFolderPath, modsAppDataPath);
					break;
				case 'u':
					Uninstall(gameFolderPath, appDataFolderPath, modsGameFolderPath, modsAppDataPath);
					break;
				case 'q':
					return;
				default:
					Console.WriteLine("Invalid key. Try again.");
					break;
			}

			Console.WriteLine("\n\n");
			Main(new string[] {gameFolderPath});
		}

		static string GetGameFolder(string searchPath)
		{
			if (File.Exists(Path.Combine(searchPath, "bin", "bg3.exe")))
			{
				return searchPath;
			}

			// try to find game folder if not found (just look in program files/steam libraries on each drive)
			Console.WriteLine($"Game not found at {searchPath} - searching for game folder...");

			try
			{
				var drives = DriveInfo.GetDrives();
				foreach (var drive in drives.Select(d => d.Name))
				{
					var searchPaths = new List<string>
				{
					Path.Combine(drive, "Program Files (x86)"),
					Path.Combine(drive, "Program Files"),
					Path.Combine(drive, "SteamLibrary"),
				};

					foreach (var path in searchPaths)
					{
						if (!Directory.Exists(path))
						{
							Console.WriteLine($"Couldn't find {path}");
							continue;
						}

						var searchOptions = new EnumerationOptions
						{
							IgnoreInaccessible = true,
							RecurseSubdirectories = true,
						};

						var dirs = Directory.GetDirectories(path, "*Baldurs Gate 3", searchOptions);
						foreach (var dir in dirs)
						{
							Console.WriteLine(dir);
							if (File.Exists(Path.Combine(dir, "bin", "bg3.exe")))
							{
								Console.WriteLine("Game located!");
								return dir;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error trying to automatically locate game folder: {ex.Message}");
				Console.WriteLine("You may be able to proceed via manually entering the path to your BG3 installation.");
			}

			Console.WriteLine($"Enter/paste the path here, or type 'q' to quit.");
			searchPath = Console.ReadLine() ?? string.Empty;
			if (searchPath == "q")
			{
				return string.Empty;
			}
			return GetGameFolder(searchPath);
		}

		static void Install(string gameFolderPath, string appDataPath, string modsGameFolderPath, string modsAppDataPath)
		{
			var settingsFilePath = Path.Combine(modsAppDataPath, "PlayerProfiles", "Public", "modsettings.lsx");
			WriteModList(modsGameFolderPath, settingsFilePath);

			Console.WriteLine("Would you like to proceed? Enter 'y' or 'n'.");
			var input = Console.ReadKey().KeyChar;
			if (input != 'y')
			{
				Console.WriteLine("\nAborting.");
				return;
			}

			Console.WriteLine("\n");

			// copy all files from game and appdata folders
			var filesToCopy = new List<string>();
			filesToCopy.AddRange(Directory.GetFiles(modsGameFolderPath, "*", SearchOption.AllDirectories));
			filesToCopy.AddRange(Directory.GetFiles(modsAppDataPath, "*", SearchOption.AllDirectories));

			foreach (var file in filesToCopy)
			{
				var destPath = file
					.Replace(modsGameFolderPath, gameFolderPath)
					.Replace(modsAppDataPath, appDataPath);
				var destDir = Path.GetDirectoryName(destPath);

				if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
				{
					Directory.CreateDirectory(destDir);
				}

				File.Copy(file, destPath, true);
				Console.WriteLine($"Copied to {destPath}");
			}
			Console.WriteLine("Installation complete!");
		}

		static void Uninstall(string gameFolderPath, string appDataPath, string modsGameFolderPath, string modsAppDataPath)
		{
			var settingsFilePath = Path.Combine(appDataPath, "PlayerProfiles", "Public", "modsettings.lsx");
			var originalSettingsFilePath = Path.Combine(modsAppDataPath, "PlayerProfiles", "Public", "modsettings_original.lsx");
			WriteModList(gameFolderPath, settingsFilePath, true);

			Console.WriteLine("Would you like to proceed? Enter 'y' or 'n'.");
			var input = Console.ReadKey().KeyChar;
			if (input != 'y')
			{
				Console.WriteLine("Aborting.");
				return;
			}

			Console.WriteLine("\n");

			if (!File.Exists(settingsFilePath) || !File.Exists(originalSettingsFilePath))
			{
				Console.WriteLine($"Unable to find settings file(s). Aborting.");
				return;
			}

			var partyLimitDllPath = Path.Combine(gameFolderPath, "bin", "bink2w64.dll");
			var originalPartyLimitDllPath = Path.Combine(modsGameFolderPath, "bin", "bink2w64_original.dll");

			var filesToDelete = new List<string>
			{
				settingsFilePath, // modsettings.lsx
				Path.Combine(appDataPath, "PlayerProfiles", "Public", "modsettings_original.lsx"), // backup in dest folder
				Path.Combine(gameFolderPath, "bin", "DWrite.dll"), // script extender dll
				Path.Combine(gameFolderPath, "bin", "ScriptExtenderSettings.json"), // script extender settings
				partyLimitDllPath, // party limit dll
				partyLimitDllPath.Replace(".dll", "_original.dll") // backup in dest folder
			};

			// delete all .pak files
			filesToDelete.AddRange(Directory.GetFiles(Path.Combine(appDataPath, "Mods")).Where(f => f.EndsWith(".pak")));

			foreach (var file in filesToDelete)
			{
				if (File.Exists(file))
				{
					File.Delete(file);
					Console.WriteLine($"Deleted {file}");
				}
			}

			// delete Mods folder added for part limit mod
			var partyLimitModsFolder = Path.Combine(gameFolderPath, "Data", "Mods");
			if (Directory.Exists(partyLimitModsFolder))
			{
				Directory.Delete(partyLimitModsFolder, true);
				Console.WriteLine($"Deleted directory {partyLimitModsFolder}");
			}

			// restore modsettings.lsx from original
			File.Copy(originalSettingsFilePath, settingsFilePath, true);
			Console.WriteLine($"Copied {originalSettingsFilePath} to {settingsFilePath}");

			// restore party limit dll
			File.Copy(originalPartyLimitDllPath, partyLimitDllPath, true);
			Console.WriteLine($"Copied {originalPartyLimitDllPath} to {partyLimitDllPath}");

			Console.WriteLine("Uninstallation complete!");
		}

		static void WriteModList(string gameFolderPath, string settingsFilePath, bool forUninstall = false)
		{
			if (!File.Exists(settingsFilePath))
			{
				Console.WriteLine($"Unable to find mod settings file at {settingsFilePath}.");
			}

			var modList = GetModListFromSettingsFile(settingsFilePath);

			var scriptExtenderFilename = Path.Combine(gameFolderPath, "bin", "DWrite.dll");
			if (File.Exists(scriptExtenderFilename))
			{
				modList.Add("Script Extender");
			}

			var partyLimitModDllFilename = Path.Combine(gameFolderPath, "bin", "bink2w64_original.dll");
			if (File.Exists(partyLimitModDllFilename))
			{
				modList.Add("Party Limit Begone");
			}

			if (!modList.Any())
			{
				Console.WriteLine($"No installed mods found. Original mod order/dlls will be restored.\n");
				return;
			}

			Console.WriteLine($"The following mods were found and will be {(forUninstall ? "un" : "")}installed:");
			foreach (var mod in modList)
			{
				Console.WriteLine(mod);
			}
			Console.WriteLine();
		}

		static List<string> GetModListFromSettingsFile(string path)
		{
			var mods = new List<string>();
			var modSettingsXml = XDocument.Load(path);
			var modsParentNode = modSettingsXml.Root?
				.Descendants().First(n => n.Attribute("id")?.Value == "ModuleSettings")?
				.Descendants().First(n => n.Attribute("id")?.Value == "root")?
				.Descendants().First()?
				.Descendants().First(n => n.Attribute("id")?.Value == "Mods")?
				.Descendants().First();

			foreach (var modNode in modsParentNode.Descendants())
			{
				// attributes are added as child nodes rather than attributes of the node
				var name = modNode.Descendants()
					.FirstOrDefault(a => a.Attribute("id")?.Value == "Name")?
					.Attribute("value")?.Value;

				if (!string.IsNullOrEmpty(name) && name != "GustavDev")
				{
					mods.Add(name);
				}
			}

			return mods;
		}
	}
}