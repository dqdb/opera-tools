// compiling:
//
// @%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc SpeedDialPatch.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.Windows.Forms;

namespace OperaTools
{
	public class Program
	{
		[STAThread]
		private static int Main(string[] args)
		{
			int result;
			
			try
			{
				ColoredConsole.WriteLine("~WOpera Speed Dial Patch 1.3.0~N");
				ColoredConsole.WriteLine("~WCopyright (c) 2013 dqdb~N");
				ColoredConsole.WriteLine();
				Settings settings = new Settings();
				settings.LoadFromConfigFile();
				settings.LoadFromCommandLine(args);
				settings.LoadFromConsole();
				settings.SaveToConfigFile();
				ColoredConsole.WriteLine();
				
				string fileName = FindLatestOperaPak(settings.OperaFolder);
				if (fileName == null)
				{
					ColoredConsole.WriteLine("~r~WError:~k~R unable to find an ~r~Wopera.pak~N to patch.~N");
					return 1;
				}

				Version version;
				
				try
				{
					version = GetOperaPakVersion(fileName);
				}
				catch (Exception)
				{
					ColoredConsole.WriteLine("~r~WError:~k~R unable to get version number from ~r~W{0}~k~R.~N", fileName);
					return 1;
				}
				
				ResourceLayout[] resourceLayouts = new ResourceLayout[]
				{
					//                 StartVersion        EndVersion           SpeeddialLayoutJs
					//                 |                   |                    |      StartPageHtml
					//                 |                   |                    |      |      PreinstalledSpeeddialsJs
					//                 |                   |                    |      |      |      ToolsCss
					//                 |                   |                    |      |      |      |      FilterCss
					//                 |                   |                    |      |      |      |      |
					new ResourceLayout(17, 0, 1232,   0,   17, 0, 1232,   0,    43021, 43515, 43027, 41010, 41008),
					new ResourceLayout(17, 0, 1224,   1,   17, 0, 1224,   1,    43020, 43515, 43026, 41010, 41008),
					
					new ResourceLayout(16, 0, 1196,  45,   16, 0, 1196,  55,    38278, 39011, 38284, 41010, 41008),
					new ResourceLayout(16, 0, 1196,  41,   16, 0, 1196,  41,    38278, 39011, 38284, 41009, 41007),
					new ResourceLayout(16, 0, 1196,  14,   16, 0, 1196,  35,    38276, 39011, 38282, 41009, 41007),
					
					new ResourceLayout(15, 0, 1147, 130,   15, 0, 1147, 153,    38248, 39011, 38254, 41009, 41007),
					new ResourceLayout(15, 0, 1147, 100,   15, 0, 1147, 100,    38248, 39011, 38254, 41008, 41006),
					new ResourceLayout(15, 0, 1147,  72,   15, 0, 1147,  72,    38247, 39011, 38253, 41007, 41005),
					new ResourceLayout(15, 0, 1147,  56,   15, 0, 1147,  61,    38245, 39011, 38251, 41007, 41005),
					new ResourceLayout(15, 0, 1147,  44,   15, 0, 1147,  44,    38247, 39011, 38253, 41007, 41005),
					new ResourceLayout(15, 0, 1147,  18,   15, 0, 1147,  24,    38247, 39010, 38253, 41007, 41005)
				};
				
				ColoredConsole.WriteLine("Opera version: ~W{0}~N", version);
				int layoutId = -1;
				bool layoutFound = false;
				
				for (int n = 0; n < resourceLayouts.Length; n++)
				{
					ResourceLayoutMatch match = resourceLayouts[n].Match(version);
					if (match == ResourceLayoutMatch.True)
					{
						layoutId = n;
						layoutFound = true;
						break;
					}
					else if (match == ResourceLayoutMatch.Maybe)
					{
						layoutId = n;
					}
				}
				
				if (!layoutFound)
				{
					ColoredConsole.WriteLine();
					if (layoutId == -1)
					{
						ColoredConsole.WriteLine("~r~WError:~k~R this Opera version is not supported yet.~N");
						return 1;
					}

					ColoredConsole.WriteLine("~y~KWarning:~k~Y this Opera version is probably supported, but it is ~y~KNOT~k~Y tested yet. Improper patching may have side effects.~N");
					ColoredConsole.WriteLine();
					if (!ColoredConsole.ReadBoolean("I understand the risks: ", false))
						return 1;
					ColoredConsole.WriteLine();
				}

				ResourceLayout resourceLayout = resourceLayouts[layoutId];
				ColoredConsole.WriteLine("Using resource layout for Opera ~W{0}~N - ~W{1}~N builds.", resourceLayout.StartVersion, resourceLayout.EndVersion);
				ColoredConsole.WriteLine();

				if (!ColoredConsole.ReadBoolean("I would like to start patching: ", true))
					return 1;
				ColoredConsole.WriteLine();
				
				PakFile pakFile = new PakFile();
				ColoredConsole.WriteLine("Reading ~W{0}~N ...", fileName);
				pakFile.Load(fileName);
				
				const string TEXT_MAX_X_COUNT = "  var MAX_X_COUNT = ";
				const string TEXT_DIAL_WIDTH = "SpeeddialObject.DIAL_WIDTH = ";
				const string TEXT_DIAL_HEIGHT = "SpeeddialObject.DIAL_HEIGHT = ";
				const string TEXT_CSS_WIDTH = "  width: ";
				const string TEXT_CSS_HEIGHT = "  height: ";
				const string TEXT_CSS_TOP = "  top: ";
				const string TEXT_CSS_LEFT = "  left: ";
				const string TEXT_PREINSTALLED_CHECK_URL_FUNCTION = "  this.checkURL = function(URL)";
				const string TEXT_PREINSTALLED_CHECK_URL_NEXTLINE = "  {";
				const string TEXT_STYLE = "</style>";

				string[] lines = pakFile.GetItem(resourceLayout.SpeeddialLayoutJs);
				for (int n = 0; n < lines.Length; n++)
				{
					string line = lines[n];
					if (line.StartsWith(TEXT_MAX_X_COUNT))
						lines[n] = TEXT_MAX_X_COUNT + settings.SpeedDialColumns.ToString() + ";";
					else if (line.StartsWith(TEXT_DIAL_WIDTH))
						lines[n] = TEXT_DIAL_WIDTH + settings.SpeedDialPreviewWidth.ToString() + ";";
					else if (line.StartsWith(TEXT_DIAL_HEIGHT))
						lines[n] = TEXT_DIAL_HEIGHT + settings.SpeedDialPreviewHeight.ToString() + ";";
				}
				pakFile.SetItem(resourceLayout.SpeeddialLayoutJs, lines);
				
				lines = pakFile.GetItem(resourceLayout.StartPageHtml);
				for (int n = 0; n < lines.Length; n++)
				{
					string line = lines[n];
					if (line == ".speeddial")
					{
						Program.PatchCssRule(lines, n, TEXT_CSS_WIDTH, settings.SpeedDialPreviewWidth);
						Program.PatchCssRule(lines, n, TEXT_CSS_HEIGHT, settings.SpeedDialPreviewHeight);
					}
					else if (line == ".dial-thumbnail")
					{
						Program.PatchCssRule(lines, n, TEXT_CSS_WIDTH, (settings.SpeedDialPreviewWidth - 36) / 2);
						Program.PatchCssRule(lines, n, TEXT_CSS_HEIGHT, (settings.SpeedDialPreviewHeight - 28) / 2);
					}
					else if (line == ".dial-thumbnail:nth-child(2)")
					{
						Program.PatchCssRule(lines, n, TEXT_CSS_LEFT, settings.SpeedDialPreviewWidth / 2);
					}
					else if (line == ".dial-thumbnail:nth-child(3)")
					{
						Program.PatchCssRule(lines, n, TEXT_CSS_TOP, settings.SpeedDialPreviewHeight / 2 - 1);
					}
					else if (line == ".dial-thumbnail:nth-child(4)")
					{
						Program.PatchCssRule(lines, n, TEXT_CSS_LEFT, settings.SpeedDialPreviewWidth / 2);
						Program.PatchCssRule(lines, n, TEXT_CSS_TOP, settings.SpeedDialPreviewHeight / 2 - 1);
					}
					else if (line.StartsWith(TEXT_STYLE))
					{
						lines[n] = TEXT_STYLE + settings.GetPatches("<style>", "</style>", CssPatchFile.StartPageHtml);
					}
				}
				pakFile.SetItem(resourceLayout.StartPageHtml, lines);
				
				lines = pakFile.GetItem(resourceLayout.ToolsCss);
				lines[lines.Length - 1] = settings.GetPatches("", "", CssPatchFile.ToolsCss);
				pakFile.SetItem(resourceLayout.ToolsCss, lines);

				lines = pakFile.GetItem(resourceLayout.FilterCss);
				lines[lines.Length - 1] = settings.GetPatches("", "", CssPatchFile.FilterCss);
				pakFile.SetItem(resourceLayout.FilterCss, lines);
				
				lines = pakFile.GetItem(resourceLayout.PreinstalledSpeeddialsJs);
				for (int n = 0; n < lines.Length; n++)
				{
					string line = lines[n];
					if (line == TEXT_PREINSTALLED_CHECK_URL_FUNCTION && n < lines.Length - 1 && 
						lines[n + 1].StartsWith(TEXT_PREINSTALLED_CHECK_URL_NEXTLINE))
					{
						lines[n + 1] = TEXT_PREINSTALLED_CHECK_URL_NEXTLINE + (settings.DisableBuiltInImages ? "return null;" : "");
						break;
					}
				}
				pakFile.SetItem(resourceLayout.PreinstalledSpeeddialsJs, lines);
				
				ColoredConsole.WriteLine("Writing ~W{0}~N ...", fileName);
				
				pakFile.Save(fileName + ".temp");
				File.Move(fileName, String.Format("{0}.backup.{1:yyyyMMddHHmmss}", fileName, DateTime.Now));
				File.Move(fileName + ".temp", fileName);
				result = 0;
			}
			catch (Exception ex)
			{
				ColoredConsole.WriteLine();
                if (ex.InnerException == null)
                    ColoredConsole.WriteLine("~r~WError:~k~R {0}~N", ex.Message);
                else
                    ColoredConsole.WriteLine("~r~WError:~k~R {0}~N ----> {1}", ex.Message, ex.InnerException.ToString());
				result = 1;
			}
			return result;
		}
		
