using Kits.API;
using Kits.Extensions;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kits.Databases
{
    public class MySQLKitDatabase : KitDatabaseCore, IKitDatabase
    {
        private readonly Kits m_Plugin;
        private readonly MySqlConnection m_MySqlConnection;

        public MySQLKitDatabase(Kits plugin) : base(plugin)
        {
            m_Plugin = plugin;

            m_MySqlConnection = new MySqlConnection(Connection);
        }

        public async Task LoadDatabaseAsync()
        {
            await m_MySqlConnection.OpenAsync(CancellationToken.None);
            await using var command = m_MySqlConnection.CreateCommand();

            command.CommandText = "SHOW TABLES LIKE @a;";
            command.Parameters.AddWithValue("a", TableName);
            Console.WriteLine(command.CommandText);
            if (await command.ExecuteScalarAsync() != null)
            {
                await m_MySqlConnection.CloseAsync();
                return;
            }

            command.CommandText = @"CREATE TABLE @a (
	            `Name` VARCHAR(255) NOT NULL COLLATE 'utf8mb4_unicode_ci',
	            `Cooldown` FLOAT(12,0) NULL DEFAULT NULL,
            	`Cost` DECIMAL(10,0) NULL DEFAULT NULL,
            	`Money` DECIMAL(10,0) NULL DEFAULT NULL,
            	`Items` BLOB NULL DEFAULT NULL,
            	PRIMARY KEY (`Name`) USING BTREE
            ) COLLATE='utf8mb4_unicode_ci';";

            await command.ExecuteNonQueryAsync();
            await m_MySqlConnection.CloseAsync();
        }

        public async Task<bool> AddKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            await m_MySqlConnection.OpenAsync();
            await using var command = m_MySqlConnection.CreateCommand();

            var bytes = kit.Items?.ConvertToByteArray() ?? Array.Empty<byte>();

            command.CommandText = $"INSERT INTO `{TableName}` (`Name`, `Cooldown`, `Cost`, `Money`, `Items`) VALUES (@a, @b, @c, @d, @e);";
            command.Parameters.AddWithValue("a", kit.Name);
            command.Parameters.AddWithValue("b", kit.Cooldown);
            command.Parameters.AddWithValue("c", kit.Cost);
            command.Parameters.AddWithValue("d", kit.Money);
            command.Parameters.AddWithValue("e", bytes);

            var i = await command.ExecuteNonQueryAsync();
            Console.WriteLine(i);
            await m_MySqlConnection.CloseAsync();
            return i != 0;
        }

        public async Task<Kit?> GetKitAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            await m_MySqlConnection.OpenAsync();
            await using var command = m_MySqlConnection.CreateCommand();

            command.CommandText = $"SELECT `Name`, `Cooldown`, `Cost`, `Money`, `Items` FROM `{TableName}` WHERE `Name` = @n;";
            command.Parameters.AddWithValue("n", name);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                await m_MySqlConnection.CloseAsync();
                return null;
            }

            lock (ConvertorExtension.s_Lock)
            {
                reader.GetBytes(4, 0, ConvertorExtension.s_Buffer, 0, ushort.MaxValue);
                var kit = new Kit()
                {
                    Name = reader.GetString(0),
                    Cooldown = reader.GetFloat(1),
                    Cost = reader.GetDecimal(2),
                    Money = reader.GetDecimal(3),
                    Items = ConvertorExtension.ConvertToKitItems(ConvertorExtension.s_Buffer)
                };
                m_MySqlConnection.Close();
                return kit;
            }
        }

        public async Task<IReadOnlyCollection<Kit>> GetKitsAsync()
        {
            await m_MySqlConnection.OpenAsync();
            await using var command = m_MySqlConnection.CreateCommand();

            command.CommandText = $"SELECT `Name`, `Cooldown`, `Cost`, `Money`, `Items` FROM `{TableName}`;";

            await using var reader = await command.ExecuteReaderAsync();

            var result = new List<Kit>();

            while (await reader.ReadAsync())
            {
                lock (ConvertorExtension.s_Lock)
                {
                    reader.GetBytes(4, 0, ConvertorExtension.s_Buffer, 0, ushort.MaxValue);
                    result.Add(new Kit()
                    {
                        Name = reader.GetString(0),
                        Cooldown = reader.GetFloat(1),
                        Cost = reader.GetDecimal(2),
                        Money = reader.GetDecimal(3),
                        Items = ConvertorExtension.ConvertToKitItems(ConvertorExtension.s_Buffer)
                    });
                }
            }
            await m_MySqlConnection.CloseAsync();
            return result;
        }

        public async Task<bool> RemoveKitAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            await m_MySqlConnection.OpenAsync();
            await using var command = m_MySqlConnection.CreateCommand();

            command.CommandText = $"DELETE FROM {TableName} WHERE `Name` = @n;";
            command.Parameters.AddWithValue("n", name);

            return await command.ExecuteNonQueryAsync() != 0;
        }

        public async Task<bool> UpdateKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            await m_MySqlConnection.OpenAsync();
            await using var command = m_MySqlConnection.CreateCommand();

            var bytes = kit.Items?.ConvertToByteArray() ?? Array.Empty<byte>();

            command.CommandText = $"UPDATE `{TableName}` SET `Cooldown`=@a, `Cost`=@b, `Money`=@c, `Items`=@d WHERE `Name` = @n;";
            command.Parameters.AddWithValue("a", kit.Cooldown);
            command.Parameters.AddWithValue("b", kit.Cost);
            command.Parameters.AddWithValue("c", kit.Money);
            command.Parameters.AddWithValue("d", bytes);
            command.Parameters.AddWithValue("n", kit.Name);

            if (await command.ExecuteNonQueryAsync() == 0)
            {
                return await AddKitAsync(kit);
            }
            return true;
        }
    }
}
