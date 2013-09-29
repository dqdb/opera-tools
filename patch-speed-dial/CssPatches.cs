using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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

        public void LoadFromConfigFile(string value)
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
                    kvp.Value.IsEnabled = ColoredConsole.ReadBoolean(kvp.Value.Description + ": ", kvp.Value.IsEnabled);
            }
        }

        public string SaveToConfigFile()
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
            return patches.ToString();
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
