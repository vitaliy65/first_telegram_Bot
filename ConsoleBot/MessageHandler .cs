using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Курсовая_работа.Controller;
using Курсовая_работа.model;

namespace ConsoleBot
{
    public class MessageHandler : IUpdateHandler
    {
        private MessageProcessor _messageProcessor;
        private BasketManager _basketManager;
        Dictionary<long, List<Product>> userBaskets = new Dictionary<long, List<Product>>();
        ProductController productController = new ProductController();
        RestaurantController restaurantController = new RestaurantController();

        public MessageHandler(ITelegramBotClient botClient)
        {
            _basketManager = new BasketManager(botClient, productController, userBaskets);
            _messageProcessor = new MessageProcessor(botClient, productController, restaurantController, userBaskets);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleUpdateQueryAsync(botClient, update.CallbackQuery, cancellationToken);
            }
            else
            {
                var message = update.Message;

                if (!userBaskets.ContainsKey(update.Message.Chat.Id))
                {
                    // Создаем новую пустую корзину для данного пользователя
                    userBaskets[update.Message.Chat.Id] = new List<Product>();
                }

                if (message != null)
                {
                    await _messageProcessor.ProcessMessageAsync(message, cancellationToken);
                }
            }
        }

        private async Task HandleUpdateQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery != null)
            {
                if (callbackQuery.Data.Contains("addToBasket"))
                {
                    await _basketManager.PutInBasketAsync(callbackQuery, cancellationToken);
                }
                else if (callbackQuery.Data.Contains("createOrder"))
                {
                    
                }
                else if (callbackQuery.Data.Contains("restaurant_"))
                {
                    await _messageProcessor.ShowProducts(callbackQuery, cancellationToken);
                }
            }
        }
    }
}
