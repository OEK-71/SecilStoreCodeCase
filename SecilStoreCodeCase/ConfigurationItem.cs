namespace SecilStoreCodeCase
{
    public class ConfigurationItem
    {
        public int Id { get; init; }
        public required string ApplicationName { get; init; }
        public required string Name { get; init; }              
        public required ConfigItemType Type { get; init; }
        public required string Value { get; init; }             
        public bool IsActive { get; init; }
        public DateTime UpdatedAtUtc { get; init; }
    }
}
