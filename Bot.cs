using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using ElGogh.Commands;
using ElGogh.Commands.RequireAttributes;
using ElGogh.Database;
using LiteDB.Async;

//TODO: DM commands
namespace ElGogh
{
	class Bot
	{
		private string token;
		public static LiteDatabaseAsync database;
		public static DiscordClient discordClient;
		public Bot(string token)
		{
			this.token = token;
		}

		public void start()
		{
			database = DatabaseManager.initDatabase("ElGogh.db");
			initBot().GetAwaiter().GetResult();
		}

		private async Task initBot()
		{
			discordClient = new DiscordClient(new DiscordConfiguration()
			{
				Token = token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMessages
			});
			
			SlashCommandsExtension slashCommands = discordClient.UseSlashCommands();
			slashCommands.RegisterCommands<ArtCommands>();
			slashCommands.ContextMenuErrored += onContextMenuErrored;
			discordClient.ComponentInteractionCreated += onComponentInteractionCreated;
			discordClient.GuildDownloadCompleted += onGuildDownloadCompleted;
			discordClient.GuildCreated += onGuildCreated;
			discordClient.GuildDeleted += onGuildDeleted;
			discordClient.ModalSubmitted += onModalSubmitted;
			discordClient.UseInteractivity(new InteractivityConfiguration());
			
			await discordClient.ConnectAsync();
			await Task.Delay(-1);
		}

		private async Task onModalSubmitted(DiscordClient client, ModalSubmitEventArgs args)
		{
			await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
			//await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("monke"));
		}

		private async Task onContextMenuErrored(SlashCommandsExtension slashCommands, ContextMenuErrorEventArgs args)
		{
			if(args.Exception is ContextMenuExecutionChecksFailedException exception)
			{
				if (exception.FailedChecks[0] is RequireSelfAuthor)
				{
					await args.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("You can only do this on messages the bot has sent").AsEphemeral());
				} else if (exception.FailedChecks[0] is RequireImage)
				{
					await args.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("You can only do this on messages containing an image").AsEphemeral());
				}
			}
		}

		private async Task onComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs args)
		{
			if (args.Id == "homeChannelSelector")
			{
				if(args.User.Id == args.Guild.OwnerId || args.User.Id == 585812474113163284)
				{
					await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("You have just done yourself a huuuuuge favor by letting me in. Thank you :3"));
					await database.GetCollection<HomeChannel>().DeleteManyAsync(channel => channel.guildId == args.Guild.Id); //Safety just incase someone manages to send the message twice before the button disappears.
					await database.GetCollection<HomeChannel>().InsertAsync(new HomeChannel { guildId = args.Guild.Id, channelId = args.Guild.GetChannel(ulong.Parse(args.Values[0])).Id });
				} else
				{
					await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("I'm not sure if you are allowed to let me in though. Can I have some headpats instead :3").AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "headpat", "Press to give headpats")));
				}
			}
			if(args.Id == "headpat")
			{
				await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("*happy noises*"));
			}
		}

		private async Task onGuildDownloadCompleted(DiscordClient client, GuildDownloadCompletedEventArgs args)
		{
			foreach(DiscordGuild guild in args.Guilds.Values)
			{
				await initGuild(guild);
			}
		}
		private async Task onGuildCreated(DiscordClient client, GuildCreateEventArgs args)
		{
			await initGuild(args.Guild);
		}
		
		private async Task onGuildDeleted(DiscordClient client, GuildDeleteEventArgs args)
		{
			await database.GetCollection<HomeChannel>().DeleteManyAsync(channel => channel.guildId == args.Guild.Id);
		}

		private async Task initGuild(DiscordGuild guild)
		{
			Console.WriteLine($"Joined {guild.Name}");
			if(await database.GetCollection<HomeChannel>().FindOneAsync(channel => channel.guildId == guild.Id) == null)
			{
				IReadOnlyList<DiscordChannel> channels = await guild.GetChannelsAsync();
				foreach(DiscordChannel channel in channels)
				{
					if(channel.Type == ChannelType.Text)
					{
						DiscordMessageBuilder builder = new DiscordMessageBuilder();
						builder.Embed = new DiscordEmbedBuilder { Description = guild.Owner.Mention + "Hello mister, good evening! I'm your new roomate. They've arranged for me to live with you. Please tell me where I should reside :3" };
						builder.AddComponents(new DiscordChannelSelectComponent("homeChannelSelector", "Select Channel", channelTypes: new List<ChannelType>() { ChannelType.Text }));
						await channel.SendMessageAsync(builder);
						break;
					}
				}
			}
		}
	}
}
