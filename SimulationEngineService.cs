using SimulationEngine.Model;
using SimulationExchange.Service;
using StrategyBase.Service;

namespace SimulationEngine.Service
{
    public class SimulationEngineService
    {
        private SimulationExchangeService _exchange;
        public List<StrategyBaseService> Strategys { get; private set; } = new List<StrategyBaseService>();

        public SimulationEngineService(SimulationExchangeService exchange)
        {
            _exchange = exchange;
        }

        public void AddStrategy(StrategyBaseService strategy)
        {
            Strategys.Add(strategy);
        }
        public async Task RunBacktest(BacktestSettingsData settings)
        {
            for (DateTime current = settings.From; current <= settings.To; current += settings.Step)
            {
                //устанавливаем дату симуляции, биржа будет возвращать бары до этой даты, сделки будут открываться этой датой и тд
                _exchange.SetCurrentDate(current);

                foreach(var strategy in Strategys)
                {
                    //запускаем логику торговли
                    await ExecuteStrategy(strategy);
                }
            }
        }

        private async Task ExecuteStrategy(StrategyBaseService strategy)
        {
            // Сначала — рекурсивно у всех вложенных стратегий
            foreach (var subStrategy in strategy.Strategys)
            {
                await ExecuteStrategy(subStrategy);
            }

            // Потом выполняем Do() текущей стратегии
            await strategy.Do();
        }
    }
}
