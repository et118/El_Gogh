
namespace El_Gogh.Database
{
	class ImageMetadata
	{
		public bool censored { get; set; }
		public DateTime generatedTime { get; set; }
		public ulong guildId { get; set; }
		public ulong userId { get; set; }
		public Settings settings { get; set; }
		public Txt2ImgRequest request { get; set; }
		public string preset { get; set; }
	}
}
