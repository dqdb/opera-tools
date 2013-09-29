using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SpeedDialPatch
{
    public class OperaVersion : IComparable, IComparable<OperaVersion>, IEquatable<OperaVersion>
    {
        private int[] Parts;
        private int Copy;

        public int Major
        {
            get { return Parts[0]; }
        }

        public int Minor
        {
            get { return Parts[1]; }
        }

        public int Build
        {
            get { return Parts[2]; }
        }

        public int Revision
        {
            get { return Parts[3]; }
        }

        private OperaVersion()
        {
            Copy = -1;
        }

        public static OperaVersion Parse(string s)
        {
            OperaVersion version;
            if (TryParseInternal(s, out version))
                return version;

            throw new FormatException("Invalid opera version.");
        }

        public static bool TryParse(string text, out OperaVersion version)
        {
            OperaVersion version1;
            bool result = TryParseInternal(text, out version1);
            version = result ? version = version1 : null;
            return result;
        }

        private static bool TryParseInternal(string text, out OperaVersion version)
        {
            version = new OperaVersion();
            int n = text.IndexOf('_');
            if (n != -1)
            {
                if (!TryParsePart(text.Substring(n + 1), out version.Copy))
                    return false;

                text = text.Substring(0, n);
            }

            string[] parts = text.Split('.');
            if (parts.Length < 4)
                return false;

            version.Parts = new int[parts.Length];
            for (n = 0; n < parts.Length; n++)
            {
                if (!TryParsePart(parts[n], out version.Parts[n]))
                    return false;
            }

            return true;
        }

        private static bool TryParsePart(string text, out int result)
        {
            return Int32.TryParse(text, out result) && result >= 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int n = 0; n < Parts.Length; n++)
            {
                if (n > 0)
                    sb.Append('.');

                sb.Append(Parts[n].ToString(CultureInfo.InvariantCulture));
            }

            if (Copy >= 0)
                sb.AppendFormat("_{0}", Copy);

            return sb.ToString();
        }

        public int CompareTo(object other)
        {
            if (other == null)
                return 1;

            OperaVersion other1 = (OperaVersion)other;
            if (other1 == null)
                throw new ArgumentException("Argument must be an OperaVersion.", "other");
            return CompareTo(other1);
        }

        public int CompareTo(OperaVersion other)
        {
            if (other == null)
                return 1;

            for (int n = 0; n < Parts.Length && n < other.Parts.Length; n++)
            {
                int n1 = Parts[n].CompareTo(other.Parts[n]);
                if (n1 != 0)
                    return n1;
            }

            if (Parts.Length == other.Parts.Length)
                return Copy.CompareTo(other.Copy);

            return Parts.Length.CompareTo(other.Parts.Length);
        }

        public override bool Equals(object other)
        {
            OperaVersion other1 = other as OperaVersion;
            return other1 != null && Equals(other1);
        }

        public bool Equals(OperaVersion other)
        {
            return CompareTo(other) == 0;
        }

        public static bool operator ==(OperaVersion v1, OperaVersion v2)
        {
            if (object.ReferenceEquals(v1, null))
                return object.ReferenceEquals(v2, null);
            return v1.Equals(v2);
        }

        public static bool operator >(OperaVersion v1, OperaVersion v2)
        {
            return v2 < v1;
        }

        public static bool operator >=(OperaVersion v1, OperaVersion v2)
        {
            return v2 <= v1;
        }

        public static bool operator !=(OperaVersion v1, OperaVersion v2)
        {
            return !(v1 == v2);
        }

        public static bool operator <(OperaVersion v1, OperaVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            return v1.CompareTo(v2) < 0;
        }

        public static bool operator <=(OperaVersion v1, OperaVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");

            return v1.CompareTo(v2) <= 0;
        }

        public override int GetHashCode()
        {
            int result = 0;
            for (int n = 0; n < Parts.Length; n++)
                result = (result << 8) | Parts[n];
            if (Copy >= 0)
                result = (result << 8) | Copy;
            return result;
        }
    }
}
