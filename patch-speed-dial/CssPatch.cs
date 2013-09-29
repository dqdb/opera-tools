using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpeedDialPatch
{
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
