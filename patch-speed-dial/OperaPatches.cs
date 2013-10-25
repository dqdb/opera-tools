using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpeedDialPatch
{
    public static class OperaPatches
    {
        public static OperaPatch[] Patches = new OperaPatch[]
		{
			//             StartVersion      EndVersion       SpeeddialLayoutJs
			//             |                 |                |      StartPageHtml
			//             |                 |                |      |      PreinstalledSpeeddialsJs
			//             |                 |                |      |      |      SpeeddialSuggestionsJs
			//             |                 |                |      |      |      |      ToolsCss
			//             |                 |                |      |      |      |      |      FilterCss
			//             |                 |                |      |      |      |      |      |
			new OperaPatch("19.0.1300.0",    "19.0.1300.0",   43020, 43515, 43026, 43021, 41010, 41008, new ExePatch(0x0073f48e, "0F 85 CA 00 00 00", "E9 CB 00 00 00 90")),
			new OperaPatch("18.0.1284.5",    "18.0.1284.5",   43020, 43515, 43026, 43021, 41010, 41008, new ExePatch(0x00733dec, "0F 85 8A 00 00 00", "E9 8B 00 00 00 90"))
		};

        public static HeuristicPatch[] HeuristicPatches = new HeuristicPatch[]
        {
            new HeuristicPatch("84 C0 0F 85 CA 00 00 00 8D 8D 3A", "0F 85 CA 00 00 00", "E9 CB 00 00 00 90"), 
            new HeuristicPatch("84 C0 0F 85 8A 00 00 00 8D 4D 8A", "0F 85 8A 00 00 00", "E9 8B 00 00 00 90") 
        };

        public static OperaPatch Find(OperaVersion version)
        {
            for (int n = 0; n < Patches.Length; n++)
            {
                OperaPatch patch = Patches[n];
                if (patch.Match(version))
                    return patch;
            }

            return null;
        }

        public static OperaPatch FindWithHeuristics(Settings settings, string pakFileName)
        {
            PakFile pakFile = new PakFile();
            pakFile.Load(pakFileName);
            string exeFileName = Path.ChangeExtension(pakFileName, ".exe");
            string originalExeFileName = exeFileName + OperaPatch.BackupExtension;
            byte[] exeFile = File.ReadAllBytes(File.Exists(originalExeFileName) ? originalExeFileName : exeFileName);

            int offset = -1;
            byte[] original = null;
            byte[] patched = null;

            if (settings.PatchOperaExe)
            {
                for (int n = 0; n < HeuristicPatches.Length; n++)
                {
                    HeuristicPatch patch = HeuristicPatches[n];
                    offset = ByteArray.Find(exeFile, patch.Find);
                    if (offset >= 0)
                    {
                        original = patch.Original;
                        patched = patch.Patched;
                        offset += ByteArray.Find(patch.Find, original);
                        break;
                    }
                }

                if (offset < 0)
                    return null;
            }

            int speeddialLayoutJs = FindInPakFile(pakFile, "var SpeeddialObject = function(");
            int startPageHtml = FindInPakFile(pakFile, "<div class=\"view\" data-view-id=\"speeddial\"></div>");
            int preinstalledSpeeddialsJs = FindInPakFile(pakFile, "var PreinstalledSpeeddials = function(");
            int speeddialSuggestionsJs = FindInPakFile(pakFile, "this.add_dial_dialog = function(");
            int toolsCss = FindInPakFile(pakFile, "This file holds CSS that brings Opera 12 style to Opera");
            int filterCss = FindInPakFile(pakFile, ".filter-active .filter.animated");

            if (speeddialLayoutJs < 0 || startPageHtml < 0 || preinstalledSpeeddialsJs < 0 ||
                toolsCss < 0 || filterCss < 0)
                return null;

            return new OperaPatch(
                "0.0.0.0", "0.0.0.0",
                speeddialLayoutJs, startPageHtml, preinstalledSpeeddialsJs, speeddialSuggestionsJs, toolsCss, filterCss, 
                offset >= 0 ? new ExePatch(offset, original, patched) : null);
        }

        private static int FindInPakFile(PakFile pakFile, string text)
        {
            foreach (int id in pakFile.Items.Keys)
            {
                string item = Encoding.ASCII.GetString(pakFile.Items[id]);
                if (item.Contains(text))
                    return id;
            }

            return -1;
        }
    }
}
