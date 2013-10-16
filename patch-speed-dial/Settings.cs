using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Drawing;

namespace SpeedDialPatch
{
    public class Settings
    {
        public int SpeedDialColumns;
        public int SpeedDialThumbnailWidth;
        public int SpeedDialThumbnailHeight;
        public bool DisableBuiltInThumbnails;
        public bool AddCustomThumbnails;
        public string OperaFolder;
        public CssPatches CssPatches;
        public Dictionary<string, string> CustomThumbnails;

        public Settings()
        {
            SpeedDialColumns = 5;
            SpeedDialThumbnailWidth = 230;
            SpeedDialThumbnailHeight = 170;
            DisableBuiltInThumbnails = false;
            OperaFolder = Directory.GetCurrentDirectory();
            CssPatches = new CssPatches();
            AddCustomThumbnails = true;
            CustomThumbnails = new Dictionary<string, string>();
        }

        public void LoadFromConfigFile()
        {
            string fileName = GetFileName();
            if (!File.Exists(fileName))
                return;

            string[] config = File.ReadAllText(fileName).Split('|');
            if (config.Length < 5 || config.Length > 7)
                return;

            SpeedDialColumns = Convert.ToInt32(config[0]);
            SpeedDialThumbnailWidth = Convert.ToInt32(config[1]);
            SpeedDialThumbnailHeight = Convert.ToInt32(config[2]);
            DisableBuiltInThumbnails = Convert.ToBoolean(config[3]);
            OperaFolder = config[4];

            if (config.Length >= 6)
                CssPatches.LoadFromConfigFile(config[5]);

            if (config.Length >= 7)
                AddCustomThumbnails = Convert.ToBoolean(config[6]);
        }

        public void LoadFromCommandLine(string[] args)
        {
            for (int n = 0; n < args.Length; n++)
            {
                if (args[n] == "-columns" && n < args.Length - 1)
                    SpeedDialColumns = Convert.ToInt32(args[++n]);
                else if (args[n] == "-width" && n < args.Length - 1)
                    SpeedDialThumbnailWidth = Convert.ToInt32(args[++n]);
                else if (args[n] == "-height" && n < args.Length - 1)
                    SpeedDialThumbnailHeight = Convert.ToInt32(args[++n]);
                else if (args[n] == "-disablebuiltinthumbnails")
                    DisableBuiltInThumbnails = true;
                else if (args[n] == "-addcustomthumbnails")
                    AddCustomThumbnails = true;
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
            SpeedDialThumbnailWidth = ColoredConsole.ReadNumber("Speed dial thumbnail width: ", SpeedDialThumbnailWidth);
            SpeedDialThumbnailHeight = ColoredConsole.ReadNumber("Speed dial thumbnail height: ", SpeedDialThumbnailHeight);
            DisableBuiltInThumbnails = ColoredConsole.ReadBoolean("Disable built-in speed dial thumbnails: ", DisableBuiltInThumbnails);
            AddCustomThumbnails = ColoredConsole.ReadBoolean("Add custom speed dial thumbnails: ", AddCustomThumbnails);
            CssPatches.LoadFromConsole();
        }

        public void SaveToConfigFile()
        {
            File.WriteAllText(GetFileName(), String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                SpeedDialColumns, SpeedDialThumbnailWidth, SpeedDialThumbnailHeight, 
                DisableBuiltInThumbnails, OperaFolder, CssPatches.SaveToConfigFile(), AddCustomThumbnails));
        }

        private static string GetFileName()
        {
            return Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".config");
        }

        public void LoadCustomThumbnails()
        {
            if (!AddCustomThumbnails)
                return;

            string[] fileNames = Directory.GetFiles("sdimages", "*.png");
            for (int n = 0; n < fileNames.Length; n++)
            {
                string fileName = fileNames[n];
                string name = Path.GetFileNameWithoutExtension(fileName);

                ColoredConsole.WriteLine("Adding thumbnail for ~W{0}~N ...", name);
                Image image = Image.FromFile(fileName);
                if (image.Width != SpeedDialThumbnailWidth || image.Height != SpeedDialThumbnailHeight)
                    ColoredConsole.WriteLine("~y~KWarning:~k~Y thumbnail image resolution is not {0}x{1}.~N", SpeedDialThumbnailWidth, SpeedDialThumbnailHeight);

                CustomThumbnails.Add(name, "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(fileName)));
            }
        }
    }
}
