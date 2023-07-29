using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using El_Gogh.Database;

namespace El_Gogh.Commands
{
	class ModelChoiceProvider : IChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<Model> models = (await Bot.database.GetCollection<Model>().FindAllAsync()).ToList();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (Model model in models)
			{
				choices.Add(new DiscordApplicationCommandOptionChoice(model.name.Replace(".safetensors","") + " - " + model.description, model.name));
			}
			return choices;
		}
	}
}
