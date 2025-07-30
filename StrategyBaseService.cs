using DataProvider.Common.Model;
using ExchangeBase.Common.Model;
using ExchangeBase.Service;
using Logger.Service;
using StrategyBase.Model;
using System;

namespace StrategyBase.Service
{
    public abstract class StrategyBaseService
    {
        protected ExchangeBaseService _exchangeService { get; }
        protected LoggerService _loggerService { get; }

        private bool _isInProgress;

        public StrategySettingsDataBase Settings { get; private set; }


        public bool IsTradingEnabled { get; private set; }
        public List<StrategyBaseService> Strategys { get; } = new List<StrategyBaseService>();

        public BarsData Bars { get; private set; } = new BarsData();

        public DateTime Date { get; private set; } = DateTime.MinValue;
        public double Open { get; private set; } = double.NaN;
        public double High { get; private set; } = double.NaN;
        public double Low { get; private set; } = double.NaN;
        public double Close { get; private set; } = double.NaN;

        public double IndicatorsExecutionTime { get; private set; } = 0;
        public double StrategyExecutionTime { get; private set; } = 0;

        protected StrategyBaseService(ExchangeBaseService exchangeService,
                                      LoggerService loggerService,
                                      StrategySettingsDataBase settings)
        {
            _exchangeService = exchangeService;
            _loggerService = loggerService;
            Settings = settings;
        }

        public void EnableTrading()
        {
            IsTradingEnabled = true;
        }
        public void DisableTrading()
        {
            IsTradingEnabled = false;
        }
        public async Task Do()
        {
            if(!_isInProgress)
            {
                _isInProgress = true;

                try
                {
                    //получаем актуальные бары с сайта биржи
                    await GetBarsFromExchange(Settings.Symbol, Settings.TimeFrame, Settings.InMemoryBarsCount);

                    if (Bars.Date.Length < Settings.InMemoryBarsCount)
                    {
                        //баров для стратегии слишком мало
                        return;
                    }

                    //вычисляем все индикаторы, необходимые для стратегии
                    var start = DateTime.Now;
                    CalculateIndicators();
                    IndicatorsExecutionTime += (DateTime.Now - start).TotalMilliseconds;

                    start = DateTime.Now;
                    await StrategyLogic();
                    StrategyExecutionTime += (DateTime.Now - start).TotalMilliseconds;
                }
                catch (ArgumentException ex)
                {
                    _loggerService.Error($"ArgumentException: {ex}");
                }
                catch (Exception ex)
                {
                    _loggerService.Error($"Exception: {ex}");
                }
                finally
                {
                    _isInProgress = false;
                }
            }
        }
        protected abstract Task StrategyLogic();
        protected async Task GetBarsFromExchange(string symbol, TimeSpan timeFrame, int barsCount)
        {
            Bars = await _exchangeService.GetBars(symbol, timeFrame, barsCount);

            //инициализируем массивы для удобного описания торговой логики
            var lastElementIndex = Bars.Date.Length - 1;

            if(lastElementIndex == -1)
            {
                Date = DateTime.MinValue;
                Open = double.NaN;
                High = double.NaN;
                Low = double.NaN;
                Close = double.NaN;

                return;
            }

            Date = Bars.Date[lastElementIndex];
            Open = Bars.Open[lastElementIndex];
            High = Bars.High[lastElementIndex];
            Low = Bars.Low[lastElementIndex];
            Close = Bars.Close[lastElementIndex];
        }
        protected abstract void CalculateIndicators();

        public abstract List<TradeData> GetTrades();
        public abstract List<TradeData> GetOpenedTrades();
        public abstract List<TradeData> GetCompletedTrades();

        public async Task<OrderData> OpenBuy(OrderType orderType, double openedValue, bool openedValueIsInCrypto = true, double? level = null)
        {
            var value = openedValue;
            if (!openedValueIsInCrypto)
                value = await _exchangeService.CalculateValueInCrypto(openedValue, Close, Settings.Symbol);

            return await _exchangeService.OpenBuy(orderType, Settings.Symbol, value, level);
        }
        public async Task<OrderData> OpenSell(OrderType orderType, double openedValue, bool openedValueIsInCrypto = true, double? level = null)
        {
            var value = openedValue;
            if (!openedValueIsInCrypto)
                value = await _exchangeService.CalculateValueInCrypto(openedValue, Close, Settings.Symbol);

            return await _exchangeService.OpenSell(orderType, Settings.Symbol, value, level);
        }
        public async Task<OrderData> CloseBuy(OrderType orderType, double openedValue, bool openedValueIsInCrypto = true, double? level = null, string trackingNo = "")
        {
            var value = openedValue;
            if (!openedValueIsInCrypto)
                value = await _exchangeService.CalculateValueInCrypto(openedValue, Close, Settings.Symbol);

            return await _exchangeService.CloseBuy(orderType, Settings.Symbol, value, level, trackingNo);
        }
        public async Task<OrderData> CloseSell(OrderType orderType, double openedValue, bool openedValueIsInCrypto = true, double? level = null, string trackingNo = "")
        {
            var value = openedValue;
            if (!openedValueIsInCrypto)
                value = await _exchangeService.CalculateValueInCrypto(openedValue, Close, Settings.Symbol);

            return await _exchangeService.CloseSell(orderType, Settings.Symbol, value, level, trackingNo);
        }

    }
}
