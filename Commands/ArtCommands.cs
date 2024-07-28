using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using ElGogh.Art;
using ElGogh.Commands.ChoiceProviders;
using ElGogh.Database;
using LiteDB;
using System.Runtime.InteropServices;
using System.Text;

namespace ElGogh.Commands
{
	class ArtCommands : ApplicationCommandModule //TODO Lora and model names should have clear [XL] in the beginning
	{
		public static async Task<bool> CheckHomeChannel(InteractionContext ctx)
		{
			HomeChannel channel = await Bot.database.GetCollection<HomeChannel>().FindOneAsync(x => x.guildId == ctx.Guild.Id);

			if (channel == null) return false;
			if (channel.channelId != ctx.Channel.Id)
			{
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hey, I live in " + ctx.Guild.GetChannel(channel.channelId).Mention + " if you want to talk").AsEphemeral());
				return false;
			}
			return true;
		}

		[SlashCommand("Create", "Creates an image")]
		public async Task Create(InteractionContext ctx,
								[ChoiceProvider(typeof(WorkflowChoiceProvider))][Option("Workflow", "Load Specific Workflow")] string workflowName,
								[Option("Prompt", "The positive prompt, aka. what you want")] string prompt,
								[Option("NegativePrompt", "The negative prompt, aka. what you don't want")] string negativePrompt = "",
								[Choice("512", 512)][Choice("768", 768)][Choice("960", 960)][Choice("1024", 1024)][Choice("1280", 1280)][Choice("1536", 1536)][Choice("1856", 1856)][Option("Width", "The width of the image")] long width = -1,
								[Choice("512", 512)][Choice("768", 768)][Choice("960", 960)][Choice("1024", 1024)][Choice("1280", 1280)][Choice("1536", 1536)][Choice("1856", 1856)][Option("Height", "The height of the image")] long height = -1,
								[ChoiceProvider(typeof(LoraChoiceProvider))][Option("Primary_Lora", "Select the primary Lora. Make sure it matches the workflow type. AnythingXL -> XL lora")] string primaryLora = "",
								[ChoiceProvider(typeof(LoraChoiceProvider))][Option("Secondary_Lora", "Select the primary Lora")] string secondaryLora = "",
								[ChoiceProvider(typeof(LoraChoiceProvider))][Option("Tertiary_Lora", "Select the primary Lora")] string tertiaryLora = "",
								[Option("Primary_Lora_Strength", "The strength of the primary Lora. Between 0 and 1")] double primaryLoraStrength = 1,
								[Option("Secondary_Lora_Strength", "The strength of the secondary Lora. Between 0 and 1")] double secondaryLoraStrength = 1,
								[Option("Tertiary_Lora_Strength", "The strength of the tertiary Lora. Between 0 and 1")] double tertiaryLoraStrength = 1,
								[Choice("1", 1)][Choice("1.5", 1.5)][Choice("2", 2)][Choice("3", 3)][Choice("4", 4)][Choice("5", 5)][Choice("6", 6)][Choice("7", 7)][Choice("8", 8)][Choice("9", 9)][Choice("10", 10)][Choice("12", 12)][Option("CFG", "Select the CFG scale")] double cfg = -1,
								[Option("AbsolutePrompt", "Replaces the entire prompt")] string absolutePrompt = "",
								[Option("AbsoluteNegativePrompt", "Replaces the entire negative prompt")] string absoluteNegativePrompt = "",
								[Option("Seed", "What seed to use")] long seed = -1,
								[Choice("-1", -1)][Choice("-2", -2)][Option("Clip_Skip", "[WARNING] already preset to best amount. -1 = off")] long clipskip = 0,
								[Choice("5", 5)][Choice("10", 10)][Choice("15", 15)][Choice("20", 20)][Choice("30", 30)][Choice("40", 40)][Choice("50", 50)][Choice("60", 60)][Choice("70", 70)][Choice("80", 80)][Option("Sampling_Steps", "[WARNING] already preset to best amount")] long steps = -1,
								[Choice("Euler", "euler")][Choice("Euler Ancestral", "euler_ancestral")][Choice("DPM++ SDE", "dpmpp_sde")][Choice("DPM++ 2M", "dpmpp_2m")][Choice("DPM++ 2M SDE", "dpmpp_2m_sde")][Option("Sampler", "[WARNING] already preset to best amount")] string sampler = "",
								[ChoiceProvider(typeof(VAEChoiceProvider))][Option("VAE", "[WARNING] already preset to best amount")] string vae = "",
								[Choice("Normal", "normal")][Choice("Karras", "karras")][Option("Scheduler", "[WARNING] already preset to best amount")] string scheduler = ""
								)
		{
			if(!(await CheckHomeChannel(ctx))) return;
			await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);
			try
			{
				LiteFileInfo<string> fileInfo = await Bot.database.GetStorage<string>("Workflows", "WorkflowChunks").FindByIdAsync(workflowName);
				Dictionary<string, Node> workflow = BsonMapper.Global.Deserialize<Dictionary<string, Node>>(JsonSerializer.Deserialize(new StreamReader(fileInfo.OpenRead()).ReadToEnd()));
				List<LiteFileInfo<string>> images = await ArtManager.AdvancedPrompt(ctx, workflow, workflowName, prompt, negativePrompt, vae, primaryLora, secondaryLora, tertiaryLora, primaryLoraStrength, secondaryLoraStrength, tertiaryLoraStrength, width, height, seed, steps, cfg, sampler, scheduler, absolutePrompt, absoluteNegativePrompt, clipskip);
				DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
				foreach (LiteFileInfo<string> image in images)
				{
					using (Stream stream = image.OpenRead()) 
					{
						if (BsonMapper.Global.ToObject<ImageMetadata>(image.Metadata).nsfw)
						{
							builder.AddFile("SPOILER_" + image.Filename, stream);
						} else
						{
							builder.AddFile(image.Filename, stream);
						}
					}

				}
				await ctx.EditResponseAsync(builder);
			} catch (Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(ex.Message)); 
				if(ex is BadRequestException ex2)
				{
					Console.Error.WriteLine(ex2.JsonMessage); //Pesky json erros when requests to discord 
				}
			}
		}

		/*[ContextMenu(DSharpPlus.ApplicationCommandType.MessageContextMenu, "Test")]
		public async Task testing(ContextMenuContext ctx)
		{
			InteractivityExtension interactivity = ctx.Client.GetInteractivity();
			await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.Modal, new DiscordInteractionResponseBuilder().WithTitle("Test").WithCustomId("registerModal").AddComponents(new TextInputComponent("Label", "customId")));
			await interactivity.WaitForModalAsync("registerModal");
		}*/

		[ContextMenu(DSharpPlus.ApplicationCommandType.MessageContextMenu, "View Info")]
		[RequireAttributes.RequireImage]
		[RequireAttributes.RequireSelfAuthor]
		public async Task ViewInfo(ContextMenuContext ctx)
		{
			try //TODO check if file is in database so no error
			{
				string fileName = ctx.TargetMessage.Attachments[0].FileName;
				if (fileName.StartsWith("SPOILER_")) fileName = fileName.Substring(8);
				ImageMetadata metadata = BsonMapper.Global.ToObject<ImageMetadata>((await Bot.database.GetStorage<string>("Images", "ImageChunks").FindByIdAsync(ctx.Guild.Id + "/" + ctx.TargetMessage.Interaction.User.Id + "/" + fileName)).Metadata);
				string prompt = "";
				string negativePrompt = "";
				string vae = "";
				string primaryLora = "";
				string secondaryLora = "";
				string tertiaryLora = "";
				double primaryLoraStrength = 0f;
				double secondaryLoraStrength = 0f;
				double tertiaryLoraStrength = 0f;
				long width = 0;
				long height = 0;
				long seed = 0;
				long steps = 0;
				double cfg = 0;
				string sampler = "";
				string scheduler = "";
				long clipskip = 0;
				Dictionary<string, Node> workflow = metadata.workflow;
				foreach (KeyValuePair<string, Node> pair in workflow)
				{
					if (pair.Value.class_type == "ElGoghPositivePrompt") prompt = (string)workflow[pair.Key].inputs["text"];
					if (pair.Value.class_type == "ElGoghNegativePrompt") negativePrompt = (string)workflow[pair.Key].inputs["text"];
					if (pair.Value.class_type == "ElGoghVAELoader") vae = (string)workflow[pair.Key].inputs["vae_name"];
					if (pair.Value.class_type == "ElGoghPrimaryLoraLoader") { primaryLora = (string)workflow[pair.Key].inputs["lora_name"]; primaryLoraStrength = Convert.ToInt64(workflow[pair.Key].inputs["strength_model"]); }
					if (pair.Value.class_type == "ElGoghSecondaryLoraLoader") { secondaryLora = (string)workflow[pair.Key].inputs["lora_name"]; secondaryLoraStrength = Convert.ToInt64(workflow[pair.Key].inputs["strength_model"]); }
					if (pair.Value.class_type == "ElGoghTertiaryLoraLoader") { tertiaryLora = (string)workflow[pair.Key].inputs["lora_name"]; tertiaryLoraStrength = Convert.ToInt64(workflow[pair.Key].inputs["strength_model"]); }
					if (pair.Value.class_type == "ElGoghCLIPSetLastLayer") clipskip = Convert.ToInt64(workflow[pair.Key].inputs["stop_at_clip_layer"]);
					if (pair.Value.class_type == "ElGoghEmptyLatentImage")
					{
						width = Convert.ToInt64(workflow[pair.Key].inputs["width"]);
						height = Convert.ToInt64(workflow[pair.Key].inputs["height"]);
					}
					if (pair.Value.class_type == "ElGoghKSamplerAdvanced")
					{
						seed = Convert.ToInt64(workflow[pair.Key].inputs["noise_seed"]);
						steps = Convert.ToInt64(workflow[pair.Key].inputs["steps"]);
						cfg = Convert.ToInt64(workflow[pair.Key].inputs["cfg"]);
						sampler = (string)workflow[pair.Key].inputs["sampler_name"];
						scheduler = (string)workflow[pair.Key].inputs["scheduler"];
					}
				}
				//TODO check prompt length and split into more embeds if required (only if prompt+negPrompt exceeds 4096 characters)
				DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
				builder.WithAuthor(ctx.TargetMessage.Interaction.User.GlobalName, iconUrl: ctx.TargetMessage.Interaction.User.AvatarUrl);
				builder.AddField("General Info", "```ansi\r\n\u001b[2;34m\u001b[1;34mWorkflow: \u001b[0m\u001b[2;34m\u001b[0m\u001b[1;2m\u001b[1;33m"+metadata.workflowName+"\r\n\u001b[1;34mDate:     \u001b[1;33m"+metadata.creationDate.ToString("yyyy-MM-dd")+"\r\n\u001b[1;34mTime:     \u001b[1;33m"+metadata.creationDate.ToString("HH:mm")+"\u001b[0m\u001b[1;34m\u001b[0m\u001b[1;33m\u001b[0m\u001b[1;34m\u001b[0m\u001b[1;33m\u001b[0m\u001b[0m\r\n```");
				builder.AddField("Settings", "```ansi\r\n\u001b[2;34m\u001b[1;34mWidth:         \u001b[1;33m"+width+"\u001b[0m\u001b[1;34m\r\nHeight:        \u001b[1;33m"+height+"\u001b[0m\u001b[1;34m\r\nPrimaryLora:   \u001b[1;33m"+primaryLora+"  (\u001b[1;32m"+primaryLoraStrength+"\u001b[0m\u001b[1;33m)\u001b[0m\u001b[1;34m\r\nSecondaryLora: \u001b[1;33m"+secondaryLora+"  (\u001b[1;32m"+secondaryLoraStrength+"\u001b[0m\u001b[1;33m)\u001b[0m\u001b[1;34m\r\nTertiaryLora:  \u001b[1;33m"+tertiaryLora+"  (\u001b[1;32m"+tertiaryLoraStrength+"\u001b[0m\u001b[1;33m)\u001b[0m\u001b[1;34m\r\nSteps:         \u001b[1;33m"+steps+"\u001b[0m\u001b[1;34m\r\nCFG:           \u001b[1;33m"+cfg+"\u001b[0m\u001b[1;34m\r\nSampler:       \u001b[1;33m"+sampler+"\u001b[0m\u001b[1;34m\r\nVAE:           \u001b[1;33m"+vae+"\u001b[0m\u001b[1;34m\r\nScheduler:     \u001b[1;33m"+scheduler+"\u001b[0m\u001b[1;34m\r\nClip Skip:     \u001b[1;33m"+clipskip+"\u001b[0m\u001b[1;34m\r\nSeed:          \u001b[1;33m"+seed+"\u001b[0m\u001b[1;34m\u001b[0m\u001b[2;34m\u001b[0m\r\n\r\n```");
				DiscordEmbedBuilder builder2 = new DiscordEmbedBuilder();
				builder2.AddField("Prompts", "```ansi\r\n\u001b[2;34m\u001b[1;34mPrompt: \u001b[1;33m"+prompt+"\u001b[0m\u001b[1;34m\r\nNegative Prompt: \u001b[1;33m"+negativePrompt+"\u001b[0m\u001b[1;34m\u001b[0m\u001b[2;34m\u001b[0m\r\n\r\n```");
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbeds(new List<DiscordEmbed>() { builder, builder2, new DiscordEmbedBuilder().WithImageUrl(ctx.TargetMessage.Attachments[0].Url) }));
			} catch (Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());
				await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(ex.Message));
			}
		}
	}
}
