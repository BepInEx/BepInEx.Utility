using BepInEx.Logging;

namespace BepInEx
{
    public partial class MessageCenter
    {
        private sealed class MessageLogListener : ILogListener
        {
            public void Dispose() { }

            public void LogEvent(object sender, LogEventArgs eventArgs) => OnEntryLogged(eventArgs);
        }
    }
}
