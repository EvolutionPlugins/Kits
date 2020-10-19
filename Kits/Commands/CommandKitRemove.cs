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
    public class CommandKitRemove : Command
    {
        private readonly IKitManager m_KitManager;
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandKitRemove(IServiceProvider serviceProvider, IKitManager kitManager, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_KitManager = kitManager;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Length != 1)
            {
                throw new CommandWrongUsageException(Context);
            }
            var kitName = Context.Parameters[0];
            if(await m_KitManager.RemoveKitAsync(kitName))
            {
                await PrintAsync(m_StringLocalizer["commands:kit:remove:success", new { Name = kitName }]);
            }
            else
            {
                await PrintAsync(m_StringLocalizer["commands:kit:remove:fail", new { Name = kitName }]);
            }
        }
    }
}
