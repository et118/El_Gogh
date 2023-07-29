using DSharpPlus.Entities;
using El_Gogh.Art;
using El_Gogh.Database;

namespace El_Gogh.Commands
{
	class UninitializedModelChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<Model> models = (await Bot.database.GetCollection<Model>().FindAllAsync()).ToList();
			IEnumerable<string> availableModels = await StableDiffusionInterface.RequestModels();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (string model in availableModels)
			{
				if(!models.Any(x => x.name == model))
				{
					choices.Add(new DiscordApplicationCommandOptionChoice(model.Replace(".safetensors", ""), model));
				}
			}
			if (choices.Count == 0)
			{
				choices.Add(new DiscordApplicationCommandOptionChoice("No more models available to add", "no"));
			}
			return choices;
		}
	}
}
