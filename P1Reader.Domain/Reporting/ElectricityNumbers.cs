using System;

namespace P1Reader.Domain.Reporting
{
    public class ElectricityNumbers
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public decimal ElectricityDeliveredToClient { get; set; }

        public decimal ElectricityDeliveredByClient { get; set; }

        public decimal ActualElectricityPowerDelivery { get; set; }

        public decimal ActualElectricityPowerDraw { get; set; }

        public decimal NetActualElectricityPower { get; set; }
    }
}
