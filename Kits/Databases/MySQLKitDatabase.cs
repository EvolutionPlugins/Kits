using Kits.API;
using Kits.Extensions;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Kits.Databases
{
    public class MySQLKitDatabase : KitDatabaseCore, IKitDatabase
    {
        private readonly MySqlConnection m_MySqlConnection;

        public MySQLKitDatabase(Kits plugin) : base(plugin)
        {
            m_MySqlConnection = new(Connection);
        }

        public async Task LoadDatabaseAsync()
        {
            try
            {
                await m_MySqlConnection.OpenAsync();
                await using var command = m_MySqlConnection.CreateCommand();

                command.CommandText = $"SHOW TABLES LIKE '{TableName}';";

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
                    $"INSERT INTO `{TableName}` (`Name`, `Cooldown`, `Cost`, `Money`, `Items`) VALUES (@a, @b, @c, @d, @e);";
                command.Parameters.AddWithValue("a", kit.Name);
                command.Parameters.AddWithValue("b", kit.Cooldown);
                command.Parameters.AddWithValue("c", kit.Cost);
                command.Parameters.AddWithValue("d", kit.Money);
                command.Parameters.AddWithValue("e", bytes);

                var i = await command.ExecuteNonQueryAsync();
                Console.WriteLine(i);
                return true;
            }
            finally
            {
                if (m_MySqlConnection.State == ConnectionState.Open)
                {
                    await m_MySqlConnection.CloseAsync();
                }
            }

            return false;
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
                    Array.Clear(ConvertorExtension.s_Buffer, 0, ConvertorExtension.s_Buffer.Length);

                    reader.GetBytes(4, 0, ConvertorExtension.s_Buffer, 0, ushort.MaxValue);
                    return new()
                    {
                        Name = reader.GetString(0),
                        Cooldown = reader.GetFloat(1),
                        Cost = reader.GetDecimal(2),
                        Money = reader.GetDecimal(3),
                        Items = ConvertorExtension.ConvertToKitItems(ConvertorExtension.s_Buffer)
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

                command.CommandText = $"SELECT `Name`, `Cooldown`, `Cost`, `Money`, `Items` FROM `{TableName}`;";

                await using var reader = await command.ExecuteReaderAsync();

                var result = new List<Kit>();

                while (await reader.ReadAsync())
                {
                    lock (ConvertorExtension.s_Lock)
                    {
                        Array.Clear(ConvertorExtension.s_Buffer, 0, ConvertorExtension.s_Buffer.Length);

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
                Console.WriteLine(i);
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
                    $"UPDATE `{TableName}` SET `Cooldown`=@a, `Cost`=@b, `Money`=@c, `Items`=@d WHERE `Name` = @n;";
                command.Parameters.AddWithValue("a", kit.Cooldown);
                command.Parameters.AddWithValue("b", kit.Cost);
                command.Parameters.AddWithValue("c", kit.Money);
                command.Parameters.AddWithValue("d", bytes);
                command.Parameters.AddWithValue("n", kit.Name);

                var i = await command.ExecuteNonQueryAsync();
                Console.WriteLine(i);
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
    }
}
