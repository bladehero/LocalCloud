using LocalCloud.Storage.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Input = LocalCloud.Data.ViewModels.Input;
using Output = LocalCloud.Data.ViewModels.Output;

namespace LocalCloud.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class FilesController : ControllerBase
    {
        private readonly IStorage _storage;

        public FilesController(IStorage storage) =>
            (_storage) = (storage);

        /// <summary>
        /// Gets file by path.
        /// </summary>
        /// <param name="path">File name</param>
        /// <returns>Downloads file if it exists or returns not found result.</returns>
        [HttpGet]
        public IActionResult Get(string path)
        {
            if (!_storage.FileExists(path))
            {
                return NotFound(path);
            }

            var fileInfo = _storage.GetFile(path);
            new FileExtensionContentTypeProvider().TryGetContentType(fileInfo.Name, out string contentType);
            return File(fileInfo.OpenRead(), contentType, fileInfo.Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<Output.Files.PostVM> Post([FromForm] Input.Files.PostVM input)
        {
            var output = new Output.Files.PostVM();

            var path = IStorage.Combine(input.Path ?? string.Empty, input.File.FileName);
            try
            {
                await _storage.CreateFileAsync(path, input.File.OpenReadStream());
                output.Data = path;
                output.Message = "File was successfully uploaded!";
            }
            catch (ArgumentNullException)
            {
                output.Message = "File cannot be empty!";
            }
            catch (Exception)
            {
                output.SetUnexpectedError();
            }
            return output;
        }


    }
}
