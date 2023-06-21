using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YourProject.Controllers;
using CMR_API.Entities;
using CMR_API.DataConnections;

namespace CMR_API.Tests
{
    [TestFixture]
    public class MeterReadingServiceTests
    {
        private ENSEK_DbContext _dbContext;
        private Mock<ILogger<MeterReadingController>> _loggerMock;
        private MeterReadingService _meterReadingService;

        [SetUp]
        public void Setup()
        {

            //Used in memory DB here as had some issues using other test formats, provided the ability not to mock up the DB and simply create an in memory one to test against

            var options = new DbContextOptionsBuilder<ENSEK_DbContext>()
                .UseInMemoryDatabase(databaseName: "ENSEK_TestDatabase")
                .Options;

            _dbContext = new ENSEK_DbContext(options);
            _loggerMock = new Mock<ILogger<MeterReadingController>>();
            _meterReadingService = new MeterReadingService(_dbContext, _loggerMock.Object);

            SeedTestData();
        }

        [TearDown]
        public void Cleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        private void SeedTestData()
        {
            var accounts = new List<Account>
            {
                new Account { AccountId = 1234, FirstName = "Some", LastName = "Body"},
                new Account {AccountId = 4321, FirstName = "Some", LastName = "Body"}
            };
            _dbContext.Accounts.AddRange(accounts);
            _dbContext.SaveChanges();
        }

        [Test]
        public async Task AccountExists_AccountExists_ReturnsTrue()
        {
            int accountId = 1234;
            var result = await _meterReadingService.AccountExists(accountId);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task AccountExists_AccountDoesNotExist_ReturnsFalse()
        {
            int accountId = 9999;
            var result = await _meterReadingService.AccountExists(accountId);
            Assert.IsFalse(result);
        }

        [Test]
        public void IsDuplicateEntry_DuplicateEntryExists_ReturnsTrue()
        {
            var meterReading = new MeterReading
            {
                AccountId = 1234,
                MeterReadingDateTime = DateTime.Now,
                MeterReadValue = 100
            };
            _dbContext.MeterReadings.Add(meterReading);
            _dbContext.SaveChanges();
            var result = _meterReadingService.IsDuplicateEntry(meterReading);
            Assert.IsTrue(result);
        }

        [Test]
        public void IsDuplicateEntry_NoDuplicateEntryExists_ReturnsFalse()
        {
            var meterReading = new MeterReading
            {
                AccountId = 1234,
                MeterReadingDateTime = DateTime.Now,
                MeterReadValue = 100
            };
            var result = _meterReadingService.IsDuplicateEntry(meterReading);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task ValidateMeterReading_InvalidMeterReadValue_ReturnsFalse()
        {
            var meterReading = new MeterReading
            {
                AccountId = 1234,
                MeterReadingDateTime = DateTime.Now,
                MeterReadValue = -100
            };
            var result = await _meterReadingService.ValidateMeterReading(meterReading);
            Assert.IsFalse(result);
        }
    }
}