		private static Version GetOperaPakVersion(string fileName)
		{
			string[] components = Path.GetFileName(Path.GetDirectoryName(fileName)).Split('.');
			return new Version(Convert.ToInt32(components[0]), Convert.ToInt32(components[1]), Convert.ToInt32(components[2]), Convert.ToInt32(components[3]));
		}
		
		private static string FindLatestOperaPak(string baseFolder)
		{
			string[] folders = Directory.GetDirectories(baseFolder);
			
			Array.Sort(folders);
			Array.Reverse(folders);

			foreach (string folder in folders)
			{
				string folder1 = Path.GetFileName(folder);
				int dots = 0;
				bool ok = true;
				foreach (char ch in folder1)
				{
					if (ch == '.')
					{
						dots++;
					}
					else if (!Char.IsNumber(ch))
					{
						ok = false;
						break;
					}
				}
				
				if (ok && dots == 3)
				{
					string fileName = Path.Combine(folder, "opera.pak");
					if (File.Exists(fileName))
						return fileName;
				}
			}
			
			return null;
		}
		
		private static void PatchCssRule(string[] lines, int index, string name, int value)
		{
			for (int n = index + 1; n < lines.Length; n++)
			{
				string text = lines[n];
				if (text == "}")
					break;
				else if (text.StartsWith(name))
					lines[n] = name + value.ToString() + "px;";
			}
		}
	}
	
