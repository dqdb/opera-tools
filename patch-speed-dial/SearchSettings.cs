using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Drawing;

namespace SpeedDialPatch
{
    public class SearchSettings
    {
        public const string BuiltinUrl = "http://www.google.com/complete/search?client=opera&q={searchTerms}&hl=en";
        public const string BuiltinName = "Google";
        public const string BuiltinKeyword = "g";
        public const string BuiltinFavicon = "http://redir.opera.com/favicons/google/favicon.ico";
        public const string BuiltinSuggestionUrl = "http://www.google.com/search?q={searchTerms}&{google:sourceId}ie=utf-8&oe=utf-8&channel=suggest";

        public bool DeletePartnerSearchEngines;
        public string DefaultUrl;
        public string DefaultName;
        public string DefaultKeyword;
        public string DefaultFavicon;
        public string DefaultSuggestionUrl;

        public SearchSettings()
        {
            DeletePartnerSearchEngines = false;
            DefaultUrl = BuiltinUrl;
            DefaultName = BuiltinName;
            DefaultKeyword = BuiltinKeyword;
            DefaultFavicon = BuiltinFavicon;
            DefaultSuggestionUrl = BuiltinSuggestionUrl;
        }

        public void LoadFromConfig(XmlNode node, string name)
        {
            node = node.SelectSingleNode(name);
            if (node == null)
                return;

            DeletePartnerSearchEngines = ConfigFile.Read(node, "deletePartnerSearchEngines", DeletePartnerSearchEngines);
            DefaultUrl = ConfigFile.Read(node, "defaultUrl", DefaultUrl);
            DefaultName = ConfigFile.Read(node, "defaultName", DefaultName);
            DefaultKeyword = ConfigFile.Read(node, "defaultKeyword", DefaultKeyword);
            DefaultFavicon = ConfigFile.Read(node, "defaultFavicon", DefaultFavicon);
            DefaultSuggestionUrl = ConfigFile.Read(node, "defaultSuggestionUrl", DefaultSuggestionUrl);
        }

        public void LoadFromConsole()
        {
            DeletePartnerSearchEngines = ColoredConsole.Read("Delete partner search engines: ", DeletePartnerSearchEngines);
            if (!DeletePartnerSearchEngines)
                return;

            DefaultUrl = ColoredConsole.Read("Default search engine URL: ", CalculateMaxLength(BuiltinUrl), DefaultUrl);
            DefaultName = ColoredConsole.Read("Default search engine name: ", CalculateMaxLength(BuiltinName), DefaultName);
            DefaultKeyword = ColoredConsole.Read("Default search engine keyword: ", 1, DefaultKeyword);
            DefaultFavicon = ColoredConsole.Read("Default search engine favicon URL: ", CalculateMaxLength(BuiltinFavicon), DefaultFavicon);
            DefaultSuggestionUrl = ColoredConsole.Read("Default search engine suggestion URL: ", CalculateMaxLength(BuiltinSuggestionUrl), DefaultSuggestionUrl);
        }

        public static int CalculateMaxLength(string s)
        {
            return (s.Length / 4 + 1) * 4 - 1;
        }

        public void SaveToConfig(XmlWriter writer, string name)
        {
            writer.WriteStartElement(name);
            ConfigFile.Write(writer, "deletePartnerSearchEngines", DeletePartnerSearchEngines);
            ConfigFile.Write(writer, "defaultUrl", DefaultUrl);
            ConfigFile.Write(writer, "defaultName", DefaultName);
            ConfigFile.Write(writer, "defaultKeyword", DefaultKeyword);
            ConfigFile.Write(writer, "defaultFavicon", DefaultFavicon);
            ConfigFile.Write(writer, "defaultSuggestionUrl", DefaultSuggestionUrl);
            writer.WriteEndElement();
        }
    }
}
