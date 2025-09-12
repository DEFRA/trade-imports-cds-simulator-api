using MongoDB.Driver;

namespace TradeImportsCdsSimulatorApi.Utils.Mongo;

public interface IMongoDbClientFactory
{
    IMongoClient GetClient();

    IMongoCollection<T> GetCollection<T>(string collection);
}