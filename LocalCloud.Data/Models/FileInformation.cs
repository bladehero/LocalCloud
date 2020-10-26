using System;
using System.IO;
using System.Runtime.Serialization;

namespace LocalCloud.Data.Models
{
    public class FileInformation
    {
        private string _path;
        private FileSystemInfo _systemInfo;

        public FileInformation(string path)
        {
            _path = path;
            _systemInfo = new FileInfo(path);
        }

        public DateTime LastWriteTime => _systemInfo.LastWriteTime;
        public DateTime LastAccessTimeUtc => _systemInfo.LastAccessTimeUtc;
        public DateTime LastAccessTime => _systemInfo.LastAccessTime;
        public string Extension => _systemInfo.Extension;
        public DateTime CreationTime => _systemInfo.CreationTime;
        public DateTime LastWriteTimeUtc => _systemInfo.LastWriteTimeUtc;
        public FileAttributes Attributes => _systemInfo.Attributes;
        public DateTime CreationTimeUtc => _systemInfo.CreationTimeUtc;

        [IgnoreDataMember]
        public Stream FileStream => new FileStream(_path, FileMode.Open);
        [IgnoreDataMember]
        public string Name => _systemInfo.Name;

    }
}
