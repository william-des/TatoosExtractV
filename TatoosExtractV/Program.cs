using CodeWalker;
using CodeWalker.GameFiles;
using CodeWalker.Properties;
using CodeWalker.Utils;
using Newtonsoft.Json;
using Pfim;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace TatoosExtractV
{
    internal class Program
    {
        private static Dictionary<uint, TatooInfos> AllTattos = new Dictionary<uint, TatooInfos>();
        private static GameFileCache GameFilesCache;
        private static string OutputPath = Directory.GetCurrentDirectory();
        private static bool DisableTextureExtract = false;

        static void Main(string[] args)
        {
            ReadCommandLineArgs(args);
            InitGameFilesCache();
            ExtractAllTatoos();
            WriteResultFile();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void ReadCommandLineArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--output="))
                {
                    OutputPath = arg.Substring("--output=".Length);
                }
                else if (arg.Equals("--disable-texture-extract"))
                {
                    DisableTextureExtract = true;
                }
            }
        }

        private static void InitGameFilesCache()
        {
            GameFilesCache = GameFileCacheFactory.Create();
            GTA5Keys.LoadFromPath(GTAFolder.CurrentGTAFolder, Settings.Default.Key);
            GameFilesCache.EnableDlc = true;
            GameFilesCache.LoadVehicles = false;
            GameFilesCache.LoadArchetypes = false;
            GameFilesCache.LoadAudio = false;
            GameFilesCache.Init(Console.WriteLine, Console.WriteLine);
        }

        private static void WriteResultFile()
        {
            string json = JsonConvert.SerializeObject(AllTattos.Values);
            File.WriteAllText("tatoos.json", json);
            Console.WriteLine(AllTattos.Values.Count + "tatoos extracted");
        }

        private static void ExtractAllTatoos()
        {
            foreach (var file in GameFilesCache.AllRpfs)
            {
                if (file.AllEntries == null) continue;
                foreach (RpfEntry entry in file.AllEntries)
                {
                    if (entry.NameLower.EndsWith("_overlays.xml") && entry.NameLower != "singleplayer_overlays.xml")
                    {
                        var fentry = entry as RpfFileEntry;
                        if (fentry != null) ExtractTatoosFromRpf(entry, fentry);
                    }
                }
            }
        }

        private static void ExtractTatoosFromRpf(RpfEntry rpfEntry, RpfFileEntry fileEntry)
        {
            Console.WriteLine("Extracting tatoos from " + fileEntry.Path);

            var xmlBytes = rpfEntry.File.ExtractFile(fileEntry);
            using (var stream = new MemoryStream(xmlBytes))
            {
                var doc = XDocument.Load(stream);

                var collection = doc.Root.Element("nameHash").Value;


                var items = doc.Descendants("Item");
                foreach (var item in items)
                {
                    var type = item.Element("type")?.Value;
                    if (type != "TYPE_TATTOO") continue;

                    var gender = item.Element("gender")?.Value;
                    var tatooName = item.Element("nameHash")?.Value;
                    var txdName = item.Element("txdHash")?.Value;
                    var zone = item.Element("zone")?.Value;
                    var hash = JenkHash.GenHash(tatooName);


                    var tatooInfos = new TatooInfos
                    {
                        Collection = collection,
                        Gender = gender,
                        OverlayHash = hash,
                        OverlayName = tatooName,
                        ZoneName = zone,
                    };

                    if (!AllTattos.ContainsKey(hash))
                    {
                        AllTattos.Add(hash, tatooInfos);
                    }
                    else
                    {
                        AllTattos[hash] = tatooInfos;
                    }

                    if (!DisableTextureExtract) ExtractTatooTexture(tatooName, txdName);
                }
            }
        }

        private static void ExtractTatooTexture(string tatooName, string txdName)
        {
            var ytdEntry = GameFilesCache.GetYtdEntry(JenkHash.GenHash(txdName));
            if (ytdEntry == null)
            {
                Console.WriteLine("Ytd entry " + txdName + " not found");
                return;
            }

            var ytd = new YtdFile();
            if (!GameFilesCache.RpfMan.LoadFile(ytd, ytdEntry))
            {
                Console.WriteLine("Failed to load ytd " + txdName);
                return;
            }

            var texture = ytd.TextureDict.Textures[0];
            var ddsByteArray = DDSIO.GetDDSFile(texture);

            using (MemoryStream ms = new MemoryStream(ddsByteArray))
            {
                using (var image = Pfimage.FromStream(ms))
                {
                    PixelFormat format;

                    // Convert from Pfim's backend agnostic image format into GDI+'s image format
                    switch (image.Format)
                    {
                        case Pfim.ImageFormat.Rgba32:
                            format = PixelFormat.Format32bppArgb;
                            break;
                        case Pfim.ImageFormat.Rgb8:
                            format = PixelFormat.Format8bppIndexed;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    // Pin pfim's data array so that it doesn't get reaped by GC, unnecessary
                    var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
                    try
                    {
                        var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                        var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, data);
                        bitmap.Save(tatooName + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
        }
    }

    internal class TatooInfos
    {
        public string ZoneName { get; set; }
        public string OverlayName { get; set; }
        public uint OverlayHash { get; set; }
        public string Collection { get; set; }
        public string Gender { get; set; }
    }
}