	public enum ResourceLayoutMatch
	{
		True,
		False,
		Maybe
	}
	
	public class ResourceLayout
	{
		public Version StartVersion;
		public Version EndVersion;
		
		public int SpeeddialLayoutJs;
		public int StartPageHtml;
		public int PreinstalledSpeeddialsJs;
		public int ToolsCss;
		public int FilterCss;

		public ResourceLayout(int startVersionMajor, int startVersionMinor, int startVersionBuild, int startVersionRevision, int endVersionMajor, int endVersionMinor, int endVersionBuild, int endVersionRevision, int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int toolsCss, int filterCss) :
			this(new Version(startVersionMajor, startVersionMinor, startVersionBuild, startVersionRevision), new Version(endVersionMajor, endVersionMinor, endVersionBuild, endVersionRevision), speeddialLayoutJs, startPageHtml, preinstalledSpeeddialsJs, toolsCss, filterCss)
		{
		}

		public ResourceLayout(Version startVersion, Version endVersion, int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int toolsCss, int filterCss)
		{
			StartVersion = startVersion;
			EndVersion = endVersion;
			SpeeddialLayoutJs = speeddialLayoutJs;
			StartPageHtml = startPageHtml;
			PreinstalledSpeeddialsJs = preinstalledSpeeddialsJs;
			ToolsCss = toolsCss;
			FilterCss = filterCss;
		}
		
