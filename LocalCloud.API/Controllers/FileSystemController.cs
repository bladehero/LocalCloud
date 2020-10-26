using System;
using System.IO;
using System.Net.Mime;
using LocalCloud.Storage.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Input = LocalCloud.Data.ViewModels.Input;
using Output = LocalCloud.Data.ViewModels.Output;

namespace LocalCloud.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class FileSystemController : ControllerBase
    {
        private readonly IStorage _storage;

        public FileSystemController(IStorage storage) =>
            (_storage) = (storage);

        /// <summary>
        /// Get all system entries.
        /// </summary>
        /// <param name="path">Use a path, if no path - uses root</param>
        /// <returns>Collection of pathes.</returns>
        [HttpGet]
        public Output.FileSystem.GetVM Get([FromQuery] Input.FileSystem.GetVM input)
        {
            var output = new Output.FileSystem.GetVM();
            try
            {
                output.Data = _storage.GetSystemNames(input.Path);
                output.Success = true;
                output.Message = "Folders were successfully provided!";
            }
            catch (Exception)
            {
                output.SetUnexpectedError();
            }
            return output;
        }

        /// <summary>
        /// Moves (renames) system entry.
        /// </summary>
        /// <param name="source">Source path for changing</param>
        /// <param name="destination">Destination path which will be after the action</param>
        /// <returns>Result of moving (renaming) entry.</returns>
        [HttpPut]
        public Output.FileSystem.PutVM Put(Input.FileSystem.PutVM input)
        {
            var output = new Output.FileSystem.PutVM();

            var sourceIsDirectory = _storage.IsDirectory(input.Source);
            if (sourceIsDirectory != _storage.IsDirectory(input.Destination))
            {
                output.Message = "Source and destination should be with the same type (folder or file)!";
                output.Success = false;
                return output;
            }

            try
            {
                if (sourceIsDirectory)
                {
                    _storage.MoveDirectory(input.Source, input.Destination);
                }
                else
                {
                    _storage.MoveFile(input.Source, input.Destination);
                }
            }
            catch (DirectoryNotFoundException)
            {
                output.Message = $"Not found source directory with the path: `{input.Source}`";
            }
            catch (FileNotFoundException)
            {
                output.Message = $"Not found source file with the path: `{input.Source}`";
            }
            catch (Exception)
            {
                output.SetUnexpectedError();
            }
            return output;
        }

        /// <summary>
        /// Moves entry to the bin directory.
        /// </summary>
        /// <param name="path">Path for existing entry</param>
        /// <returns>Result if entry is deleted or error and message why if it is not.</returns>
        [HttpDelete]
        public Output.FileSystem.DeleteVM Delete(Input.FileSystem.DeleteVM input)
        {
            var output = new Output.FileSystem.DeleteVM();

            try
            {
                output.Data = _storage.MoveToBin(input.Path);
                output.Message = "Successfully moved to bin!";
                output.Success = true;
            }
            catch (ArgumentException ex)
            {
                output.Message = ex.Message;
            }
            catch (DirectoryNotFoundException ex)
            {
                output.Message = ex.Message;
            }
            catch (FileNotFoundException ex)
            {
                output.Message = ex.Message;
            }
            catch (Exception)
            {
                output.SetUnexpectedError();
            }

            return output;
        }
    }
}
