using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unPAK
{
    public static class BinaryReaderExtension
    {
        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            List<byte> ch = new List<byte>();
            do
            {
                ch.Add(reader.ReadByte());
            } while (reader.PeekChar() != 0x0);
            reader.ReadByte();
            return Encoding.GetEncoding("shift-jis").GetString(ch.ToArray());
        }
    }
}