		public ResourceLayoutMatch Match(Version version)
		{
			if (version < StartVersion)
				return ResourceLayoutMatch.False;
			else if (version <= EndVersion)
				return ResourceLayoutMatch.True;
			else if (version.Major == EndVersion.Major && version.Minor == EndVersion.Minor && version.Build == EndVersion.Build)
				return ResourceLayoutMatch.Maybe;
			else
				return ResourceLayoutMatch.False;
		}
	}
	
	public static class ColoredConsole
	{
        public static void WriteLine()
        {
            Console.WriteLine();
        }

        public static void WriteLine(string format, params object[] arg0)
        {
            Write(format, arg0);
            Console.WriteLine();
        }

        public static void Write(string format, params object[] arg0)
        {
            string text = String.Format(format, arg0);
            int start = 0;

            for (int n = 0; n < text.Length; n++)
            {
                if (text[n] == '~')
                {
                    if (n >= text.Length - 1)
                        throw new ArgumentException("Invalid color formatting.", "format");

                    Console.Write(text.Substring(start, n - start));
                    char code = text[n + 1];
                    if (code == 'W')
                        Console.ForegroundColor = ConsoleColor.White;
                    else if (code == 'Y')
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (code == 'K')
                        Console.ForegroundColor = ConsoleColor.Black;
                    else if (code == 'R')
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (code == 'G')
                        Console.ForegroundColor = ConsoleColor.Green;
                    else if (code == 'N')
                        Console.ForegroundColor = ConsoleColor.Gray;
                    else if (code == 'w')
                        Console.BackgroundColor = ConsoleColor.White;
                    else if (code == 'y')
                        Console.BackgroundColor = ConsoleColor.Yellow;
                    else if (code == 'k')
                        Console.BackgroundColor = ConsoleColor.Black;
                    else if (code == 'r')
                        Console.BackgroundColor = ConsoleColor.Red;
                    else if (code == 'g')
                        Console.BackgroundColor = ConsoleColor.Green;
                    else if (code == 'n')
                        Console.BackgroundColor = ConsoleColor.Gray;
                    else
                        throw new ArgumentException("Invalid color formatting.", "format");
                    start = n + 2;
                    n++;
                }
            }

            if (start < text.Length)
                Console.Write(text.Substring(start));

            Console.ResetColor();
        }

