using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedDialPatch
{
    public static class ByteArray
    {
        public static int Find(byte[] data, byte[] find)
        {
            for (int n = 0; n < data.Length - find.Length; n++)
            {
                for (int n1 = 0; n1 < find.Length; n1++)
                {
                    if (data[n + n1] != find[n1])
                        break;
                    else if (n1 == find.Length - 1)
                        return n;
                }
            }

            return -1;
        }

        public static byte[] Clone(byte[] data)
        {
            byte[] result = new byte[data.Length];
            Array.Copy(data, result, data.Length);
            return result;
        }

        public static byte[] Clone(byte[] data, int offset, int size)
        {
            byte[] result = new byte[size];
            Array.Copy(data, offset, result, 0, size);
            return result;
        }

        public static byte[] GetBytes(string s)
        {
            int n1 = 0;
            for (int n = 0; n < s.Length; n++)
            {
                char ch = s[n];
                if (ch == ' ')
                    continue;

                GetByte(ch);
                n1++;
            }

            if ((n1 & 1) != 0)
                throw new InvalidOperationException("Invalid executable patch: odd hextring length.");

            byte[] result = new byte[n1 / 2];
            n1 = 0;
            for (int n = 0; n < s.Length; n++)
            {
                char ch = s[n];
                if (ch == ' ')
                    continue;

                byte b = GetByte(ch);
                if ((n1 & 1) == 0)
                    result[n1++ / 2] = (byte)(b << 4);
                else
                    result[n1++ / 2] |= b;
            }
            return result;
        }

        private static byte GetByte(char ch)
        {
            if (ch >= '0' && ch <= '9')
                return (byte)(ch - '0');
            else if (ch >= 'a' && ch <= 'f')
                return (byte)(ch - 'a' + 10);
            else if (ch >= 'A' && ch <= 'F')
                return (byte)(ch - 'A' + 10);
            else
                throw new InvalidOperationException("Invalid executable patch: invalid character in hexstring.");
        }
    }
}
