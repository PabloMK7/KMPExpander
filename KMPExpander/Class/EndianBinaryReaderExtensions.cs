using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibCTR.Collections;
using LibEndianBinaryIO;

namespace LibCTR.IO
{
    public static class EndianBinaryReaderExtensions
    {
        public static Single ReadFx16(this EndianBinaryReader er)
        {
            return er.ReadInt16() / 4096f;
        }

        public static Vector3 ReadVecFx16(this EndianBinaryReader er)
        {
            return new Vector3(er.ReadFx16(), er.ReadFx16(), er.ReadFx16());
        }

        public static Single[] ReadFx16s(this EndianBinaryReader er, int Count)
        {
            float[] result = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                result[i] = er.ReadFx16();
            }
            return result;
        }

        public static Single ReadFx32(this EndianBinaryReader er)
        {
            return er.ReadInt32() / 4096f;
        }

        public static Vector3 ReadVecFx32(this EndianBinaryReader er)
        {
            return new Vector3(er.ReadFx32(), er.ReadFx32(), er.ReadFx32());
        }

        public static Single[] ReadFx32s(this EndianBinaryReader er, int Count)
        {
            float[] result = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                result[i] = er.ReadFx32();
            }
            return result;
        }
    }
}
