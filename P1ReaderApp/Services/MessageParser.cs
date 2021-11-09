﻿using P1Reader.Domain.Interface;
using P1Reader.Domain.P1;
using P1ReaderApp.Attributes;
using P1ReaderApp.Exceptions;
using P1ReaderApp.Interfaces;
using P1ReaderApp.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace P1ReaderApp.Services
{
    public class MessageParser :
        IMessageParser
    {
        private readonly IMessageBuffer<Measurement> _measurementsBuffer;
        private readonly IMapper<P1Measurements, Measurement> _measurementMapper;
        private IDictionary<string, OBISField> _fields;

        public MessageParser(
            IMessageBuffer<Measurement> measurementsBuffer,
            IMapper<P1Measurements, Measurement> measurementMapper)
        {
            _measurementsBuffer = measurementsBuffer;
            _measurementMapper = measurementMapper;
            CreateFieldDictionary();
        }

        public async Task<P1Measurements> ParseSerialMessages(
            P1MessageCollection messageCollection)
        {
            P1Measurements measurements = null;
            try
            {
                var messages = messageCollection.Messages;

                measurements = new P1Measurements
                {
                    ActualElectricityPowerDelivery = GetDecimalField(nameof(P1Measurements.ActualElectricityPowerDelivery), messages),
                    ActualElectricityPowerDraw = GetDecimalField(nameof(P1Measurements.ActualElectricityPowerDraw), messages),
                    ElectricityDeliveredByClientTariff1 = GetDecimalField(nameof(P1Measurements.ElectricityDeliveredByClientTariff1), messages),
                    ElectricityDeliveredByClientTariff2 = GetDecimalField(nameof(P1Measurements.ElectricityDeliveredByClientTariff2), messages),
                    ElectricityDeliveredToClientTariff1 = GetDecimalField(nameof(P1Measurements.ElectricityDeliveredToClientTariff1), messages),
                    ElectricityDeliveredToClientTariff2 = GetDecimalField(nameof(P1Measurements.ElectricityDeliveredToClientTariff2), messages),
                    EquipmentIdentifier = GetStringField(nameof(P1Measurements.EquipmentIdentifier), messages),
                    InstantaneousActivePowerDeliveryL1 = GetDecimalField(nameof(P1Measurements.InstantaneousActivePowerDeliveryL1), messages),
                    InstantaneousActivePowerDeliveryL2 = GetDecimalField(nameof(P1Measurements.InstantaneousActivePowerDeliveryL2), messages),
                    InstantaneousActivePowerDeliveryL3 = GetDecimalField(nameof(P1Measurements.InstantaneousActivePowerDeliveryL3), messages),
                    InstantaneousActivePowerDrawL1 = GetDecimalField(nameof(P1Measurements.InstantaneousActivePowerDrawL1), messages),
                    InstantaneousActivePowerDrawL2 = GetDecimalField(nameof(P1Measurements.InstantaneousActivePowerDrawL2), messages),
                    InstantaneousActivePowerDrawL3 = GetDecimalField(nameof(P1Measurements.InstantaneousActivePowerDrawL3), messages),
                    InstantaneousCurrentL1 = GetIntegerField(nameof(P1Measurements.InstantaneousCurrentL1), messages),
                    InstantaneousCurrentL2 = GetIntegerField(nameof(P1Measurements.InstantaneousCurrentL2), messages),
                    InstantaneousCurrentL3 = GetIntegerField(nameof(P1Measurements.InstantaneousCurrentL3), messages),
                    InstantaneousVoltageL1 = GetDecimalField(nameof(P1Measurements.InstantaneousVoltageL1), messages),
                    InstantaneousVoltageL2 = GetDecimalField(nameof(P1Measurements.InstantaneousVoltageL2), messages),
                    InstantaneousVoltageL3 = GetDecimalField(nameof(P1Measurements.InstantaneousVoltageL3), messages),
                    LongPowerFailuresInAnyPhase = GetIntegerField(nameof(P1Measurements.LongPowerFailuresInAnyPhase), messages),
                    PowerFailuresInAnyPhase = GetIntegerField(nameof(P1Measurements.PowerFailuresInAnyPhase), messages),
                    Tariff = GetIntegerField(nameof(P1Measurements.Tariff), messages),
                    TimeStamp = messageCollection.ReceivedUtc,
                    //Using the actual timestamp is for now ignored, due to not knowing wich time zone is applicable
                    //TimeStamp = GetDateTimeField(nameof(P1Measurements.TimeStamp), messages),
                    Version = GetStringField(nameof(P1Measurements.Version), messages)
                };

                await _measurementsBuffer.QueueMessage(_measurementMapper.Map(measurements), CancellationToken.None);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Could not parse serial message");
            }

            return measurements;
        }

        private void CreateFieldDictionary()
        {
            _fields = new Dictionary<string, OBISField>();

            PropertyInfo[] properties = typeof(P1Measurements).GetProperties();
            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                {
                    if (attribute is OBISField obisField)
                    {
                        _fields.Add(property.Name, obisField);
                    }
                }
            }
        }

        private decimal GetDecimalField(
            string fieldName,
            List<string> messages)
        {
            var (_, fieldValue) = GetField(fieldName, messages);
            try
            {
                return string.IsNullOrWhiteSpace(fieldValue) ? 0M :
                    decimal.Parse(fieldValue, CultureInfo.InvariantCulture);
            }
            catch (Exception exc)
            {
                throw new MessageParseException(fieldName, fieldValue, "decimal", exc);
            }
        }

        private (OBISField, string) GetField(
            string fieldName,
            List<string> messages)
        {
            var obisField = _fields[fieldName];

            foreach (var message in messages)
            {
                if (message.StartsWith(obisField.Reference))
                {
                    var match = Regex.Match(message, obisField.ValueRegex);

                    if (match.Success)
                    {
                        var matchGroup = match.Groups[1];

                        return (obisField, matchGroup.Value);
                    }
                }
            }

            return (obisField, string.Empty);
        }

        private int GetIntegerField(
            string fieldName,
            List<string> messages)
        {
            var (_, fieldValue) = GetField(fieldName, messages);

            try
            {
                return string.IsNullOrWhiteSpace(fieldValue) ? 0 :
                    int.Parse(fieldValue, CultureInfo.InvariantCulture);
            }
            catch (Exception exc)
            {
                throw new MessageParseException(fieldName, fieldValue, "int", exc);
            }
        }

        private string GetStringField(
            string fieldName,
            List<string> messages)
        {
            var (_, fieldValue) = GetField(fieldName, messages);

            return fieldValue;
        }
    }
}