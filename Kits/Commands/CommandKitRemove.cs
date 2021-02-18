using Kits.API;
using Microsoft.Extensions.Localization;
using OpenMod.Core.Commands;
using System;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("remove")]
    [CommandAlias("-")]
    [CommandAlias("delete")]
    [CommandParent(typeof(CommandKit))]
    [CommandSyntax("<name>")]
    public class CommandKitRemove : Command
    {
        private readonly IKitStore m_KitStore;
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandKitRemove(IServiceProvider serviceProvider, IKitStore kitStore,
            IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_KitStore = kitStore;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Length != 1)
            {
                throw new CommandWrongUsageException(Context);
            }

            var kitName = Context.Parameters[0];
            await m_KitStore.RemoveKit(kitName);
            await PrintAsync(m_StringLocalizer["commands:kit:remove:success", new { Name = kitName }]);
        }
    }
}
