using CMR_API.DataConnections;
using CMR_API.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using YourProject.Controllers;

namespace CMR_API.Tests
{
    [TestFixture]
    public class MeterReadingServiceTests
    {


        //private Mock<ENSEK_DbContext> _dbContextMock;
        //private Mock<ILogger<MeterReadingController>> _loggerMock;
        //private MeterReadingService _meterReadingService;

        //[SetUp]
        //public void Setup()
        //{
        //    _dbContextMock = new Mock<ENSEK_DbContext>();
        //    _loggerMock = new Mock<ILogger<MeterReadingController>>();
        //    _meterReadingService = new MeterReadingService(_dbContextMock.Object, _loggerMock.Object);
        //}


        [Test]
        public void TestingMyNugets()
        {
            var result = 1;
            Assert.IsTrue(result == 1);
        }

        [Test]
        public async Task ProcessMeterReadings_ValidCsvFile_ReturnsSuccessCounts()
        {

        }
    }
}
