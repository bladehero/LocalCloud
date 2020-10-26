using LocalCloud.Data.Models;
using LocalCloud.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Input = LocalCloud.Data.ViewModels.Input;
using Output = LocalCloud.Data.ViewModels.Output;

namespace LocalCloud.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly IUserService _userService;
        public UsersController(IJwtService jwtService, IUserService userService) =>
            (_jwtService, _userService) = (jwtService, userService);


        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate(Input.AuthenticateVM model)
        {
            var result = new Output.AuthenticateVM
            {
                Login = model.Login
            };

            var user = (User)null;
            try
            {
                user = _userService.Authenticate(model.Login, model.Password);
            }
            catch (ArgumentException ex)
            {
                // TODO: Add Localization
                result.Message = ex.Message;
            }

            if (user == null)
            {
                return BadRequest(result);
            }

            var token = _jwtService.CreateToken(user);
            result.Author = user.Author;
            result.Token = token;

            // TODO: Add Localization
            result.Message = "Authenticated";
            return Ok(result);
        }
    }
}
