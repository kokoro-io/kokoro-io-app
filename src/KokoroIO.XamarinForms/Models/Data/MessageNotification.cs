using Realms;

namespace KokoroIO.XamarinForms.Models.Data
{
    public class MessageNotification : RealmObject
    {
        [PrimaryKey]
        public int MessageId { get; set; }

        public string ChannelId { get; set; }

        public string NotificationId { get; set; }
    }
}