namespace Defra.TradeImportsCdsSimulator.Exceptions
{
    public class InvalidRequestException(string message, Exception? ex = null) : Exception(message, ex) { }
}
