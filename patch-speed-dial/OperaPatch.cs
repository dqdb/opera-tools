using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpeedDialPatch
{
    public class OperaPatch
    {
        public const string BackupExtension = ".sdpatch-original";

        public OperaVersion StartVersion;
        public OperaVersion EndVersion;

        public int SpeeddialLayoutJs;
        public int StartPageHtml;
        public int PreinstalledSpeeddialsJs;
        public int ToolsCss;
        public int FilterCss;
        public ExePatch ExePatch;

        public OperaPatch(string startVersion, string endVersion, int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int toolsCss, int filterCss) :
            this(OperaVersion.Parse(startVersion), OperaVersion.Parse(endVersion), speeddialLayoutJs, startPageHtml, preinstalledSpeeddialsJs, toolsCss, filterCss, null)
        {
        }

        public OperaPatch(string startVersion, string endVersion, int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int toolsCss, int filterCss, ExePatch exePatch) :
            this(OperaVersion.Parse(startVersion), OperaVersion.Parse(endVersion), speeddialLayoutJs, startPageHtml, preinstalledSpeeddialsJs, toolsCss, filterCss, exePatch)
        {
        }

        public OperaPatch(OperaVersion startVersion, OperaVersion endVersion, int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int toolsCss, int filterCss, ExePatch exePatch)
        {
            StartVersion = startVersion;
            EndVersion = endVersion;
            SpeeddialLayoutJs = speeddialLayoutJs;
            StartPageHtml = startPageHtml;
            PreinstalledSpeeddialsJs = preinstalledSpeeddialsJs;
            ToolsCss = toolsCss;
            FilterCss = filterCss;
            ExePatch = exePatch;
        }

        public bool Match(OperaVersion version)
        {
            return version >= StartVersion && version <= EndVersion;
        }

        public void Apply(Settings settings, string pakFileName)
        {
            string originalFileName = pakFileName + BackupExtension;
            CssPatches cssPatches = settings.CssPatches;

            PakFile pakFile = new PakFile();
            ColoredConsole.WriteLine("Reading ~W{0}~N ...", pakFileName);

            if (!File.Exists(originalFileName))
                File.Copy(pakFileName, originalFileName);
            else
                File.Copy(originalFileName, pakFileName, true);

            pakFile.Load(pakFileName);

            const string TEXT_MAX_X_COUNT = "  var MAX_X_COUNT = ";
            const string TEXT_DIAL_WIDTH = "SpeeddialObject.DIAL_WIDTH = ";
            const string TEXT_DIAL_HEIGHT = "SpeeddialObject.DIAL_HEIGHT = ";
            const string TEXT_CSS_WIDTH = "  width: ";
            const string TEXT_CSS_HEIGHT = "  height: ";
            const string TEXT_CSS_TOP = "  top: ";
            const string TEXT_CSS_LEFT = "  left: ";
            const string TEXT_PREINSTALLED_CHECK_URL_FUNCTION = "  this.checkURL = function(URL)";
            const string TEXT_PREINSTALLED_CHECK_URL_NEXTLINE = "  {";
            const string TEXT_STYLE = "</style>";

            string[] lines = pakFile.GetItem(SpeeddialLayoutJs);
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                if (line.StartsWith(TEXT_MAX_X_COUNT))
                    lines[n] = TEXT_MAX_X_COUNT + settings.SpeedDialColumns.ToString() + ";";
                else if (line.StartsWith(TEXT_DIAL_WIDTH))
                    lines[n] = TEXT_DIAL_WIDTH + settings.SpeedDialPreviewWidth.ToString() + ";";
                else if (line.StartsWith(TEXT_DIAL_HEIGHT))
                    lines[n] = TEXT_DIAL_HEIGHT + settings.SpeedDialPreviewHeight.ToString() + ";";
            }
            pakFile.SetItem(SpeeddialLayoutJs, lines);

            lines = pakFile.GetItem(StartPageHtml);
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                if (line == ".speeddial")
                {
                    ApplyCssRule(lines, n, TEXT_CSS_WIDTH, settings.SpeedDialPreviewWidth);
                    ApplyCssRule(lines, n, TEXT_CSS_HEIGHT, settings.SpeedDialPreviewHeight);
                }
                else if (line == ".dial-thumbnail")
                {
                    ApplyCssRule(lines, n, TEXT_CSS_WIDTH, (settings.SpeedDialPreviewWidth - 36) / 2);
                    ApplyCssRule(lines, n, TEXT_CSS_HEIGHT, (settings.SpeedDialPreviewHeight - 28) / 2);
                }
                else if (line == ".dial-thumbnail:nth-child(2)")
                {
                    ApplyCssRule(lines, n, TEXT_CSS_LEFT, settings.SpeedDialPreviewWidth / 2);
                }
                else if (line == ".dial-thumbnail:nth-child(3)")
                {
                    ApplyCssRule(lines, n, TEXT_CSS_TOP, settings.SpeedDialPreviewHeight / 2 - 1);
                }
                else if (line == ".dial-thumbnail:nth-child(4)")
                {
                    ApplyCssRule(lines, n, TEXT_CSS_LEFT, settings.SpeedDialPreviewWidth / 2);
                    ApplyCssRule(lines, n, TEXT_CSS_TOP, settings.SpeedDialPreviewHeight / 2 - 1);
                }
                else if (line.StartsWith(TEXT_STYLE))
                {
                    lines[n] = TEXT_STYLE + cssPatches.Apply("<style>", "</style>", CssPatchFile.StartPageHtml);
                }
            }
            pakFile.SetItem(StartPageHtml, lines);

            lines = pakFile.GetItem(ToolsCss);
            lines[lines.Length - 1] = cssPatches.Apply("", "", CssPatchFile.ToolsCss);
            pakFile.SetItem(ToolsCss, lines);

            lines = pakFile.GetItem(FilterCss);
            lines[lines.Length - 1] = cssPatches.Apply("", "", CssPatchFile.FilterCss);
            pakFile.SetItem(FilterCss, lines);

            lines = pakFile.GetItem(PreinstalledSpeeddialsJs);
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                if (line == TEXT_PREINSTALLED_CHECK_URL_FUNCTION && n < lines.Length - 1 &&
                    lines[n + 1].StartsWith(TEXT_PREINSTALLED_CHECK_URL_NEXTLINE))
                {
                    lines[n + 1] = TEXT_PREINSTALLED_CHECK_URL_NEXTLINE + (settings.DisableBuiltInImages ? "return null;" : "");
                    break;
                }
            }
            pakFile.SetItem(PreinstalledSpeeddialsJs, lines);

            ColoredConsole.WriteLine("Writing ~W{0}~N ...", pakFileName);

            pakFile.Save(pakFileName + ".temp");
            File.Delete(pakFileName);
            File.Move(pakFileName + ".temp", pakFileName);


            if (ExePatch != null)
                ExePatch.Apply(Path.ChangeExtension(pakFileName, ".exe"));
        }

        private static void ApplyCssRule(string[] lines, int index, string name, int value)
        {
            for (int n = index + 1; n < lines.Length; n++)
            {
                string text = lines[n];
                if (text == "}")
                    break;
                else if (text.StartsWith(name))
                    lines[n] = name + value.ToString() + "px;";
            }
        }
    }
}
