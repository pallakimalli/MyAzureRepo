using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ValmontApp.Data.Models
{
    public class BusinessSegment : TableEntity
    {
        public BusinessSegment(string skey, string srow)
        {
            this.PartitionKey = skey;
            this.RowKey = srow;
        }

        public BusinessSegment() { }
        public DateTime Timestamp { get; set; }
        public int  Id { get; set; }
        public string BusinessSegmentDescription { get; set; }
        public bool IsDefault { get; set; }
    }

}

