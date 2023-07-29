
namespace El_Gogh.Database
{
	class Settings
	{
		public string sd_model_checkpoint { get; set; } = "";
		public string sd_vae { get; set; } = "vae-ft-mse-840000-ema-pruned.ckpt";
		public int CLIP_stop_at_last_layers { get; set; } = 0;
	}
}
