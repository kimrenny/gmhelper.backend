using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatHelper.Tests.Services
{
    public class RequestLogServiceTests
    {
        private readonly Mock<IRequestLogRepository> _requestLogRepoMock;
        private readonly Mock<IAuthLogRepository> _authLogRepoMock;
        private readonly Mock<IErrorLogRepository> _errorLogRepoMock;
        private readonly Mock<ILogger<RequestLogService>> _loggerMock;
        private readonly RequestLogService _service;

        public RequestLogServiceTests()
        {
            _requestLogRepoMock = new Mock<IRequestLogRepository>();
            _authLogRepoMock = new Mock<IAuthLogRepository>();
            _errorLogRepoMock = new Mock<IErrorLogRepository>();
            _loggerMock = new Mock<ILogger<RequestLogService>>();

            _service = new RequestLogService(
                _requestLogRepoMock.Object,
                _authLogRepoMock.Object,
                _errorLogRepoMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetRequestStats_ReturnsData_WhenRepositorySucceeds()
        {
            var expected = new CombinedRequestLogDto();
            _requestLogRepoMock.Setup(r => r.GetRequestStatsAsync()).ReturnsAsync(expected);

            var result = await _service.GetRequestStats();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetRequestStats_ThrowsException_WhenRepositoryFails()
        {
            _requestLogRepoMock.Setup(r => r.GetRequestStatsAsync()).ThrowsAsync(new Exception("db error"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _service.GetRequestStats());

            Assert.Equal("Error occured during get requests stats.", ex.Message);
        }

        [Fact]
        public async Task GetRequestLogs_ReturnsData_WhenRepositorySucceeds()
        {
            var expected = new List<RequestLogDetail>
            {
                new RequestLogDetail
                {
                    Path = "/test",
                    Method = "GET"
                }
            };
            _requestLogRepoMock.Setup(r => r.GetAllRequestLogsAsync()).ReturnsAsync(expected);

            var result = await _service.GetRequestLogs();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetRequestLogs_ThrowsException_WhenRepositoryFails()
        {
            _requestLogRepoMock.Setup(r => r.GetAllRequestLogsAsync()).ThrowsAsync(new Exception("db error"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _service.GetRequestLogs());

            Assert.Equal("Error occured during get requests.", ex.Message);
        }

        [Fact]
        public async Task GetAuthLogs_ReturnsData_WhenRepositorySucceeds()
        {
            var expected = new List<AuthLog> { new AuthLog() };
            _authLogRepoMock.Setup(r => r.GetAllAuthLogsAsync()).ReturnsAsync(expected);

            var result = await _service.GetAuthLogs();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetAuthLogs_ThrowsException_WhenRepositoryFails()
        {
            _authLogRepoMock.Setup(r => r.GetAllAuthLogsAsync()).ThrowsAsync(new Exception("db error"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _service.GetAuthLogs());

            Assert.Equal("Error occured during get auth logs.", ex.Message);
        }

        [Fact]
        public async Task GetErrorLogs_ReturnsData_WhenRepositorySucceeds()
        {
            var expected = new List<ErrorLog> { new ErrorLog() };
            _errorLogRepoMock.Setup(r => r.GetAllErrorLogsAsync()).ReturnsAsync(expected);

            var result = await _service.GetErrorLogs();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetErrorLogs_ThrowsException_WhenRepositoryFails()
        {
            _errorLogRepoMock.Setup(r => r.GetAllErrorLogsAsync()).ThrowsAsync(new Exception("db error"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _service.GetErrorLogs());

            Assert.Equal("Error occured during get error logs.", ex.Message);
        }
    }
}
