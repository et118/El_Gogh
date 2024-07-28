using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using ElGogh.Art;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElGogh.Commands.ChoiceProviders
{
	class VAEChoiceProvider : IChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<string> vaes = await ComfyUIInterface.RequestVAEs();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (string vae in vaes)
			{
				choices.Add(new DiscordApplicationCommandOptionChoice(vae.Replace(".pt", "").Replace(".safetensors", ""), vae.Replace(".pt", "").Replace(".safetensors", "")));
			}
			return choices;
		}
	}
}
