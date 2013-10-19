using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedDialPatch
{
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

        public static string Read(string title, string defaultValue)
        {
            return Read(title, -1, defaultValue);
        }

        public static string Read(string title, int maxLength, string defaultValue)
        {
            string value = defaultValue;
            Console.Write(title);
            Console.ForegroundColor = ConsoleColor.White;

            if (maxLength != -1 && value.Length > maxLength)
                value = value.Substring(0, maxLength);

            Console.Write(value);

            for (; ; )
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.ResetColor();
                    Console.WriteLine();

                    if (keyInfo.Key == ConsoleKey.Escape)
                        Environment.Exit(1);

                    return value;
                }
                else if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.ResetColor();
                    Console.WriteLine();
                    Environment.Exit(1);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (value.Length > 0)
                    {
                        value = value.Substring(0, value.Length - 1);
                        ClearToLeft();
                    }
                }
                else if (maxLength == -1 || value.Length < maxLength)
                {
                    value += keyInfo.KeyChar;
                    Console.Write(keyInfo.KeyChar);
                }
            }
        }

        public static int Read(string title, int defaultValue)
        {
            string value = defaultValue.ToString();
            Console.Write(title);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(value);

            for (; ; )
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.ResetColor();
                    Console.WriteLine();

                    if (keyInfo.Key == ConsoleKey.Escape)
                        Environment.Exit(1);

                    return value.Length == 0 ? 0 : Convert.ToInt32(value);
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
                    ClearToLeft();
                }
            }
        }

        public static bool Read(string title, bool defaultValue)
        {
            bool value = defaultValue;
            Console.Write(title);
            Console.ForegroundColor = ConsoleColor.White;
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            Console.Write(value ? "yes" : "no");

            for (bool prevValue = value; ; )
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Enter || key == ConsoleKey.Escape)
                {
                    Console.ResetColor();
                    Console.WriteLine();

                    if (key == ConsoleKey.Escape)
                        Environment.Exit(1);

                    return value;
                }

                if (key == ConsoleKey.Y)
                    value = true;
                else if (key == ConsoleKey.N)
                    value = false;
                else if (key == ConsoleKey.Spacebar)
                    value = !value;
                else
                    continue;

                Console.SetCursorPosition(left, top);
                Console.Write(value ? "yes" : "no ");
                if (!value)
                    ClearToLeft();
            }
        }

        public static void ClearToLeft()
        {
            if (Console.CursorLeft > 0)
            {
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                Console.Write(' ');
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }
            else
            {
                Console.SetCursorPosition(Console.WindowWidth - 1, Console.CursorTop - 1);
                Console.Write(' ');
                Console.SetCursorPosition(Console.WindowWidth - 1, Console.CursorTop - 1);
            }
        }
    }
}
