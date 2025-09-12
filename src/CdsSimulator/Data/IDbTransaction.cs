namespace Defra.TradeImportsCdsSimulator.Data;

public interface IDbTransaction : IDisposable
{
    Task Commit(CancellationToken cancellationToken);
}
