using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace LocalCloud.Telegram.API.Services
{
    public interface IUpdateService
    {
        Task EchoAsync(Update update);
    }
}