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
        MessageProcessor _messageProcessor;
        BasketManager _basketManager;

        Dictionary<long, List<Product>> _userBaskets = new Dictionary<long, List<Product>>();
        Dictionary<long, bool> _locationRequested = new Dictionary<long, bool>();

        ProductController _productController = new ProductController();
        RestaurantController _restaurantController = new RestaurantController();

        public MessageHandler(ITelegramBotClient botClient)
        {
            _basketManager = new BasketManager(botClient, _productController, _userBaskets);
            _messageProcessor = new MessageProcessor(botClient, _productController, _restaurantController, _userBaskets);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleUpdateQueryAsync(update.CallbackQuery, cancellationToken);
            }
            else
            {
                await HandleMessageAsync(update, cancellationToken);
            }
        }

        private async Task HandleMessageAsync(Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;

            if (message != null)
            {
                var ChatId = message.Chat.Id;

                if (!_locationRequested.ContainsKey(ChatId) || !_locationRequested[ChatId])
                {
                    // Пользователю еще не запрашивали местоположение, поэтому запросим
                    await _messageProcessor.ProcessMessageAsync(message, cancellationToken);
                    _locationRequested[ChatId] = true;
                    return;
                }

                if (!_userBaskets.ContainsKey(ChatId))
                     _userBaskets[ChatId] = new List<Product>(); // Создаем новую пустую корзину для данного пользователя

                if (message.Type == MessageType.Location)
                {
                    await _messageProcessor.AcceptRequestLocationAsync(ChatId, cancellationToken);
                }
                else
                    await _messageProcessor.ProcessMessageAsync(message, cancellationToken);
            }
        }

        private async Task HandleUpdateQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery != null)
            {

                if (callbackQuery.Data.Contains("addToBasket"))
                    await _basketManager.PutInBasketAsync(callbackQuery, cancellationToken);
                else
                    if (callbackQuery.Data == "createOrder")
                        { }
                else
                    if(callbackQuery.Data.Contains("restaurant_"))
                        await _messageProcessor.ShowProducts(callbackQuery, cancellationToken);
            }
        }
    }
}
