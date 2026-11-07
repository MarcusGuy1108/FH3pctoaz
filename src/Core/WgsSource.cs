using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AzimuthSaveImporter.Core
{
    /// <summary>
    /// Reads the saves the Windows (Microsoft Store / Xbox app) version of Forza Horizon 3 keeps in
    ///   %LOCALAPPDATA%\Packages\Microsoft.OpusPG_8wekyb3d8bbwe\SystemAppData\wgs\&lt;user&gt;_&lt;scid&gt;\
    /// Layout (after github.com/Z1ni/XGP-save-extractor, MIT):
    ///   containers.index      list of containers: name, number N, folder GUID
    ///   &lt;GUID&gt;\container.N   list of files: 64-char UTF-16 name, two GUIDs (one is the blob's file name)
    ///   &lt;GUID&gt;\&lt;file GUID&gt;   the file contents, the same bytes the Xbox stores (FH3 is Play Anywhere)
    /// </summary>
    public sealed partial class XboxSource
    {
        public const string WgsIndexFile = "containers.index";

        public static string DefaultPcSaveFolder
        {
            get
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages", "Microsoft.OpusPG_8wekyb3d8bbwe", "SystemAppData", "wgs");
            }
        }

        /// <summary>The wgs user folders (with a containers.index) at or below <paramref name="folder"/>.</summary>
        public static List<string> FindWgsUserFolders(string folder)
        {
            var found = new List<string>();
            var level = new List<string> { folder };
            for (var depth = 0; depth < 5 && level.Count > 0 && found.Count == 0; depth++)
            {
                var next = new List<string>();
                foreach (var dir in level)
                {
                    var name = System.IO.Path.GetFileName(dir);
                    // "t" holds temporary files, "*backup*" folders are copies the Xbox app made.
                    if (depth > 0 && (name == "t" || name.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                    if (File.Exists(System.IO.Path.Combine(dir, WgsIndexFile))) { found.Add(dir); continue; }
                    try { next.AddRange(Directory.GetDirectories(dir)); }
                    catch (Exception e) when (e is UnauthorizedAccessException || e is IOException) { }
                }
                level = next;
            }
            return found;
        }

        void ReadWgs(string userFolder)
        {
            try
            {
                using (var r = new BinaryReader(File.OpenRead(System.IO.Path.Combine(userFolder, WgsIndexFile)), Encoding.Unicode))
                {
                    r.ReadInt32();                       // version
                    var count = ReadCount(r);
                    ReadString(r);                       // package display name
                    ReadString(r);                       // package name
                    r.ReadBytes(8 + 4);                  // creation time, unknown
                    ReadString(r);                       // unknown id
                    r.ReadBytes(8);                      // unknown
                    for (var i = 0; i < count; i++)
                    {
                        var name = ReadString(r);
                        ReadString(r);                   // name again
                        ReadString(r);                   // quoted hex number
                        var number = r.ReadByte();
                        r.ReadInt32();                   // unknown
                        var folder = System.IO.Path.Combine(userFolder, GuidName(r.ReadBytes(16)));
                        r.ReadBytes(8 + 16);             // creation time, unknown
                        AddWgsContainer(name, System.IO.Path.Combine(folder, "container." + number));
                    }
                }
            }
            catch (EndOfStreamException e)
            {
                throw new ImportException("The PC save index " + System.IO.Path.Combine(userFolder, WgsIndexFile) +
                    " ended early. Start Forza Horizon 3 on PC once, let it sync, close it and try again.", e);
            }
        }

        void AddWgsContainer(string name, string containerFile)
        {
            if (!File.Exists(containerFile))
            {
                // The Xbox app lists containers it has not downloaded yet.
                Warnings.Add("Skipped '" + name + "': its files are not on this PC (not synced yet).");
                return;
            }
            var folder = System.IO.Path.GetDirectoryName(containerFile);
            var container = new SourceContainer(name);
            using (var r = new BinaryReader(File.OpenRead(containerFile), Encoding.Unicode))
            {
                r.ReadInt32();                           // version
                var count = ReadCount(r);
                for (var i = 0; i < count; i++)
                {
                    var fileName = new string(r.ReadChars(64)).Split('\0')[0];
                    var first = System.IO.Path.Combine(folder, GuidName(r.ReadBytes(16)));
                    var second = System.IO.Path.Combine(folder, GuidName(r.ReadBytes(16)));
                    var path = first == second || (File.Exists(first) && !File.Exists(second)) ? first
                             : File.Exists(second) && !File.Exists(first) ? second : null;
                    // Never import half a container: a missing profile file would lose progress.
                    if (path == null || !File.Exists(path))
                        throw new ImportException("The PC save '" + name + "' is incomplete (file '" + fileName +
                            "' is missing or ambiguous). Start Forza Horizon 3 on PC once, let it sync, close it and try again.");
                    AddFile(container, fileName, new FileInfo(path).Length, () => File.OpenRead(path));
                }
            }
            Finish(container);
        }

        static int ReadCount(BinaryReader r)
        {
            var count = r.ReadInt32();
            if (count < 0 || count > 100000) throw new ImportException("This is not a PC save index this tool understands.");
            return count;
        }

        static string ReadString(BinaryReader r)
        {
            var length = r.ReadInt32();
            if (length < 0 || length > 4096) throw new ImportException("This is not a PC save index this tool understands.");
            return new string(r.ReadChars(length)).TrimEnd('\0');
        }

        // Same as Python's uuid.UUID(bytes_le=b).hex.upper(): .NET's Guid(byte[]) uses that byte order.
        static string GuidName(byte[] bytes) { return new Guid(bytes).ToString("N").ToUpperInvariant(); }
    }
}
