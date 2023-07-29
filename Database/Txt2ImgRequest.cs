
namespace El_Gogh.Database
{
	class Txt2ImgRequest
	{
		public string prompt { get; set; } = "";
		public int seed { get; set; } = -1;
		public string sampler_name { get; set; } = "Euler a";
		public int batch_size { get; set; } = 1;
		public int n_iter { get; set; } = 1;
		public int steps { get; set; } = 20;
		public float cfg_scale { get; set; } = 7;
		public int width { get; set; } = 512;
		public int height { get; set; } = 512;
		public bool tiling { get; set; } = false;
		public string negative_prompt { get; set; } = "";
		public string sampler_index { get; set; } = "Euler a";
		public bool send_images { get; set; } = true;
		public bool save_images { get; set; } = false;
	}
}