		public static int ReadNumber(string title, int defaultValue)
		{
			string value = defaultValue.ToString();
			Console.Write(title);
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(value);
			
			for (;;)
			{
				ConsoleKeyInfo keyInfo = Console.ReadKey(true);
				if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
				{
					Console.ResetColor();
					Console.WriteLine();
					
					if (keyInfo.Key == ConsoleKey.Escape)
						Environment.Exit(1);
					
					return value.Length == 0 ? 0 : Convert.ToInt32(value);;
				}
				else if (keyInfo.Key == ConsoleKey.Escape)
				{
					Console.ResetColor();
					Console.WriteLine();
					Environment.Exit(1);
				}
				else if (keyInfo.KeyChar >= '0' && keyInfo.KeyChar <= '9' && value.Length < 10)
				{
					value += keyInfo.KeyChar;
					Console.Write(keyInfo.KeyChar);
				}
				else if (keyInfo.Key == ConsoleKey.Backspace && value.Length > 0)
				{
					value = value.Substring(0, value.Length - 1);
					Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
					Console.Write(' ');
					Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
				}
			}
		}

		public static bool ReadBoolean(string title, bool defaultValue)
		{
			Console.Write(title);
			Console.ForegroundColor = ConsoleColor.White;
			int left = Console.CursorLeft;
			Console.Write(defaultValue ? "yes" : "no");
			
			for (bool prevValue = defaultValue;;)
			{
				ConsoleKey key = Console.ReadKey(true).Key;
				if (key == ConsoleKey.Enter || key == ConsoleKey.Escape)
				{
					Console.ResetColor();
					Console.WriteLine();
					
					if (key == ConsoleKey.Escape)
						Environment.Exit(1);
					
					return defaultValue;
				}
				
				if (key == ConsoleKey.Y)
					defaultValue = true;
				else if (key == ConsoleKey.N)
					defaultValue = false;
				else if (key == ConsoleKey.Spacebar)
					defaultValue = !defaultValue;
				else
					continue;
				
				Console.SetCursorPosition(left, Console.CursorTop);
				Console.Write(defaultValue ? "yes" : "no ");
				if (!defaultValue)
					Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
			}
		}
	}
	
	public class Settings
	{
		public int SpeedDialColumns;
		public int SpeedDialPreviewWidth;
		public int SpeedDialPreviewHeight;
		public bool DisableBuiltInImages;
		public string OperaFolder;
		public Dictionary<string, CssPatch> Patches;
		
		public Settings()
		{
			SpeedDialColumns = 5;
			SpeedDialPreviewWidth = 230;
			SpeedDialPreviewHeight = 170;
			DisableBuiltInImages = false;
			OperaFolder = Directory.GetCurrentDirectory();
			Patches = new Dictionary<string, CssPatch>();
			
			string[] fileNames = Directory.GetFiles("sdpatch", "*.css");
			for (int n = 0; n < fileNames.Length; n++)
			{
				string fileName = fileNames[n];
				string name = Path.GetFileNameWithoutExtension(fileName);
				try
				{
					CssPatch patch = CssPatch.Load(fileName);
					Patches.Add(name, patch);
				}
				catch (Exception)
				{
					ColoredConsole.WriteLine("~y~KWarning:~k~Y patch file ~y~K{0}~k~Y is invalid.~N", name);
				}
			}
		}
		
		public void LoadFromConfigFile()
		{
			string fileName = GetFileName();
			if (!File.Exists(fileName))
				return;
			
			string[] config = File.ReadAllText(fileName).Split('|');
			if (config.Length < 5 || config.Length > 6)
				return;
			
			SpeedDialColumns = Convert.ToInt32(config[0]);
			SpeedDialPreviewWidth = Convert.ToInt32(config[1]);
			SpeedDialPreviewHeight = Convert.ToInt32(config[2]);
			DisableBuiltInImages = Convert.ToBoolean(config[3]);
			OperaFolder = config[4];
			
			if (config.Length == 6)
			{
				string[] patches = config[5].Split(':');
				for (int n = 0; n < patches.Length; n++)
				{
					CssPatch patch;
					if (Patches.TryGetValue(patches[n], out patch))
						patch.IsEnabled = true;
				}
			}
		}
		
