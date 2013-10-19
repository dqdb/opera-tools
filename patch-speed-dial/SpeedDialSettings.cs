using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Drawing;

namespace SpeedDialPatch
{
    public class SpeedDialSettings
    {
        public int Columns;
        public int MarginX;
        public int MarginY;
        public int ThumbnailWidth;
        public int ThumbnailHeight;
        public bool DisableBuiltInThumbnails;
        public bool AddCustomThumbnails;
        public Dictionary<string, string> CustomThumbnails;
        public bool CropPageForThumbnail;
        public int CropArea;

        public SpeedDialSettings()
        {
            Columns = 5;
            ThumbnailWidth = 230;
            ThumbnailHeight = 170;
            MarginX = 32;
            MarginY = 32;
            DisableBuiltInThumbnails = false;
            AddCustomThumbnails = true;
            CustomThumbnails = new Dictionary<string, string>();
            CropPageForThumbnail = false;
            CropArea = 25;
        }

        public void LoadFromConfig(XmlNode node, string name)
        {
            node = node.SelectSingleNode(name);
            if (node == null)
                return;

            Columns = ConfigFile.Read(node, "columns", Columns);
            ThumbnailWidth = ConfigFile.Read(node, "thumbnailWidth", ThumbnailWidth);
            ThumbnailHeight = ConfigFile.Read(node, "thumbnailHeight", ThumbnailHeight);
            MarginX = ConfigFile.Read(node, "marginX", MarginX);
            MarginY = ConfigFile.Read(node, "marginY", MarginY);
            DisableBuiltInThumbnails = ConfigFile.Read(node, "disableBuiltInThumbnails", DisableBuiltInThumbnails);
            AddCustomThumbnails = ConfigFile.Read(node, "addCustomThumbnails", AddCustomThumbnails);
            CropPageForThumbnail = ConfigFile.Read(node, "cropPageForThumbnail", CropPageForThumbnail);
            CropArea = ConfigFile.Read(node, "cropArea", CropArea);
        }

        public void LoadFromConsole()
        {
            Columns = ColoredConsole.Read("Speed dial columns: ", Columns);
            ThumbnailWidth = ColoredConsole.Read("Thumbnail width: ", ThumbnailWidth);
            ThumbnailHeight = ColoredConsole.Read("Thumbnail height: ", ThumbnailHeight);
            MarginX = ColoredConsole.Read("Horizontal space between thumbnails: ", MarginX);
            MarginY = ColoredConsole.Read("Vertical space between thumbnails: ", MarginY);
            DisableBuiltInThumbnails = ColoredConsole.Read("Disable built-in thumbnails: ", DisableBuiltInThumbnails);
            AddCustomThumbnails = ColoredConsole.Read("Add custom thumbnails: ", AddCustomThumbnails);
            CropPageForThumbnail = ColoredConsole.Read("Crop page for thumbnail like in Opera 12: ", CropPageForThumbnail);
            if (CropPageForThumbnail)
            {
                for (; ; )
                {
                    CropArea = ColoredConsole.Read("Crop area (top left 1-100% of the page): ", CropArea);
                    ColoredConsole.WriteLine();
                    if (CropArea > 0 && CropArea <= 100)
                    {
                        double ratio = 100.0 / CropArea;

                        ColoredConsole.WriteLine("~y~KWarning:~k~Y each thumbnail will consume {0:G3}x memory than without cropping.~N", ratio * ratio);
                        ColoredConsole.WriteLine();
                        if (ColoredConsole.Read("I would like to modify the value: ", false))
                        {
                            ColoredConsole.WriteLine();
                            continue;
                        }
                        break;
                    }

                    ColoredConsole.WriteLine("~r~WError:~k~R invalid crop area.~N");
                    ColoredConsole.WriteLine();
                }

                CropPageForThumbnail = CropArea != 100;
                ColoredConsole.WriteLine();
            }

            if (AddCustomThumbnails)
            {
                string[] fileNames = Directory.GetFiles("sdimages", "*.png");
                for (int n = 0; n < fileNames.Length; n++)
                {
                    string fileName = fileNames[n];
                    string name = Path.GetFileNameWithoutExtension(fileName);

                    ColoredConsole.WriteLine("Adding thumbnail for ~W{0}~N ...", name);
                    Image image = Image.FromFile(fileName);
                    if (image.Width != ThumbnailWidth || image.Height != ThumbnailHeight)
                        ColoredConsole.WriteLine("~y~KWarning:~k~Y thumbnail image resolution is not {0}x{1}.~N", ThumbnailWidth, ThumbnailHeight);

                    CustomThumbnails.Add(name, "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(fileName)));
                }
            }
        }

        public void SaveToConfig(XmlWriter writer, string name)
        {
            writer.WriteStartElement(name);
            ConfigFile.Write(writer, "columns", Columns);
            ConfigFile.Write(writer, "thumbnailWidth", ThumbnailWidth);
            ConfigFile.Write(writer, "thumbnailHeight", ThumbnailHeight);
            ConfigFile.Write(writer, "marginX", MarginX);
            ConfigFile.Write(writer, "marginY", MarginY);
            ConfigFile.Write(writer, "disableBuiltInThumbnails", DisableBuiltInThumbnails);
            ConfigFile.Write(writer, "addCustomThumbnails", AddCustomThumbnails);
            ConfigFile.Write(writer, "cropPageForThumbnail", CropPageForThumbnail);
            ConfigFile.Write(writer, "cropArea", CropArea);
            writer.WriteEndElement();
        }
    }
}
