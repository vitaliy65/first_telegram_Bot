using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Курсовая_работа.Controller;
using Курсовая_работа.model;

public class BasketManager
{
    private readonly ITelegramBotClient _botClient;
    private readonly ProductController _productController;
    private readonly Dictionary<long, List<Product>> _userBaskets;

    public BasketManager(ITelegramBotClient botClient, ProductController productController, Dictionary<long, List<Product>> userBaskets)
    {
        _botClient = botClient;
        _productController = productController;
        _userBaskets = userBaskets;
    }

    public async Task PutInBasketAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var productId = int.Parse(callbackQuery.Data.Split("_")[1]);
        var product = _productController.GetElements().FirstOrDefault(p => p.ProductId == productId);

        if (product != null && _userBaskets.ContainsKey(callbackQuery.Message.Chat.Id))
        {
            _userBaskets[callbackQuery.Message.Chat.Id].Add(product);

            var replyKeyboardMarkup = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Added to Basket ✅", "addedToBasket") } });

            await _botClient.EditMessageCaptionAsync(callbackQuery.Message.Chat,
                                                     callbackQuery.Message.MessageId,
                                                     callbackQuery.Message.Caption,
                                                     replyMarkup: replyKeyboardMarkup,
                                                     parseMode: ParseMode.Html);
        }
    }
}
