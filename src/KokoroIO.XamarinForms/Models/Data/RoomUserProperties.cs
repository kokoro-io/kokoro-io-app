using System;
using Realms;

namespace KokoroIO.XamarinForms.Models.Data
{
    public class RoomUserProperties : RealmObject
    {
        [PrimaryKey]
        public string RoomId { get; set; }

        public string UserId { get; set; }

        public DateTimeOffset LastVisited { get; set; }
    }
}