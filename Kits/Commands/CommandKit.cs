using System;
using System.Linq;
using System.Threading.Tasks;
using Kits.API;
using OpenMod.API.Permissions;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Extensions.Games.Abstractions.Players;

namespace Kits.Commands
{
    [Command("kit")]
    [CommandSyntax("[create|remove] [player] <name>")]
    [RegisterCommandPermission("give.other", Description = "Give the kit to another player but it will checks if another player has permission, money, etc..")] // will check if player has permission to get kit
    [RegisterCommandPermission("give.other.force", Description = "Force give the kit to another player without checks if the player has permission, money, etc..")] // will not check if player has permission, money, etc.. to get kit
    public class CommandKit : Command
    {
        private readonly IKitManager m_KitManager;

        public CommandKit(IServiceProvider serviceProvider, IKitManager kitManager) : base(serviceProvider)
        {
            m_KitManager = kitManager;
        }

        protected override async Task OnExecuteAsync()
        {
            var giveKitUser = (Context.Parameters.Count == 2 ? await Context.Parameters.GetAsync<IPlayerUser>(0)
                    : Context.Actor as IPlayerUser) ?? throw new CommandWrongUsageException(Context);

            var isNotExecutor = giveKitUser != Context.Actor;
            var forceGiveKit = false;
            var kitName = Context.Parameters.LastOrDefault() ?? throw new CommandWrongUsageException(Context);
            
            if (isNotExecutor)
            {
                if (await CheckPermissionAsync("give.other.force") == PermissionGrantResult.Grant)
                {
                    forceGiveKit = true;
                }
                else if (await CheckPermissionAsync("give.other") == PermissionGrantResult.Grant)
                {
                    forceGiveKit = false;
                }
                else
                {
                    throw new NotEnoughPermissionException(Context, "give.other");
                }
            }

            await m_KitManager.GiveKitAsync(giveKitUser, kitName, isNotExecutor ? Context.Actor : null, forceGiveKit);
        }
    }
}