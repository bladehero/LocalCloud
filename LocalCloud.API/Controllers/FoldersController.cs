using LocalCloud.Storage.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using Input = LocalCloud.Data.ViewModels.Input;
using Output = LocalCloud.Data.ViewModels.Output;

namespace LocalCloud.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly IStorage _storage;

        public FoldersController(IStorage storage) =>
            (_storage) = (storage);

        [HttpGet]
        public Output.Folders.GetVM Get(string path)
        {
            var output = new Output.Folders.GetVM();
            try
            {
                output.Data = _storage.GetSystemNames(path);
                output.Success = true;
                output.Message = "Folders were successfully provided!";
            }
            catch (Exception)
            {
                output.SetUnexpectedError();
            }
            return output;
        }

        [HttpPost]
        public void Post(string path)
        {
            _storage.CreateDirectory(path);
        }
    }
}
