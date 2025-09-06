namespace SecilStoreConfigWeb.Models
{
    public class ConfigUpsertModel
    {
        public string ApplicationName { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } = "string"; // "string|int|bool|double"
        public string Value { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
