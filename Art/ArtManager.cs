using DSharpPlus.SlashCommands;
using ElGogh.Database;
using LiteDB;
using DSharpPlus.Entities;
using Polly;
using System.Text.Encodings.Web;

namespace ElGogh.Art
{
    class ArtManager
    {
		static string lastWorkflow = "";
		static string lastImageId = "";

		public async static Task<string> WaitForWorkflow(InteractionContext ctx, Dictionary<string, Node> workflow)
		{
			string requestId = await ComfyUIInterface.ProcessWorkflow(workflow);
			while (true)
			{
				string progress = await ComfyUIInterface.RequestProgress(requestId);
				if (progress == "Workflow not in queue" || progress == "")
				{
					while ((await Bot.database.GetStorage<string>("TempImages", "TempChunks").FindAsync(image => image.Id.StartsWith(requestId))).ToList().Count == 0) //Fix because like 1/30 calls are too fast for the data to get saved
					{
						Console.WriteLine("Waiting: " + requestId); //triggers if cached
						await Task.Delay(100);
					}
					break;
				}
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle(progress.Split("\n")[0]).WithDescription(progress.Split("\n").Last())));
				await Task.Delay(1500);
			}
			return requestId;
		}

		public async static Task<List<LiteFileInfo<string>>> AdvancedPrompt(InteractionContext ctx, Dictionary<string, Node> workflow, string workflowName, string prompt, string negativePrompt = "", string vae = "", string primaryLora = "", string secondaryLora = "", string tertiaryLora = "", double primaryLoraStrength = 1.0, double secondaryLoraStrength = 1.0, double tertiaryLoraStrength = 1.0, long width = -1, long height = -1, long seed = -1, long steps = -1, double cfg = -1, string sampler = "", string scheduler = "", string absolutePrompt = "", string absoluteNegativePrompt = "", long clipskip = 0)
		{
			
			if(seed == -1) seed = new Random().NextInt64(999999999999999);
			foreach (KeyValuePair<string, Node> pair in workflow)
			{
				if (pair.Value.class_type == "ElGoghPositivePrompt") workflow[pair.Key].inputs["text"] += ", " + prompt;
				if (pair.Value.class_type == "ElGoghNegativePrompt" && negativePrompt != "") workflow[pair.Key].inputs["text"] += ", " + negativePrompt;
				if (pair.Value.class_type == "ElGoghPositivePrompt" && absolutePrompt != "") workflow[pair.Key].inputs["text"] = absolutePrompt;
				if (pair.Value.class_type == "ElGoghNegativePrompt" && absoluteNegativePrompt != "") workflow[pair.Key].inputs["text"] = absoluteNegativePrompt;
				if (pair.Value.class_type == "ElGoghVAELoader" && vae != "") workflow[pair.Key].inputs["vae_name"] = vae;
				if (pair.Value.class_type == "ElGoghPrimaryLoraLoader" && primaryLora != "") { workflow[pair.Key].inputs["lora_name"] = primaryLora + ".safetensors"; workflow[pair.Key].inputs["strength_model"] = primaryLoraStrength; workflow[pair.Key].inputs["strength_clip"] = primaryLoraStrength; }
				if (pair.Value.class_type == "ElGoghSecondaryLoraLoader" && secondaryLora != "") { workflow[pair.Key].inputs["lora_name"] = secondaryLora + ".safetensors"; workflow[pair.Key].inputs["strength_model"] = secondaryLoraStrength; workflow[pair.Key].inputs["strength_clip"] = secondaryLoraStrength; }
				if (pair.Value.class_type == "ElGoghTertiaryLoraLoader" && tertiaryLora != "") { workflow[pair.Key].inputs["lora_name"] = tertiaryLora + ".safetensors"; workflow[pair.Key].inputs["strength_model"] = tertiaryLoraStrength; workflow[pair.Key].inputs["strength_clip"] = tertiaryLoraStrength; }
				if (pair.Value.class_type == "ElGoghEmptyLatentImage" && width != -1) workflow[pair.Key].inputs["width"] = width;
				if (pair.Value.class_type == "ElGoghEmptyLatentImage" && height != -1) workflow[pair.Key].inputs["height"] = height;
				if (pair.Value.class_type == "ElGoghKSamplerAdvanced" && seed != -1) workflow[pair.Key].inputs["noise_seed"] = seed;
				if (pair.Value.class_type == "ElGoghKSamplerAdvanced" && steps != -1) workflow[pair.Key].inputs["steps"] = steps;
				if (pair.Value.class_type == "ElGoghKSamplerAdvanced" && cfg != -1) workflow[pair.Key].inputs["cfg"] = cfg;
				if (pair.Value.class_type == "ElGoghKSamplerAdvanced" && sampler != "") workflow[pair.Key].inputs["sampler_name"] = sampler;
				if (pair.Value.class_type == "ElGoghKSamplerAdvanced" && scheduler != "") workflow[pair.Key].inputs["scheduler"] = scheduler;
				if (pair.Value.class_type == "ElGoghCLIPSetLastLayer" && clipskip != 0) workflow[pair.Key].inputs["stop_at_clip_layer"] = clipskip;

			}
			if (lastWorkflow == System.Text.Json.JsonSerializer.Serialize(workflow, new System.Text.Json.JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) //TODO make this program start ComyUI so that comfyui wont have cache while we dont
			{
				return new List<LiteFileInfo<string>>() { await Bot.database.GetStorage<string>("Images", "ImageChunks").FindByIdAsync(lastImageId) }; //TODO If multiple images then the lastImageId wont work
			} else
			{
				lastWorkflow = System.Text.Json.JsonSerializer.Serialize(workflow, new System.Text.Json.JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
				string requestId = await WaitForWorkflow(ctx, workflow);
				List<LiteFileInfo<string>> images = (await Bot.database.GetStorage<string>("TempImages", "TempChunks").FindAsync(image => image.Id.StartsWith(requestId))).ToList();
				List<LiteFileInfo<string>> savedImages = new List<LiteFileInfo<string>>();
				foreach (LiteFileInfo<string> img in images)
				{
					string fileName = Guid.NewGuid() + ".png";
					using (LiteFileStream<string> stream = img.OpenRead())
					{
						ImageMetadata data = new ImageMetadata() { nsfw = BsonMapper.Global.ToObject<ImageMetadata>(img.Metadata).nsfw, creationDate = DateTime.Now, guildId = ctx.Guild.Id, userId = ctx.User.Id, workflow = workflow, workflowName = workflowName };
						lastImageId = ctx.Guild.Id + "/" + ctx.User.Id + "/" + fileName;
						savedImages.Add(await Bot.database.GetStorage<string>("Images", "ImageChunks").UploadAsync(lastImageId, fileName, stream, metadata: BsonMapper.Global.ToDocument(data)));
					}
					await Bot.database.GetStorage<string>("TempImages", "TempChunks").DeleteAsync(img.Id);
				}
				return savedImages;
			}
		}
	}
}
