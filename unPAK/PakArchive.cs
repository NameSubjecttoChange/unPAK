using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unPAK
{
    class PakArchive : IDisposable
    {
        private Stream _fileStream;
        private const int _fileTableLocation = 0x18;
        private int _nameTableLocation;
        private List<string> _nameTable = new List<string>();
        private Func<int, PakEntry> ReadEntryFunc;
        private BinaryReader _fileReader;

        public NameMode Mode { get; private set; }
        public List<PakEntry> Entries { get; private set; }
        public int TotalEntries { get; private set; }

        public PakArchive(Stream stream)
        {
            _fileStream = stream;
            _fileReader = new BinaryReader(stream, Encoding.UTF8, true);
            ReadEntries();
        }

        public static PakArchive Load(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var stream = File.Open(path, FileMode.Open);
            return new PakArchive(stream);
        }

        public static void CreateNew(string inPath, string outPath)
        {
            FileStream pakFile = new FileStream(outPath, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(pakFile);
            int numFiles = Directory.GetFiles(inPath, "*", SearchOption.TopDirectoryOnly).Length;
            int allLength = 0;
            bool idBased = false;
            string[] files = Directory.GetFiles(inPath);
            if (Path.GetFileName(files[0]) == "0")
            {
                files = files.OrderBy(x => int.Parse(x.Substring(x.LastIndexOf("\\")+1))).ToArray();
                idBased = true;
            }
            long namPos = 0x18 + numFiles * 8;
            bw.Write(numFiles);
            pakFile.Seek(0x18, SeekOrigin.Begin);
            int fileP;
            if (!idBased)
            {
                for (int i = 0; i < numFiles; i++)
                {
                    allLength += (Path.GetFileName(files[i]) + char.MinValue).Length;
                }
                fileP = 0x18 + numFiles * 8 + allLength;
            }
            else
            {
                fileP = 0x18 + numFiles * 8;
            }
            for (int i = 1; i < numFiles + 1; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("Packing file " + i + "/" + numFiles + "...");
                FileStream in_file = new FileStream(files[i - 1], FileMode.Open);
                BinaryReader br = new BinaryReader(in_file);
                var filePos = RoundTo2KB(fileP);
                int fileSize = Convert.ToInt32(new FileInfo(files[i - 1]).Length);
                bw.Write(filePos);
                bw.Write(fileSize);
                var curPos = pakFile.Position;
                if (!idBased)
                {
                    pakFile.Seek(namPos, SeekOrigin.Begin);
                    bw.Write(Encoding.ASCII.GetBytes(Path.GetFileName(files[i - 1]) + char.MinValue));
                    namPos = Convert.ToInt32(pakFile.Position);
                }
                pakFile.Seek(filePos, SeekOrigin.Begin);
                bw.Write(br.ReadBytes(fileSize));
                fileP = Convert.ToInt32(pakFile.Position);
                pakFile.Seek(curPos, SeekOrigin.Begin);
            }
            pakFile.Seek(4, SeekOrigin.Begin);
            bw.Write(Convert.ToInt32(pakFile.Length));
            pakFile.Close();
        }

        private void ReadEntries()
        {
            Entries = new List<PakEntry>();
            TotalEntries = _fileReader.ReadInt32();
            _nameTableLocation = _fileTableLocation + TotalEntries * 8;
            _fileStream.Seek(_nameTableLocation, SeekOrigin.Begin);
            Mode = _fileReader.PeekChar() == 0x0 ? NameMode.IdBased : NameMode.NameBased;
            if (Mode == NameMode.NameBased)
            {
                ReadEntryFunc = ReadEntryName;
                ReadNameTable();
            }
            else ReadEntryFunc = ReadEntryId;
            _fileStream.Seek(_fileTableLocation, SeekOrigin.Begin);
            for (int i = 0; i < TotalEntries; i++)
            {
                Entries.Add(ReadEntryFunc(i));
            }
        }

        private PakEntry ReadEntryId(int id)
        {
            int position = _fileReader.ReadInt32();
            int size = _fileReader.ReadInt32();
            return new PakEntry(id,position,size);
        }

        private PakEntry ReadEntryName(int id)
        {
            int position = _fileReader.ReadInt32();
            int size = _fileReader.ReadInt32();
            return new PakEntry(id, position, size, _nameTable[id]);        
        }

        private void ReadNameTable()
        {
            for (int i = 0; i < TotalEntries; i++)
            {
                _nameTable.Add(_fileReader.ReadNullTerminatedString());
            }
        }

        public byte[] ExctractEntry(PakEntry entry)
        {
            _fileStream.Seek(entry.Position, SeekOrigin.Begin);
            return _fileReader.ReadBytes(entry.Size);
        }

        public void Dispose()
        {
            _fileReader?.Dispose();
            _fileStream?.Close();
        }

        private static int RoundTo2KB(int input)
        {
            return ((input / 2048) + (((input % 2048) == 0) ? 0 : 1)) * 2048;
        }

        public enum NameMode
        {
            IdBased,
            NameBased
        }
    }
}
