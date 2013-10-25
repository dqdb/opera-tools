using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpeedDialPatch
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            int result;

            try
            {
                ColoredConsole.WriteLine("~WOpera Speed Dial Patch for {0}~N", OperaPatches.Patches[0].StartVersion);
                ColoredConsole.WriteLine("~WCopyright (c) 2013 dqdb~N");
                ColoredConsole.WriteLine();
                ColoredConsole.WriteLine("Thanks to ~WIzer0~N for all patches.");
                ColoredConsole.WriteLine();
                Settings settings = new Settings();
                settings.LoadFromConfigFile();
                settings.LoadFromConsole();
                settings.SaveToConfigFile();
                ColoredConsole.WriteLine();

                string pakFileName = FindLatestOperaPak(settings.OperaFolder);
                if (pakFileName == null)
                {
                    ColoredConsole.WriteLine("~r~WError:~k~R unable to find an ~r~Wopera.pak~N to patch.~N");
                    return 1;
                }

                OperaVersion version;

                try
                {
                    version = GetOperaPakVersion(pakFileName);
                }
                catch (Exception)
                {
                    ColoredConsole.WriteLine("~r~WError:~k~R unable to get version number from ~r~W{0}~k~R.~N", pakFileName);
                    return 1;
                }

                ColoredConsole.WriteLine("Opera version: ~W{0}~N", version);

                OperaPatch operaPatch = OperaPatches.Find(version);
                if (operaPatch == null)
                {
                    operaPatch = OperaPatches.FindWithHeuristics(settings, pakFileName);
                    if (operaPatch == null)
                    {
                        ColoredConsole.WriteLine();
                        ColoredConsole.WriteLine("~r~WError:~k~R this Opera version is not supported yet.~N");
                        return 1;
                    }

                    ColoredConsole.WriteLine();
                    ColoredConsole.WriteLine("Processing ...");
                    ColoredConsole.WriteLine();
                    ColoredConsole.WriteLine("OperaExeOffset: ~W0x{0:x8}~N", operaPatch.ExePatch.Offset);
                    ColoredConsole.WriteLine("SpeeddialLayoutJs: ~W{0}~N", operaPatch.SpeeddialLayoutJs);
                    ColoredConsole.WriteLine("StartPageHtml: ~W{0}~N", operaPatch.StartPageHtml);
                    ColoredConsole.WriteLine("PreinstalledSpeeddialsJs: ~W{0}~N", operaPatch.PreinstalledSpeeddialsJs);
                    ColoredConsole.WriteLine("SpeeddialSuggestionsJs: ~W{0}~N", operaPatch.SpeeddialSuggestionsJs);
                    ColoredConsole.WriteLine("ToolsCss: ~W{0}~N", operaPatch.ToolsCss);
                    ColoredConsole.WriteLine("FilterCss: ~W{0}~N", operaPatch.FilterCss);
                    ColoredConsole.WriteLine();

                    ColoredConsole.WriteLine("~y~KWarning:~k~Y this Opera version is probably supported, but it is ~y~KNOT~k~Y tested yet. Improper patching may have side effects.~N");
                    ColoredConsole.WriteLine();
                    if (!ColoredConsole.Read("I understand the risks: ", false))
                        return 1;
                    ColoredConsole.WriteLine();
                }
                else
                {
                    ColoredConsole.WriteLine("Using resource layout for Opera ~W{0}~N - ~W{1}~N builds.", operaPatch.StartVersion, operaPatch.EndVersion);
                    ColoredConsole.WriteLine();
                }

                if (!ColoredConsole.Read("I would like to start patching: ", true))
                    return 1;

                ColoredConsole.WriteLine();

                operaPatch.Apply(settings, pakFileName);
                result = 0;
            }
            catch (Exception ex)
            {
                ColoredConsole.WriteLine();
                if (ex.InnerException == null)
                    ColoredConsole.WriteLine("~r~WError:~k~R {0}~N", ex.Message);
                else
                    ColoredConsole.WriteLine("~r~WError:~k~R {0}~N ----> {1}", ex.Message, ex.InnerException.ToString());
                result = 1;
            }
            return result;
        }

        private static OperaVersion GetOperaPakVersion(string fileName)
        {
            return OperaVersion.Parse(Path.GetFileName(Path.GetDirectoryName(fileName)));
        }

        private static string FindLatestOperaPak(string baseFolder)
        {
            string[] folders = Directory.GetDirectories(baseFolder);
            string result = null;
            OperaVersion resultVersion = null;

            foreach (string folder in folders)
            {
                string fileName = Path.Combine(folder, "opera.pak");
                OperaVersion version;
                if (!File.Exists(fileName) || !OperaVersion.TryParse(Path.GetFileName(folder), out version))
                    continue;

                if (resultVersion == null || version > resultVersion)
                {
                    result = fileName;
                    resultVersion = version;
                }
            }

            return result;
        }
    }
}
