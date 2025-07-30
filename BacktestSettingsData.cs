namespace SimulationEngine.Model
{
    public class BacktestSettingsData
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public TimeSpan Step { get; set; }
    }
}
