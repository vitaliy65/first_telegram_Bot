using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Курсовая_работа.Controller;
using Курсовая_работа.model;

namespace ConsoleBot
{
    public class MessageHandler : IUpdateHandler
    {
        long rememberClickRestaurantButton = 0;
        List<long> rememberedMessages = new List<long>();
        Dictionary<long, List<Product>> userBaskets = new Dictionary<long, List<Product>>();
        ProductController productController = new ProductController();
        RestaurantController restaurantController = new RestaurantController();

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandelUpdateQueryAsync(botClient, update.CallbackQuery, cancellationToken);
            }
            else
            {
                var message = update.Message;
                if (message?.Text != null)
                {
                    var messageText = message.Text;
                    var chatId = message.Chat.Id;
                    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}, user > {message.Chat.Username}.");

                    long userId = message.Chat.Id;

                    // Проверяем, существует ли уже корзина для данного пользователя
                    if (!userBaskets.ContainsKey(userId))
                    {
                        // Создаем новую пустую корзину для данного пользователя
                        userBaskets[userId] = new List<Product>();
                    }

                    // handle commands
                    if (message.Text.Contains("/"))
                    {
                        await handleCommandsMessageAsync(botClient, update, cancellationToken);
                    }
                    else
                        await HandleButtonMessageAsync(botClient, update, chatId, messageText, cancellationToken);
                }
            }
        }

        private async Task HandelUpdateQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery != null)
            {
                if (callbackQuery.Data.Contains("inBasket"))
                {
                    await putInBacket(botClient, callbackQuery, callbackQuery.Message.Chat.Id, cancellationToken);
                }
                else if (!callbackQuery.Data.Contains("addToBasket") && !callbackQuery.Data.Contains("createOrder"))
                {
                    await ShowProducts(botClient, callbackQuery, callbackQuery.Message.Chat.Id, cancellationToken);
                }
            }
        }

        private async Task putInBacket(ITelegramBotClient botClient, CallbackQuery callbackQuery, long id, CancellationToken cancellationToken)
        {
            var productId = int.Parse(callbackQuery.Data.Split("_")[1]);
            var product = productController.GetElements().FirstOrDefault(p => p.ProductId == productId);

            if (product != null && userBaskets.ContainsKey(callbackQuery.Message.Chat.Id))
            {
                userBaskets[callbackQuery.Message.Chat.Id].Add(product);

                // Create a new inline keyboard markup with the updated button text
                var replyKeyboardMarkup = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Added to Basket ✅", "addToBasket") } });

                await botClient.EditMessageTextAsync(callbackQuery.Message.Chat,
                                                     callbackQuery.Message.MessageId,
                                                     callbackQuery.Message.Text,
                                                     replyMarkup: replyKeyboardMarkup);
            }
        }

        private async Task ShowProducts(ITelegramBotClient botClient, CallbackQuery callbackQuery, long chatId, CancellationToken cancellationToken)
        {

            if (rememberedMessages.Count != 0)
                foreach (var item in rememberedMessages)
                {
                    botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, (int)item);
                }
            rememberedMessages.Clear();

            // Отправляем пустое сообщение для отключения свечения кнопки
            botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            string data = callbackQuery.Data;

            int restaurantId = int.Parse(data.Split("_")[1]);
            Restaurant restaurant = restaurantController.GetElements().Where(item => item.Id == restaurantId).FirstOrDefault();

            foreach (var item in productController.GetElements())
            {
                if (item.RestaurantId == restaurant.Id)
                {
                    var replyKeyboardMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("В кошик 🛒", $"inBasket_{item.ProductId}"));

                    string message = CreateProductText(item);
                    var _message = await botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                    rememberedMessages.Add(_message.MessageId);
                }
            }
        }

        private async Task ShowRestaurants(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
            List<InlineKeyboardButton> currentRow = new List<InlineKeyboardButton>();

            foreach (var restaurant in restaurantController.GetElements())
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
            var message = await botClient.SendTextMessageAsync(chatId, "Виберіть заклад:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
            rememberClickRestaurantButton = message.MessageId;
        }

        private async Task HandleButtonMessageAsync(ITelegramBotClient botClient, Update update, long chatId, string messageText, CancellationToken cancellationToken)
        {
            // Define reply keyboard markup with four buttons
            if (rememberClickRestaurantButton != 0)
                await botClient.DeleteMessageAsync(chatId, (int)rememberClickRestaurantButton, cancellationToken);
            rememberClickRestaurantButton = 0;

            if (rememberedMessages.Count != 0)
                foreach (var item in rememberedMessages)
                {
                    await botClient.DeleteMessageAsync(chatId, (int)item);
                }
            rememberedMessages.Clear();

            var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Ресторани", "Кошик" } });
            replyKeyboardMarkup.ResizeKeyboard = true;

            // Check if the received message matches any of the expected button labels
            switch (messageText)
            {
                case "Ресторани":
                    await ShowRestaurants(botClient, chatId, cancellationToken);
                    break;
                case "Кошик":
                    await handleCommandsMessageAsync(botClient, update, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId, "Я тебе не розумію... виберіть команду будь ласка!", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                    break;
            }
        }

        private async Task handleCommandsMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                ReplyKeyboardMarkup replyKeyboardMarkup = null;

                switch (message.Text)
                {
                    case "/start":
                        replyKeyboardMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Ресторани", "Кошик" }, });
                        replyKeyboardMarkup.ResizeKeyboard = true;
                        await botClient.SendTextMessageAsync(message.Chat, "Доброго дня, виберіть команду.", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                        break;
                    case "/stop":
                        replyKeyboardMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "/start" }, });
                        replyKeyboardMarkup.ResizeKeyboard = true;
                        await botClient.SendTextMessageAsync(message.Chat, "Гарного вам дня, ще побачимось!", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
                        break;
                    case "/help":
                        await botClient.SendTextMessageAsync(message.Chat, "Для початку роботи вам потрібно натиснути на кнопку під полем вводу повідомлення 'Ресторани'!");
                        break;
                    case "/showbasket":
                    case "Кошик":
                        if (userBaskets.ContainsKey(message.Chat.Id) && userBaskets[message.Chat.Id].Count != 0)
                        {
                            var replyBasketMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Оформити замовлення 💸", "createOrder"));
                            float sum = 0;
                            string messageBasket = "--------------------------< Кошик >--------------------------\n";
                            foreach (var item in userBaskets[message.Chat.Id])
                            {
                                messageBasket += "|\n";
                                messageBasket += "|\n";
                                messageBasket += CreateProductText(item);
                                sum += item.Price;
                            }
                            messageBasket += "--------------------------------------------------------------------\n\n\n";
                            messageBasket += "Повністю до сплати: " + Math.Round(sum, 2) + "грн";

                            await botClient.SendTextMessageAsync(message.Chat, messageBasket, replyMarkup: replyBasketMarkup);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "🕸 На даний момент ваш кошик порожній 🕸");
                        }
                        break;
                    case "/clearbasket":
                        if(userBaskets[message.Chat.Id].Count == 0)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Здається ваш кошик і так порожній 😄.");
                        }
                        else
                        {
                            userBaskets[message.Chat.Id] = new List<Product>();
                            await botClient.SendTextMessageAsync(message.Chat, "Здається ви загубили свій кошик 😄\nТримайте новий - 🛒");
                        }
                        break;
                    default:
                        await botClient.SendTextMessageAsync(message.Chat, "Будь ласка виберіть команду.");
                        break;
                }
            }
        }

        string CreateProductText(Product item)
        {
            string message = "Назва: " + item.Name + "\n" +
                             "📝 Опис: " + item.Description + "\n" +
                             "🤑 Ціна: " + item.Price + "грн\n";
            return message;
        }
    }
}
