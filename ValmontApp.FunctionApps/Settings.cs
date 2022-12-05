using System;

namespace ValmontApp.FunctionApps
{
    public class Settings : ISettings
    {
        public string FunctionStorageConnectionString { get; set; }
        public Settings(Func<string, string> getter)
        {
            FunctionStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=stvalmontappdevcus001;AccountKey=hxLhSzlThr8Yj3UXB2geYOKrnfeAKdPEaE4oA8+KUMr8R9UGaS0Mo0uLjkDKiikYUJuoWKM3+4kX+ASttd8C6A==;EndpointSuffix=core.windows.net";

        }
    }
}
