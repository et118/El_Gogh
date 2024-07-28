using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using ElGogh.Database;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElGogh.Commands.ChoiceProviders
{
	class WorkflowChoiceProvider : IChoiceProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			List<LiteFileInfo<string>> workflows = (await Bot.database.GetStorage<string>("Workflows","WorkflowChunks").FindAllAsync()).ToList();
			List<DiscordApplicationCommandOptionChoice> choices = new List<DiscordApplicationCommandOptionChoice>();
			foreach (LiteFileInfo<string> workflow in workflows) 
			{
				choices.Add(new DiscordApplicationCommandOptionChoice(workflow.Id.Remove(0,8).Replace("-ElGogh-API.json",""), workflow.Id));
			}
			return choices;
		}
	}
}
