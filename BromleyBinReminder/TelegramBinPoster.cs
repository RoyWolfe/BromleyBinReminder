namespace BromleyBinReminder;

using Options;
using Telegram.Bots;
using Telegram.Bots.Requests;
using Telegram.Bots.Types;

public class TelegramBinPoster
{
    private readonly IBotClient _botClient;
    private readonly TelegramOptions _options;

    public TelegramBinPoster(IBotClient botClient, TelegramOptions options)
    {
        _botClient = botClient;
        _options = options;
    }

    public async Task<Response<TextMessage>> PostReminderMessage(string message)
    {
        var textRequest = new SendText(_options.TargetChatId, message);
        return await _botClient.HandleAsync(textRequest);
    }
}