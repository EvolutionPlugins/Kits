using System;
using System.Threading.Tasks;
using Kits.API;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;

namespace Kits.Commands
{
    [Command("kit")]
    [CommandSyntax("[create|remove] <name>")]
    public class CommandKit : Command
    {
        private readonly IKitManager m_KitManager;

        public CommandKit(IServiceProvider serviceProvider, IKitManager kitManager) : base(serviceProvider)
        {
            m_KitManager = kitManager;
        }

        protected override Task OnExecuteAsync()
        {
            if (Context.Parameters.Count != 1)
            {
                throw new CommandWrongUsageException(Context);
            }

            if (Context.Actor is not IPlayerUser playerUser)
            {
                throw new CommandWrongUsageException(Context);
            }

            var kitName = Context.Parameters[0];
            return m_KitManager.GiveKitAsync(playerUser, kitName);
        }
    }
}