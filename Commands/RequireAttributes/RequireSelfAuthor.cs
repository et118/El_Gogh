using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElGogh.Commands.RequireAttributes
{
	class RequireSelfAuthor : ContextMenuCheckBaseAttribute
	{
		public override async Task<bool> ExecuteChecksAsync(ContextMenuContext ctx)
		{
			if (ctx.TargetMessage.Author.Id == ctx.Client.CurrentUser.Id && ctx.TargetMessage != null)
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
