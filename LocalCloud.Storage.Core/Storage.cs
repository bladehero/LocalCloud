using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace LocalCloud.Storage.Core
{
    public class Storage : IStorage
    {
        public const char ExtensionSeparator = '.';
        public const string ZipExtension = "zip";
        public const string ExtensionGuidMatchRegex = @"[.][{(]?[0-9a-fA-F]{8}[-]?(?:[0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[)}]?$";
        private DateRanges dateRange;

        public string Root { get; set; }
        public DateRanges DateRange
        {
            get => dateRange;
            set
            {
                dateRange = value;
                ReorganizeAutoSystem().Wait();
            }
        }

        public string Meta { get; set; } = ".meta";
        public string BinExtension { get; set; } = ".bin";
        public string AutoExtension { get; set; } = ".auto";
        public string PathToBin => Path.Combine(Root, BinExtension);
        public string PathToAuto => Path.Combine(Root, AutoExtension);
        public EntryInfo Bin => new EntryInfo(PathToBin);
        public EntryInfo Auto => new EntryInfo(PathToAuto);

        public Storage(string root)
        {
            Root = root;

            Bin.Create();
            Auto.Create();

            Bin.Hide();
            Auto.Hide();

            DateRange = DateRanges.Month;
        }

        #region Auto
        public IEnumerable<EntryInfo> GetAutoSystemEntries(DateTime? dateTime = null)
        {
            var entry = new EntryInfo(PathToAuto, DateRange.GetPath(dateTime));
            return GetSystemEntries(entry.RootPath);
        }

        public IEnumerable<string> GetAutoSystemNames(DateTime? dateTime = null)
        {
            var entry = new EntryInfo(PathToAuto, DateRange.GetPath(dateTime));
            return GetSystemNames(entry.RootPath);
        }

        public async Task<EntryInfo> CreateAutoFileAsync(string name, Stream stream, FileMode fileMode = FileMode.OpenOrCreate, DateTime? dateTime = null)
        {
            var path = DateRange.GetPath(dateTime);

            var directory = new EntryInfo(PathToAuto, path);
            directory.Create();

            var file = new EntryInfo(PathToAuto, path, name);
            using var fileStream = file.Create();
            await stream.CopyToAsync(fileStream);

            return file;
        }

        public void MoveAutoFile(string name, DateTime source, DateTime destination, bool overwrite = true)
        {
            var sourceEntry = new EntryInfo(PathToAuto, DateRange.GetPath(source), name);
            var destinationEntry = new EntryInfo(PathToAuto, DateRange.GetPath(destination), name);

            File.Move(sourceEntry.RootPath, destinationEntry.RootPath, overwrite);
        }

        public void DeleteAuto(string name, DateTime? dateTime = null)
        {
            var entry = new EntryInfo(PathToAuto, DateRange.GetPath(dateTime), name);
            entry.Delete();
        }
        public bool EntryAutoExists(string name, DateTime? dateTime = null)
        {
            var entry = new EntryInfo(PathToAuto, DateRange.GetPath(dateTime), name);
            return entry.Exists;
        }

        public async Task ReorganizeAutoSystem()
        {
            _removeEmptyDirectories(PathToAuto);

            var files = _getOnlyFilePaths(PathToAuto).ToList();
            var file = files.FirstOrDefault();
            if (file == null)
            {
                return;
            }

            var autoDirectory = Path.GetDirectoryName(file).Replace(PathToAuto, string.Empty);
            var previousDateRange = Extensions.CastToDateRange(autoDirectory);
            if (previousDateRange == DateRange)
            {
                return;
            }

            string unifyName(string path, int counter = 0)
            {
                if (EntryAutoExists(path))
                {
                    var directory = Path.GetDirectoryName(path);
                    var file = Path.GetFileNameWithoutExtension(path);
                    var index = file.LastIndexOf(" (");
                    file = index < 0 ? file : file.Substring(0, index);
                    var extension = Path.GetExtension(path);
                    path = Path.Combine(directory, $"{file} ({++counter}){extension}");
                    path = unifyName(path, counter);
                }
                return path;
            };

            var fileDatas = files.Select(x => new { FileStream = new FileStream(x, FileMode.Open), new FileInfo(x).CreationTime }).ToList();
            foreach (var data in fileDatas)
            {
                var name = Path.GetFileName(data.FileStream.Name);
                var path = DateRange.GetPath(data.CreationTime);
                path = Path.Combine(PathToAuto, path);
                Directory.CreateDirectory(path);
                path = Path.Combine(path, name);
                path = unifyName(path);
                await CreateFileAsync(path, data.FileStream, FileMode.CreateNew);
                await data.FileStream.DisposeAsync();
            }
            files.ForEach(x => new EntryInfo(Root, x).Delete());
            _removeEmptyDirectories(PathToAuto);
        }
        #endregion

        public EntryInfo GetFile(string path, bool withHidden = false)
        {
            var entry = new EntryInfo(Root, path);
            return entry.FileExists && (withHidden || entry.IsHidden)
                ? entry
                : throw new FileNotFoundException("File wasn't found!", path);
        }

        public IEnumerable<EntryInfo> GetFiles(string path, bool withHidden = false)
        {
            var entry = new EntryInfo(Root, path);
            var files = entry.GetEntries().Where(x => x.IsFile && (withHidden || !x.IsHidden));
            return files;
        }

        public EntryInfo GetDirectory(string path, bool withHidden = false)
        {
            var entry = new EntryInfo(Root, path);
            return entry.DirectoryExists && (withHidden || entry.IsHidden)
                ? entry
                : throw new DirectoryNotFoundException($"Directory with a path: `{path}` wasn't found!");
        }

        public IEnumerable<EntryInfo> GetDirectories(string path, bool withHidden = false)
        {
            var entry = new EntryInfo(Root, path);
            var directories = entry.GetEntries().Where(x => x.IsDirectory && (withHidden || !x.IsHidden));
            return directories;
        }

        public EntryInfo GetSystemEntry(string path, bool withHidden = false)
        {
            var entry = new EntryInfo(Root, path);
            return withHidden || !entry.IsHidden
                ? entry
                : null;
        }

        public IEnumerable<EntryInfo> GetSystemEntries(string path = null, bool withHidden = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = string.Empty;
            }

            var directory = new EntryInfo(Root, path);
            if (!directory.DirectoryExists)
            {
                yield break;
            }

            var entries = Directory.GetFileSystemEntries(directory.RootPath)
                                   .Select(x => new EntryInfo(x)).OrderBy(x => !x.IsDirectory);

            foreach (var entry in entries)
            {
                if (withHidden || !entry.IsHidden)
                {
                    yield return entry;
                }
            }
        }

        public IEnumerable<string> GetSystemNames(string path = null, bool withHidden = false)
        {
            foreach (var item in GetSystemEntries(path, withHidden))
            {
                yield return item.FullName.Replace(Root, string.Empty).Trim('\\');
            }
        }

        public EntryInfo CreateDirectory(string path)
        {
            var entry = new EntryInfo(Root, path);
            entry.Create();
            return entry;
        }

        public async Task<FileInfo> CreateFileAsync(string path, Stream stream, FileMode mode = FileMode.OpenOrCreate)
        {
            var entry = new EntryInfo(Root, path);
            using var entryStream = entry.Create(mode);
            await entryStream.CopyToAsync(stream);
            return new FileInfo(path);
        }

        public void MoveFile(string source, string destination, bool overwrite = true)
        {
            var sourceEntry = new EntryInfo(Root, source);
            var destinationEntry = new EntryInfo(Root, destination);
            File.Move(sourceEntry.RootPath, destinationEntry.RootPath, overwrite);
        }

        public void MoveDirectory(string source, string destination)
        {
            var sourceEntry = new EntryInfo(Root, source);
            var destinationEntry = new EntryInfo(Root, destination);
            Directory.Move(sourceEntry.RootPath, destinationEntry.RootPath);
        }

        public ZipArchive Archive(string source, string destination)
        {
            var destinationEntry = new EntryInfo(destination);
            if (destinationEntry.FileExists)
            {
                destinationEntry.Delete();
            }

            var archive = ZipFile.Open(destination, ZipArchiveMode.Update);
            var sourceEntry = new EntryInfo(source);
            var entries = sourceEntry.GetEntries();

            var sourceDirectory = Path.GetDirectoryName(sourceEntry.RootPath);
            foreach (var entry in entries)
            {
                var archiveEntry = entry.RootPath.Replace(sourceDirectory, "").TrimStart('\\');
                if (entry.IsDirectory)
                {
                    if (!Directory.EnumerateFileSystemEntries(entry.RootPath).Any())
                    {
                        archive.CreateEntry($"{archiveEntry}/");
                    }
                }
                else
                {
                    archive.CreateEntryFromFile(entry.RootPath, archiveEntry);
                }
            }
            return archive;
        }

        public void Extract(string source, string destination)
        {
            var sourceEntry = new EntryInfo(Root, source);
            var destinationEntry = new EntryInfo(Root, destination);
            ZipFile.ExtractToDirectory(sourceEntry.RootPath, destinationEntry.RootPath);
        }

        public IEnumerable<string> GetEntriesFromBin() => _getEntryPaths(PathToBin).Select(x => Path.GetFileNameWithoutExtension(x));

        public string MoveToBin(string path)
        {
            var entry = new EntryInfo(Root, path);
            if (Bin.Equals(entry))
            {
                throw new ArgumentException($"The system folder `{PathToBin}` cannot be deleted manually!", nameof(path));
            }

            if (!entry.DirectoryExists)
            {
                throw new DirectoryNotFoundException("Directory wasn't found!");
            }
            else if (!entry.FileExists)
            {
                throw new FileNotFoundException("File wasn't found!", path);
            }

            var directoryInfo = new DirectoryInfo(path);
            var name = $"{directoryInfo.Name}.{Guid.NewGuid()}";
            var wrapper = Path.Combine(PathToBin, name);

            var archive = Archive(path, wrapper);

            var metaData = new Metadata(directoryInfo, Path.GetDirectoryName(path));
            var metaDataString = JsonConvert.SerializeObject(metaData);
            var sourceDirectory = Path.GetDirectoryName(entry.RootPath);
            var metaPath = Path.Combine(sourceDirectory, Meta);
            File.WriteAllText(metaPath, metaDataString);
            archive.CreateEntryFromFile(metaPath, Meta);
            File.Delete(metaPath);

            entry.Delete();
            archive.Dispose();
            return name;
        }
        public EntryInfo RestoreEntryFromBin(string name)
        {
            var entry = new EntryInfo(PathToBin, name);
            if (!entry.Exists)
            {
                throw new FileNotFoundException("File wasn't found!", entry.FullName);
            }

            var restored = (EntryInfo)null;
            using (var archive = ZipFile.Open(entry.RootPath, ZipArchiveMode.Update))
            {
                var meta = archive.GetEntry(Meta);
                using (var metaStream = meta.Open())
                {
                    using var metaReader = new StreamReader(metaStream);
                    var metaData = metaReader.ReadToEnd();

                    var metaObject = JsonConvert.DeserializeObject<Metadata>(metaData);
                    restored = new EntryInfo(Root, metaObject.Path);
                }

                meta.Delete();
                archive.ExtractToDirectory(restored.RootPath);
            }
            File.Delete(entry.RootPath);

            return restored;
        }

        public void DeleteEntryFromBin(string name) => new EntryInfo(PathToBin, name).Delete();

        public void EraseBin()
        {
            var entries = Directory.GetFileSystemEntries(PathToBin);
            foreach (var entry in entries)
            {
                DeleteEntryFromBin(entry);
            }
        }


        #region Helpers
        private IEnumerable<string> _getOnlyFilePaths(string path)
        {
            var entry = new EntryInfo(Root, path);
            var queue = new Queue<string>();
            queue.Enqueue(entry.RootPath);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                foreach (string sub in Directory.GetDirectories(path))
                {
                    queue.Enqueue(sub);
                }

                var files = Directory.GetFiles(path);
                if (files?.Length > 0)
                {
                    foreach (var file in files)
                    {
                        yield return file;
                    }
                }
            }
        }
        private IEnumerable<string> _getOnlyDirectoryPaths(string path)
        {
            var entry = new EntryInfo(Root, path);
            var queue = new Queue<string>();
            queue.Enqueue(entry.RootPath);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                foreach (string sub in Directory.GetDirectories(path))
                {
                    queue.Enqueue(sub);
                    yield return sub;
                }
            }
        }
        private IEnumerable<string> _getEntryPaths(string path)
        {
            var entry = new EntryInfo(Root, path);
            var queue = new Queue<string>();
            queue.Enqueue(entry.RootPath);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                foreach (string sub in Directory.GetDirectories(path))
                {
                    queue.Enqueue(sub);
                    yield return sub;
                }

                var files = Directory.GetFiles(path);
                if (files?.Length > 0)
                {
                    foreach (var file in files)
                    {
                        yield return file;
                    }
                }
            }
        }
        private void _removeEmptyDirectories(string path)
        {
            var entry = new EntryInfo(Root, path);
            foreach (var directory in Directory.GetDirectories(entry.RootPath))
            {
                _removeEmptyDirectories(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
        #endregion
    }
}
