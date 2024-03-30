using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Курсовая_работа.Controller;
using Курсовая_работа.model;
using Telegram.Bot.Types.Enums;

public class MessageProcessor
{
    private readonly ITelegramBotClient _botClient;
    private readonly ProductController _productController;
    private readonly RestaurantController _restaurantController;
    private readonly Dictionary<long, List<Product>> _userBaskets;

    private List<int> _rememberMassages;

    public MessageProcessor(ITelegramBotClient botClient, ProductController productController, RestaurantController restaurantController, Dictionary<long, List<Product>> userBaskets)
    {
        _botClient = botClient;
        _productController = productController;
        _restaurantController = restaurantController;
        _userBaskets = userBaskets;
        _rememberMassages = new List<int>();
    }

    public async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text != null)
        {
            var messageText = message.Text;
            var chatId = message.Chat.Id;
            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}, user > {message.Chat.Username}.");

            long userId = message.Chat.Id;

            // Проверяем, существует ли уже корзина для данного пользователя
            if (!_userBaskets.ContainsKey(userId))
            {
                // Создаем новую пустую корзину для данного пользователя
                _userBaskets[userId] = new List<Product>();
            }

            // Обрабатываем сообщение
            if (messageText.Contains("/"))
            {
                await HandleCommandsAsync(message, cancellationToken);
            }
            else
            {
                await HandleButtonMessageAsync(message, cancellationToken);
            }
        }
    }

    private async Task HandleCommandsAsync(Message message, CancellationToken cancellationToken)
    {
        switch (message.Text)
        {
            case "/start":
                await SendStartMessageAsync(message.Chat, cancellationToken);
                break;
            case "/stop":
                await SendStopMessageAsync(message.Chat, cancellationToken);
                break;
            case "/help":
                await SendHelpMessageAsync(message.Chat, cancellationToken);
                break;
            case "/showbasket":
            case "Кошик":
                await SendBasketMessageAsync(message.Chat, cancellationToken);
                break;
            case "/clearbasket":
                await ClearBasketAsync(message.Chat, cancellationToken);
                break;
            default:
                await SendUnknownCommandMessageAsync(message.Chat, cancellationToken);
                break;
        }
    }

    private async Task HandleButtonMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Ресторани", "Кошик" } });
        replyKeyboardMarkup.ResizeKeyboard = true;

        // Check if the received message matches any of the expected button labels
        switch (message.Text)
        {
            case "Ресторани":
                await SendRestaurantsMessageAsync(message.Chat, cancellationToken);
                break;
            case "Кошик":
                await SendBasketMessageAsync(message.Chat, cancellationToken);
                break;
            default:
                await _botClient.SendTextMessageAsync(message.Chat, "Я тебе не розумію... виберіть команду будь ласка!", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                break;
        }
    }

    public  async Task ShowProducts(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        ClearRememberedMessages(callbackQuery.Message.Chat, cancellationToken);
        // Отправляем пустое сообщение для отключения свечения кнопки
        _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        string data = callbackQuery.Data;

        int restaurantId = int.Parse(data.Split("_")[1]);
        Restaurant restaurant = _restaurantController.GetElements().Where(item => item.Id == restaurantId).FirstOrDefault();

        foreach (var item in _productController.GetElements())
        {
            if (item.RestaurantId == restaurant.Id)
            {
                var replyKeyboardMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("В кошик 🛒", $"inBasket_{item.ProductId}"));

                string message = CreateProductText(item);
                var _message = await _botClient.SendPhotoAsync(chatId: callbackQuery.Message.Chat,
                                                              photo: InputFile.FromUri("https://raw.githubusercontent.com/vitaliy65/first_telegram_Bot/master/ConsoleBot/images/" + item.FilePathimage.Split("\\")[2]),
                                                              caption: message,
                                                              parseMode: ParseMode.Html,
                                                              cancellationToken: cancellationToken,
                                                              replyMarkup: replyKeyboardMarkup);
                RememberMessage(_message.MessageId, callbackQuery.Message.Chat, cancellationToken);
            }
        }
    }

    private async Task SendStartMessageAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Ресторани", "Кошик" } });
        replyKeyboardMarkup.ResizeKeyboard = true;
        var message = await _botClient.SendTextMessageAsync(chatId, "Доброго дня, виберіть команду.", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
    }

    private async Task SendStopMessageAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "/start" }, });
        replyKeyboardMarkup.ResizeKeyboard = true;
        var message = await _botClient.SendTextMessageAsync(chatId, "Гарного вам дня, ще побачимось!", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
        RememberMessage(message.MessageId, chatId, cancellationToken);
    }

    private async Task SendHelpMessageAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        var message = await _botClient.SendTextMessageAsync(chatId, "Для початку роботи вам потрібно натиснути на кнопку під полем вводу повідомлення 'Ресторани'!", cancellationToken: cancellationToken);
        RememberMessage(message.MessageId, chatId, cancellationToken);
    }

    private async Task SendBasketMessageAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        ClearRememberedMessages(chatId, cancellationToken);
        if (_userBaskets.ContainsKey((long)chatId.Identifier) && _userBaskets[(long)chatId.Identifier].Count != 0)
        {
            var replyBasketMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Оформити замовлення 💸", "createOrder"));
            float sum = 0;
            string messageBasket = "--------------------------< Кошик >--------------------------\n";
            foreach (var item in _userBaskets[(long)chatId.Identifier])
            {
                messageBasket += "|\n";
                messageBasket += "|\n";
                messageBasket += CreateProductText(item);
                sum += item.Price;
            }
            messageBasket += "--------------------------------------------------------------------\n\n\n";
            messageBasket += "Повністю до сплати: " + Math.Round(sum, 2) + "грн";

            var message = await _botClient.SendTextMessageAsync(chatId, messageBasket, replyMarkup: replyBasketMarkup);
            RememberMessage(message.MessageId, chatId, cancellationToken);
        }
        else
        {
            var message = await _botClient.SendTextMessageAsync(chatId, "🕸 На даний момент ваш кошик порожній 🕸");
            RememberMessage(message.MessageId, chatId, cancellationToken);
        }
    }

    private async Task ClearBasketAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        if (_userBaskets[(long)chatId.Identifier].Count == 0)
        {
            var message = await _botClient.SendTextMessageAsync(chatId, "Здається ваш кошик і так порожній 😄.");
            RememberMessage(message.MessageId, chatId, cancellationToken);
        }
        else
        {
            _userBaskets[(long)chatId.Identifier] = new List<Product>();
            var message = await _botClient.SendTextMessageAsync(chatId, "Здається ви загубили свій кошик 😄\nТримайте новий - 🛒");
            RememberMessage(message.MessageId, chatId, cancellationToken);
        }
    }

    private async Task SendUnknownCommandMessageAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        var message = await _botClient.SendTextMessageAsync(chatId, "Будь ласка виберіть команду.", cancellationToken: cancellationToken);
        RememberMessage(message.MessageId, chatId, cancellationToken);
    }

    private async Task SendRestaurantsMessageAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        ClearRememberedMessages(chatId, cancellationToken);
        List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
        List<InlineKeyboardButton> currentRow = new List<InlineKeyboardButton>();

        foreach (var restaurant in _restaurantController.GetElements())
        {
            // Создаем кнопку для каждого ресторана
            var button = InlineKeyboardButton.WithCallbackData(restaurant.Name, $"restaurant_{restaurant.Id}");

            // Добавляем кнопку в текущий ряд
            currentRow.Add(button);

            // Если текущий ряд заполнен, добавляем его в список рядов и создаем новый ряд
            rows.Add(currentRow.ToArray());
            currentRow.Clear();
        }

        // Создаем объект InlineKeyboardMarkup с сформированными рядами кнопок
        InlineKeyboardMarkup inlineKeyboard = rows.ToArray();

        // Отправляем сообщение с клавиатурой
        var message = await _botClient.SendTextMessageAsync(chatId, "Виберіть заклад:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        RememberMessage(message.MessageId, chatId, cancellationToken);
    }

    string CreateProductText(Product item)
    {
        string message = "Назва: " + item.Name + "\n" +
                         "📝 Опис: " + item.Description + "\n" +
                         "🤑 Ціна: " + item.Price + "грн\n";
        return message;
    }

    private void ClearRememberedMessages(ChatId chatId, CancellationToken cancellationToken)
    {
        try
        {
            if (_rememberMassages.Count != 0)
            {
                foreach (var message in _rememberMassages)
                {
                    _botClient.DeleteMessageAsync(chatId, message, cancellationToken);
                }
            }
            _rememberMassages.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void RememberMessage(int messageId, ChatId chatId, CancellationToken cancellationToken)
    {
        _rememberMassages.Add(messageId);
    }
}