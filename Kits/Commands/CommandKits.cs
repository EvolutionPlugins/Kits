using System;
using System.Threading.Tasks;
using EvolutionPlugins.Economy.Stub.Services;
using JetBrains.Annotations;
using Kits.API;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Permissions;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Players;

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
        private readonly ILogger<CommandKits> m_Logger;

        public CommandKits(IServiceProvider serviceProvider, IKitManager kitManager, IStringLocalizer stringLocalizer,
            IEconomyProvider economyProvider, ILogger<CommandKits> logger) : base(serviceProvider)
        {
            m_KitManager = kitManager;
            m_StringLocalizer = stringLocalizer;
            m_EconomyProvider = economyProvider;
            m_Logger = logger;
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

            try
            {
                // prevent some exceptions
                if (m_EconomyProvider is not EconomyProviderStub)
                {
                    moneySymbol = m_EconomyProvider.CurrencySymbol;
                    moneyName = m_EconomyProvider.CurrencyName;
                }
            }
            catch (Exception e)
            {
                m_Logger.LogWarning(e, "Failed to get currency symbol and currency name");
            }

            var kits = await m_KitManager.GetAvailableKitsForPlayerAysnc(showKitsUser);
            kits = kits.Count > 0 ? kits : null;

            await PrintAsync(m_StringLocalizer["commands:kits", new
            {
                Kits = kits,
                MoneySymbol = moneySymbol,
                MoneyName = moneyName
            }]);
        }
    }
}