
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using El_Gogh.Database;

namespace El_Gogh.Commands
{
	class UpscalePresetChoiceProvider : IChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<Img2ImgPreset> presets = (await Bot.database.GetCollection<Img2ImgPreset>().FindAllAsync()).ToList();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (Img2ImgPreset preset in presets)
			{
				choices.Add(new DiscordApplicationCommandOptionChoice(preset.name + $" - [{(await Bot.client.GetUserAsync(preset.creator)).Username}]", preset.name));
			}
			return choices;
		}
	}
}
