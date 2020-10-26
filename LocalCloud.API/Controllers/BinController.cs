using LocalCloud.Storage.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace LocalCloud.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class BinController : ControllerBase
    {
        private readonly IStorage _storage;

        public BinController(IStorage storage) =>
            (_storage) = (storage);


    }
}
