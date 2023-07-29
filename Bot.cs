using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using El_Gogh.Art;
using El_Gogh.Commands;
using El_Gogh.Database;
using LiteDB;
using LiteDB.Async;
using System.Net.Mail;

namespace El_Gogh
{
	class Bot
	{
		public static DiscordClient client;
		public static LiteDatabaseAsync database;
		public Bot(String token) => init(token).GetAwaiter().GetResult();

		private async Task init(String token)
		{
			database = new LiteDatabaseAsync("ElGogh.db");
			(await database.GetStorage<string>("Images","Chunks").FindByIdAsync("879047797238812784/585812474113163284/fea2b654-2971-452c-b025-1af8fcaa3a16.png")).SaveAs("out.png");
			BsonMapper.Global.EmptyStringToNull = false;
			BsonMapper.Global.TrimWhitespace = false;
			await ArtDatabaseInitializer.InitializePresets();
			await ArtDatabaseInitializer.UpdateModels();
			await ArtDatabaseInitializer.UpdateLoras();
			client = new DiscordClient(new DiscordConfiguration()
			{
				Token = token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMessages
			});
			SlashCommandsExtension slashCommands = client.UseSlashCommands();
			slashCommands.RegisterCommands<MiscCommands>();
			slashCommands.RegisterCommands<ArtCommands>();
			slashCommands.ContextMenuErrored += onSlashCommandErrored;

			client.ComponentInteractionCreated += onComponentInteractionCreated;
			client.GuildDownloadCompleted += onGuildDownloadCompleted;
			client.GuildCreated += onGuildCreated;
			client.GuildDeleted += onGuildDeleted;
			//TODO check if we have homechannel before we can use slash commands
			await client.ConnectAsync();
			await Task.Delay(-1);
		}

		private async Task onSlashCommandErrored(SlashCommandsExtension slashCommands, ContextMenuErrorEventArgs args)
		{
			if(args.Exception is ContextMenuExecutionChecksFailedException exception)
			{
				ContextMenuCheckBaseAttribute attribute = exception.FailedChecks[0];
				if (attribute is RequireSelfMessageAuthor)
				{
					await args.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hey, I haven't sent this message").AsEphemeral());
				}
				else if(attribute is RequireImage)
				{
					await args.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hey, There are no of my images here to look at").AsEphemeral());
				} else
				{
					throw args.Exception;
				}
			} else
			{
				throw args.Exception;
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
			Console.WriteLine($"Added to {guild.Name}");
			if(await database.GetCollection<HomeChannel>().FindOneAsync(channel => channel.guildId == guild.Id) == null)
			{
				IReadOnlyList<DiscordChannel> channels = await guild.GetChannelsAsync();
				foreach(DiscordChannel channel in channels)
				{
					if(channel.Type == ChannelType.Text)
					{
						DiscordMessageBuilder builder = new DiscordMessageBuilder();
						builder.Embed = new DiscordEmbedBuilder { Description = "Hello there! Where should I live? :3" };
						builder.AddComponents(new DiscordChannelSelectComponent("channelselector", "Select Channel", channelTypes: new List<ChannelType>() { ChannelType.Text }));
						await channel.SendMessageAsync(builder);
						break;
					}
				}
			}
		}

		private async Task onComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs args)
		{
			if(args.Id == "channelselector")
			{
				if(args.User.Id == args.Guild.OwnerId || args.User.Id == 585812474113163284)
				{
					await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent($"See you in {args.Guild.GetChannel(ulong.Parse(args.Values[0])).Name}!"));
					await database.GetCollection<HomeChannel>().DeleteManyAsync(channel => channel.guildId == args.Guild.Id);
					await database.GetCollection<HomeChannel>().InsertAsync(new HomeChannel { guildId = args.Guild.Id, channelId = args.Guild.GetChannel(ulong.Parse(args.Values[0])).Id });
				}
			}
		}
	}
}
