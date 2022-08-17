using P1Reader.Domain.P1;
using P1Reader.Domain.Reporting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P1Reader.Domain.Interfaces
{
    public interface IMeasurementsService
    {
        Task<IEnumerable<Measurement>> GetMeasurementsBetweenAsync(
            DateTime start,
            DateTime end);

        Task<ElectricityNumbers> GetElectricityNumbersBetweenAsync(
            DateTime start,
            DateTime end);
    }
}