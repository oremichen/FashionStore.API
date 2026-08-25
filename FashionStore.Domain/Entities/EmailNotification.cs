using System.Text.Json.Serialization;

namespace FashionStore.Domain.Entities
{
    public class EmailNotification
    {
        [JsonPropertyName("From")]
        public string From { get; set; }

        [JsonPropertyName("ReplyTo")]
        public string? ReplyTo { get; set; }

        [JsonPropertyName("To")]
        public List<string> To { get; set; }

        [JsonPropertyName("Cc")]
        public List<string> Cc { get; set; }

        [JsonPropertyName("Bcc")]
        public List<string> Bcc { get; set; }

        [JsonPropertyName("Subject")]
        public string Subject { get; set; }

        [JsonPropertyName("Body")]
        public string Body { get; set; }

        [JsonPropertyName("Attachments")]
        public List<Attachment> Attchements { get; set; }
    }

    public class Attachment
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("attachmentfile")]
        public byte[] Attachmentfile { get; set; }
    }
}
