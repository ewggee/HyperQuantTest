namespace Connector.Core.Models;

public class Ticker
{
    /// <summary> 
    /// Максимальная ставка 
    /// </summary>
    public decimal Bid { get; set; }

    /// <summary>
    /// Сумма 25-ти самых высоких ставок
    /// </summary>
    public decimal BidSize { get; set; }

    /// <summary> 
    /// Минимальная ставка
    /// </summary>
    public decimal Ask { get; set; }

    /// <summary>
    /// Сумма 25-ти самых низких ставок
    /// </summary>
    public decimal AskSize { get; set; }

    /// <summary> 
    /// Изменение цены за сутки
    /// </summary>
    public decimal DailyChange { get; set; }

    /// <summary> 
    /// Изменение цены за сутки (в процентах)
    /// </summary>
    public decimal DailyChangeRelative { get; set; }

    /// <summary> 
    /// Последняя цена 
    /// </summary>
    public decimal LastPrice { get; set; }

    /// <summary>
    /// Дневной общий объём
    /// </summary>
    public decimal DailyVolume { get; set; }

    /// <summary>
    /// Дневной максимум
    /// </summary>
    public decimal DailyHigh { get; set; }

    /// <summary>
    /// Дневной минимум
    /// </summary>
    public decimal DailyLow { get; set; }
}
