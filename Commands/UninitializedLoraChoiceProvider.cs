using DSharpPlus.Entities;
using El_Gogh.Art;
using El_Gogh.Database;

namespace El_Gogh.Commands
{
	class UninitializedLoraChoiceProvider : LoraChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<Lora> loras = (await Bot.database.GetCollection<Lora>().FindAllAsync()).ToList();
			IEnumerable<string> availableLoras = await StableDiffusionInterface.RequestLoras();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (string lora in availableLoras)
			{
				if(!loras.Any(x => x.name == lora))
				{
					choices.Add(new DiscordApplicationCommandOptionChoice(lora, lora));
				}
			}
			if(choices.Count == 0) {
				choices.Add(new DiscordApplicationCommandOptionChoice("No more loras available to add", "no"));
			}
			return choices;
		}
	}
}
