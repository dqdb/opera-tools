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
			//             StartVersion      EndVersion         SpeeddialLayoutJs
			//             |                 |                  |      StartPageHtml
			//             |                 |                  |      |      PreinstalledSpeeddialsJs
			//             |                 |                  |      |      |      ToolsCss
			//             |                 |                  |      |      |      |      FilterCss
			//             |                 |                  |      |      |      |      |
			// newer builds with protected opera.pak            |      |      |      |      |
			new OperaPatch("18.0.1274.0.8",  "18.0.1274.0.8",   43020, 43515, 43026, 41010, 41008, new ExePatch(0x006042ec, "0F 85 8A 00 00 00", "E9 8B 00 00 00 90")),
			new OperaPatch("18.0.1271.0",    "18.0.1271.0",     43020, 43515, 43026, 41010, 41008, new ExePatch(0x00015b1c, "0F 85 8A 00 00 00", "E9 8B 00 00 00 90")),
			new OperaPatch("18.0.1267.0",    "18.0.1267.0",     43020, 43515, 43026, 41010, 41008, new ExePatch(0x0001583c, "0F 85 8A 00 00 00", "E9 8B 00 00 00 90")),
			new OperaPatch("18.0.1264.0",    "18.0.1264.0",     43020, 43515, 43026, 41010, 41008, new ExePatch(0x00015dfc, "0F 85 8A 00 00 00", "E9 8B 00 00 00 90")),
			new OperaPatch("18.0.1258.1",    "18.0.1258.1",     43020, 43515, 43026, 41010, 41008, new ExePatch(0x0001602c, "0F 85 8A 00 00 00", "E9 8B 00 00 00 90")),
			//             |                 |                  |      |      |      |      |
			// older builds with unprotected opera.pak          |      |      |      |      |
			new OperaPatch("17.0.1232.0",    "17.0.1232.0",     43021, 43515, 43027, 41010, 41008),
			new OperaPatch("17.0.1224.1",    "17.0.1224.1",     43020, 43515, 43026, 41010, 41008),
                                                            
			new OperaPatch("16.0.1196.45",   "16.0.1196.55",    38278, 39011, 38284, 41010, 41008),
			new OperaPatch("16.0.1196.41",   "16.0.1196.41",    38278, 39011, 38284, 41009, 41007),
			new OperaPatch("16.0.1196.14",   "16.0.1196.35",    38276, 39011, 38282, 41009, 41007),
                                                            
			new OperaPatch("15.0.1147.130",  "15.0.1147.153",   38248, 39011, 38254, 41009, 41007),
			new OperaPatch("15.0.1147.100",  "15.0.1147.100",   38248, 39011, 38254, 41008, 41006),
			new OperaPatch("15.0.1147.72",   "15.0.1147.72",    38247, 39011, 38253, 41007, 41005),
			new OperaPatch("15.0.1147.56",   "15.0.1147.61",    38245, 39011, 38251, 41007, 41005),
			new OperaPatch("15.0.1147.44",   "15.0.1147.44",    38247, 39011, 38253, 41007, 41005),
			new OperaPatch("15.0.1147.18",   "15.0.1147.24",    38247, 39010, 38253, 41007, 41005)
						
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

        public static OperaPatch FindWithHeuristics(string pakFileName)
        {
            PakFile pakFile = new PakFile();
            pakFile.Load(pakFileName);
            string exeFileName = Path.ChangeExtension(pakFileName, ".exe");
            string originalExeFileName = exeFileName + OperaPatch.BackupExtension;
            byte[] exeFile = File.ReadAllBytes(File.Exists(originalExeFileName) ? originalExeFileName : exeFileName);

            byte[] find = ByteArray.GetBytes("84 C0 0F 85 8A 00 00 00 8D 4D 8A");
            byte[] original = ByteArray.GetBytes("0F 85 8A 00 00 00");
            byte[] patched = ByteArray.GetBytes("E9 8B 00 00 00 90");

            int offset = ByteArray.Find(exeFile, find);
            if (offset < 0)
                return null;

            offset += ByteArray.Find(find, original);

            int speeddialLayoutJs = FindInPakFile(pakFile, "var SpeeddialObject = function(");
            int startPageHtml = FindInPakFile(pakFile, "<div class=\"view\" data-view-id=\"speeddial\"></div>");
            int preinstalledSpeeddialsJs = FindInPakFile(pakFile, "var PreinstalledSpeeddials = function(");
            int toolsCss = FindInPakFile(pakFile, "This file holds CSS that brings Opera 12 style to Opera");
            int filterCss = FindInPakFile(pakFile, ".filter-active .filter.animated");

            if (speeddialLayoutJs < 0 || startPageHtml < 0 || preinstalledSpeeddialsJs < 0 ||
                toolsCss < 0 || filterCss < 0)
                return null;

            return new OperaPatch(
                "0.0.0.0", "0.0.0.0", 
                speeddialLayoutJs, startPageHtml, preinstalledSpeeddialsJs, toolsCss, filterCss, 
                new ExePatch(offset, original, patched));
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
