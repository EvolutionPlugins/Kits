using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using EvolutionPlugins.Economy.Stub.Services;
using JetBrains.Annotations;
using Kits.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Permissions;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Players;
using SmartFormat.ZString;

namespace Kits.Commands
{
    [Command("kits")]
    [RegisterCommandPermission("show.other", Description = "Shows the available kits of another player")]
    [CommandSyntax("[player]")]
    [UsedImplicitly]
    public class CommandKits : Command
    {
        private readonly IKitManager m_KitManager;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IEconomyProvider m_EconomyProvider;
        private readonly IConfiguration m_Configuration;

        public CommandKits(IServiceProvider serviceProvider, IKitManager kitManager, IStringLocalizer stringLocalizer,
            IEconomyProvider economyProvider, IConfiguration configuration) : base(serviceProvider)
        {
            m_KitManager = kitManager;
            m_StringLocalizer = stringLocalizer;
            m_EconomyProvider = economyProvider;
            m_Configuration = configuration;
        }

        protected override async Task OnExecuteAsync()
        {
            var showKitsUser = (Context.Parameters.Count == 1 ? await Context.Parameters.GetAsync<IPlayerUser>(0)
                : Context.Actor as IPlayerUser) ?? throw new CommandWrongUsageException(Context);

            var isNotExecutor = showKitsUser != Context.Actor;
            if (isNotExecutor && await CheckPermissionAsync("show.other") != PermissionGrantResult.Grant)
            {
                throw new NotEnoughPermissionException(Context, "show.other");
            }

            var moneySymbol = "$";
            var moneyName = string.Empty;

            // prevent some exceptions
            if (m_EconomyProvider is not EconomyProviderStub)
            {
                moneySymbol = m_EconomyProvider.CurrencySymbol;
                moneyName = m_EconomyProvider.CurrencyName;
            }

            var kits = await m_KitManager.GetAvailableKitsForPlayerAsync(showKitsUser);
            kits = kits.Count > 0 ? kits : null;

            await PrintAsync(m_StringLocalizer["commands:kits", new
            {
                Kits = kits,
                MoneySymbol = moneySymbol,
                MoneyName = moneyName
            }]);
        }

        public override async Task PrintAsync(string message)
        {
            if (m_Configuration.GetValue("wrapLines", true))
            {
                foreach (var msg in WrapLines(message))
                {
                    await PrintAsync(msg, Color.White);
                }
                return;
            }

            await PrintAsync(message, Color.White);
        }

        private static IEnumerable<string> WrapLines(string line)
        {
            const int MaxLength = 90;

            using var currentLine = new ZStringBuilder(false);

            foreach (var currentWord in line.Split(' '))
            {
                if (currentLine.Length > MaxLength ||
                    currentLine.Length + currentWord.Length > MaxLength)
                {
                    yield return currentLine.ToString();
                    currentLine.Clear();
                }

                if (currentLine.Length > 0)
                {
                    currentLine.Append(" ");
                    currentLine.Append(currentWord);
                }
                else
                {
                    currentLine.Append(currentWord);
                }
            }

            if (currentLine.Length > 0)
            {
                yield return currentLine.ToString();
            }
        }
    }
}