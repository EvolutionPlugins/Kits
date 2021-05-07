using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Autofac;
using Kits.API;
using Kits.Extensions;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Kits.Databases
{
    public sealed class MySqlKitDatabase : KitDatabaseCore, IKitDatabase, IAsyncDisposable
    {
        private readonly ILogger<MySqlKitDatabase> m_Logger;
        private readonly MySqlConnection m_MySqlConnection;

        public MySqlKitDatabase(Kits plugin) : base(plugin)
        {
            m_Logger = plugin.LifetimeScope.Resolve<ILogger<MySqlKitDatabase>>();
            m_MySqlConnection = new(Connection);
        }

        public async Task LoadDatabaseAsync()
        {
            try
            {
                await m_MySqlConnection.OpenAsync();
                await using var command = m_MySqlConnection.CreateCommand();

                command.CommandText = $@"CREATE TABLE `kits` (
	                `Id` INT(11) NOT NULL AUTO_INCREMENT,
	                `Name` VARCHAR(255) NOT NULL COLLATE 'utf8mb4_unicode_ci',
	                `Cooldown` FLOAT(12,0) NULL DEFAULT NULL,
	                `Cost` DECIMAL(10,0) NULL DEFAULT NULL,
	                `Money` DECIMAL(10,0) NULL DEFAULT NULL,
	                `VehicleId` VARCHAR(255) NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	                `Items` BLOB NULL DEFAULT NULL,
	                PRIMARY KEY (`Id`) USING BTREE);";

                var i = await command.ExecuteNonQueryAsync();
                Console.WriteLine(i);

                await using var command1 = m_MySqlConnection.CreateCommand();

                command1.CommandText = $@"ALTER TABLE `{TableName}`
	                ADD COLUMN `Id` INT(11) NOT NULL AUTO_INCREMENT FIRST,
	                ADD PRIMARY KEY (`Id`);";

                await command1.ExecuteNonQueryAsync();
            }
            finally
            {
                if (m_MySqlConnection.State == ConnectionState.Open)
                {
                    await m_MySqlConnection.CloseAsync();
                }
            }
        }

        public async Task<bool> AddKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            try
            {
                await m_MySqlConnection.OpenAsync();
                await using var command = m_MySqlConnection.CreateCommand();
                var bytes = kit.Items?.ConvertToByteArray() ?? Array.Empty<byte>();

                command.CommandText =
                    $"INSERT INTO `{TableName}` (`Name`, `Cooldown`, `Cost`, `Money`, `VehicleId`, `Items`) VALUES (@a, @b, @c, @d, @f, @e);";
                command.Parameters.AddWithValue("a", kit.Name);
                command.Parameters.AddWithValue("b", kit.Cooldown);
                command.Parameters.AddWithValue("c", kit.Cost);
                command.Parameters.AddWithValue("d", kit.Money);
                command.Parameters.AddWithValue("f", kit.VehicleId);
                command.Parameters.AddWithValue("e", bytes);

                var i = await command.ExecuteNonQueryAsync();
                return true;
            }
            finally
            {
                if (m_MySqlConnection.State == ConnectionState.Open)
                {
                    await m_MySqlConnection.CloseAsync();
                }
            }
        }

        public async Task<Kit?> GetKitAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            try
            {
                await m_MySqlConnection.OpenAsync();
                await using var command = m_MySqlConnection.CreateCommand();

                command.CommandText =
                    $"SELECT * FROM `{TableName}` WHERE LOWER(`Name`) LIKE @n; ";
                command.Parameters.AddWithValue("n", name.ToLower());

                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    await m_MySqlConnection.CloseAsync();
                    return null;
                }

                lock (ConvertorExtension.s_Lock)
                {
                    Array.Clear(ConvertorExtension.s_Buffer, 0, ConvertorExtension.s_Buffer.Length);

                    if (!reader.IsDBNull(6))
                    {
                        reader.GetBytes(6, 0, ConvertorExtension.s_Buffer, 0, ushort.MaxValue);
                    }

                    return new()
                    {
                        Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Cooldown = reader.IsDBNull(2) ? null : reader.GetFloat(2),
                        Cost = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                        Money = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                        VehicleId = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Items = ConvertorExtension.ConvertToKitItems(ConvertorExtension.s_Buffer, m_Logger)
                    };
                }
            }
            finally
            {
                if (m_MySqlConnection.State == ConnectionState.Open)
                {
                    await m_MySqlConnection.CloseAsync();
                }
            }
        }

        public async Task<IReadOnlyCollection<Kit>> GetKitsAsync()
        {
            try
            {
                await m_MySqlConnection.OpenAsync();
                await using var command = m_MySqlConnection.CreateCommand();

                command.CommandText =
                    $"SELECT * FROM `{TableName}` ORDER BY `Id` ASC;";

                await using var reader = await command.ExecuteReaderAsync();

                var result = new List<Kit>();

                while (await reader.ReadAsync())
                {
                    lock (ConvertorExtension.s_Lock)
                    {
                        Array.Clear(ConvertorExtension.s_Buffer, 0, ConvertorExtension.s_Buffer.Length);

                        if (!reader.IsDBNull(6))
                        {
                            reader.GetBytes(6, 0, ConvertorExtension.s_Buffer, 0, ushort.MaxValue);
                        }

                        result.Add(new Kit()
                        {
                            Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                            Cooldown = reader.IsDBNull(2) ? null : reader.GetFloat(2),
                            Cost = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                            Money = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                            VehicleId = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Items = ConvertorExtension.ConvertToKitItems(ConvertorExtension.s_Buffer, m_Logger)
                        });
                    }
                }

                return result;
            }
            finally
            {
                if (m_MySqlConnection.State == ConnectionState.Open)
                {
                    await m_MySqlConnection.CloseAsync();
                }
            }
        }

        public async Task<bool> RemoveKitAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            try
            {
                await m_MySqlConnection.OpenAsync();
                await using var command = m_MySqlConnection.CreateCommand();

                command.CommandText = $"DELETE FROM {TableName} WHERE `Name` = @n;";
                command.Parameters.AddWithValue("n", name);

                var i = await command.ExecuteNonQueryAsync();
                return i != 0;
            }
            finally
            {
                if (m_MySqlConnection.State == ConnectionState.Open)
                {
                    await m_MySqlConnection.CloseAsync();
                }
            }
        }

        public async Task<bool> UpdateKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            try
            {
                await m_MySqlConnection.OpenAsync();
                await using var command = m_MySqlConnection.CreateCommand();

                var bytes = kit.Items?.ConvertToByteArray() ?? Array.Empty<byte>();

                command.CommandText =
                    $"UPDATE `{TableName}` SET `Cooldown`=@a, `Cost`=@b, `Money`=@c, `VehicleId`=@f, `Items`=@d WHERE `Name` = @n;";
                command.Parameters.AddWithValue("a", kit.Cooldown);
                command.Parameters.AddWithValue("b", kit.Cost);
                command.Parameters.AddWithValue("c", kit.Money);
                command.Parameters.AddWithValue("d", bytes);
                command.Parameters.AddWithValue("f", kit.VehicleId);
                command.Parameters.AddWithValue("n", kit.Name);

                var i = await command.ExecuteNonQueryAsync();
                if (i != 0)
                {
                    return true;
                }

                await m_MySqlConnection.CloseAsync();
                return await AddKitAsync(kit);
            }
            finally
            {
                if (m_MySqlConnection.State == ConnectionState.Open)
                {
                    await m_MySqlConnection.CloseAsync();
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            return new(m_MySqlConnection.DisposeAsync());
        }
    }
}