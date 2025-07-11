namespace Connector.Core.Exceptions;

public class BitfinexApiException : Exception
{
    public int ErrorCode { get; set; }

    public BitfinexApiException(int errorCode, string message) 
        : base($"Bitfinex API error {errorCode}: {message}")
    {
        ErrorCode = errorCode;
    }
}
