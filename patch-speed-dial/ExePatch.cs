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

        public void Apply(string exeFileName)
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

            ColoredConsole.WriteLine("Writing ~W{0}~N ...", exeFileName);
            File.WriteAllBytes(exeFileName + ".temp", exeFile);
            File.Delete(exeFileName);
            File.Move(exeFileName + ".temp", exeFileName);
        }

    }
}
