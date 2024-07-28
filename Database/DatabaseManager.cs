
using LiteDB;
using LiteDB.Async;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.IO;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ElGogh.Database
{
	class DatabaseManager
	{
		/*
		 * Filestorages:
		 *	TempImages
		 *	TempChunks
		 *	
		 *	Images
		 *	ImageChunks
		 *	
		 *	Workflows (Only for default workflows or workflows added through discord (maybe new function in the future))
		 *	WorkflowChunks
		 * 
		 */

		//Change whenever database structure changes, and update migrateDatabase()
		public const int DatabaseVersion = 1;
		public static LiteDatabaseAsync initDatabase(string name)
		{
			BsonMapper.Global.EmptyStringToNull = false;
			BsonMapper.Global.TrimWhitespace = false;
			LiteDatabaseAsync database = new LiteDatabaseAsync("ElGogh.db");
			database = migrateDatabase(database);
			
			Console.WriteLine("Initializing database");
			Dictionary<string, Byte[]> workflows = new Dictionary<string, byte[]> {
				{ "AnythingXL-ElGogh-API.json", Properties.Resources.AnythingXL_ElGogh_API },
				{ "AnimagineXL-ElGogh-API.json", Properties.Resources.animagineXL_ElGogh_API },
				{ "a7b3-ElGogh-API.json", Properties.Resources.a7b3_ElGogh_API },
				{ "aZovyaRPGArtistTools-ElGogh-API.json", Properties.Resources.aZovyaRPGArtistTools_ElGogh_API },
				{ "Counterfeit-ElGogh-API.json", Properties.Resources.Counterfeit_ElGogh_API },
				{ "ForRealXL-ElGogh-API.json", Properties.Resources.forRealXL_ElGogh_API },
				{ "NightSkyYozoraStyle-ElGogh-API.json", Properties.Resources.nightSkyYozoraStyle_ElGogh_API },
				{ "PonyDiffusion-ElGogh-API.json", Properties.Resources.ponyDiffusion_ElGogh_API },
				{ "RealisticVision-ElGogh-API.json", Properties.Resources.realisticVision_ElGogh_API },
				{ "TMND-Mix-ElGogh-API.json", Properties.Resources.tmndMix_ElGogh_API },
				{ "UnstableDiffusers-ElGogh-API.json", Properties.Resources.UnstableDiffusers_ElGogh_API }};
			//Always initialize workflows incase update
			Console.WriteLine("Resetting default database");
			foreach (LiteFileInfo<string> workflow in database.GetStorage<string>("Workflows","WorkflowChunks").FindAsync(x => x.Id.StartsWith("default/")).GetAwaiter().GetResult())
			{
				database.GetStorage<string>("Workflows", "WorkflowChunks").DeleteAsync(workflow.Id).GetAwaiter().GetResult();
			}
			Console.WriteLine("Done");
			Console.WriteLine("Restoring default database");
			foreach (KeyValuePair<string, Byte[]> workflow in workflows)
			{
				if (database.GetStorage<string>("Workflows", "WorkflowChunks").FindByIdAsync("default/" + workflow.Key).GetAwaiter().GetResult() == null)
				{
					Console.WriteLine("Missing " + workflow.Key + ". Adding to database");
					MemoryStream stream = new MemoryStream(workflow.Value);
					workflow.Value.CopyTo(workflow.Value, 0);
					stream.Position = 0;
					LiteFileInfo<string> file = database.GetStorage<string>("Workflows", "WorkflowChunks").UploadAsync("default/" + workflow.Key, workflow.Key, stream).GetAwaiter().GetResult();
					stream.Close();
				}
			}
			Console.WriteLine("Done");
			

			//TOOD: add files from workflow directory (where users can put their own)
			return database;
		}


		private static LiteDatabaseAsync migrateDatabase(LiteDatabaseAsync database)
		{
			if (database.UserVersion == 0)
			{
				Console.WriteLine("Migrating to version (1/" + DatabaseVersion + "). This might take a while");
				database.RenameCollectionAsync("Images", "OldImages");
				IEnumerable<LiteFileInfo<string>> images = database.GetStorage<string>("OldImages", "Chunks").FindAllAsync().GetAwaiter().GetResult();
				int i = 0;
				int max = images.Count();
				foreach (LiteFileInfo<string> image in images)
				{
					i++;
					Console.WriteLine("Converting image (" + i + "/" + max + ")");
					Dictionary<string, Node> workflow = BsonMapper.Global.Deserialize<Dictionary<string, Node>>(JsonSerializer.Deserialize(Encoding.UTF8.GetString(Properties.Resources.Automatic1111_ElGogh_API,0, Properties.Resources.Automatic1111_ElGogh_API.Length)));
					foreach (KeyValuePair<string, Node> pair in workflow)
					{
						if (pair.Value.class_type == "ElGoghCheckpointLoaderSimple") workflow[pair.Key].inputs["ckpt_name"] = image.Metadata["settings"]["sd_model_checkpoint"].AsString + ".safetensors";
						if (pair.Value.class_type == "ElGoghVAELoader") workflow[pair.Key].inputs["vae_name"] = image.Metadata["settings"]["sd_vae"].AsString;
						if (pair.Value.class_type == "ElGoghCLIPSetLastLayer") workflow[pair.Key].inputs["stop_at_clip_layer"] = image.Metadata["settings"]["CLIP_stop_at_last_layers"].AsInt64 == 0 ? -1 : -2;
						if (pair.Value.class_type == "ElGoghPositivePrompt") workflow[pair.Key].inputs["text"] = image.Metadata["request"]["prompt"].AsString;
						if (pair.Value.class_type == "ElGoghNegativePrompt") workflow[pair.Key].inputs["text"] += image.Metadata["request"]["negative_prompt"].AsString;
						if (pair.Value.class_type == "ElGoghEmptyLatentImage") workflow[pair.Key].inputs["width"] = image.Metadata["request"]["width"].AsInt64;
						if (pair.Value.class_type == "ElGoghEmptyLatentImage") workflow[pair.Key].inputs["height"] = image.Metadata["request"]["height"].AsInt64;
						if (pair.Value.class_type == "ElGoghKSamplerAdvanced")
						{
							workflow[pair.Key].inputs["noise_seed"] = image.Metadata["request"]["seed"].AsInt64;
							workflow[pair.Key].inputs["steps"] = image.Metadata["request"]["steps"].AsInt64;
							workflow[pair.Key].inputs["cfg"] = image.Metadata["request"]["cfg_scale"].AsDouble;
							string sampler = image.Metadata["request"]["sampler_name"].AsString;
							switch (sampler)
							{
								case "Euler a":
									workflow[pair.Key].inputs["sampler_name"] = "euler_ancestral";
									break;
								case "DPM++ 2M":
									workflow[pair.Key].inputs["sampler_name"] = "dpmpp_2m";
									break;
								case "DPM++ 2S a Karras":
									workflow[pair.Key].inputs["sampler_name"] = "dpmpp_2s_ancestral";
									workflow[pair.Key].inputs["scheduler"] = "karras";
									break;
								case "DPM++ SDE Karras":
									workflow[pair.Key].inputs["sampler_name"] = "dpmpp_sde";
									workflow[pair.Key].inputs["scheduler"] = "karras";
									break;
							}
						}
					}
					ImageMetadata metadata = new ImageMetadata() {workflow = workflow,  workflowName = "[LEGACY]" + image.Metadata["settings"]["sd_model_checkpoint"].AsString, nsfw = image.Metadata["censored"].AsBoolean, creationDate = image.Metadata["generatedTime"].AsDateTime, guildId = Convert.ToUInt64(image.Metadata["guildId"].AsInt64), userId = Convert.ToUInt64(image.Metadata["userId"].AsInt64) };
					using (LiteFileStream<string> stream = image.OpenRead())
					{
						
						database.GetStorage<string>("Images", "ImageChunks").UploadAsync(image.Id, image.Filename, stream, metadata: BsonMapper.Global.ToDocument(metadata)).GetAwaiter().GetResult();
					}
					database.GetStorage<string>("OldImages", "Chunks").DeleteAsync(image.Id).GetAwaiter().GetResult();
				}
				database.DropCollectionAsync("OldImages").GetAwaiter().GetResult();
				database.DropCollectionAsync("Chunks").GetAwaiter().GetResult();
				database.DropCollectionAsync("Img2ImgPreset").GetAwaiter().GetResult();
				database.DropCollectionAsync("Lora").GetAwaiter().GetResult();
				database.DropCollectionAsync("Model").GetAwaiter().GetResult();
				database.DropCollectionAsync("Txt2ImgPreset").GetAwaiter().GetResult();
				database.UserVersion = 1;
			}

			return database;
		}
	}
}
