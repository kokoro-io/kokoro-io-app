namespace KokoroIO.XamarinForms.Views
{
    public enum MessagesViewUpdateType
    {
        // Inbound events.
        // 1. set_Messages.
        // 2. Messages#CollectionChanged.
        // 3. MessageInfo#PropertyChanged.
        // 4. Show message
        // 5. set_HasUnread
        Added,

        Removed,
        IsMergedChanged,
        Show,
    }
}