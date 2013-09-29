using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace SpeedDialPatch
{
    public class Settings
    {
        public int SpeedDialColumns;
        public int SpeedDialPreviewWidth;
        public int SpeedDialPreviewHeight;
        public bool DisableBuiltInImages;
        public string OperaFolder;
        public CssPatches CssPatches;

        public Settings()
        {
            SpeedDialColumns = 5;
            SpeedDialPreviewWidth = 230;
            SpeedDialPreviewHeight = 170;
            DisableBuiltInImages = false;
            OperaFolder = Directory.GetCurrentDirectory();
            CssPatches = new CssPatches();
        }

        public void LoadFromConfigFile()
        {
            string fileName = GetFileName();
            if (!File.Exists(fileName))
                return;

            string[] config = File.ReadAllText(fileName).Split('|');
            if (config.Length < 5 || config.Length > 6)
                return;

            SpeedDialColumns = Convert.ToInt32(config[0]);
            SpeedDialPreviewWidth = Convert.ToInt32(config[1]);
            SpeedDialPreviewHeight = Convert.ToInt32(config[2]);
            DisableBuiltInImages = Convert.ToBoolean(config[3]);
            OperaFolder = config[4];

            if (config.Length == 6)
                CssPatches.LoadFromConfigFile(config[5]);
        }

        public void LoadFromCommandLine(string[] args)
        {
            for (int n = 0; n < args.Length; n++)
            {
                if (args[n] == "-columns" && n < args.Length - 1)
                    SpeedDialColumns = Convert.ToInt32(args[++n]);
                else if (args[n] == "-width" && n < args.Length - 1)
                    SpeedDialPreviewWidth = Convert.ToInt32(args[++n]);
                else if (args[n] == "-height" && n < args.Length - 1)
                    SpeedDialPreviewHeight = Convert.ToInt32(args[++n]);
                else if (args[n] == "-disablebuiltinimages")
                    DisableBuiltInImages = true;
                else if (args[n] == "-folder" && n < args.Length - 1)
                    OperaFolder = args[++n];
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
            SpeedDialColumns = ColoredConsole.ReadNumber("Number of speed dial columns: ", SpeedDialColumns);
            SpeedDialPreviewWidth = ColoredConsole.ReadNumber("Speed dial preview width: ", SpeedDialPreviewWidth);
            SpeedDialPreviewHeight = ColoredConsole.ReadNumber("Speed dial preview height: ", SpeedDialPreviewHeight);
            DisableBuiltInImages = !ColoredConsole.ReadBoolean("Use built-in preview images: ", !DisableBuiltInImages);
            CssPatches.LoadFromConsole();

        }

        public void SaveToConfigFile()
        {
            File.WriteAllText(GetFileName(), String.Format("{0}|{1}|{2}|{3}|{4}|{5}",
                SpeedDialColumns, SpeedDialPreviewWidth, SpeedDialPreviewHeight, 
                DisableBuiltInImages, OperaFolder, CssPatches.SaveToConfigFile()));
        }

        private static string GetFileName()
        {
            return Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".config");
        }
    }
}
