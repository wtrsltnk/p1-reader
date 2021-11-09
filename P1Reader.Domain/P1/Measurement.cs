using System;

namespace P1Reader.Domain.P1
{
    public class Measurement
    {
        public decimal ActualElectricityPowerDelivery { get; set; }

        public decimal ActualElectricityPowerDraw { get; set; }

        public decimal ElectricityDeliveredByClientTariff1 { get; set; }

        public decimal ElectricityDeliveredByClientTariff2 { get; set; }

        public decimal ElectricityDeliveredToClientTariff1 { get; set; }

        public decimal ElectricityDeliveredToClientTariff2 { get; set; }

        public string EquipmentIdentifier { get; set; }

        public decimal InstantaneousActivePowerDeliveryL1 { get; set; }

        public decimal InstantaneousActivePowerDeliveryL2 { get; set; }

        public decimal InstantaneousActivePowerDeliveryL3 { get; set; }

        public decimal InstantaneousActivePowerDrawL1 { get; set; }

        public decimal InstantaneousActivePowerDrawL2 { get; set; }

        public decimal InstantaneousActivePowerDrawL3 { get; set; }

        public int InstantaneousCurrentL1 { get; set; }

        public int InstantaneousCurrentL2 { get; set; }

        public int InstantaneousCurrentL3 { get; set; }

        public decimal InstantaneousVoltageL1 { get; set; }

        public decimal InstantaneousVoltageL2 { get; set; }

        public decimal InstantaneousVoltageL3 { get; set; }

        public int LongPowerFailuresInAnyPhase { get; set; }

        public decimal NetActualElectricityPower { get; set; }

        public int PowerFailuresInAnyPhase { get; set; }

        public int Tariff { get; set; }

        public DateTime TimeStamp { get; set; }

        public int TotalInstantaneousCurrent { get; set; }

        public decimal TotalInstantaneousVoltage { get; set; }

        public string Version { get; set; }
    }
}
