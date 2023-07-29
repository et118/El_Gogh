using DSharpPlus.SlashCommands;

namespace El_Gogh.Commands
{
	class RequireImage : ContextMenuCheckBaseAttribute
	{
		public override async Task<bool> ExecuteChecksAsync(ContextMenuContext ctx)
		{
			if (ctx.TargetMessage.Attachments.Count == 0) return false;
			if (ctx.TargetMessage.Attachments[0].MediaType == "image/png" || ctx.TargetMessage.Attachments[0].MediaType == "image/jpg")
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
