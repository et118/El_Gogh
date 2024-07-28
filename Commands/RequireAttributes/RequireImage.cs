using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElGogh.Commands.RequireAttributes
{
	class RequireImage : ContextMenuCheckBaseAttribute
	{
		public override async Task<bool> ExecuteChecksAsync(ContextMenuContext ctx)
		{
			//TODO add more image types
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
