using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using El_Gogh.Art;
using El_Gogh.Database;
using LiteDB;
using System.Reflection;


namespace El_Gogh.Commands
{
	class ArtCommands : ApplicationCommandModule
	{
		[SlashCommand("Anime", "Create an Anime style image using this simple preset")]
		public async Task Anime(InteractionContext ctx, [Option("prompt", "The input prompt")] string prompt, [Option("negativeprompt", "The negative prompt")] string negativePrompt = "", [Option("seed", "The image seed (no, not that kind of seed)")] long seed = -1)
		{
			if ((await Bot.database.GetCollection<HomeChannel>().FindOneAsync(x => x.guildId == ctx.Guild.Id && x.channelId == ctx.Channel.Id)) == null)
			{
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hey kid... just a tip. I live in " + ctx.Guild.GetChannel((await Bot.database.GetCollection<HomeChannel>().FindOneAsync(x => x.guildId == ctx.Guild.Id)).channelId).Mention).AsEphemeral());
				return;
			}
			await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);
			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					LiteFileInfo<string> file = await ArtManager.GenerateSimplePresetImage("Anime", prompt, negativePrompt, seed, ctx);
					file.CopyTo(ms);
					ms.Position = 0;
					string fileName = file.Filename;
					ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>(file.Metadata);
					if (metadata.censored) fileName = "SPOILER_" + fileName;
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(fileName, ms));
				}
			}
			catch (Exception e)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(e.Message));
			}
		}
		[SlashCommand("Fantasy", "Create a Fantasy style image using this simple preset")]
		public async Task Fantasy(InteractionContext ctx, [Option("prompt", "The input prompt")] string prompt, [Option("negativeprompt", "The negative prompt")] string negativePrompt = "", [Option("seed", "The image seed (no, not that kind of seed)")] long seed = -1)
		{
			if ((await Bot.database.GetCollection<HomeChannel>().FindOneAsync(x => x.guildId == ctx.Guild.Id && x.channelId == ctx.Channel.Id)) == null)
			{
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hey kid... just a tip. I live in " + ctx.Guild.GetChannel((await Bot.database.GetCollection<HomeChannel>().FindOneAsync(x => x.guildId == ctx.Guild.Id)).channelId).Mention).AsEphemeral());
				return;
			}
			await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);
			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					LiteFileInfo<string> file = await ArtManager.GenerateSimplePresetImage("Fantasy", prompt, negativePrompt, seed, ctx);
					file.CopyTo(ms);
					ms.Position = 0;
					string fileName = file.Filename;
					ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>(file.Metadata);
					if (metadata.censored) fileName = "SPOILER_" + fileName;
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(fileName, ms));
				}
			}
			catch (Exception e)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(e.Message));
			}
		}
		[SlashCommand("Create", "Create an image")] //VAE in the future, but not really that important?
		public async Task Create(InteractionContext ctx,
								[ChoiceProvider(typeof(PresetChoiceProvider))][Option("preset", "Load specific preset")] string preset,
								[ChoiceProvider(typeof(ModelChoiceProvider))][Option("model", "Select specific model")] string model,
								[Option("prompt", "The input prompt")] string prompt,
								[Option("negativeprompt", "The negative prompt")] string negativeprompt = "",
								[ChoiceProvider(typeof(LoraChoiceProvider))][Option("primary_lora", "Select the primary lora")] string primaryLora = "",
								[ChoiceProvider(typeof(LoraChoiceProvider))][Option("secondary_lora", "Select the secondary lora")] string secondaryLora = "",
								[ChoiceProvider(typeof(LoraChoiceProvider))][Option("tertiary_lora", "Select the tertiary lora")] string tertiaryLora = "",
								[Choice("Euler a - Fast, lower step counts ~20 steps works fine","Euler a")][Choice("DPM++ 2M - Fast","DPM++ 2M")][Choice("DPM++ 2S a Karras - Slower and requires a bit higher step counts","DPM++ 2S a Karras")][Choice("DPM++ SDE Karras - Slower","DPM++ SDE Karras")]
								[Option("sampler","What sampler to use. Affects performance. Depends on number of steps")] string sampler = "",
								[Choice("20",20)][Choice("30",30)][Choice("40",40)][Choice("50",50)][Choice("150",150)]
								[Option("steps", "How many steps to generate an image. Heavily depends on the sampler used")] long steps = -1,
								[Choice("512",512)][Choice("768",768)][Choice("1024",1024)][Choice("1536",1536)]
								[Option("width", "The width of the image")] long width = -1,
								[Choice("512",512)][Choice("768",768)][Choice("1024",1024)][Choice("1536",1536)]
								[Option("height", "The height of the image")] long height = -1,
								[Option("seed", "The image seed")] long seed = -1,
								[Option("tiling", "Whether to make the image tilable")] bool tiling = false,
								[Choice("5",5)][Choice("6",6)][Choice("7",7)][Choice("8",8)][Choice("9",9)][Choice("10",10)]
								[Option("cfg_scale", "How strongly you want to enforce your prompt. Default is 7")] long cfg_scale = -1,
								[Choice("0",0)][Choice("1",1)][Choice("2",2)]
								[Option("clip_skip", "Very specific option. Google it yourself")] long clip_skip = -1)
		{
			if ((await Bot.database.GetCollection<HomeChannel>().FindOneAsync(x => x.guildId == ctx.Guild.Id && x.channelId == ctx.Channel.Id)) == null)
			{
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hey kid... just a tip. I live in " + ctx.Guild.GetChannel((await Bot.database.GetCollection<HomeChannel>().FindOneAsync(x => x.guildId == ctx.Guild.Id)).channelId).Mention).AsEphemeral());
				return;
			}
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					LiteFileInfo<string> file = await ArtManager.GenerateImage(ctx, preset, model, prompt, negativeprompt, primaryLora, secondaryLora, tertiaryLora, sampler, steps, width, height, seed, tiling, cfg_scale, clip_skip);
					file.CopyTo(ms);
					ms.Position = 0;
					string fileName = file.Filename;
					ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>(file.Metadata);
					if (metadata.censored) fileName = "SPOILER_" + fileName;
					DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(fileName, ms));
				}
			}
			catch (Exception e)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(e.Message));
			}
		}

		/*[ContextMenu(ApplicationCommandType.MessageContextMenu, "2x Anime Upscale")]
		[RequireSelfMessageAuthor]
		[RequireImage]
		public async Task ContextUpscale(ContextMenuContext ctx)
		{
			LiteFileInfo<string> image = await Bot.database.GetStorage<string>("Images", "Chunks").FindByIdAsync(ctx.Guild.Id + "/" + ctx.TargetMessage.Interaction.User.Id + "/" + ctx.TargetMessage.Attachments[0].FileName.Replace("SPOILER_",""));
			if (image == null)
			{
				Console.WriteLine(ctx.Guild.Id + "/" + ctx.TargetMessage.Interaction.User.Id + "/" + ctx.TargetMessage.Attachments[0].FileName);
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Image not found in database").AsEphemeral());
				return;
			}
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					LiteFileInfo<string> file = await ArtManager.UpscaleImage(ctx, image, "Anime");
					file.CopyTo(ms);
					ms.Position = 0;
					string fileName = file.Filename;
					ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>(file.Metadata);
					if (metadata.censored) fileName = "SPOILER_" + fileName;
					DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(fileName, ms));
				}
				
			} catch (Exception e)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(e.Message));
			}
		}*/

		[SlashCommand("Interrupt", "Interrupts the current execution")]
		public async Task Interrupt(InteractionContext ctx)
		{
			await StableDiffusionInterface.Interrupt();
			await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Sent interruption request. This may take a little while"));
		}

		[SlashCommand("Upscale", "Upscale an image 2x")]
		public async Task Upscale(InteractionContext ctx,
								 [Option("messagelink", "The link to the message. Select image and click 'Copy Link'")] string imageLink,
								 [ChoiceProvider(typeof(UpscalePresetChoiceProvider))][Option("preset", "Load specific preset")] string preset,
								 [Choice("0.1",0.1)][Choice("0.15",0.15)][Choice("0.2",0.2)][Choice("0.25",0.25)][Choice("0.3",0.3)][Choice("0.35",0.35)][Choice("0.4",0.4)]
								 [Option("denoising_strength","How large changes are to be made.")] double denoisingStrength,
								 [Choice("4x-Ultrasharp",3)][Choice("R-ESRGAN 4x+ Anime6B",7)]
								 [Option("upscaler","Override the upscaler")]long upscaler = -1,
								 [Choice("1x (If you just want to use img2img i guess)",1)][Choice("2x",2)]
								 [Option("scale","The scale to use")] double scale = -1,
								 [Option("prompt", "Override the preset prompt")] string prompt = "",
								 [Option("negativeprompt", "Override the preset negative prompt")] string negativePrompt = "",
								 [Option("steps", "Override the preset amount of steps to use")] long steps = -1,
								 [Option("seed", "The seed to use (still not that kind of seed)")] long seed = -1,
								 [Option("sampler", "Override the sampler which was used to generate the image")]string sampler = "",
								 [ChoiceProvider(typeof(ModelChoiceProvider))][Option("model", "Override the model which was used to generate the image")] string model = "")

		{
			ulong messageId;
			if (!imageLink.StartsWith("https://discord.com/channels/") || !ulong.TryParse(imageLink.Split("/").Last(), out messageId))
			{
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("That isn't a valid message link").AsEphemeral());
				return;
			}
			DiscordMessage message = await ctx.Channel.GetMessageAsync(ulong.Parse(imageLink.Split("/").Last()));
			if (message == null)
			{
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Can't access that message").AsEphemeral());
				return;
			}
			LiteFileInfo<string> image = await Bot.database.GetStorage<string>("Images", "Chunks").FindByIdAsync(ctx.Guild.Id + "/" + message.Interaction.User.Id + "/" + message.Attachments[0].FileName.Replace("SPOILER_", ""));
			if (image == null)
			{
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Image not found in database").AsEphemeral());
				return;
			}
			await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					LiteFileInfo<string> file = await ArtManager.UpscaleImage(ctx, image, preset, denoisingStrength, scale, upscaler, prompt, negativePrompt, steps, seed, sampler, model);
					file.CopyTo(ms);
					ms.Position = 0;
					string fileName = file.Filename;
					ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>(file.Metadata);
					if (metadata.censored) fileName = "SPOILER_" + fileName;
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(fileName, ms));
				}

			}
			catch (Exception e)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(e.Message));
			}
		}
		[ContextMenu(ApplicationCommandType.MessageContextMenu, "View Info")]
		[RequireSelfMessageAuthor]
		[RequireImage]
		public async Task ViewInfo(ContextMenuContext ctx) //TODO Fix when prompt is too big to show
		{
			string fileName = ctx.TargetMessage.Attachments[0].FileName;
			if (fileName.StartsWith("SPOILER_")) fileName = fileName.Substring(8);
			ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>((await Bot.database.GetStorage<string>("Images", "Chunks").FindByIdAsync(ctx.Guild.Id + "/" + ctx.TargetMessage.Interaction.User.Id + "/" + fileName)).Metadata);

			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			builder.WithAuthor(ctx.TargetMessage.Interaction.User.GlobalName, iconUrl:ctx.TargetMessage.Interaction.User.AvatarUrl);
			builder.AddField("General Info", "```ansi\r\n\u001b[2;30m\u001b[1;30m\u001b[1;30m\u001b[0;30m\u001b[0;33m\u001b[1;33m\u001b[1;32m\u001b[1;34m\u001b[1;34m\u001b[0m\u001b[1;34m\u001b[0m\u001b[1;32m\u001b[0m\u001b[1;33m\u001b[0m\u001b[0;33m\u001b[0m\u001b[0;30m\u001b[0m\u001b[1;30m\u001b[0m\u001b[1;30m\u001b[0m\u001b[2;30m\u001b[0m\u001b[0;2m\u001b[0m\u001b[0;2m\u001b[0;34m\u001b[1;34mPreset:\u001b[0m\u001b[0;34m\u001b[0m   \u001b[0;33m\u001b[1;33m" + metadata.preset + "\u001b[0m\u001b[0;33m\u001b[0m\r\n\u001b[0;34m\u001b[1;34mDate:\u001b[0m\u001b[0;34m\u001b[0m     \u001b[0;33m\u001b[1;33m" + metadata.generatedTime.ToString("yyyy-MM-dd") + "\u001b[0m\u001b[0;33m\u001b[0m\r\n\u001b[0;34m\u001b[1;34mTime:\u001b[0m\u001b[0;34m\u001b[0m     \u001b[0;33m\u001b[1;33m" + metadata.generatedTime.ToString("HH:mm") + "\u001b[0m\u001b[0;33m\u001b[0m\u001b[0m\u001b[2;31m\u001b[2;36m\u001b[2;33m\u001b[0m\u001b[2;36m\u001b[0m\u001b[2;31m\u001b[0m\u001b[0;2m\u001b[0m\r\n```");
			builder.AddField("Settings", "```ansi\r\n\u001b[2;34m\u001b[1;34mModel:\u001b[0m\u001b[2;34m\u001b[0m    \u001b[2;33m\u001b[1;33m" + metadata.settings.sd_model_checkpoint + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[2;34m\u001b[1;34mVAE:\u001b[0m\u001b[2;34m\u001b[0m      \u001b[2;33m\u001b[1;33m" + metadata.settings.sd_vae + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[2;34m\u001b[1;34mCLIP:\u001b[0m\u001b[2;34m\u001b[0m     \u001b[2;33m\u001b[1;33m" + metadata.settings.CLIP_stop_at_last_layers + "\u001b[0m\u001b[2;33m\u001b[0m\r\n```");
			builder.AddField("Parameters", "```ansi\r\n\u001b[2;34m\u001b[1;34mPrompt:\u001b[0m\u001b[2;34m\u001b[0m   \u001b[2;33m\u001b[1;33m" + metadata.request.prompt.Replace("\n", "").Replace("\r", "") + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[2;34m\u001b[1;34mNegative:\u001b[0m\u001b[2;34m\u001b[0m \u001b[2;33m\u001b[1;33m" + metadata.request.negative_prompt.Replace("\n", "").Replace("\r", "") + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[2;34m\u001b[1;34mSeed:\u001b[0m\u001b[2;34m\u001b[0m     \u001b[2;33m\u001b[1;33m" + metadata.request.seed + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[1;2m\u001b[1;34mSampler:\u001b[0m\u001b[0m  \u001b[2;33m\u001b[1;33m" + metadata.request.sampler_name + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[2;34m\u001b[1;34mSteps:\u001b[0m\u001b[2;34m\u001b[0m    \u001b[2;33m\u001b[1;33m" + metadata.request.steps + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[2;34m\u001b[1;34mWidth:\u001b[0m\u001b[2;34m\u001b[0m    \u001b[2;33m\u001b[1;33m" + metadata.request.width + "\u001b[0m\u001b[2;33m\u001b[0m\r\n\u001b[2;34m\u001b[1;34mHeight:\u001b[0m\u001b[2;34m\u001b[0m   \u001b[2;33m\u001b[1;33m" + metadata.request.height + "\u001b[0m\u001b[2;33m\u001b[0m\r\n```");
			await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbeds(new List<DiscordEmbed>() { builder, new DiscordEmbedBuilder().WithImageUrl(ctx.TargetMessage.Attachments[0].Url)}));
		}

		[SlashCommand("AddModel","Enables the model and sets the description")]
		public async Task AddModel(InteractionContext ctx, [ChoiceProvider(typeof(UninitializedModelChoiceProvider))][Option("model", "Select the model to add")] string model, [Option("description","The model description")]string description)
		{
			Model m = new Model() { name=model, description=description };
			await Bot.database.GetCollection<Model>().InsertAsync(m);
			await ArtDatabaseInitializer.UpdateModels();
			await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{m.name} has now been added with the description: {description}").AsEphemeral());
		}
		[SlashCommand("EditModel", "Edits the model")]
		public async Task EditModel(InteractionContext ctx, [ChoiceProvider(typeof(ModelChoiceProvider))][Option("model", "Select the model to edit")] string model, [Option("description", "The new model description")] string description)
		{
			Model m = new Model() { name = model, description = description };
			await Bot.database.GetCollection<Model>().DeleteManyAsync(x => x.name == model);
			await Bot.database.GetCollection<Model>().InsertAsync(m);
			await ArtDatabaseInitializer.UpdateModels();
			await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{m.name} has now been edited with the description: {description}").AsEphemeral());
		}

		[SlashCommand("AddLora", "Enables the lora and sets the triggerword")]
		public async Task AddLora(InteractionContext ctx, [ChoiceProvider(typeof(UninitializedLoraChoiceProvider))][Option("lora", "Select the lora to create")] string lora, [Option("TriggerWord", "The trigger word. If it doesn't require any type: empty")] string triggerWord, [Option("Strength", "The strength of the lora. Usually between 0 and 1")] double strength)
		{
			Lora m = new Lora() { name = lora, strength = strength};
			m.triggerWord = triggerWord;
			if (triggerWord == "empty") m.triggerWord = "";
			await Bot.database.GetCollection<Lora>().InsertAsync(m);
			await ArtDatabaseInitializer.UpdateLoras();
			await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{m.name} has now been added with the trigger word: {triggerWord}, and the strength: {strength}").AsEphemeral());
		}

		[SlashCommand("EditLora", "Edits the lora")]
		public async Task EditLora(InteractionContext ctx, [ChoiceProvider(typeof(LoraChoiceProvider))][Option("lora", "Select the lora to edit")] string lora, [Option("TriggerWord", "The trigger word. If it doesn't require any type: empty")] string triggerWord, [Option("Strength", "The strength of the lora. Usually between 0 and 1")] double strength)
		{
			Lora m = new Lora() { name = lora, strength = strength };
			m.triggerWord = triggerWord;
			if (triggerWord == "empty") m.triggerWord = "";
			await Bot.database.GetCollection<Lora>().DeleteManyAsync(x => x.name == lora);
			await Bot.database.GetCollection<Lora>().InsertAsync(m);
			await ArtDatabaseInitializer.UpdateLoras();
			await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{m.name} has now been edited with the trigger word: {triggerWord}, and the strength: {strength}").AsEphemeral());
		}

		[SlashCommand("Refresh", "Refreshes loras and models. Don't use too often. Discord has a rate limit")]
		public async Task Refresh(InteractionContext ctx)
		{
			await ctx.Client.GetSlashCommands().RefreshCommands();
		}
	}
}
