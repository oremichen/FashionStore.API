using Newtonsoft.Json;

namespace FashionStore.Domain.Entities
{
    public class EmailNotification
    {
        [JsonProperty("From")]
        public string From { get; set; }

        [JsonProperty("To")]
        public List<string> To { get; set; }

        [JsonProperty("Cc")]
        public List<string> Cc { get; set; }

        [JsonProperty("Bcc")]
        public List<string> Bcc { get; set; }

        [JsonProperty("Subject")]
        public string Subject { get; set; }

        [JsonProperty("Body")]
        public string Body { get; set; }

        [JsonProperty("Attachments")]
        public List<Attachment> Attchements { get; set; }
    }

    public class Attachment
    {
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("attachmentfile")]
        public byte[] Attachmentfile { get; set; }
    }
}
