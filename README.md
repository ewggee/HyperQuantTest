# HyperQuantTest
Реализация коннектора к API v2 Bitfinex
## Реализованные функции
**REST API Клиент:**
- [x] [Получение данных по трейдам](https://docs.bitfinex.com/reference/rest-public-trades)
- [x] [Получение данных о свечах](https://docs.bitfinex.com/reference/rest-public-candles)
- [x] [Получение информации о тикере](https://docs.bitfinex.com/reference/rest-public-ticker)

**WebSocket API Клиент:**
- [x] [Подписка на получение трейдов](https://docs.bitfinex.com/reference/ws-public-trades)
- [x] [Подписка на получение свечей](https://docs.bitfinex.com/reference/ws-public-candles)

**Дополнительно:**
- [x] Расчет баланса портфеля

## Использование
Коннектор находится в библиотеке Connector.Core по пути: `src\Connector.Core\Connectors\BitfinexConnector.cs`. 

Для использования коннектора нужно создать его инстанс:
```cs
var connector = new BitfinexConnector();
```

### REST клиент
Для получения трейдов, свечей и тикера из REST API нужно воспользоваться асинхронными методами:
```cs
// Пример: Получение 100 трейдов за последние 24 часа, сортированные по убыванию
var trade = await connector.GetNewTradesAsync("tBTCUSD", DateTimeOffset.UtcNow.AddHours(-24), DateTimeOffset.UtcNow, false, 100);
```

### WebSocket клиент
Для получения трейдов и свечей в реальном времени с помощью Web Socket нужно:
```cs
// Пример: Обработка свечей
// 1. Определить обработчик для события
wsClient.CandleSeriesProcessing(candles => {
	...
});

// 2. Подписаться на событие
connector.SubscribeCandlesAsync("tBTCUSD", 300);
```

### Расчёт баланса
Для тестирования метода расчёта баланса я реализовал endpoint:
```
POST api/portfolio/balance
{
  "tBTCUSD": 1,
  "tXRPUSD": 15000,
  "tXMRUSD": 50
}
```

Для тестирования с помощью Swagger нужно запустить Connector.WebAPI и перейти по адресу: https://localhost:7116/swagger/index.html.
## Тестирование

Интеграционные тесты покрывают:
- REST API методы
- WebSocket подключение
- Парсинг входящих сообщений

## Архитектура
### Общая архитектура
```
HyperQuantTest/
├── src/                          # Исходный код
│   ├── Connector.Core/           # Библиотека коннектора
│   └── Connector.WebAPI/         # Web-API интерфейс
│
└── tests/                        # Тесты
    └── Connector.IntegrationTests # Интеграционные тесты
```

### Connector.Core
```
Connector.Core/
├── Clients/                      # Клиенты для работы с API
│   ├── BitfinexRestClient.cs     # REST клиент Bitfinex
│   ├── BitfinexWebSocketClient.cs # WebSocket клиент Bitfinex
│   └── IBitfinexApi.cs           # Интерфейс для Refit
│
├── Connectors/                   # Коннекторы
│   └── BitfinexConnector.cs      # Основной класс коннектора
│
├── Helpers/                      # Вспомогательные классы
│   └── BitfinexTimeframeHelper.cs # Конвертер времени
│
├── Interfaces/                   # Контракты
│   ├── ITestConnector.cs         # Интерфейс коннектора (REST + WebSocket)
│   ├── IRestConnector.cs         # Интерфейс REST части коннектора
│   └── IWebSocketConnector.cs    # Интерфейс WebSocket части коннектора
│
├── Models/                       # Модели данных
│   ├── Candle.cs                 # Модель свечи
│   ├── Trade.cs                  # Модель трейда
│   └── Ticker.cs                 # Модель тикера
│
└── Services/                     # Сервисы
    └── PortfolioService.cs       # Расчет портфеля
```

### Connector.WebAPI
Для реализации задачи вывода общего баланса портфеля. По условию можно использовать любой ASP.NET.
```
Connector.WebAPI/
├── Controllers/                  # Контроллеры
│   └── PortfolioController.cs    # Контроллер для расчёта баланса
└── Program.cs                    # Входная точка проекта Connector.WebApi
```

## Правки, спорные моменты
1. В ТЗ неоднозначно указано, нужно ли реализовывать функцию получения тикера для REST клиента:
> Класс клиента для REST API  биржи Bitfinex, который реализует 2 функции:
> - Получение трейдов (trades) 
> - Получение свечей (candles) 
> - Получение информации о тикере (Ticker)

2. Спорный момент насчёт `decimal`-свойств класса Candle, т.к. в [документации API](https://docs.bitfinex.com/reference/rest-public-candles#response-fields) указано, что для параметров: OPEN, CLOSE, HIGH, LOW — возвращается натуральное число. Можно рассмотреть вариант замены на тип `long`.

3. Во всех REST-методах, где есть обращение к API Biftinex я использую метод `ConfigureAwait(false)`, чтобы отключить контекст синхронизации, тем самым повышая производительность асинхронных методов. По условию не сказано, должна ли библиотека быть совместима с приложениями с пользовательским интерфейсом.

4. В методе коннектора `GetCandleSeriesAsync` для параметра `count` изменил тип с `long` на `int`, т.к. в [документации API](<https://docs.bitfinex.com/reference/rest-public-candles#response-fields:~:text=Number%20of%20records%20in%20response%20(max.%2010000)>) указано, что максимум может вернуть 10000 записей, поэтому `long` в данном случае считаю излишним.

5. Интерфейс `ITestConnector` разбил на два интерфейса: `IRestConnector` и `IWebSocketConnector` для соблюдения принципа Interface Segregation.

6. В интерфейсе `IWebSocketConnector`: 
	- Переделал методы с синхронного формата на асинхронный
	- Удалил неиспользуемые параметры в методах
	- Исправил тип в ивенте `CandleSeriesProcessing` с `Candle` на `IEnumerable<Candle>`
