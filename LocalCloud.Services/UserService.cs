using LocalCloud.Data.Models;
using LocalCloud.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalCloud.Services
{
    public class UserService : IUserService
    {
        private readonly IEnumerable<User> _users;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IOptions<Authentication> authentication, IHttpContextAccessor httpContextAccessor)
        {
            _users = authentication.Value.Users;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAnonymous => Current == null;
        public User Current => GetByLogin(_httpContextAccessor.HttpContext.User.Identity.Name);

        public User Authenticate(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException($"Parameter `{nameof(login)}` cannot be empty or null!");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException($"Parameter `{nameof(password)}` cannot be empty or null!");
            }

            var user = _users.FirstOrDefault(x => x.Login == login && x.Password == password);

            return user;
        }

        public IEnumerable<User> GetAll() => _users;

        public User GetByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException($"Parameter `{nameof(login)}` cannot be empty or null!");
            }

            var user = _users.FirstOrDefault(x => x.Login == login);

            return user;
        }
    }
}
