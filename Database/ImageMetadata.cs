using LiteDB;

namespace ElGogh.Database
{
    class ImageMetadata
    {
        public Dictionary<string, Node> workflow { get; set; } = new Dictionary<string, Node>();
		public string workflowName { get; set; } = "";
		public bool nsfw { get; set; } = false;
		public DateTime creationDate { get; set; } = DateTime.UnixEpoch;
		public ulong guildId { get; set; } = 0;
		public ulong userId { get; set; } = 0;
    }
}
