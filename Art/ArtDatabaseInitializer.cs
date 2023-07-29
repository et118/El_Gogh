using El_Gogh.Database;
using LiteDB.Async;
using System.Net.Http.Headers;
using System.Reflection;

namespace El_Gogh.Art
{
	static class ArtDatabaseInitializer
	{
		public async static Task InitializePresets() 
		{
			await Bot.database.GetCollection<Txt2ImgPreset>().DeleteManyAsync(x => x.name == "Empty" || x.name == "Anime" || x.name == "Fantasy" || x.name == "Realistic" || x.name == "SDXL");
			await Bot.database.GetCollection<Txt2ImgPreset>().InsertAsync(new Txt2ImgPreset()
			{
				name = "Empty",
				creator = 585812474113163284,
				settings = new Settings(),
				request = new Txt2ImgRequest()
			});
			await Bot.database.GetCollection<Txt2ImgPreset>().InsertAsync(new Txt2ImgPreset()
			{
				name = "Anime",
				creator = 585812474113163284,
				settings = new Settings()
				{
					sd_model_checkpoint = "CounterfeitV30_v30.safetensors",
					sd_vae = "vae-ft-mse-840000-ema-pruned.ckpt",
					CLIP_stop_at_last_layers = 0
				},
				request = new Txt2ImgRequest()
				{
					sampler_name = "DPM++ 2S a Karras",
					sampler_index = "DPM++ 2S a Karras",
					steps = 40,
					negative_prompt = "(worst quality, low quality:1.4), (bad hands) (disfigured) (grain) (deformed) (poorly drawn) (mutilated) (lowres) (lowpoly) (duplicate) (frame) (border) (watermark) (label) (signature) (text) (cropped) (artifacts), bad-artist-anime, bad-picture-chill-75v, bad_prompt_version2, badhandv4, easynegative, ng_deepnegative_v1_75t"
				}
			});
			await Bot.database.GetCollection<Txt2ImgPreset>().InsertAsync(new Txt2ImgPreset()
			{
				name = "Fantasy",
				creator = 585812474113163284,
				settings = new Settings()
				{
					sd_model_checkpoint = "aZovyaRPGArtistTools_v3.safetensors",
					sd_vae = "vae-ft-mse-840000-ema-pruned.ckpt",
					CLIP_stop_at_last_layers = 0
				},
				request = new Txt2ImgRequest()
				{
					sampler_name = "DPM++ 2S a Karras",
					sampler_index = "DPM++ 2S a Karras",
					steps = 40,
					negative_prompt = "(worst quality, low quality:1.4), (bad hands) (disfigured) (grain) (deformed) (poorly drawn) (mutilated) (lowres) (lowpoly) (blurry) (out-of-focus) (duplicate) (frame) (border) (watermark) (label) (signature) (text) (cropped) (artifacts), bad-picture-chill-75v, bad_prompt_version2, badhandv4, easynegative, ng_deepnegative_v1_75t"
				}
			});

			await Bot.database.GetCollection<Img2ImgPreset>().DeleteManyAsync(x => x.name == "Empty" || x.name == "Anime" || x.name == "Fantasy");
			await Bot.database.GetCollection<Img2ImgPreset>().InsertAsync(new Img2ImgPreset()
			{
				name = "Empty",
				creator = 585812474113163284,
				settings = new Settings(),
				request = new Img2ImgRequest()
			});

			await Bot.database.GetCollection<Img2ImgPreset>().InsertAsync(new Img2ImgPreset()
			{
				name = "Anime",
				creator = 585812474113163284,
				settings = new Settings(),
				request = new Img2ImgRequest()
				{
					prompt = "masterpiece, high quality, best quality, anime, beautiful",
					negative_prompt = "text,username,logo,(low quality, worst quality:1.4), (bad anatomy), (inaccurate limb:1.2), bad composition, inaccurate eyes, extra digit, fewer digits, (extra arms:1.2), bad-artist-anime, bad-picture-chill-75v, bad_prompt_version2, badhandv4, easynegative, ng_deepnegative_v1_75t",
					sampler_name = "DPM++ 2S a Karras",
					sampler_index = "DPM++ 2S a Karras",
					steps = 60,
					script_args = new List<object>(){
						null,
						512, //tile width
						0, //Tile height
						8, 
						32, 
						64, 
						0.35,
						32,
						3, // 3 = Upscaler 4x-UltraSharp, 7 = R-ESRGAN 4x+ Anime6B
						true,
						0,
						false,
						8,
						0,
						2, //Use scale
						1024,
						1024,
						2 //Scale from image size
					}
				}
			});

			/*await Bot.database.GetCollection<Txt2ImgPreset>().InsertAsync(new Txt2ImgPreset()
			{
				name = "SDXL",
				creator = 585812474113163284,
				settings = new Settings()
				{
					sd_model_checkpoint = "sd_xl_base_1.0.safetensors",
					sd_vae = "sdxl_vae.safetensors",
					CLIP_stop_at_last_layers = 0
				},
				request = new Txt2ImgRequest()
				{
					save_images = true,
					sampler_name = "DPM++ 2S a Karras",
					sampler_index = "DPM++ 2S a Karras",
					steps = 40,
					negative_prompt = "(worst quality, low quality:1.4), (bad hands) (disfigured) (grain) (deformed) (poorly drawn) (mutilated) (lowres) (lowpoly) (duplicate) (frame) (border) (watermark) (label) (signature) (text) (cropped) (artifacts), bad-picture-chill-75v, bad_prompt_version2, badhandv4, easynegative, ng_deepnegative_v1_75t"
				}
			});*/
		}
		public async static Task UpdateModels()
		{
			IEnumerable<Model> oldModels = await Bot.database.GetCollection<Model>().FindAllAsync();
			IEnumerable<string> currentModels = await StableDiffusionInterface.RequestModels();
			List<Model> newModels = new List<Model>();
			foreach (string modelName in currentModels)
			{
				if (oldModels.Any(x => x.name == modelName))
				{
					newModels.Add(oldModels.Where(x => x.name == modelName).First());
				}
			}
			if (newModels.Count() == 0)
			{
				newModels.Add(new Model() { name = "No models found. Use /addmodel to add some", description="" });
			}
			await Bot.database.GetCollection<Model>().DeleteAllAsync();
			await Bot.database.GetCollection<Model>().InsertBulkAsync(newModels);
		}
		public async static Task UpdateLoras()
		{
			IEnumerable<Lora> existingLoras = await Bot.database.GetCollection<Lora>().FindAllAsync();
			IEnumerable<string> currentLoras = await StableDiffusionInterface.RequestLoras();
			List<Lora> newLoras = new List<Lora>();
			foreach (string lora in currentLoras)
			{
				if(existingLoras.Any(x => x.name == lora))
				{
					newLoras.Add(existingLoras.Where(x => x.name == lora).First());
				}
			}
			if(newLoras.Count() == 0)
			{
				newLoras.Add(new Lora() { name = "No loras found. Use /addlora to add some", triggerWord = "" });
			}
			await Bot.database.GetCollection<Lora>().DeleteAllAsync();
			await Bot.database.GetCollection<Lora>().InsertBulkAsync(newLoras);
		}
	}
}
