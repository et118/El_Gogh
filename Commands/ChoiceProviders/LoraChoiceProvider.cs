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
	class LoraChoiceProvider : IChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<string> loras = await ComfyUIInterface.RequestLoras();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach(string lora in loras)
			{
				choices.Add(new DiscordApplicationCommandOptionChoice(lora.Replace(".safetensors", ""), lora.Replace(".safetensors", "")));
			}
			return choices;
		}
	}
}
