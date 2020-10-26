using LocalCloud.Data.Models;

namespace LocalCloud.Interfaces.Services
{
    public interface IJwtService
    {
        string CreateToken(User user);
    }
}
