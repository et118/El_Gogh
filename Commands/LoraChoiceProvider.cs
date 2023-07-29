using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using El_Gogh.Database;

namespace El_Gogh.Commands
{
	class LoraChoiceProvider : IChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<Lora> loras = (await Bot.database.GetCollection<Lora>().FindAllAsync()).ToList();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (Lora lora in loras)
			{
				choices.Add(new DiscordApplicationCommandOptionChoice(lora.name, lora.name));
			}
			return choices;
		}
	}
}
