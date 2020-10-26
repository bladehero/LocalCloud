using System;
using System.IO;

namespace LocalCloud.Storage.Core
{
    public class Metadata
    {
        public Metadata()
        {

        }
        public Metadata(FileSystemInfo systemInfo, string path)
        {
            FullName = systemInfo.FullName;
            Extension = systemInfo.FullName;
            Name = systemInfo.FullName;
            LastWriteTime = systemInfo.LastWriteTime;
            LastAccessTimeUtc = systemInfo.LastAccessTimeUtc;
            LastAccessTime = systemInfo.LastAccessTime;
            CreationTime = systemInfo.CreationTime;
            LastWriteTimeUtc = systemInfo.LastWriteTimeUtc;
            CreationTimeUtc = systemInfo.CreationTimeUtc;
            Attributes = systemInfo.Attributes;
            Path = path;
        }

        public string Path { get; set; }
        public string FullName { get; internal set; }
        public string Extension { get; internal set; }
        public string Name { get; internal set; }
        public DateTime LastWriteTime { get; internal set; }
        public DateTime LastAccessTimeUtc { get; internal set; }
        public DateTime LastAccessTime { get; internal set; }
        public DateTime CreationTime { get; internal set; }
        public DateTime LastWriteTimeUtc { get; internal set; }
        public DateTime CreationTimeUtc { get; internal set; }
        public FileAttributes Attributes { get; internal set; }
    }
}
