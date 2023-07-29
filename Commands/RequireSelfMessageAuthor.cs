using DSharpPlus.SlashCommands;

namespace El_Gogh.Commands
{
	class RequireSelfMessageAuthor : ContextMenuCheckBaseAttribute
	{
		public override async Task<bool> ExecuteChecksAsync(ContextMenuContext ctx)
		{
			if(ctx.TargetMessage.Author.Id == ctx.Client.CurrentUser.Id && ctx.TargetMessage != null)
			{
				return true;
			} else
			{
				return false;
			}
		}
	}
}
