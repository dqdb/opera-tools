using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace SpeedDialPatch
{
    public class CssPatches
    {
        public Dictionary<string, CssPatch> Patches;

        public CssPatches()
        {
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

        public void LoadFromConfig(XmlNode node, string name)
        {
            node = node.SelectSingleNode(name);
            if (node == null)
                return;

            XmlNodeList nodes = node.SelectNodes("cssPatch");
            if (nodes == null)
                return;

            foreach (XmlNode node1 in nodes)
            {
                CssPatch patch;
                if (Patches.TryGetValue(node1.InnerText, out patch))
                    patch.IsEnabled = true;
            }
        }

        public void LoadFromString(string value)
        {
            string[] values = value.Split(':');
            for (int n = 0; n < values.Length; n++)
            {
                CssPatch patch;
                if (Patches.TryGetValue(values[n], out patch))
                    patch.IsEnabled = true;
            }
        }

        public void LoadFromConsole()
        {
            if (Patches.Count > 0)
            {
                ColoredConsole.WriteLine();
                foreach (KeyValuePair<string, CssPatch> kvp in Patches)
                    kvp.Value.IsEnabled = ColoredConsole.Read(kvp.Value.Description + ": ", kvp.Value.IsEnabled);

                ColoredConsole.WriteLine();
            }
        }

        public void SaveToConfig(XmlWriter writer, string name)
        {
            writer.WriteStartElement(name);
            foreach (KeyValuePair<string, CssPatch> kvp in Patches)
            {
                if (kvp.Value.IsEnabled)
                    writer.WriteElementString("cssPatch", kvp.Key);
            }
            writer.WriteEndElement();
        }

        public string Apply(string prefix, string suffix, CssPatchFile file)
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
}
