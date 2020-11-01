using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace LocalCloud.Storage.Core
{
    public interface IStorage
    {
        EntryInfo Auto { get; }
        string AutoExtension { get; set; }
        EntryInfo Bin { get; }
        string BinExtension { get; set; }
        DateRanges DateRange { get; set; }
        string Meta { get; set; }
        string PathToAuto { get; }
        string PathToBin { get; }
        string Root { get; set; }

        ZipArchive Archive(string source, string destination);
        Task<EntryInfo> CreateAutoFileAsync(string name, Stream stream, FileMode fileMode = FileMode.OpenOrCreate, DateTime? dateTime = null);
        EntryInfo CreateDirectory(string path);
        Task<FileInfo> CreateFileAsync(string path, Stream stream, FileMode mode = FileMode.OpenOrCreate);
        void DeleteAuto(string name, DateTime? dateTime = null);
        void DeleteEntryFromBin(string name);
        bool EntryAutoExists(string name, DateTime? dateTime = null);
        void EraseBin();
        void Extract(string source, string destination);
        IEnumerable<EntryInfo> GetAutoSystemEntries(DateTime? dateTime = null);
        IEnumerable<string> GetAutoSystemNames(DateTime? dateTime = null);
        IEnumerable<EntryInfo> GetDirectories(string path, bool withHidden = false);
        EntryInfo GetDirectory(string path, bool withHidden = false);
        IEnumerable<string> GetEntriesFromBin();
        EntryInfo GetFile(string path, bool withHidden = false);
        IEnumerable<EntryInfo> GetFiles(string path, bool withHidden = false);
        IEnumerable<EntryInfo> GetSystemEntries(string path = null, bool withHidden = false);
        EntryInfo GetSystemEntry(string path, bool withHidden = false);
        IEnumerable<string> GetSystemNames(string path = null, bool withHidden = false);
        void MoveAutoFile(string name, DateTime source, DateTime destination, bool overwrite = true);
        void MoveDirectory(string source, string destination);
        void MoveFile(string source, string destination, bool overwrite = true);
        string MoveToBin(string path);
        Task ReorganizeAutoSystem();
        EntryInfo RestoreEntryFromBin(string name);
    }
}