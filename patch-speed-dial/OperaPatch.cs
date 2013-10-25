using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

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
        public int SpeeddialSuggestionsJs;
        public int ToolsCss;
        public int FilterCss;
        public ExePatch ExePatch;

        public OperaPatch(string startVersion, string endVersion, int speeddialLayoutJs, int speeddialSuggestionsJs, int startPageHtml, int preinstalledSpeeddialsJs, int toolsCss, int filterCss) :
            this(OperaVersion.Parse(startVersion), OperaVersion.Parse(endVersion), speeddialLayoutJs, speeddialSuggestionsJs, startPageHtml, preinstalledSpeeddialsJs, toolsCss, filterCss, null)
        {
        }

        public OperaPatch(string startVersion, string endVersion, int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int speeddialSuggestionsJs, int toolsCss, int filterCss, ExePatch exePatch) :
            this(OperaVersion.Parse(startVersion), OperaVersion.Parse(endVersion), speeddialLayoutJs, startPageHtml, preinstalledSpeeddialsJs, speeddialSuggestionsJs, toolsCss, filterCss, exePatch)
        {
        }

        public OperaPatch(OperaVersion startVersion, OperaVersion endVersion, int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int speeddialSuggestionsJs, int toolsCss, int filterCss, ExePatch exePatch)
        {
            StartVersion = startVersion;
            EndVersion = endVersion;
            SpeeddialLayoutJs = speeddialLayoutJs;
            StartPageHtml = startPageHtml;
            PreinstalledSpeeddialsJs = preinstalledSpeeddialsJs;
            SpeeddialSuggestionsJs = speeddialSuggestionsJs;
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
            const string TEXT_MARGIN_X = "  var MARGIN_X = ";
            const string TEXT_MARGIN_Y = "  var MARGIN_Y = ";
            const string TEXT_DIAL_WIDTH = "SpeeddialObject.DIAL_WIDTH = ";
            const string TEXT_DIAL_HEIGHT = "SpeeddialObject.DIAL_HEIGHT = ";
            const string TEXT_THUMBNAIL_URL1 = "    \"size=\" + this.DIAL_WIDTH + \"x\" + this.DIAL_HEIGHT +";
            const string TEXT_THUMBNAIL_URL2 = "    \"size=\" + (({0:F4} * this.DIAL_WIDTH + 0.5) >> 0) + \"x\" + (({0:F4} * this.DIAL_HEIGHT + 0.5) >> 0) +";
            const string TEXT_CSS_WIDTH = "  width: ";
            const string TEXT_CSS_HEIGHT = "  height: ";
            const string TEXT_CSS_TOP = "  top: ";
            const string TEXT_CSS_LEFT = "  left: ";
            const string TEXT_CSS_BACKGROUND_POSITION = "  background-position: ";
            const string TEXT_PREINSTALLED_CHECK_URL_FUNCTION = "  this.checkURL = function(URL)";
            const string TEXT_PREINSTALLED_CHECK_URL_NEXTLINE = "  {";
            const string TEXT_PREINSTALLED_LAST_LINE = "}).apply(PreinstalledSpeeddials);";
            const string TEXT_STYLE = "</style>";
            const string TEXT_DIAL_URL = "SpeeddialObject.DIAL_URL + ";
            const string TEXT_DIAL_RELOAD_URL = "SpeeddialObject.DIAL_RELOAD_URL +";
            const string TEXT_ADD_DIAL_URL = "var sdURL = sd_obj.sdURL || SpeeddialObject.DIAL_URL + ";

            string[] lines = pakFile.GetItem(SpeeddialLayoutJs);
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                if (line.StartsWith(TEXT_MAX_X_COUNT))
                    lines[n] = TEXT_MAX_X_COUNT + settings.SpeedDial.Columns.ToString() + ";";
                else if (line.StartsWith(TEXT_MARGIN_X))
                    lines[n] = TEXT_MARGIN_X + settings.SpeedDial.MarginX.ToString() + ";";
                else if (line.StartsWith(TEXT_MARGIN_Y))
                    lines[n] = TEXT_MARGIN_Y + settings.SpeedDial.MarginY.ToString() + ";";
                else if (line.StartsWith(TEXT_DIAL_WIDTH))
                    lines[n] = TEXT_DIAL_WIDTH + settings.SpeedDial.ThumbnailWidth.ToString() + ";";
                else if (line.StartsWith(TEXT_DIAL_HEIGHT))
                    lines[n] = TEXT_DIAL_HEIGHT + settings.SpeedDial.ThumbnailHeight.ToString() + ";";
                else if (settings.SpeedDial.CropPageForThumbnail && line.StartsWith(TEXT_THUMBNAIL_URL1))
                    lines[n] = String.Format(CultureInfo.InvariantCulture, TEXT_THUMBNAIL_URL2, 100.0 / settings.SpeedDial.CropArea);
                else if (settings.SpeedDial.AddCustomThumbnails && line.Contains(TEXT_DIAL_URL))
                    lines[n] = "          PreinstalledSpeeddials.transformURL(this.navigateURL) || (SpeeddialObject.DIAL_URL + encodeURIComponent(this.navigateURL))";
                else if (settings.SpeedDial.AddCustomThumbnails && line.Contains(TEXT_DIAL_RELOAD_URL))
                {
                    lines[n] = "          PreinstalledSpeeddials.transformURL(this.navigateURL) || (SpeeddialObject.DIAL_RELOAD_URL + encodeURIComponent(this.navigateURL))";
                    lines[n + 1] = "";
                }
            }
            pakFile.SetItem(SpeeddialLayoutJs, lines);

            if (settings.SpeedDial.AddCustomThumbnails)
            {
                lines = pakFile.GetItem(SpeeddialSuggestionsJs);
                for (int n = 0; n < lines.Length; n++)
                {
                    if (lines[n].Contains(TEXT_ADD_DIAL_URL))
                    {
                        lines[n] = "    var sdURL = sd_obj.sdURL || PreinstalledSpeeddials.transformURL(sd_obj.url) || SpeeddialObject.DIAL_URL + encodeURIComponent(sd_obj.url);";
                        break;
                    }
                }
                pakFile.SetItem(SpeeddialSuggestionsJs, lines);
            }

            lines = pakFile.GetItem(StartPageHtml);
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                if (line == ".speeddial")
                {
                    ApplyCssPixelRule(lines, n, TEXT_CSS_WIDTH, settings.SpeedDial.ThumbnailWidth);
                    ApplyCssPixelRule(lines, n, TEXT_CSS_HEIGHT, settings.SpeedDial.ThumbnailHeight);
                    if (settings.SpeedDial.CropPageForThumbnail)
                        ApplyCssRule(lines, n, TEXT_CSS_BACKGROUND_POSITION, "0% 0%");
                }
                else if (line == ".dial-thumbnail")
                {
                    ApplyCssPixelRule(lines, n, TEXT_CSS_WIDTH, (settings.SpeedDial.ThumbnailWidth - 36) / 2);
                    ApplyCssPixelRule(lines, n, TEXT_CSS_HEIGHT, (settings.SpeedDial.ThumbnailHeight - 28) / 2);
                }
                else if (line == ".dial-thumbnail:nth-child(2)")
                {
                    ApplyCssPixelRule(lines, n, TEXT_CSS_LEFT, settings.SpeedDial.ThumbnailWidth / 2);
                }
                else if (line == ".dial-thumbnail:nth-child(3)")
                {
                    ApplyCssPixelRule(lines, n, TEXT_CSS_TOP, settings.SpeedDial.ThumbnailHeight / 2 - 1);
                }
                else if (line == ".dial-thumbnail:nth-child(4)")
                {
                    ApplyCssPixelRule(lines, n, TEXT_CSS_LEFT, settings.SpeedDial.ThumbnailWidth / 2);
                    ApplyCssPixelRule(lines, n, TEXT_CSS_TOP, settings.SpeedDial.ThumbnailHeight / 2 - 1);
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
                    lines[n + 1] = TEXT_PREINSTALLED_CHECK_URL_NEXTLINE + (settings.SpeedDial.DisableBuiltInThumbnails ? "return null;" : "");
                }
                else if (settings.SpeedDial.AddCustomThumbnails && line == TEXT_PREINSTALLED_LAST_LINE)
                {
                    StringBuilder sb = new StringBuilder("var _transformData = { re: null, getRe: function(dict) { return new RegExp(\"^.{4,5}:\\\\/\\\\/(?:[^.]+\\\\.)?(\" + _arr_to_regexp(Object.keys(dict)) + \")(?:\\\\/|$)\"); }, thumbnails: { ");
                    bool f = false;

                    foreach (KeyValuePair<string, string> kvp in settings.SpeedDial.CustomThumbnails)
                    {
                        if (f)
                            sb.Append(',');
                        sb.AppendFormat("\"{0}\": \"{1}\"", kvp.Key, kvp.Value);
                        f = true;
                    }

                    sb.Append("} }; this.transformURL = function(URL) { if (!_transformData.re) _transformData.re = _transformData.getRe(_transformData.thumbnails); var match = _transformData.re.exec(URL); return match ? _transformData.thumbnails[match[1]] : null; };");
                    lines[n] = sb.ToString() + TEXT_PREINSTALLED_LAST_LINE;
                }
            }
            pakFile.SetItem(PreinstalledSpeeddialsJs, lines);

            ColoredConsole.WriteLine("Writing ~W{0}~N ...", pakFileName);

            pakFile.Save(pakFileName + ".temp");
            File.Delete(pakFileName);
            File.Move(pakFileName + ".temp", pakFileName);

            string contentFileName = Path.Combine(Path.GetDirectoryName(pakFileName), "resources\\default_partner_content.json");
            string originalContentFileName = contentFileName + BackupExtension;
            if (!File.Exists(originalContentFileName) && File.Exists(contentFileName))
                File.Copy(contentFileName, originalContentFileName);
            else if (File.Exists(originalContentFileName))
                File.Copy(originalContentFileName, contentFileName, true);

            if (settings.Search.DeletePartnerSearchEngines)
                File.Delete(contentFileName);

            if (ExePatch != null && settings.PatchOperaExe)
                ExePatch.Apply(Path.ChangeExtension(pakFileName, ".exe"), settings);
        }

        private static void ApplyCssPixelRule(string[] lines, int index, string name, int value)
        {
            ApplyCssRule(lines, index, name, value.ToString() + "px");
        }

        private static void ApplyCssRule(string[] lines, int index, string name, string value)
        {
            for (int n = index + 1; n < lines.Length; n++)
            {
                string text = lines[n];
                if (text == "}")
                    break;
                else if (text.StartsWith(name))
                    lines[n] = name + value + ";";
            }
        }
    }
}
