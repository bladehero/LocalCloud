using System;
using System.IO;

namespace LocalCloud.Storage.Core
{
    public sealed class EntryInfo : FileSystemInfo
    {
        private string _root;
        public EntryInfo(string root, string path)
        {
            _root = root;
            FullPath = Path.Combine(root, path);
            OriginalPath = path;

            var systemInfo = IsDirectory ? (FileSystemInfo)new DirectoryInfo(FullPath) : new FileInfo(FullPath);

            Attributes = systemInfo.Attributes;
            CreationTime = systemInfo.CreationTime;
            CreationTimeUtc = systemInfo.CreationTimeUtc;
            LastAccessTime = systemInfo.LastAccessTime;
            LastAccessTimeUtc = systemInfo.LastAccessTimeUtc;
            LastWriteTime = systemInfo.LastWriteTime;
            LastWriteTimeUtc = systemInfo.LastWriteTimeUtc;
        }

        public EntryInfo(string root, params string[] path) : this(root, Path.Combine(path))
        {
        }

        internal string RootPath => FullPath;
        public override string FullName => _withoutRoot;
        public override bool Exists => FileExists() || DirectoryExists();
        public override string Name => Path.GetFileName(FullName);
        public Stream Create()
        {
            if (IsDirectory)
            {
                new DirectoryInfo(FullPath).Create();
                return null;
            }
            else
            {
                return new FileInfo(FullPath).Create();
            }
        }
        public override void Delete()
        {
            if (IsDirectory)
            {
                _deleteDirectory(FullPath);
            }
            else
            {
                new FileInfo(FullPath).Delete();
            }
        }


        public bool FileExists() => File.Exists(FullPath);
        public bool DirectoryExists() => Directory.Exists(FullPath);

        public bool IsDirectory => File.GetAttributes(FullPath).HasFlag(FileAttributes.Directory);
        public bool IsFile => !IsDirectory;
        public bool IsHidden() => HasFlag(FileAttributes.Hidden);
        private bool HasFlag(FileAttributes fileAttributes) => Attributes.HasFlag(fileAttributes);

        public void Hide() => Attributes |= FileAttributes.Hidden;
        public void UnHide() => Attributes &= ~FileAttributes.Hidden;

        private string _withoutRoot => FullPath.IndexOf(_root) == 0 ? FullPath.Substring(_root.Length) : throw new ArgumentException("Path should be started with a root!", nameof(FullPath));
        private void _deleteDirectory(string path)
        {
            var files = Directory.GetFiles(FullPath);
            var directories = Directory.GetDirectories(FullPath);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var directory in directories)
            {
                _deleteDirectory(directory);
            }

            Directory.Delete(path);
        }
    }
}
