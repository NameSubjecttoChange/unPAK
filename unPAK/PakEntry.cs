using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unPAK
{
    class PakEntry
    {
        public int Id { get; }
        public int Position { get; }
        public int Size { get; }
        public string Name { get; }

        public PakEntry(int id, int position, int size, string name = "")
        {
            Id = id;
            Position = position;
            Size = size;
            Name = name;
        }
    }
}
