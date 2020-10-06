using Kits.API;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("kits")]
    [CommandActor(typeof(IPlayerUser))]
    public class CommandKits : Command
    {
        private readonly IKitManager kitManager;
        private readonly IUserManager userManager;

        public CommandKits(IServiceProvider serviceProvider, IKitManager kitManager, IUserManager userManager) : base(serviceProvider)
        {
            this.kitManager = kitManager;
            this.userManager = userManager;
        }

        protected override async Task OnExecuteAsync()
        {
            var playerUser = (IPlayerUser)Context.Actor;

        }
    }
}
