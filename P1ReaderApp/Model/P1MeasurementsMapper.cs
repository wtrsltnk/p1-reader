using P1Reader.Domain.Interface;
using P1Reader.Domain.P1;

namespace P1ReaderApp.Model
{
    public class P1MeasurementsMapper :
        IMapper<P1Measurements, Measurement>
    {
        public Measurement Map(
            P1Measurements source)
        {
            return new Measurement()
            {
                ActualElectricityPowerDelivery = source.ActualElectricityPowerDelivery,
                ActualElectricityPowerDraw = source.ActualElectricityPowerDraw,
                ElectricityDeliveredByClientTariff1 = source.ElectricityDeliveredByClientTariff1,
                ElectricityDeliveredByClientTariff2 = source.ElectricityDeliveredByClientTariff2,
                ElectricityDeliveredToClientTariff1 = source.ElectricityDeliveredToClientTariff1,
                ElectricityDeliveredToClientTariff2 = source.ElectricityDeliveredToClientTariff2,
                EquipmentIdentifier = source.EquipmentIdentifier,
                InstantaneousActivePowerDeliveryL1 = source.InstantaneousActivePowerDeliveryL1,
                InstantaneousActivePowerDeliveryL2 = source.InstantaneousActivePowerDeliveryL2,
                InstantaneousActivePowerDeliveryL3 = source.InstantaneousActivePowerDeliveryL3,
                InstantaneousActivePowerDrawL1 = source.InstantaneousActivePowerDrawL1,
                InstantaneousActivePowerDrawL2 = source.InstantaneousActivePowerDrawL2,
                InstantaneousActivePowerDrawL3 = source.InstantaneousActivePowerDrawL3,
                InstantaneousCurrentL1 = source.InstantaneousCurrentL1,
                InstantaneousCurrentL2 = source.InstantaneousCurrentL2,
                InstantaneousCurrentL3 = source.InstantaneousCurrentL3,
                InstantaneousVoltageL1 = source.InstantaneousVoltageL1,
                InstantaneousVoltageL2 = source.InstantaneousVoltageL2,
                InstantaneousVoltageL3 = source.InstantaneousVoltageL3,
                LongPowerFailuresInAnyPhase = source.LongPowerFailuresInAnyPhase,
                NetActualElectricityPower = source.NetActualElectricityPower,
                PowerFailuresInAnyPhase = source.PowerFailuresInAnyPhase,
                Tariff = source.Tariff,
                TimeStamp = source.TimeStamp,
                TotalInstantaneousCurrent = source.TotalInstantaneousCurrent,
                TotalInstantaneousVoltage = source.TotalInstantaneousVoltage,
                Version = source.Version,
            };
        }
    }
}
