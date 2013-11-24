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

        public int SpeeddialLayoutJs;
        public int StartPageHtml;
        public int PreinstalledSpeeddialsJs;
        public int SpeeddialSuggestionsJs;
        public int ToolsCss;
        public int FilterCss;
        public int OperaPakHashOffset;

        public OperaPatch(int speeddialLayoutJs, int startPageHtml, int preinstalledSpeeddialsJs, int speeddialSuggestionsJs, int toolsCss, int filterCss, int operaPakHashOffset)
        {
            SpeeddialLayoutJs = speeddialLayoutJs;
            StartPageHtml = startPageHtml;
            PreinstalledSpeeddialsJs = preinstalledSpeeddialsJs;
            SpeeddialSuggestionsJs = speeddialSuggestionsJs;
            ToolsCss = toolsCss;
            FilterCss = filterCss;
            OperaPakHashOffset = operaPakHashOffset;
        }

        public void Apply(Settings settings, string pakFileName)
        {
            string originalPakFileName = pakFileName + BackupExtension;
            CssPatches cssPatches = settings.CssPatches;

            PakFile pakFile = new PakFile();
            ColoredConsole.WriteLine("Reading ~W{0}~N ...", pakFileName);

            if (!File.Exists(originalPakFileName))
                File.Copy(pakFileName, originalPakFileName);
            else
                File.Copy(originalPakFileName, pakFileName, true);

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

            if (settings.PatchOperaExe)
            {
                string exeFileName = Path.ChangeExtension(pakFileName, ".exe");
                string originalExeFileName = exeFileName + OperaPatch.BackupExtension;

                ColoredConsole.WriteLine("Reading ~W{0}~N ...", exeFileName);
                if (!File.Exists(originalExeFileName))
                    File.Copy(exeFileName, originalExeFileName);
                else
                    File.Copy(originalExeFileName, exeFileName, true);

                byte[] exeFile = File.ReadAllBytes(exeFileName);
                byte[] patched = OperaPatches.GetPakFileHash(pakFileName);
                byte[] original = OperaPatches.GetPakFileHash(originalPakFileName);
                if (exeFile.Length < OperaPakHashOffset + patched.Length)
                    throw new InvalidOperationException("Invalid executable patch: offset error.");

                for (int n = 0; n < original.Length; n++)
                {
                    if (exeFile[OperaPakHashOffset + n] != original[n])
                        throw new InvalidOperationException("Invalid executable patch: original data do not match.");

                    exeFile[OperaPakHashOffset + n] = patched[n];
                }

                if (settings.Search.DeletePartnerSearchEngines)
                {
                    byte[] search = Encoding.ASCII.GetBytes(SearchSettings.BuiltinUrl);
                    for (int offset = 0; offset < exeFile.Length - search.Length - 1; offset++)
                    {
                        bool found = true;
                        for (int n = 0; found && n < search.Length; n++)
                            found = exeFile[offset + n] == search[n];

                        if (found && exeFile[offset + search.Length] == 0)
                        {
                            byte[] builtin = BuildSearchSettings(SearchSettings.BuiltinUrl, SearchSettings.BuiltinName, SearchSettings.BuiltinKeyword, SearchSettings.BuiltinFavicon, SearchSettings.BuiltinSuggestionUrl);
                            byte[] default1 = BuildSearchSettings(settings.Search.DefaultUrl, settings.Search.DefaultName, settings.Search.DefaultKeyword, settings.Search.DefaultFavicon, settings.Search.DefaultSuggestionUrl);
                            for (int n = 0; found && n < builtin.Length; n++)
                                found = exeFile[offset + n] == builtin[n];

                            if (found)
                                Array.Copy(default1, 0, exeFile, offset, default1.Length);
                            else
                                ColoredConsole.WriteLine("~y~KWarning:~k~Y built-in search engine structure has been changed. You must update SpeedDialPatch.~N");
                        }
                    }
                }

                ColoredConsole.WriteLine("Writing ~W{0}~N ...", exeFileName);
                File.WriteAllBytes(exeFileName + ".temp", exeFile);
                File.Delete(exeFileName);
                File.Move(exeFileName + ".temp", exeFileName);
            }
        }

        private static byte[] BuildSearchSettings(string searchUrl, string searchName, string searchKeyword, string searchFavicon, string searchSuggestionUrl)
        {
            int searchUrlSize = SearchSettings.CalculateMaxLength(SearchSettings.BuiltinUrl) + 1;
            int searchNameSize = SearchSettings.CalculateMaxLength(SearchSettings.BuiltinName) + 1;
            int searchKeywordSize = SearchSettings.CalculateMaxLength(SearchSettings.BuiltinKeyword) + 1;
            int searchFaviconSize = SearchSettings.CalculateMaxLength(SearchSettings.BuiltinFavicon) + 1;
            int searchDummySize = SearchSettings.CalculateMaxLength("") + 1;
            int searchSuggestionUrlSize = SearchSettings.CalculateMaxLength(SearchSettings.BuiltinSuggestionUrl) + 1;
            int size = searchUrlSize + searchNameSize + searchKeywordSize + searchFaviconSize + searchDummySize + searchSuggestionUrlSize;
            byte[] result = new byte[size];
            byte[] searchUrl1 = Encoding.ASCII.GetBytes(searchUrl);
            byte[] searchName1 = Encoding.ASCII.GetBytes(searchName);
            byte[] searchKeyword1 = Encoding.ASCII.GetBytes(searchKeyword);
            byte[] searchFavicon1 = Encoding.ASCII.GetBytes(searchFavicon);
            byte[] searchSuggestionUrl1 = Encoding.ASCII.GetBytes(searchSuggestionUrl);
            int offset = 0;

            Array.Copy(searchUrl1, 0, result, offset, searchUrl1.Length);
            offset += searchUrlSize;
            Array.Copy(searchName1, 0, result, offset, searchName1.Length);
            offset += searchNameSize;
            Array.Copy(searchKeyword1, 0, result, offset, searchKeyword1.Length);
            offset += searchKeywordSize;
            Array.Copy(searchFavicon1, 0, result, offset, searchFavicon1.Length);
            offset += searchFaviconSize;
            offset += searchDummySize;
            Array.Copy(searchSuggestionUrl1, 0, result, offset, searchSuggestionUrl1.Length);
            return result;
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
