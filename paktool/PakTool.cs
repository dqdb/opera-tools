// @%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc PakTool.cs 
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PakTool
{
    class Program
    {
        public const ConsoleColor InfoColor = ConsoleColor.White;
        public const ConsoleColor ToolColor = ConsoleColor.Gray;
        public const ConsoleColor ErrorColor = ConsoleColor.Red;
        public const ConsoleColor DefaultColor = ConsoleColor.Gray;
        public const ConsoleColor WarningrColor = ConsoleColor.Yellow;

        public const int FileVersion = 4;
        public const int FileHeaderSize = 9;
        public const int ItemHeaderSize = 6;

        static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args[0] == "decode" && Decode(args))
                        return 0;
                    else if (args[0] == "encode" && Encode(args))
                        return 0;
                }

                Error("Invalid parameters.");
                NewLine();
                Highlight("PakTool.exe decode -in filename [-out folder]");
                Highlight("PakTool.exe encode -in folder -out filename");
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }

        public static bool Decode(string[] args)
        {
            string in1 = null;
            string out1 = "output";

            for (int n = 1; n < args.Length; n++)
            {
                if (args[n] == "-in" && n < args.Length - 1)
                    in1 = args[++n];
                else if (args[n] == "-out" && n < args.Length - 1)
                    out1 = args[++n];
            }

            if (in1 == null)
                return false;

            try
            {
                Directory.CreateDirectory(out1);

                byte[] data = File.ReadAllBytes(in1);

                if (data.Length < FileHeaderSize)
                {
                    Error("Invalid file format.");
                    return true;
                }

                int version = BitConverter.ToInt32(data, 0);
                int count = BitConverter.ToInt32(data, 4);

                if (version != FileVersion)
                {
                    Error("Invalid file version [version is {0} instead of {1}].", version, FileVersion);
                    return true;
                }

                for (int n = 0, position = FileHeaderSize; n < count; n++, position += ItemHeaderSize)
                {
                    int id = BitConverter.ToUInt16(data, position);
                    int offset = BitConverter.ToInt32(data, position + 2);
                    int size = BitConverter.ToInt32(data, position + 8) - offset;
                    byte[] item = new byte[size];
                    Array.Copy(data, offset, item, 0, size);
                    string text = Encoding.ASCII.GetString(item);
                    string filename = id.ToString("D5");
                    if (item.Length >= 6 && Encoding.ASCII.GetString(item, 1, 5) == "PNG\r\n")
                        filename += ".png";
                    else if (item.Length >= 4 && Encoding.ASCII.GetString(item, 0, 4) == "RIFF")
                        filename += ".wav";
                    else if (item.Length >= 6 && Encoding.ASCII.GetString(item, 0, 6) == "GIF89a")
                        filename += ".gif";
                    else if (item.Length >= 2 && item[0] == 0xff && item[1] == 0xd8)
                        filename += ".jpg";
                    else if (item.Length > 0 && item[0] == '<')
                        filename += ".html";
                    else if (item.Length > 0 && item[0] == '/')
                        filename += ".js";
                    else if (item.Length > 0 && item[0] == '{')
                        filename += ".json";
                    else if (text.Contains("<body>") || text.Contains("<html>"))
                        filename += ".html";
                    else if (text.Contains("var ") || text.Contains("function ") || text.Contains("function"))
                        filename += ".js";
                    else if (text.Contains("px;"))
                        filename += ".css";

                    File.WriteAllBytes(Path.Combine(out1, filename), item);
                }

            }
            catch (Exception e)
            {
                throw new Exception("Unable to decode input file.", e);
            }

            return true;
        }

        public static bool Encode(string[] args)
        {
            string in1 = null;
            string out1 = null;

            for (int n = 1; n < args.Length; n++)
            {
                if (args[n] == "-in" && n < args.Length - 1)
                    in1 = args[++n];
                else if (args[n] == "-out" && n < args.Length - 1)
                    out1 = args[++n];
            }

            if (in1 == null || out1 == null)
                return false;

            try
            {
                string[] files = Directory.GetFiles(in1);
                Array.Sort(files);

                using (FileStream output = new FileStream(out1, FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(output))
                {
                    writer.Write(FileVersion);
                    writer.Write(files.Length);
                    writer.Write((byte)1);

                    int offset = FileHeaderSize + (files.Length + 1) * ItemHeaderSize;
                    for (int n = 0; n < files.Length; n++)
                    {
                        string file = files[n];
                        int id = Convert.ToInt32(Path.GetFileNameWithoutExtension(file));
                        writer.Write((ushort)id);
                        writer.Write(offset);
                        offset += (int)new FileInfo(file).Length;
                    }

                    writer.Write((ushort)0);
                    writer.Write(offset);

                    for (int n = 0; n < files.Length; n++)
                    {
                        string file = files[n];
                        writer.Write(File.ReadAllBytes(file));
                    }
                }

            }
            catch (Exception e)
            {
                throw new Exception("Unable to encode output file.", e);
            }

            return true;
        }

        public static void NewLine()
        {
            Console.WriteLine();
        }

        public static void Text(string format, params object[] args)
        {
            Console.ForegroundColor = DefaultColor;
            Console.WriteLine(format, args);
        }

        public static void Highlight(string format, params object[] args)
        {
            Console.ForegroundColor = InfoColor;
            Console.WriteLine(format, args);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Warning(string format, params object[] args)
        {
            Console.ForegroundColor = WarningrColor;
            Console.WriteLine(format, args);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Error(string format, params object[] args)
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine(format, args);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Error(Exception e)
        {
            if (e.InnerException == null)
                Error("{0}", e.Message);
            else
#if DEBUG
                Error("{0} ----> {1}", e.Message, e.InnerException.ToString());
#else
                Error("{0} ----> {1}", e.Message, e.InnerException.Message);
#endif
        }

    }
}