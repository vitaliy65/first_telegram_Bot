using Telegram.Bot;

namespace ConsoleBot
{
    public interface IPollingErrorHandler
    {
        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken);
    }
}
