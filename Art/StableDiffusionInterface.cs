using El_Gogh.Database;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Text.Encodings.Web;
using LiteDB;

namespace El_Gogh.Art
{
	class StableDiffusionInterface {
		private static HttpClient client = new HttpClient() { Timeout=Timeout.InfiniteTimeSpan};
		private static List<string> requestQueue = new List<string>();
		private static int maxQueue = 5;

		public static async Task<String> RequestProgress(string requestId)
		{
			if(requestQueue.FirstOrDefault() == requestId)
			{
				HttpResponseMessage response = await client.GetAsync("http://localhost:7860/sdapi/v1/progress?skip_current_image=true");
				if (response.StatusCode == HttpStatusCode.OK)
				{
					dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
					float progress = jsonObj["progress"];
					string message = "```";
					for (int i = 0; i < 2 * (int)(progress * 10); i++)
					{
						message += "█";
					}
					for (int i = 0; i < 2 * (10 - (int)(progress * 10)); i++)
					{
						message += "░";
					}
					return message + "```";
				}
				else
				{
					return await response.Content.ReadAsStringAsync();
				}
			} else if(requestQueue.Contains(requestId))
			{
				return "In Queue: " + requestQueue.IndexOf(requestId);
			} else
			{
				return "Request not in queue";
			}
		}

		public static async Task<Tuple<Image,bool>> RequestTxt2Img(Settings settings, Txt2ImgRequest request, string requestId)
		{
			if(requestQueue.Count >= maxQueue) throw new Exception("Queue is full. Please wait :3");
			requestQueue.Add(requestId);
			while (true)
			{
				if (requestQueue.FirstOrDefault() == requestId)
				{
					Thread.Sleep(200);
					HttpResponseMessage response = await client.PostAsync("http://localhost:7860/sdapi/v1/options", CreateJSON(settings));
					
					if(response.StatusCode != HttpStatusCode.OK)
					{
						requestQueue.Remove(requestId);
						throw new Exception(await response.Content.ReadAsStringAsync());
					}
					Thread.Sleep(200); //Apparently very important
					response = await client.PostAsync("http://localhost:7860/sdapi/v1/txt2img", CreateJSON(request));
					if (response.StatusCode != HttpStatusCode.OK)
					{
						requestQueue.Remove(requestId);
						throw new Exception(await response.Content.ReadAsStringAsync());
					}
					dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
					bool censored = false;
					Image image = null;
					foreach(string style in ((dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject((string)json.info)).styles)
					{
						if(style.Equals("NSFW"))
						{
							censored = true;
						}
					}
					foreach (string rawimage in json["images"]) //Might fuck up if we have a batch size larger than 1 image
					{
						byte[] bytes = Convert.FromBase64String(rawimage);
						using(MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length))
						{
							image = await Image.LoadAsync(stream);
						}
					}
					requestQueue.Remove(requestId);
					return Tuple.Create(image, censored);
				} else {
					await Task.Delay(500);
				}
			}
		}

		public static async Task<Image> RequestImg2Img(LiteFileInfo<string> image, Settings settings, Img2ImgRequest request, string requestId)
		{
			if (requestQueue.Count >= maxQueue) throw new Exception("Queue is full. Please wait :3");
			requestQueue.Add(requestId);
			while (true)
			{
				if (requestQueue.FirstOrDefault() == requestId)
				{
					Thread.Sleep(200);
					HttpResponseMessage response = await client.PostAsync("http://localhost:7860/sdapi/v1/options", CreateJSON(settings));

					if (response.StatusCode != HttpStatusCode.OK)
					{
						requestQueue.Remove(requestId);
						throw new Exception(await response.Content.ReadAsStringAsync());
					}
					Thread.Sleep(200); //Apparently very important
					
					using(MemoryStream stream = new MemoryStream())
					{
						image.CopyTo(stream);
						request.init_images = new List<string>() {Convert.ToBase64String(stream.ToArray())};
					}

					Console.WriteLine("Before Img2Img");
					response = await client.PostAsync("http://localhost:7860/sdapi/v1/img2img", CreateJSON(request));
					Console.WriteLine("After Img2Img");
					if (response.StatusCode != HttpStatusCode.OK)
					{
						requestQueue.Remove(requestId);
						throw new Exception(await response.Content.ReadAsStringAsync());
					}
					dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
					Image img = null;
					foreach (string rawimage in json["images"]) //Might fuck up if we have a batch size larger than 1 image
					{
						byte[] bytes = Convert.FromBase64String(rawimage);
						using (MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length))
						{
							img = await Image.LoadAsync(stream);
						}
					}
					requestQueue.Remove(requestId);
					return img;
				}
				else
				{
					await Task.Delay(500);
				}
			}
		}

		public static async Task<IEnumerable<string>> RequestModels()
		{
			await client.PostAsync("http://localhost:7860/sdapi/v1/refresh-checkpoints", new StringContent(""));
			HttpResponseMessage response = await client.GetAsync("http://localhost:7860/sdapi/v1/sd-models");
			List<string> models = new List<string>();
			if(response.StatusCode == HttpStatusCode.OK)
			{
				dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
				foreach(dynamic model in jsonObj) {
					models.Add(((string)model["model_name"]));
				}
				return models;
			} else
			{
				throw new Exception(await response.Content.ReadAsStringAsync());
			}
		}
		public static async Task<IEnumerable<string>> RequestLoras()
		{
			await client.PostAsync("http://localhost:7860/sdapi/v1/refresh-loras", new StringContent(""));
			HttpResponseMessage response = await client.GetAsync("http://localhost:7860/sdapi/v1/loras");
			List<string> loras = new List<string>();
			if (response.StatusCode == HttpStatusCode.OK)
			{
				dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
				foreach (dynamic lora in jsonObj)
				{
					loras.Add((string)lora["name"]);
				}
				return loras;
			}
			else
			{
				throw new Exception(await response.Content.ReadAsStringAsync());
			}
		}
		
		public static async Task Interrupt()
		{
			await client.PostAsync("http://localhost:7860/sdapi/v1/interrupt", new StringContent(""));
		}
		
		private static StringContent CreateJSON(object obj)
		{
			return new StringContent(System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), Encoding.UTF8, "application/json");
		}

		
	}
}
