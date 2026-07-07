using System.Data;

namespace KiaiControl.Core.Interfaces;

public interface IDbConnectionFactory
{
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
