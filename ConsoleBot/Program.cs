using Telegram.Bot;

namespace ConsoleBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var botClient = new TelegramBotClient("7061907936:AAF3v-jlo8SFGE1qW4Jb8KDOH7P_xg8GDNc");

            IUpdateHandler updateHandler = new MessageHandler();
            IPollingErrorHandler errorHandler = new ErrorHandler();

            botClient.StartReceiving(updateHandler.HandleUpdateAsync, errorHandler.HandlePollingErrorAsync);
            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
        }
    }
}
