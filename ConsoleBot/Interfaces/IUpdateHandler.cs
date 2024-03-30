using Telegram.Bot.Types;
using Telegram.Bot;

namespace ConsoleBot
{
    public interface IUpdateHandler
    {
        Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    }
}
