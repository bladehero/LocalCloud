using Telegram.Bot;

namespace LocalCloud.Telegram.API.Services
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }
    }
}