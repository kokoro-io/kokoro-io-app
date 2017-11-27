using System;
using Realms;

namespace KokoroIO.XamarinForms.Models.Data
{
    public class ChannelUserProperties : RealmObject
    {
        [PrimaryKey]
        public string ChannelId { get; set; }

        public string UserId { get; set; }

        public DateTimeOffset LastVisited { get; set; }

        /// <summary>
        /// Gets or sets the id of th most recente message.
        /// </summary>
        public long? LastReadId { get; set; }
    }
}