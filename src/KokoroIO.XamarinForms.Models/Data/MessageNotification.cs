using Realms;

namespace KokoroIO.XamarinForms.Models.Data
{
    public class MessageNotification : RealmObject
    {
        [PrimaryKey]
        public long MessageId { get; set; }

        public string ChannelId { get; set; }

        public string NotificationId { get; set; }
    }
}