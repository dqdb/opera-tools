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
				Settings settings = new Settings();
				settings.LoadFromConfigFile();
				settings.LoadFromCommandLine(args);
				settings.LoadFromConsole();
				settings.SaveToConfigFile();
				Console.WriteLine();
				
				string fileName = FindLatestOperaPak(settings.OperaFolder);
				if (fileName == null)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Error: unable to find an \"opera.pak\" to patch.");
					Console.ResetColor();
					return 1;
				}

				PakFile pakFile = new PakFile();
				Console.Write("Reading ");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(fileName);
				Console.ResetColor();
				Console.WriteLine(" ...");
				pakFile.Load(fileName);
				
				const int FILE_SPEEDDIAL_LAYOUT_JS = 38276;
				const int FILE_MAIN_CSS = 38274;
				const int FILE_STARTPAGE_HTML = 39011;
				const int FILE_PREINSTALLED_SPEEDDIALS_JS = 38282;
				const string TEXT_MAX_X_COUNT = "  var MAX_X_COUNT = ";
				const string TEXT_DIAL_WIDTH = "SpeeddialObject.DIAL_WIDTH = ";
				const string TEXT_DIAL_HEIGHT = "SpeeddialObject.DIAL_HEIGHT = ";
				const string TEXT_CSS_WIDTH = "  width: ";
				const string TEXT_CSS_HEIGHT = "  height: ";
				const string TEXT_CSS_TOP = "  top: ";
				const string TEXT_CSS_LEFT = "  left: ";
				const string TEXT_PREINSTALLED_CHECK_URL_FUNCTION = "  this.checkURL = function(URL)";
				const string TEXT_PREINSTALLED_CHECK_URL_NEXTLINE = "  {";
				
				string[] lines = pakFile.GetItem(FILE_SPEEDDIAL_LAYOUT_JS);
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
				pakFile.SetItem(FILE_SPEEDDIAL_LAYOUT_JS, lines);
				
				foreach (int id in new int[] { FILE_MAIN_CSS, FILE_STARTPAGE_HTML })
				{
					lines = pakFile.GetItem(id);
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
					}
					pakFile.SetItem(id, lines);
				}
				
				lines = pakFile.GetItem(FILE_PREINSTALLED_SPEEDDIALS_JS);
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
				pakFile.SetItem(FILE_PREINSTALLED_SPEEDDIALS_JS, lines);
				
				Console.Write("Writing ");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(fileName);
				Console.ResetColor();
				Console.WriteLine(" ...");
				
				pakFile.Save(fileName + ".temp");
				File.Move(fileName, String.Format("{0}.backup.{1:yyyyMMddHHmmss}", fileName, DateTime.Now));
				File.Move(fileName + ".temp", fileName);
				result = 0;
			}
			catch (Exception value)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(value);
				Console.ResetColor();
				result = 1;
			}
			return result;
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
		
		private static void WriteHighlightedLine(string before, string highlighted, string after)
		{
			Console.Write(before);
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(highlighted);
			Console.ResetColor();
			Console.WriteLine(after);
		}
	}
	
	public class Settings
	{
		public int SpeedDialColumns;
		public int SpeedDialPreviewWidth;
		public int SpeedDialPreviewHeight;
		public bool DisableBuiltInImages;
		public string OperaFolder;
		
		public Settings()
		{
			SpeedDialColumns = 5;
			SpeedDialPreviewWidth = 230;
			SpeedDialPreviewHeight = 170;
			DisableBuiltInImages = false;
			OperaFolder = Directory.GetCurrentDirectory();
		}
		
		public void LoadFromConfigFile()
		{
			string fileName = GetFileName();
			if (!File.Exists(fileName))
				return;
			
			string[] config = File.ReadAllText(fileName).Split('|');
			if (config.Length != 5)
				return;
			
			SpeedDialColumns = Convert.ToInt32(config[0]);
			SpeedDialPreviewWidth = Convert.ToInt32(config[1]);
			SpeedDialPreviewHeight = Convert.ToInt32(config[2]);
			DisableBuiltInImages = Convert.ToBoolean(config[3]);
			OperaFolder = config[4];
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
			
			Console.Write("Opera folder: ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(OperaFolder);
			Console.ResetColor();
			SpeedDialColumns = ReadNumberFromConsole("Number of speed dial columns: ", SpeedDialColumns);
			SpeedDialPreviewWidth = ReadNumberFromConsole("Speed dial preview width: ", SpeedDialPreviewWidth);
			SpeedDialPreviewHeight = ReadNumberFromConsole("Speed dial preview height: ", SpeedDialPreviewHeight);
			DisableBuiltInImages = !ReadBoolFromConsole("Use built-in preview images: ", !DisableBuiltInImages);
		}
		
		public void SaveToConfigFile()
		{
			File.WriteAllText(GetFileName(), String.Format("{0}|{1}|{2}|{3}|{4}", 
				SpeedDialColumns, SpeedDialPreviewWidth, SpeedDialPreviewHeight, DisableBuiltInImages, OperaFolder));
		}
		
		private static string GetFileName()
		{
			return Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".config");
		}
		
		private static int ReadNumberFromConsole(string prefix, int defaultValue)
		{
			string value = defaultValue.ToString();
			Console.Write(prefix);
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
		
		private static bool ReadBoolFromConsole(string prefix, bool defaultValue)
		{
			Console.Write(prefix);
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
}
