using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace SpeedDialPatch
{
    public static class OperaPatches
    {
        public static OperaPatch FindWithHeuristics(Settings settings, string pakFileName)
        {
            PakFile pakFile = new PakFile();
            pakFile.Load(pakFileName);
            string exeFileName = Path.ChangeExtension(pakFileName, ".exe");
            string originalExeFileName = exeFileName + OperaPatch.BackupExtension;
            byte[] exeFile = File.ReadAllBytes(File.Exists(originalExeFileName) ? originalExeFileName : exeFileName);

            int operaPakHashOffset = -1;

            if (settings.PatchOperaExe)
            {
                string originalPakFileName = pakFileName + OperaPatch.BackupExtension;
                operaPakHashOffset = ByteArray.Find(exeFile, GetPakFileHash(File.Exists(originalPakFileName) ? originalPakFileName : pakFileName));
                if (operaPakHashOffset < 0)
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
                speeddialLayoutJs, startPageHtml, preinstalledSpeeddialsJs, speeddialSuggestionsJs, toolsCss, filterCss, 
                operaPakHashOffset);
        }

        public static byte[] GetPakFileHash(string fileName)
        {
            // base64(sha1(file))
            return Encoding.ASCII.GetBytes(Convert.ToBase64String(SHA1.Create().ComputeHash(File.ReadAllBytes(fileName))));
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
