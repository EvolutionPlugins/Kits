using Kits.API;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("kit")]
    [CommandSyntax("[create|remove] <name>")]
    [CommandActor(typeof(IPlayerUser))]
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
            var playerUser = (IPlayerUser)Context.Actor;
            var kitName = Context.Parameters[0];
            return m_KitManager.GiveKitAsync(playerUser, kitName);
        }
    }
}
