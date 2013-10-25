using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedDialPatch
{
    public class HeuristicPatch
    {
        public byte[] Find;
        public byte[] Original;
        public byte[] Patched;

        public HeuristicPatch(byte[] find, byte[] original, byte[] patched)
        {
            Find = find;
            Original = original;
            Patched = patched;
        }

        public HeuristicPatch(string find, string original, string patched) :
            this(ByteArray.GetBytes(find), ByteArray.GetBytes(original), ByteArray.GetBytes(patched))
        {
        }
    }
}
