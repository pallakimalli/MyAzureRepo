using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ValmontApp.Data.Models
{
    public class Location : TableEntity
    {
        public Location(string skey, string srow)
        {
            this.PartitionKey = skey;
            this.RowKey = srow;
        }

        public Location() { }
        public DateTime Timestamp { get; set; }
        public int Id { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
    }

}

