using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unPAK
{
    class Program
    {
        private static Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>()
        {
            ["open"] = Open,
            ["ls"] = ListEntries,
            ["extract"] = Extract,
            ["extract-all"] = ExtractAll,
            ["create"] = Create,
            ["help"] = Help,
            ["exit"] = Exit
        };
        private static PakArchive _archive;
        private static string _path;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var path = args[0];
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                {
                    _path = path;
                    _commands["create"].Invoke(new[] {"create", path});
                }
                else
                {
                    _path = path;
                    _commands["open"].Invoke(new[] { "open", path });
                    _commands["extract-all"].Invoke(new[] {path});
                }
            }
            else
            {
                InteractiveMode();
                Console.WriteLine("Not enough arguments! Please specify the folder to pack or file to unpack!");
            }
        }

        static void Open(string[] args)
        {
            if (args.Length == 2)
            {
                _archive = PakArchive.Load(args[1]);
                _path = args[1];
                Console.WriteLine("Archive opened!");
            }
            else Console.WriteLine("Invalid arguments!");
        }

        static void ListEntries(string[] args)
        {
            Console.WriteLine($"{"ID",-5} {"Name", -20} {"Size",-5}");
            foreach (var entry in _archive.Entries)
            {
                double size = (double)entry.Size / (1024 * 1024);
                Console.WriteLine($"{entry.Id,-5} {entry.Name,-20} {$"{Math.Round(size, 3)} mb",-5}");
            }
        }

        static void Extract(string[] args)
        {
            if (args.Length == 2)
            {
                if (!Directory.Exists(Path.ChangeExtension(_path, "")))
                    Directory.CreateDirectory(Path.ChangeExtension(_path, ""));
                var entry = _archive.Entries.SingleOrDefault(x => x.Id == int.Parse(args[1]));
                if (entry != null)
                {
                    var file = _archive.ExctractEntry(entry);
                    string name = entry.Name == "" ? entry.Id.ToString() : entry.Name;
                    FileStream outFile = new FileStream(Path.ChangeExtension(_path, "") + "\\" + name, FileMode.Create);
                    BinaryWriter bw = new BinaryWriter(outFile);
                    bw.Write(file);
                    outFile.Close();
                    Console.WriteLine($"File exctracted! {entry.Name}");
                }
                else Console.WriteLine("Incorrect ID!");
            }
            else Console.WriteLine("Invalid arguments!");
        }

        static void ExtractAll(string[] args)
        {
            if (!Directory.Exists(Path.ChangeExtension(_path, "")))
                Directory.CreateDirectory(Path.ChangeExtension(_path, ""));
            foreach (PakEntry entry in _archive.Entries)
            {
                var file = _archive.ExctractEntry(entry);
                string name = entry.Name == "" ? entry.Id.ToString() : entry.Name;
                FileStream outFile = new FileStream(Path.ChangeExtension(_path, "") + "\\" + name, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(outFile);
                bw.Write(file);
                outFile.Close();
                Console.WriteLine($"File exctracted! {entry.Name}");
            }
        }

        static void Create(string[] args)
        {
            if (args.Length == 2)
                PakArchive.CreateNew(args[1], args[1] + ".PAK");
            else Console.WriteLine("Invalid arguments!");
        }

        static void Exit(string[] args)
        {
            _archive?.Dispose();
        }

        static void InteractiveMode()
        {
            Help();
            string command;
            do
            {
                command = Console.ReadLine();
                var args = command.Split(' ');
                if (_commands.ContainsKey(args[0]))
                {
                    _commands[args[0]].Invoke(args);
                }
                else Console.WriteLine("Unknown command!");

            } while (!command.Equals("exit", StringComparison.OrdinalIgnoreCase));
        }

        static void Help(string[] args = null)
        {
            Console.WriteLine("unPAK for VNs based on CatSystem2 Vita");
            Console.WriteLine("Available commands:");
            Console.WriteLine("open - Opens .PAK archive in the current directory");
            Console.WriteLine("ls - List entries in the archive");
            Console.WriteLine("extract <id> - Extracts a file with specified ID");
            Console.WriteLine("extract-all - Extracts all files from the archive");
            Console.WriteLine("create <folder_name> - Creates a new .PAK archive from specified folder");
            Console.WriteLine("exit - Exit the program");
        }
    }
}
