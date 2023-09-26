namespace BromleyBinReminder;

public class BinEvent
{
    public DateTime Date { get; set; }
    public IEnumerable<string> Bins { get; set; }
}