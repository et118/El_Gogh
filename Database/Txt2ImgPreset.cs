using LiteDB;

namespace El_Gogh.Database
{
	class Txt2ImgPreset
	{
		public string name { get; set; }
		public ulong creator { get; set; }
		public Settings settings { get; set; }
		public Txt2ImgRequest request { get; set; }
	}
}
