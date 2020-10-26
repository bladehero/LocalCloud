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
        public string Bin { get; set; } = ".bin";
        public string Auto { get; set; } = ".auto";
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

        public string PathToBin => Path.Combine(Root, Bin);
        public string PathToAuto => Path.Combine(Root, Auto);

        public Storage(string root)
        {
            Root = root;

            var bin = Path.Combine(root, Bin);
            var auto = Path.Combine(root, Auto);

            var binInfo = Directory.CreateDirectory(bin);
            var autoInfo = Directory.CreateDirectory(auto);

            _hideEntry(binInfo);
            _hideEntry(autoInfo);

            DateRange = DateRanges.Month;
        }

        #region Auto
        public IEnumerable<EntryInfo> GetAutoSystemEntries(DateTime? dateTime = null)
        {
            var entry = new EntryInfo(Root, Auto, DateRange.GetPath(dateTime));
            return GetSystemEntries(entry.RootPath);
        }

        public IEnumerable<string> GetAutoSystemNames(DateTime? dateTime = null)
        {
            var entry = new EntryInfo(Root, Auto, DateRange.GetPath(dateTime));
            return GetSystemNames(entry.RootPath);
        }

        public async Task<EntryInfo> CreateAutoFileAsync(string name, Stream stream, FileMode fileMode = FileMode.OpenOrCreate, DateTime? dateTime = null)
        {
            var path = DateRange.GetPath(dateTime);

            var directory = new EntryInfo(Root, Auto, path);
            directory.Create();

            var file = new EntryInfo(Root, Auto, path, name);
            using var fileStream = file.Create();
            await stream.CopyToAsync(fileStream);

            return file;
        }

        public void MoveAutoFile(string name, DateTime source, DateTime destination, bool overwrite = true)
        {
            var sourceEntry = new EntryInfo(Auto, DateRange.GetPath(source), name);
            var destinationEntry = new EntryInfo(Auto, DateRange.GetPath(destination), name);

            File.Move(sourceEntry.RootPath, destinationEntry.RootPath, overwrite);
        }

        public void DeleteAuto(string name, DateTime? dateTime = null)
        {
            var entry = new EntryInfo(Auto, DateRange.GetPath(dateTime), name);
            entry.Delete();
        }
        public bool EntryAutoExists(string name, DateTime? dateTime = null)
        {
            var entry = new EntryInfo(Auto, DateRange.GetPath(dateTime), name);
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
            files.ForEach(x => _deleteEntry(x));
            _removeEmptyDirectories(PathToAuto);
        }
        #endregion

        public FileInfo GetFile(string path, bool withHidden = false)
        {
            path = _fullPath(path);
            return File.Exists(path) && (withHidden || !IsHidden(path))
                ? new FileInfo(path)
                : throw new FileNotFoundException("File wasn't found!", path);
        }

        public IEnumerable<FileInfo> GetFiles(string path, bool withHidden = false)
        {
            path = _fullPath(path);
            var files = Directory.GetFiles(path).Where(x => withHidden || IsHidden(x));
            foreach (var file in files)
            {
                yield return new FileInfo(file);
            }
        }

        public DirectoryInfo GetDirectory(string path, bool withHidden = false)
        {
            path = _fullPath(path);
            return Directory.Exists(path) && (withHidden || !IsHidden(path))
                ? new DirectoryInfo(path)
                : throw new DirectoryNotFoundException($"Directory with a path: `{path}` wasn't found!");
        }

        public IEnumerable<DirectoryInfo> GetDirectories(string path, bool withHidden = false)
        {
            path = _fullPath(path);
            var directories = Directory.GetDirectories(path).Where(x => withHidden || IsHidden(x));
            foreach (var directory in directories)
            {
                yield return new DirectoryInfo(directory);
            }
        }

        public FileSystemInfo GetSystemEntry(string path, bool withHidden = false)
        {
            path = _fullPath(path);
            return IsDirectory(path) && (withHidden || !IsHidden(path))
                ? new DirectoryInfo(path)
                : (FileSystemInfo)new FileInfo(path);
        }

        public IEnumerable<FileSystemInfo> GetSystemEntries(string path = null, bool withHidden = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Root;
            }
            else
            {
                path = _fullPath(path);
            }

            if (!Directory.Exists(path))
            {
                yield break;
            }
            var entries = Directory.GetFileSystemEntries(path)
                                   .Where(x => !x.StartsWith(PathToBin))
                                   .OrderByDescending(x => IsDirectory(x));

            foreach (var entry in entries)
            {
                if (withHidden || !IsHidden(entry))
                {
                    if (IsDirectory(entry))
                    {
                        yield return new DirectoryInfo(entry);
                    }
                    else
                    {
                        yield return new FileInfo(entry);
                    }
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

        public DirectoryInfo CreateDirectory(string path)
        {
            path = _fullPath(path);
            Directory.CreateDirectory(path);
            return new DirectoryInfo(path);
        }

        public async Task<FileInfo> CreateFileAsync(string path, Stream stream, FileMode fileMode = FileMode.OpenOrCreate)
        {
            path = _fullPath(path);
            using var fileStream = new FileStream(path, fileMode);
            await stream.CopyToAsync(fileStream);
            return new FileInfo(path);
        }

        public void MoveFile(string source, string destination, bool overwrite = true)
        {
            source = _fullPath(source);
            destination = _fullPath(destination);
            File.Move(source, destination, overwrite);
        }

        public void MoveDirectory(string source, string destination)
        {
            source = _fullPath(source);
            destination = _fullPath(destination);
            Directory.Move(source, destination);
        }

        public ZipArchive Archive(string source, string destination)
        {
            destination = _fullPath(destination);
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
            var archive = ZipFile.Open(destination, ZipArchiveMode.Update);
            var entries = IsDirectory(source)
                ? _getEntryPaths(source).ToArray()
                : new[] { _fullPath(source) };

            var sourceDirectory = Path.GetDirectoryName(_fullPath(source));
            foreach (var entry in entries)
            {
                var archiveEntry = entry.Replace(sourceDirectory, "").TrimStart('\\');
                if (IsDirectory(entry))
                {
                    if (!Directory.EnumerateFileSystemEntries(entry).Any())
                    {
                        archive.CreateEntry($"{archiveEntry}/");
                    }
                }
                else
                {
                    archive.CreateEntryFromFile(entry, archiveEntry);
                }
            }
            return archive;
        }

        public void Extract(string source, string destination)
        {
            source = _fullPath(source);
            destination = _fullPath(destination);
            ZipFile.ExtractToDirectory(source, destination);
        }

        public IEnumerable<string> GetEntriesFromBin() => _getEntryPaths(PathToBin).Select(x => Path.GetFileNameWithoutExtension(x));

        public string MoveToBin(string path)
        {
            path = _fullPath(path);
            if (path.Equals(PathToBin, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The system folder `{Bin}` cannot be deleted manually!", nameof(path));
            }

            if (IsDirectory(path))
            {
                if (!Directory.Exists(path))
                {
                    throw new DirectoryNotFoundException("Directory wasn't found!");
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("File wasn't found!", path);
                }
            }

            var directoryInfo = new DirectoryInfo(path);
            var name = $"{directoryInfo.Name}.{Guid.NewGuid()}";
            var wrapper = Path.Combine(PathToBin, name);

            var archive = Archive(path, wrapper);

            var metaData = new Metadata(directoryInfo, Path.GetDirectoryName(path));
            var metaDataString = JsonConvert.SerializeObject(metaData);
            var sourceDirectory = Path.GetDirectoryName(_fullPath(path));
            var metaPath = Path.Combine(sourceDirectory, Meta);
            File.WriteAllText(metaPath, metaDataString);
            archive.CreateEntryFromFile(metaPath, Meta);
            File.Delete(metaPath);

            _deleteEntry(path);
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

        //private string _fullPath(string part)
        //{
        //    part ??= string.Empty;
        //    return part.IndexOf(Root) == 0 ? part : Path.Combine(Root, part);
        //}
        //private void _hideEntry(FileSystemInfo systemInfo) => systemInfo.Attributes |= FileAttributes.Hidden;
        //private void _deleteEntry(string path)
        //{
        //    path = _fullPath(path);
        //    if (IsDirectory(path))
        //    {
        //        _deleteDirectory(path);
        //    }
        //    else
        //    {
        //        GetSystemEntry(path).Delete();
        //    }
        //}
        //private void _deleteDirectory(string path)
        //{
        //    path = _fullPath(path);
        //    var files = Directory.GetFiles(path);
        //    var directories = Directory.GetDirectories(path);

        //    foreach (var file in files)
        //    {
        //        File.SetAttributes(file, FileAttributes.Normal);
        //        File.Delete(file);
        //    }

        //    foreach (var directory in directories)
        //    {
        //        _deleteDirectory(directory);
        //    }

        //    Directory.Delete(path);
        //}
        //private bool _entryHasFlag(string path, FileAttributes fileAttributes)
        //{
        //    path = _fullPath(path);
        //    var fileSystemInfo = IsDirectory(path) ? new DirectoryInfo(path) : (FileSystemInfo)new FileInfo(path);
        //    return fileSystemInfo.Attributes.HasFlag(fileAttributes);
        //}
        #endregion
    }
}
