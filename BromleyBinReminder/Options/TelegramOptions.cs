namespace BromleyBinReminder.Options;

public class TelegramOptions
{
    public string BotKey { get; set; }
    public long TargetChatId { get; set; } // get from https://api.telegram.org/bot[bot key here]/getUpdates
    public string MessagePrefix { get; set; } // Recommendation: End in a colon; will append list of bins types that need to be put out.
}