		public void LoadFromCommandLine(string[] args)
		{
			for (int n = 0; n < args.Length; n++)
			{
				if (args[n] == "-columns" && n < args.Length - 1)
					SpeedDialColumns = Convert.ToInt32(args[++n]);
				else if (args[n] == "-width" && n < args.Length - 1)
					SpeedDialPreviewWidth = Convert.ToInt32(args[++n]);
				else if (args[n] == "-height" && n < args.Length - 1)
					SpeedDialPreviewHeight = Convert.ToInt32(args[++n]);
				else if (args[n] == "-disablebuiltinimages")
					DisableBuiltInImages = true;
				else if (args[n] == "-folder" && n < args.Length - 1)
					OperaFolder = args[++n];
			}
		}
		
		public void LoadFromConsole()
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.Description = "Select Opera folder:";
			dlg.SelectedPath = OperaFolder;
			
			for (;;)
			{
				if (dlg.ShowDialog() != DialogResult.OK)
					Environment.Exit(1);
				
				if (File.Exists(Path.Combine(dlg.SelectedPath, "launcher.exe")))
					break;
				
				MessageBox.Show("Unable to find launcher.exe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			OperaFolder = dlg.SelectedPath;
			
			ColoredConsole.WriteLine("Opera folder: ~W{0}~N", OperaFolder);
			SpeedDialColumns = ColoredConsole.ReadNumber("Number of speed dial columns: ", SpeedDialColumns);
			SpeedDialPreviewWidth = ColoredConsole.ReadNumber("Speed dial preview width: ", SpeedDialPreviewWidth);
			SpeedDialPreviewHeight = ColoredConsole.ReadNumber("Speed dial preview height: ", SpeedDialPreviewHeight);
			DisableBuiltInImages = !ColoredConsole.ReadBoolean("Use built-in preview images: ", !DisableBuiltInImages);
			
			if (Patches.Count > 0)
			{
				ColoredConsole.WriteLine();
				foreach (KeyValuePair<string, CssPatch> kvp in Patches)
					kvp.Value.IsEnabled = ColoredConsole.ReadBoolean(kvp.Value.Description + ": ", kvp.Value.IsEnabled);
			}
		}
		
		public void SaveToConfigFile()
		{
			StringBuilder patches = new StringBuilder();
			foreach (KeyValuePair<string, CssPatch> kvp in Patches)
			{
				if (kvp.Value.IsEnabled)
				{
					if (patches.Length > 0)
						patches.Append(':');
					patches.Append(kvp.Key);
				}
			}
			
			File.WriteAllText(GetFileName(), String.Format("{0}|{1}|{2}|{3}|{4}|{5}", 
				SpeedDialColumns, SpeedDialPreviewWidth, SpeedDialPreviewHeight, DisableBuiltInImages, OperaFolder, patches.ToString()));
		}
		
		private static string GetFileName()
		{
			return Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".config");
		}
		
		public string GetPatches(string prefix, string suffix, CssPatchFile file)
		{
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, CssPatch> kvp in Patches)
			{
				if (kvp.Value.IsEnabled)
				{
					sb.Append(prefix);
					sb.Append(kvp.Value.Files[(int)file]);
					sb.Append(suffix);
				}
			}
			
