// compiling:
//
// @%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc SpeedDialPatch.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OperaTools
{
	public class Program
	{
		public const int MAX_X_COUNT = 5; // maximum number of speed dial previews in a row, default value is 5
		public const int DIAL_WIDTH = 270; // speed dial preview width in pixels, default value is 270
		public const int DIAL_HEIGHT = 130; // speed dial preview height in pixels, default value is 130
		public const bool UsePreinstalledSpeedDials = true; // set false to replace built-in SD images, default value is true
		
		private static int Main(string[] args)
		{
			int result;
			
			try
			{
				PakFile pakFile = new PakFile();
				
				string[] folders = Directory.GetDirectories(".");
				string fileName = null;
				
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
						string fileName1 = Path.Combine(folder1, "opera.pak");
						if (File.Exists(fileName1))
						{
							fileName = fileName1;
							break;
						}
					}
				}
				
				if (fileName == null)
					throw new FileNotFoundException("Unable to find \"opera.pak\" file to patch.");

				Console.WriteLine("Reading {0} ...", fileName);
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
				
				string[] lines = pakFile.GetItem(FILE_SPEEDDIAL_LAYOUT_JS);
				for (int n = 0; n < lines.Length; n++)
				{
					string line = lines[n];
					if (line.StartsWith(TEXT_MAX_X_COUNT))
						lines[n] = TEXT_MAX_X_COUNT + MAX_X_COUNT.ToString() + ";";
					else if (line.StartsWith(TEXT_DIAL_WIDTH))
						lines[n] = TEXT_DIAL_WIDTH + DIAL_WIDTH.ToString() + ";";
					else if (line.StartsWith(TEXT_DIAL_HEIGHT))
						lines[n] = TEXT_DIAL_HEIGHT + DIAL_HEIGHT.ToString() + ";";
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
							Program.PatchCssRule(lines, n, TEXT_CSS_WIDTH, 100);
							Program.PatchCssRule(lines, n, TEXT_CSS_HEIGHT, 60);
						}
						else if (line == ".dial-thumbnail")
						{
							Program.PatchCssRule(lines, n, TEXT_CSS_WIDTH, 32);
							Program.PatchCssRule(lines, n, TEXT_CSS_HEIGHT, 16);
						}
						else if (line == ".dial-thumbnail:nth-child(2)")
						{
							Program.PatchCssRule(lines, n, TEXT_CSS_LEFT, 50);
						}
						else if (line == ".dial-thumbnail:nth-child(3)")
						{
							Program.PatchCssRule(lines, n, TEXT_CSS_TOP, 29);
						}
						else if (line == ".dial-thumbnail:nth-child(4)")
						{
							Program.PatchCssRule(lines, n, TEXT_CSS_LEFT, 50);
							Program.PatchCssRule(lines, n, TEXT_CSS_TOP, 29);
						}
					}
					pakFile.SetItem(id, lines);
				}
				
				if (!UsePreinstalledSpeedDials)
					pakFile.SetItem(FILE_PREINSTALLED_SPEEDDIALS_JS, "\"use strict\";\r\n\r\nvar PreinstalledSpeeddials = function() {};\r\n\r\n(function()\r\n{\r\n  this.checkURL = function(URL)\r\n  {\r\n    return null;\r\n  };\r\n\r\n}).apply(PreinstalledSpeeddials);\r\n");
				
				Console.WriteLine("Writing {0} ...", fileName);
				pakFile.Save(fileName + ".temp");
				File.Move(fileName, String.Format("{0}.backup.{1:yyyyMMddHHmmss}", fileName, DateTime.Now));
				File.Move(fileName + ".temp", fileName);
				result = 0;
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
				result = 1;
			}
			return result;
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
	
	public class PakFile
	{
		public const int FileVersion = 4;
		public const int FileHeaderSize = 9;
		public const int ItemHeaderSize = 6;
		public SortedDictionary<int, byte[]> Items;
		
		public PakFile()
		{
			Items = new SortedDictionary<int, byte[]>();
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

			for (int n = 0, position = FileHeaderSize; n < count; n++, position += 6)
			{
				int id = (int)BitConverter.ToUInt16(data, position);
				int offset = BitConverter.ToInt32(data, position + 2);
				int size = BitConverter.ToInt32(data, position + 8) - offset;
				byte[] item = new byte[size];
				Array.Copy(data, offset, item, 0, size);
				Items.Add(id, item);
			}
		}
		
		public void Save(string fileName)
		{
			using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
				{
					binaryWriter.Write(4);
					binaryWriter.Write(Items.Count);
					binaryWriter.Write((byte)1);
					int offset = 9 + (Items.Count + 1) * 6;
					foreach (KeyValuePair<int, byte[]> current in Items)
					{
						binaryWriter.Write((ushort)current.Key);
						binaryWriter.Write(offset);
						offset += current.Value.Length;
					}
					binaryWriter.Write((ushort)0);
					binaryWriter.Write(offset);
					foreach (KeyValuePair<int, byte[]> current in Items)
						binaryWriter.Write(current.Value);
				}
			}
		}
		
		public string[] GetItem(int id)
		{
			return Encoding.UTF8.GetString(Items[id]).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
		}
		
		public void SetItem(int id, string value)
		{
			Items[id] = Encoding.UTF8.GetBytes(value);
		}
		
		public void SetItem(int id, string[] lines)
		{
			Items[id] = Encoding.UTF8.GetBytes(String.Join("\r\n", lines));
//			File.WriteAllLines(id.ToString(), lines);
		}
	}
}