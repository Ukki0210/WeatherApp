using Microsoft.AspNetCore.Mvc;
using WeatherApp.Server.Services;
using WeatherApp.Shared.Models;

namespace WeatherApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
        {
            _alertService = alertService;
            _logger = logger;
        }

        [HttpGet("{location}")]
        public async Task<ActionResult<List<ActiveWeatherAlert>>> GetAlerts(string location)
        {
            try
            {
                var alerts = await _alertService.GetActiveAlertsAsync(location);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alerts for location: {Location}", location);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ActiveWeatherAlert>> CreateAlert([FromBody] ActiveWeatherAlert alert)
        {
            try
            {
                if (alert == null)
                {
                    return BadRequest("Alert data is required");
                }

                var createdAlert = await _alertService.CreateAlertAsync(alert);
                return CreatedAtAction(nameof(GetAlerts), new { location = alert.Location }, createdAlert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{alertId}")]
        public async Task<ActionResult> DeleteAlert(string alertId)
        {
            try
            {
                var result = await _alertService.DeleteAlertAsync(alertId);
                if (result)
                    return NoContent();
                return NotFound($"Alert with ID {alertId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert: {AlertId}", alertId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
