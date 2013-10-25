using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Xml;

namespace SpeedDialPatch
{
    public class Settings
    {
        public string OperaFolder;
        public bool PatchOperaExe;
        public SpeedDialSettings SpeedDial;
        public CssPatches CssPatches;
        public SearchSettings Search;

        public Settings()
        {
            OperaFolder = Directory.GetCurrentDirectory();
            PatchOperaExe = true;
            SpeedDial = new SpeedDialSettings();
            CssPatches = new CssPatches();
            Search = new SearchSettings();
        }

        public void LoadFromConfigFile()
        {
            string fileName = GetFileName();
            if (!File.Exists(fileName))
                return;

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(fileName);
                XmlNode node = doc.DocumentElement;

                OperaFolder = ConfigFile.Read(node, "operaFolder", OperaFolder);
                PatchOperaExe = ConfigFile.Read(node, "patchOperaExe", PatchOperaExe);
                SpeedDial.LoadFromConfig(node, "speedDial");
                CssPatches.LoadFromConfig(node, "cssPatches");
                Search.LoadFromConfig(node, "search");
            }
            catch (XmlException)
            {
                string[] config = File.ReadAllText(fileName).Split('|');
                if (config.Length < 5 || config.Length > 7)
                    return;

                SpeedDial.Columns = Convert.ToInt32(config[0]);
                SpeedDial.ThumbnailWidth = Convert.ToInt32(config[1]);
                SpeedDial.ThumbnailHeight = Convert.ToInt32(config[2]);
                SpeedDial.DisableBuiltInThumbnails = Convert.ToBoolean(config[3]);
                OperaFolder = config[4];

                if (config.Length >= 6)
                    CssPatches.LoadFromString(config[5]);

                if (config.Length >= 7)
                    SpeedDial.AddCustomThumbnails = Convert.ToBoolean(config[6]);
            }
        }

        public void LoadFromConsole()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "Select Opera folder:";
            dlg.SelectedPath = OperaFolder;

            for (; ; )
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    Environment.Exit(1);

                if (File.Exists(Path.Combine(dlg.SelectedPath, "launcher.exe")))
                    break;

                MessageBox.Show("Unable to find launcher.exe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            OperaFolder = dlg.SelectedPath;

            ColoredConsole.WriteLine("Opera folder: ~W{0}~N", OperaFolder);
            SpeedDial.LoadFromConsole();
            CssPatches.LoadFromConsole();
            Search.LoadFromConsole();
        }

        public void SaveToConfigFile()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(GetFileName(), settings))
            {
                writer.WriteStartElement("config");
                
                ConfigFile.Write(writer, "operaFolder", OperaFolder);
                ConfigFile.Write(writer, "patchOperaExe", PatchOperaExe);
                SpeedDial.SaveToConfig(writer, "speedDial");
                CssPatches.SaveToConfig(writer, "cssPatches");
                Search.SaveToConfig(writer, "search");
            }
        }

        private static string GetFileName()
        {
            return Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".config");
        }

    }
}
