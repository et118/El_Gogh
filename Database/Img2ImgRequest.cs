
namespace El_Gogh.Database
{
	class Img2ImgRequest
	{
		public List<string> init_images { get; set; }
		public int resize_mode { get; set; } = 0;
		public float denoising_strength { get; set; } = 0.25f;
		public string prompt { get; set; } = "";
		public string negative_prompt { get; set; } = "";
		public int seed { get; set; } = -1;
		public string sampler_name { get; set; } = "Euler a";
		public string sampler_index { get; set; } = "Euler a";
		public int batch_size { get; set; } = 1;
		public int steps { get; set; } = 40;
		public int width { get; set; } = 1024;
		public int height { get; set; } = 1024;
		public bool include_init_images { get; set; } = false;
		public string script_name { get; set; } = "ultimate sd upscale";
		public List<object> script_args { get; set; }
		public bool send_images = true;
		public bool save_images = false;
	}
}
