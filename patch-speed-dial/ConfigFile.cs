using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SpeedDialPatch
{
    public static class ConfigFile
    {
        public static string Read(XmlNode node, string name, string value)
        {
            node = node.SelectSingleNode(name);
            return node != null ? node.InnerText : value;
        }

        public static int Read(XmlNode node, string name, int value)
        {
            node = node.SelectSingleNode(name);
            return node != null ? Convert.ToInt32(node.InnerText) : value;
        }

        public static bool Read(XmlNode node, string name, bool value)
        {
            node = node.SelectSingleNode(name);
            return node != null ? Convert.ToBoolean(node.InnerText) : value;
        }

        public static void Write(XmlWriter writer, string name, string value)
        {
            writer.WriteElementString(name, value);
        }

        public static void Write(XmlWriter writer, string name, int value)
        {
            writer.WriteElementString(name, value.ToString());
        }

        public static void Write(XmlWriter writer, string name, bool value)
        {
            writer.WriteElementString(name, value.ToString());
        }
    }
}
