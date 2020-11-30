using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using HandyHansel.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandyHansel
{
    internal class CategoryHelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder _embed;
        private Command _command;

        public CategoryHelpFormatter(CommandContext ctx) : base(ctx)
        {
            this._embed = new DiscordEmbedBuilder()
                .WithTitle("Help")
                .WithColor(this.Context.Member.Color);
        }

        public override CommandHelpMessage Build()
        {
            if (this._command == null)
            {
                this._embed.WithDescription("Listing all top-level commands, modules, and groups. Specify a command to see more information.");
            }

            return new CommandHelpMessage(embed: this._embed);
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            this._command = command;

            this._embed.WithDescription($"{Formatter.InlineCode(command.Name)}: {command.Description ?? "No description provided. Please contact the dev team."}");

            if (command is CommandGroup cgroup && cgroup.IsExecutableWithoutSubcommands)
            {
                this._embed.WithDescription($"{this._embed.Description}\n\nThis group can be executed as a command.");
            }

            if (command.Aliases?.Any() == true)
            {
                this._embed.AddField("Aliases", string.Join(',', command.Aliases.Select(Formatter.InlineCode)), false);
            }

            if (command.Overloads?.Any() == true)
            {
                StringBuilder allUsageStrings = new StringBuilder();
                StringBuilder allArguments = new StringBuilder();

                ISet<string> argNames = new HashSet<string>();
                foreach (CommandOverload co in command.Overloads.OrderByDescending(x => x.Priority))
                {
                    allUsageStrings.Append('`').Append(command.QualifiedName);
                    foreach (CommandArgument ca in co.Arguments)
                    {
                        bool opt = ca.IsOptional;
                        bool cAll = ca.IsCatchAll;
                        allUsageStrings.Append(opt || cAll ? " [" : " <").Append(ca.Name).Append(cAll ? "..." : "").Append(opt || cAll ? "]" : ">");

                        if (!argNames.Contains(ca.Name))
                        {
                            allArguments
                                .Append('`')
                                .Append(ca.Name)
                                .Append(" (")
                                .Append(this.CommandsNext.GetUserFriendlyTypeName(ca.Type))
                                .Append(")` - ")
                                .Append(opt ? "optional - " : "")
                                .Append(ca.Description ?? "No description provided. Please contact the dev team.")
                                .Append('\n');
                        }

                        argNames.Add(ca.Name);
                    }
                    allUsageStrings.Append("`\n");
                }
                this._embed.AddField("Usage:", allUsageStrings.ToString(), false);
                if (command.Overloads.Any(co => co.Arguments.Any()))
                {
                    this._embed.AddField("Arguments:", allArguments.ToString(), false);
                }
            }

            // TODO: Add the permissions needed to use this command. 

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            Dictionary<string, ISet<string>> modules = new Dictionary<string, ISet<string>>();
            List<CommandGroup> groupCommands = new List<CommandGroup>();
            List<Command> currentLevelCommands = new List<Command>();

            foreach (Command command in subcommands.OrderBy(test => test.Name))
            {
                if (command.CustomAttributes.FirstOrDefault(item => item is BotCategoryAttribute) is BotCategoryAttribute module)
                {
                    if (!modules.ContainsKey(module.Name))
                    {
                        modules[module.Name] = new HashSet<string>();
                    }
                    modules[module.Name].Add(command.Name);
                }
                else if (command is CommandGroup cgroup)
                {
                    groupCommands.Add(cgroup);
                }
                else
                {
                    currentLevelCommands.Add(command);
                }
            }

            if (currentLevelCommands.Any())
            {
                this._embed.AddField(this._command == null ? "Commands:" : "Sub-commands:", string.Join(", ", currentLevelCommands.Select(x => Formatter.InlineCode(x.Name))));
            }

            if (groupCommands.Any())
            {
                this._embed.AddField(this._command == null ? "Groups:" : "Sub-groups:", string.Join(", ", groupCommands.Select(x => Formatter.InlineCode(x.Name))));
            }

            if (modules.Any())
            {
                foreach (KeyValuePair<string, ISet<string>> moduleLists in modules)
                {
                    this._embed.AddField($"{moduleLists.Key}:", string.Join(", ", moduleLists.Value.Select(Formatter.InlineCode)));
                }
            }

            return this;
        }
    }
}
