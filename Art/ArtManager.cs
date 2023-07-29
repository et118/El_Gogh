using DSharpPlus.SlashCommands;
using El_Gogh.Database;
using DSharpPlus.Entities;
using LiteDB;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace El_Gogh.Art
{
	static class ArtManager
	{
		public async static Task<LiteFileInfo<string>> GenerateSimplePresetImage(string preset, string prompt, string negativeprompt, long seed, InteractionContext ctx)
		{
			Txt2ImgPreset txt2ImgPreset = await Bot.database.GetCollection<Txt2ImgPreset>().FindOneAsync(x => x.name == preset);
			txt2ImgPreset.request.prompt = prompt;
			if (seed <= 0)
			{
				txt2ImgPreset.request.seed = new Random().Next();
			}
			else {
				txt2ImgPreset.request.seed = (int)seed;
			}
			if (negativeprompt != "") txt2ImgPreset.request.negative_prompt = negativeprompt;

			string fileName = Guid.NewGuid().ToString() + ".png";
			string imageIdentifier = ctx.Guild.Id + "/" + ctx.User.Id + "/" + fileName;

			Task<Tuple<Image,bool>> request = StableDiffusionInterface.RequestTxt2Img(txt2ImgPreset.settings, txt2ImgPreset.request, imageIdentifier);
			Task progress = Task.Run(async () =>
			{
				while(true)
				{
					string progress = await StableDiffusionInterface.RequestProgress(imageIdentifier);
					if (progress == "Request not in queue")
					{
						return;
					}
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Progress").WithDescription(progress)));
					
					await Task.Delay(1500);
				}
			});
			
			await Task.WhenAll(request, progress);
			(Image image, bool censored) = await request;
			

			using (MemoryStream stream = new MemoryStream())
			{
				await image.SaveAsPngAsync(stream);
				stream.Position = 0;
				return await Bot.database.GetStorage<string>("Images", "Chunks").UploadAsync(imageIdentifier, fileName, stream, metadata: BsonMapper.Global.ToDocument(new ImageMetadata() {censored = censored, guildId=ctx.Guild.Id, userId=ctx.User.Id, generatedTime=DateTime.Now, request=txt2ImgPreset.request, settings= txt2ImgPreset.settings, preset=txt2ImgPreset.name}));
			}
		}
		public async static Task<LiteFileInfo<string>> GenerateImage(InteractionContext ctx, string preset, string model, string prompt, string negativeprompt = "", string primaryLora = "", string secondaryLora = "", string tertiaryLora = "", string sampler = "", long steps = -1, long width = -1, long height = -1, long seed = -1, bool tiling = false, long cfg_scale = -1, long clip_skip = -1)
		{
			Txt2ImgPreset txt2ImgPreset = await Bot.database.GetCollection<Txt2ImgPreset>().FindOneAsync(x => x.name == preset);
			Settings settings = txt2ImgPreset.settings;
			Txt2ImgRequest request = txt2ImgPreset.request;

			settings.sd_model_checkpoint = model;
			if(clip_skip != -1) settings.CLIP_stop_at_last_layers = (int)clip_skip;

			request.prompt = prompt;
			if(negativeprompt != "") request.negative_prompt = negativeprompt;
			if (tertiaryLora != "") {
				Lora lora = await Bot.database.GetCollection<Lora>().FindOneAsync(x => x.name == tertiaryLora);
				string beginningPrompt = String.Join(",", request.prompt.Split(",").Take(2)) + ",";
				request.prompt = beginningPrompt + (lora.triggerWord != "" ? lora.triggerWord + "," : "") + String.Join(",",request.prompt.Split(",").Skip(2)) + $", <lora:{lora.name}:{lora.strength}>";
			}
			if (secondaryLora != "") {
				Lora lora = await Bot.database.GetCollection<Lora>().FindOneAsync(x => x.name == secondaryLora);
				string beginningPrompt = String.Join(",", request.prompt.Split(",").Take(2)) + ",";
				request.prompt = beginningPrompt + (lora.triggerWord != "" ? lora.triggerWord + "," : "") + String.Join(",", request.prompt.Split(",").Skip(2)) + $", <lora:{lora.name}:{lora.strength}>";
			}
			if (primaryLora != "") {
				Lora lora = await Bot.database.GetCollection<Lora>().FindOneAsync(x => x.name == primaryLora);
				string beginningPrompt = String.Join(",", request.prompt.Split(",").Take(2)) + ",";
				request.prompt = beginningPrompt + (lora.triggerWord != "" ? lora.triggerWord + "," : "") + String.Join(",", request.prompt.Split(",").Skip(2)) + $", <lora:{lora.name}:{lora.strength}>";
			}
			request.prompt = request.prompt.Replace(",,", ",");
			if (sampler != "") {
				request.sampler_index = sampler;
				request.sampler_name = sampler;
			}
			if (steps != -1)request.steps = (int)steps;
			if (width != -1)request.width = (int)width;
			if (height != -1)request.height = (int)height;
			if (seed != -1) {
				request.seed = (int)seed;
			} else {
				request.seed = new Random().Next();
			}
			if (tiling)request.tiling = true;
			if (cfg_scale != -1) request.cfg_scale = cfg_scale;

			string fileName = Guid.NewGuid().ToString() + ".png";
			string imageIdentifier = ctx.Guild.Id + "/" + ctx.User.Id + "/" + fileName;
			Task<Tuple<Image, bool>> req = StableDiffusionInterface.RequestTxt2Img(settings, request, imageIdentifier);
			Task progress = Task.Run(async () =>
			{
				while (true)
				{
					string progress = await StableDiffusionInterface.RequestProgress(imageIdentifier);
					if (progress == "Request not in queue")
					{
						return;
					}
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Progress").WithDescription(progress)));

					await Task.Delay(1500);
				}
			});
			await Task.WhenAny(req, progress);
			(Image image, bool censored) = await req;
			using (MemoryStream stream = new MemoryStream())
			{
				await image.SaveAsPngAsync(stream);
				stream.Position = 0;
				return await Bot.database.GetStorage<string>("Images", "Chunks").UploadAsync(imageIdentifier, fileName, stream, metadata: BsonMapper.Global.ToDocument(new ImageMetadata() { censored = censored, guildId = ctx.Guild.Id, userId = ctx.User.Id, generatedTime = DateTime.Now, request = request, settings = settings, preset = preset }));
			}
		}
		public async static Task<LiteFileInfo<string>> UpscaleImage(InteractionContext ctx, LiteFileInfo<string> image, string preset, double denoisingStrength, double scale = -1, long upscaler = -1, string prompt = "", string negativePrompt = "", long steps = -1, long seed = -1, string sampler = "", string model = "")
		{
			Img2ImgPreset p = await Bot.database.GetCollection<Img2ImgPreset>().FindOneAsync(x => x.name == preset);
			ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>(image.Metadata);
			Settings settings = p.settings;
			Img2ImgRequest request = p.request;

			request.denoising_strength = (float)denoisingStrength;
			request.sampler_name = metadata.request.sampler_name;
			request.sampler_index = metadata.request.sampler_index;
			settings.sd_model_checkpoint = metadata.settings.sd_model_checkpoint;
			settings.sd_vae = metadata.settings.sd_vae;

			if (upscaler != -1) request.script_args[8] = upscaler;
			if (scale != -1) request.script_args[17] = scale;
			if (prompt != "") request.prompt = prompt;
			if (negativePrompt != "") request.negative_prompt = negativePrompt;
			if (steps != -1) request.steps = (int)steps;
			if (sampler != "") request.sampler_name = sampler;
			if (sampler != "") request.sampler_index = sampler;
			if (model != "")settings.sd_model_checkpoint = model;
			if (seed != -1)
			{
				request.seed = (int)seed;
			} else
			{
				request.seed = new Random().Next();
			}

			//Prompt + lora
			IEnumerable<Lora> loras = await Bot.database.GetCollection<Lora>().FindAllAsync();
			string addLoras = "";
			foreach(Lora lora in loras)
			{
				if(metadata.request.prompt.Contains($"<lora:{lora.name}:{lora.strength}") && !request.prompt.Contains($"<lora:{lora.name}:{lora.strength}"))
				{
					addLoras += $"{lora.triggerWord}, <lora:{lora.name}:{lora.strength}>";
				}
			}
			request.prompt += ", " + addLoras;

			string fileName = Guid.NewGuid().ToString() + ".png";
			string imageIdentifier = ctx.Guild.Id + "/" + ctx.User.Id + "/" + fileName;
			
			Task<Image> req = StableDiffusionInterface.RequestImg2Img(image, settings, request, imageIdentifier);
			Task progress = Task.Run(async () =>
			{
				while (true)
				{
					string progress = await StableDiffusionInterface.RequestProgress(imageIdentifier);
					if (progress == "Request not in queue")
					{
						return;
					}
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Progress").WithDescription(progress)));

					await Task.Delay(1500);
				}
			});
			await Task.WhenAny(req, progress);
			Image img = await req;
			using (MemoryStream stream = new MemoryStream())
			{
				await img.SaveAsPngAsync(stream);
				stream.Position = 0;
				return await Bot.database.GetStorage<string>("Images", "Chunks").UploadAsync(imageIdentifier, fileName, stream, metadata: BsonMapper.Global.ToDocument(new ImageMetadata() { censored = metadata.censored, guildId = metadata.guildId, userId = metadata.userId, generatedTime = DateTime.Now, request = metadata.request, settings = settings, preset = preset }));
			}
		}
	}
}
