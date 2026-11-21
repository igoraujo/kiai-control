using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Notifications;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlPushDeliveryRepository(IDbConnectionFactory factory) : IPushDeliveryRepository
{
    public async Task<IReadOnlyList<PushDelivery>> ClaimAsync(CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        using var tx = c.BeginTransaction();
        await c.ExecuteAsync(new CommandDefinition(PushDeliveryQueries.FailExhausted, transaction: tx, cancellationToken: ct));
        await c.ExecuteAsync(new CommandDefinition(PushDeliveryQueries.Enqueue, transaction: tx, cancellationToken: ct));
        var deliveries = (await c.QueryAsync<PushDelivery>(new CommandDefinition(PushDeliveryQueries.Claim, transaction: tx, cancellationToken: ct))).ToArray();
        tx.Commit();
        return deliveries;
    }

    public async Task CompleteAsync(PushDelivery delivery, PushSendResult result, CancellationToken ct)
    {
        if (result == PushSendResult.Retry) return;
        using var c = await factory.OpenConnectionAsync(ct);
        using var tx = c.BeginTransaction();
        await c.ExecuteAsync(new CommandDefinition(result == PushSendResult.Accepted ? PushDeliveryQueries.Accept : PushDeliveryQueries.RemoveInvalidToken, delivery, tx, cancellationToken: ct));
        tx.Commit();
    }
}
