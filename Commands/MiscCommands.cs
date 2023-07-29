using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus;

namespace El_Gogh.Commands
{
	class MiscCommands : ApplicationCommandModule
	{
		[SlashCommand("ChangeHomeChannel","Changes the home channel in which El Gogh resides")]
		public async Task ChangeHomeChannelCommand(InteractionContext ctx)
		{
			if(ctx.User.Id == ctx.Guild.Owner.Id || ctx.User.Id == 585812474113163284)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().AddComponents(new DiscordChannelSelectComponent("channelselector", "Select Channel", channelTypes: new List<ChannelType>() { ChannelType.Text })).AsEphemeral());
			} else
			{
				await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You don't have permission to run this command").AsEphemeral());
			}
		}
	}
}
