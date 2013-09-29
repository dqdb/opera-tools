using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpeedDialPatch
{
    public class PakFile
    {
        public SortedDictionary<int, byte[]> Items;

        public PakFile()
        {
            Items = new SortedDictionary<int, byte[]>();
        }

        public void Load(string fileName)
        {
            byte[] data = File.ReadAllBytes(fileName);
            if (data.Length < 9)
                throw new InvalidDataException("Invalid file format.");

            int version = BitConverter.ToInt32(data, 0);
            int count = BitConverter.ToInt32(data, 4);
            if (version != 4)
                throw new InvalidDataException("Invalid file version.");

            for (int n = 0, position = 9; n < count; n++, position += 6)
            {
                int id = (int)BitConverter.ToUInt16(data, position);
                int offset = BitConverter.ToInt32(data, position + 2);
                int size = BitConverter.ToInt32(data, position + 8) - offset;
                byte[] item = new byte[size];
                Array.Copy(data, offset, item, 0, size);
                Items.Add(id, item);
            }
        }

        public void Save(string fileName)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(4);
                    binaryWriter.Write(Items.Count);
                    binaryWriter.Write((byte)1);
                    int offset = 9 + (Items.Count + 1) * 6;
                    foreach (KeyValuePair<int, byte[]> current in Items)
                    {
                        binaryWriter.Write((ushort)current.Key);
                        binaryWriter.Write(offset);
                        offset += current.Value.Length;
                    }
                    binaryWriter.Write((ushort)0);
                    binaryWriter.Write(offset);
                    foreach (KeyValuePair<int, byte[]> current in Items)
                        binaryWriter.Write(current.Value);
                }
            }
        }

        public string[] GetItem(int id)
        {
            return Encoding.UTF8.GetString(Items[id]).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        public void SetItem(int id, string value)
        {
            Items[id] = Encoding.UTF8.GetBytes(value);
        }

        public void SetItem(int id, string[] lines)
        {
            Items[id] = Encoding.UTF8.GetBytes(String.Join("\r\n", lines));
            //			File.WriteAllLines(id.ToString(), lines);
        }
    }
}
