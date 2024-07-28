using System.Diagnostics;

namespace ElGogh
{
	internal class Program
	{
		public static Process comfyUIProcess;
		static void Main(string[] args)
		{
			if (args.Length < 2) {
				Console.Error.WriteLine("Invalid arguments provided");
				Console.Error.WriteLine(
					"""
					Usage: 
						./ElGogh <Path to ComfyUI Launch Script> <Discord Bot Token>

					Note:
						To avoid a messy python environment your ComfyUI Launch Script should invoke its own python environment.
						Create one with "python3 -m venv <name of environment>" and enter the environment using the activate file generated in 
						"<name of environment>/Scripts"
					"""
					);
				return;
			}
			if (!File.Exists(args[0]))
			{
				Console.Error.WriteLine($"File \"{args[0]}\" does not exist");
				return;
			}
			//TODO: Install comfyUI custom extension
			#if !DEBUG
			startComfyUIProcess(args[0]);
			#endif
			/* --COMFYUI custom nodes--
			 * ComfyUI-Manager
			 * ComfyUI-Impact-Pack
			 * ComfyUI-CLIPSeg
			 * UltimateSDUpscale
			 * ComfyUI-safety-checker
			 * ComfyUI-ElGogh-Nodes
			 * 
			 * 
			 * --COMFYUI checkpoints--
			 * a7b3_v10.safetensors
			 * animagineXLV31_v31.safetensors
			 * AnythingXL_xl.safetensors
			 * aZovyaRPGArtistTools_v3.safetensors
			 * aZovyaRPGArtistTools_v4VAE.safetensors
			 * CounterfeitV30_v30.safetensors
			 * forrealxl_v05Fp16.safetensors
			 * nightSkyYOZORAStyle.safetensors
			 * ponyDiffusionV6XL_v6.safetensors
			 * profantasy_v21.safetensors
			 * realisticVisionV60B1_v51HyperVAE.safetensors
			 * sdxlUnstableDiffusers_nihilmania.safetensors
			 * tmndMix_tmndMixSPRAINBOW.safetensors
			 * 
			 * --COMFYUI embeddings--
			 * bad-artist.pt
			 * bad-artist-anime.pt
			 * badhandv4.pt
			 * bad_prompt_version2.pt
			 * BeyondSDXLv3.safetensors
			 * easynegative.safetensors
			 * negativeXL_D.safetensors
			 * ng_deepnegative_v1_75t.pt
			 * UnrealisticDream.pt
			 * verybadimagenegative_v1.3.pt
			 * 
			 * --COMFYUI loras--
			 * add_detail.safetensors
			 * add-detail-xl.safetensors
			 * animeoutlineV4_16.safetensors
			 * ayanami_reiXL.safetensors
			 * GachaSplash4.safetensors
			 * gorou.safetensors
			 * Gorou-v0.0.16-000040.safetensors
			 * gorouXL.safetensors
			 * lappland.safetensors
			 * Mari.safetensors
			 * POVCheekSquash_XLPD.safetensors
			 * ps1_style_SDXL_v2.safetensors
			 * rei_ayanami.safetensors
			 * xl_more_art-full_v1.safetensors
			 * XlFantasyKnights.safetensors
			 * yuri.safetensors
			 * zavy-cntrst-sdxl.safetensors
			 * 
			 * --COMFYUI VAEs--
			 * anything-v4.0.vae.pt
			 * orangemixvaeReupload_v10.pt
			 * sdxl_vae.safetensors
			 * vae-ft-mse-840000-ema-pruned.ckpt
			 * 
			 * TODO add upscale models to list
			 * */

			Bot bot = new Bot(args[1]);
			bot.start();
		}

		public static void startComfyUIProcess(string launchScriptPath)
		{
			if (comfyUIProcess != null) return;
			Console.WriteLine("Starting ComfyUI...");
			comfyUIProcess = new Process();
			comfyUIProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(launchScriptPath);
			comfyUIProcess.StartInfo.UseShellExecute = false;
			comfyUIProcess.StartInfo.RedirectStandardOutput = true;
			comfyUIProcess.StartInfo.RedirectStandardError = true;
			comfyUIProcess.StartInfo.FileName = launchScriptPath;
			comfyUIProcess.ErrorDataReceived += (sender, args) => Console.Error.WriteLine(args.Data);
			comfyUIProcess.Start();
			comfyUIProcess.BeginErrorReadLine();
			DateTime dateTime = DateTime.Now;
			while (true) {
				//comfyUIProcess.StandardOutput.ReadLine().Contains("To see the GUI go to:")
				if ((DateTime.Now-dateTime).TotalSeconds > 10) break; //TODO wtf why does only timer work
			}
			Console.WriteLine("ComfyUI started");
		}
	}
}