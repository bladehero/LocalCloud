using LocalCloud.Data.Models;
using System.Collections.Generic;

namespace LocalCloud.Interfaces.Services
{
    public interface IUserService
    {
        User Current { get; }
        bool IsAnonymous { get; }
        User Authenticate(string login, string password);
        IEnumerable<User> GetAll();
        User GetByLogin(string login);
    }
}
