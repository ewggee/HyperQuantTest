namespace Connector.Core.Interfaces;

//todo: вынести в доки
// Методы интерфейса вынес в два отдельных интерфейса для соблюдения принципа Interface Segregation
public interface ITestConnector : IRestConnector, IWebSocketConnector
{ }
