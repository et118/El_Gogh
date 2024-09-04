using ElGogh.Database;
using System.Text.Encodings.Web;
using System.Text;
using Websocket.Client;
using System.Reactive.Linq;
using LiteDB;

namespace ElGogh.Art
{
	class ComfyUIInterface
	{
		/*
		 * API Endpointy
		 * extensions
		 * embeddings
		 * object_info/LoraLoader
		 * queue
		 * system_stats
		 * */
		public static string serverAddress = "localhost:8188";
		private static string clientId { get; } = Guid.NewGuid().ToString();
		private static HttpClient httpClient = new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
		private static WebsocketClient websocketClient;

		public static int queue = 0;
		public static string activeRequestId = "";
		public static int activeNodeId = 0;
		public static double activeNodeProgress = -1;
		public static bool nsfw = false;
		public static BsonValue generatedImages = new BsonValue();

		public static async Task<List<string>> RequestLoras()
		{
			HttpResponseMessage response = await httpClient.GetAsync($"http://{serverAddress}/object_info/LoraLoader");
			BsonDocument json = JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync()).AsDocument;
			List<string> loras = new List<string>();
			foreach(BsonValue lora in json["LoraLoader"]["input"]["required"]["lora_name"][0].AsArray)
			{
				loras.Add(lora.AsString);
			}
			return loras;
		}
		public static async Task<List<string>> RequestVAEs()
		{
			HttpResponseMessage response = await httpClient.GetAsync($"http://{serverAddress}/object_info/VAELoader");
			BsonDocument json = JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync()).AsDocument;
			List<string> vaes = new List<string>();
			foreach (BsonValue vae in json["VAELoader"]["input"]["required"]["vae_name"][0].AsArray)
			{
				vaes.Add(vae.AsString);
			}
			return vaes;
		}

		public static async Task<String> RequestProgress(string requestId)
		{
			HttpResponseMessage response = await httpClient.GetAsync($"http://{serverAddress}/queue");
			BsonDocument json = JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync()).AsDocument;

			if (json["queue_running"].AsArray.Count == 0 && json["queue_pending"].AsArray.Count == 0) return "Workflow not in queue";
			Console.WriteLine("2");
			if (json["queue_running"][0][1].AsString == requestId)
			{
				Console.WriteLine("3");
				if (activeNodeId == 0) { return "Initializing"; }
				Console.WriteLine("4");
				string message = $"Node: {json["queue_running"][0][2][activeNodeId.ToString()]["class_type"].AsString}";
				Console.WriteLine("5");
				if (activeNodeProgress != -1)
				{
					message += $" {Math.Round(activeNodeProgress * 100, 2)}%";
					message += "\n```";
					for (int i = 0; i < (int)(activeNodeProgress * 20); i++)
					{
						message += "█";
					}
					for (int i = 0; i < 20 - (int)(activeNodeProgress * 20); i++)
					{
						message += "░";
					}
					message += $"```";
				}
				Console.WriteLine("6");
				return message;
			}
			Console.WriteLine("7");
			foreach (BsonValue item in json["queue_pending"].AsArray)
			{
				Console.WriteLine("8");
				if (item.AsArray.Count == 0) continue;
				Console.WriteLine("9");
				if (item[1].AsString == requestId)
				{
					Console.WriteLine("10");
					return $"In Queue: {json["queue_pending"].AsArray.IndexOf(item)} remaining";
				}
				Console.WriteLine("11");
			}
			Console.WriteLine("12");
			return "";
		}

		/// <summary>
		/// Sends a request to the server to start processing the provided workflow and returns the request ID
		/// </summary>
		/// <param name="workflow"></param>
		/// <returns>string requestId</returns>
		/// <returns>bool nsfw</returns>
		public static async Task<string> ProcessWorkflow(Dictionary<string, Node> workflow)
		{
			if (websocketClient == null || !websocketClient.IsRunning)
			{
				websocketClient = new WebsocketClient(new Uri($"ws://{serverAddress}/ws?clientId={clientId}"));
				await websocketClient.Start();
				websocketClient.MessageReceived
					.Where(msg => msg.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
					.Subscribe(async message => await HandleWebsocketMessage(message));
				websocketClient.DisconnectionHappened.Subscribe(message => throw new Exception("ComfyUI server connection failed. Try again later"));
			}
			HttpResponseMessage response = await httpClient.PostAsync($"http://{serverAddress}/prompt",  CreateJSON(workflow) );
			BsonDocument responseJson = JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync()).AsDocument;
			//if (responseJson["node_errors"].AsDocument.Keys.Count > 0) throw new Exception(responseJson["node_errors"].ToString()); //TODO figure out this so it doesnt crash
			return responseJson["prompt_id"].AsString;
		}
		private static StringContent CreateJSON(object obj)
		{
			return new StringContent("{\"prompt\": " + System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + ", \"client_id\":\"" + clientId + "\"}", Encoding.UTF8, "application/json");
		}
		public static async Task HandleWebsocketMessage(ResponseMessage response)
		{
			BsonValue json = JsonSerializer.Deserialize(response.Text);
			//Console.WriteLine(json.ToString());
			if (json["type"] == "ElGogh-NSFW") 
			{ 
				nsfw = json["data"].AsBoolean;
				await SaveImages(generatedImages["data"]["output"]["images"].AsArray, generatedImages["data"]["prompt_id"]);
			}
			if (json["type"] == "status") queue = json["data"]["status"]["exec_info"]["queue_remaining"].AsInt32;
			if (json["type"] == "execution_start") activeRequestId = json["data"]["prompt_id"];
			if (json["type"] == "executing") activeNodeId = json["data"]["node"].AsInt32; activeNodeProgress = -1;
			if (json["type"] == "progress") activeNodeProgress = json["data"]["value"].AsDouble / json["data"]["max"].AsDouble;
			if (json["type"] == "executed")
			{
				//Save image names because the NSFW tag is the final one and is broadcasted later
				generatedImages = json;
			}
			if (json["type"] == "executing" && json["data"]["node"] == BsonValue.Null && json["data"]["prompt_id"] == activeRequestId) //Triggers if a request is fully cached. Resets the request since there is no "executed" call sent. (now fixed in ArtManager)
			{
				activeRequestId = ""; 
			}
		}


		private static async Task SaveImages(BsonArray images, string imageIdentifier)
		{
			foreach (BsonValue image in images)
			{
				HttpResponseMessage message = await httpClient.GetAsync($"http://{serverAddress}/view?filename={image["filename"].AsString}&subfolder={image["subfolder"].AsString}&type={image["type"].AsString}");
				if(message.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Image not found");
				await Bot.database.GetStorage<string>("TempImages", "TempChunks").UploadAsync(imageIdentifier + "/" + image["filename"].AsString, image["filename"].AsString, await message.Content.ReadAsStreamAsync(), metadata: BsonMapper.Global.ToDocument(new ImageMetadata() { nsfw=nsfw }));
				activeRequestId = "";
			}
		}

		
	}
}
