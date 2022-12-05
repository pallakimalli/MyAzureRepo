using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ValmontApp.Data.Models
{

    public class UsersEntity : TableEntity
    {
        public UsersEntity(string skey, string srow)
        {
            this.PartitionKey = skey;
            this.RowKey = srow;
        }

        public UsersEntity() { }
        public DateTime Timestamp { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string BusinessSegment { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Mail { get; set; }
        public string Id { get; set; }
        public string MobilePhone { get; set; }
        public string OfficePhone { get; set; }
        public string[] BusinessPhones { get; set; }
        public string ProfilePic { get; set; }
        public string QRCode { get; set; }
        public string State { get; set; }
        public string JobTitle { get; set; }
        public string Website { get; set; }
        public string Zip { get; set; }
        public string GivenName { get; set; }
        public string DisplayName { get; set; }
        public string Surname { get; set; }
        public string ProfileURL { get; set; }
    }


    public class RequestParam
    {
        public RequestParam(string id, string token)
        {
            this.Id = id;
            this.Token = token;
        }

        public RequestParam() { }
        public string Id { get; set; }
        public string Token { get; set; }
    }
}

