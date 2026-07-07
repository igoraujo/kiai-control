using System.Data;
using KiaiControl.Core.Interfaces;
using Npgsql;

namespace KiaiControl.Repositories.Connections;

public sealed class NpgsqlDbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
