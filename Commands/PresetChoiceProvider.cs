using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using El_Gogh.Database;

namespace El_Gogh.Commands
{
	class PresetChoiceProvider : IChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			Console.WriteLine("Loaded preset choices");
			List<Txt2ImgPreset> presets = (await Bot.database.GetCollection<Txt2ImgPreset>().FindAllAsync()).ToList();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (Txt2ImgPreset preset in presets)
			{
				choices.Add(new DiscordApplicationCommandOptionChoice(preset.name + $" - [{(await Bot.client.GetUserAsync(preset.creator)).Username}]", preset.name));
			}
			return choices;
		}
	}
}
