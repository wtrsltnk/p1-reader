using FluentAssertions;
using P1ReaderApp.Model;
using P1ReaderApp.Services;
using System.Threading.Tasks;
using Xunit;

namespace P1ReaderApp.Tests
{
    public class MessageParserTests
    {
        [Fact]
        public async Task MessageVariant1_ParseSucces()
        {
            // Arrange
            var messageBuffer = new MessageBuffer<P1Measurements>();
            var parser = new MessageParser(messageBuffer);

            // Act
            var measurements = await parser.ParseSerialMessages(TestP1Messages.MessageVariant1);

            // Assert
            measurements.Should().NotBeNull();
        }
    }
}