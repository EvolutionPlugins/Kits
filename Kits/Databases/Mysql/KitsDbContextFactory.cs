using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.MySql;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kits.Databases.Mysql
{
    public class KitsDbContextFactory : OpenModMySqlDbContextFactory<KitsDbContext>
    {
    }
}
