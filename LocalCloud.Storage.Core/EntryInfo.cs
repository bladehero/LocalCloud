using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace LocalCloud.Storage.Core
{
    public sealed class EntryInfo : FileSystemInfo
    {
        private string _root;
        public EntryInfo(string root, string path)
        {
            _root = root;
            if (!string.IsNullOrWhiteSpace(string.Empty))
            {
                path = string.Empty;
            }
            else
            {
                path = _tryWithoutRoot(path).Path;
            }
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

        public override string FullName => WithoutRoot;
        public override bool Exists => FileExists || DirectoryExists;
        public override string Name => Path.GetFileName(FullName);
        public Stream Create(FileMode mode = FileMode.OpenOrCreate)
        {
            if (IsDirectory)
            {
                new DirectoryInfo(FullPath).Create();
                return null;
            }
            else
            {
                return new FileInfo(FullPath).Open(mode);
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

        internal string RootPath => FullPath;
        public bool FileExists => IsFile && File.Exists(FullPath);
        public bool DirectoryExists => IsDirectory && Directory.Exists(FullPath);
        public bool IsDirectory => File.GetAttributes(FullPath).HasFlag(FileAttributes.Directory);
        public bool IsFile => !IsDirectory;
        public bool IsHidden => HasFlag(FileAttributes.Hidden);
        private bool HasFlag(FileAttributes fileAttributes) => Attributes.HasFlag(fileAttributes);

        public void Hide() => Attributes |= FileAttributes.Hidden;
        public void UnHide() => Attributes &= ~FileAttributes.Hidden;
        public IEnumerable<EntryInfo> GetEntries()
        {
            if (DirectoryExists)
            {
                return Directory.GetFiles(RootPath).Select(x => new EntryInfo(_root, _withoutRoot(x)));
            }

            return new[] { this };
        }

        public override bool Equals(object obj)
        {
            return obj is EntryInfo info &&
                   FullPath.Equals(info.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FullPath);
        }

        #region Helpers
        private (bool Success, string Path) _tryWithoutRoot(string path) =>
                    FullPath.IndexOf(_root) == 0
                    ? (true, path.Substring(_root.Length))
                    : (false, path);
        private string _withoutRoot(string path) =>
            FullPath.IndexOf(_root) == 0
            ? path.Substring(_root.Length)
            : throw new ArgumentException("Path should be started with a root!", nameof(path));
        private string WithoutRoot => _withoutRoot(FullPath);
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
        #endregion
    }
}
