using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpeedDialPatch
{
    public class ExePatch
    {
        public int Offset;
        public byte[] Original;
        public byte[] Patched;

        public ExePatch(int offset, byte[] original, byte[] patched)
        {
            Offset = offset;
            Original = original;
            Patched = patched;
            if (original.Length != patched.Length)
                throw new InvalidOperationException("Invalid executable patch: original and patched block sizes have different size.");
        }

        public ExePatch(int offset, string original, string patched) :
            this(offset, ByteArray.GetBytes(original), ByteArray.GetBytes(patched))
        {
        }

        public void Apply(string exeFileName, Settings settings)
        {
            string originalExeFileName = exeFileName + OperaPatch.BackupExtension;

            ColoredConsole.WriteLine("Reading ~W{0}~N ...", exeFileName);
            if (!File.Exists(originalExeFileName))
                File.Copy(exeFileName, originalExeFileName);
            else
                File.Copy(originalExeFileName, exeFileName, true);

            byte[] exeFile = File.ReadAllBytes(exeFileName);
            if (exeFile.Length < Offset + Original.Length)
                throw new InvalidOperationException("Invalid executable patch: offset error.");

            for (int n = 0; n < Original.Length; n++)
            {
                if (exeFile[Offset + n] != Original[n])
                    throw new InvalidOperationException("Invalid executable patch: original data do not match.");

                exeFile[Offset + n] = Patched[n];
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
    }
}