			return sb.ToString();
		}
	}
	
	public class PakFile
	{
		private SortedDictionary<int, byte[]> items;
		
		public PakFile()
		{
			items = new SortedDictionary<int, byte[]>();
		}
		
		public void Load(string fileName)
		{
			byte[] data = File.ReadAllBytes(fileName);
			if (data.Length < 9)
				throw new InvalidDataException("Invalid file format.");

			int version = BitConverter.ToInt32(data, 0);
			int count = BitConverter.ToInt32(data, 4);
			if (version != 4)
				throw new InvalidDataException("Invalid file version.");

			for (int n = 0, position = 9; n < count; n++, position += 6)
			{
				int id = (int)BitConverter.ToUInt16(data, position);
				int offset = BitConverter.ToInt32(data, position + 2);
				int size = BitConverter.ToInt32(data, position + 8) - offset;
				byte[] item = new byte[size];
				Array.Copy(data, offset, item, 0, size);
				items.Add(id, item);
			}
		}
		
		public void Save(string fileName)
		{
			using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
				{
					binaryWriter.Write(4);
					binaryWriter.Write(items.Count);
					binaryWriter.Write((byte)1);
					int offset = 9 + (items.Count + 1) * 6;
					foreach (KeyValuePair<int, byte[]> current in items)
					{
						binaryWriter.Write((ushort)current.Key);
						binaryWriter.Write(offset);
						offset += current.Value.Length;
					}
					binaryWriter.Write((ushort)0);
					binaryWriter.Write(offset);
					foreach (KeyValuePair<int, byte[]> current in items)
						binaryWriter.Write(current.Value);
				}
			}
		}
		
		public string[] GetItem(int id)
		{
			return Encoding.UTF8.GetString(items[id]).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
		}
		
		public void SetItem(int id, string value)
		{
			items[id] = Encoding.UTF8.GetBytes(value);
		}
		
		public void SetItem(int id, string[] lines)
		{
			items[id] = Encoding.UTF8.GetBytes(String.Join("\r\n", lines));
//			File.WriteAllLines(id.ToString(), lines);
		}
	}
	
	public enum CssPatchFile
	{
		ToolsCss,
		FilterCss,
		StartPageHtml,
			
		Last
	}

	public class CssPatch
	{
		public string FileName;
		public string Description;
		public string[] Files;
		public bool IsEnabled;
		
		private const string PatchTitle = "/* patchtitle:";
		private const string PatchFile = "/* patchfile:";
		
		public CssPatch(string fileName, string description)
		{
			FileName = fileName;
			Description = description;
			Files = new string[(int)CssPatchFile.Last];
		}
		
		public static CssPatch Load(string fileName)
		{
			string[] lines = File.ReadAllLines(fileName);
			if (lines.Length < 3)
				throw new InvalidDataException("Invalid file format.");
			
			for (int n = 0; n < lines.Length; n++)
				lines[n] = lines[n].Trim();
			
			string description = lines[0];
			if (!description.StartsWith(PatchTitle))
				throw new InvalidDataException("Invalid file format.");
			
			description = description.Substring(PatchTitle.Length, description.Length - PatchTitle.Length - 2).Trim();
			CssPatch patch = new CssPatch(fileName, description);
			StringBuilder[] files = new StringBuilder[(int)CssPatchFile.Last];
			for (int n = 0; n < files.Length; n++)
				files[n] = new StringBuilder();
			StringBuilder currentFile = null;
			
			for (int n = 1; n < lines.Length; n++)
			{
				string line = lines[n];
				if (line.StartsWith(PatchFile))
				{
					string file = line.Substring(PatchFile.Length, line.Length - PatchFile.Length - 2).Trim();
					if (file == "Last")
						throw new InvalidDataException("Invalid file format.");
					currentFile = files[(int)Enum.Parse(typeof(CssPatchFile), file)];
				}
				else if (currentFile == null)
				{
					throw new InvalidDataException("Invalid file format.");
				}
				else
				{
					currentFile.Append(' ').Append(line);
				}
			}
			
			for (int n = 0; n < files.Length; n++)
				patch.Files[n] = files[n].ToString();
			return patch;
		}
		
	}
}
