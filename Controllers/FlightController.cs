using Microsoft.AspNetCore.Mvc;

namespace FlightQualityAnalysis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightController : ControllerBase
{
    private readonly FlightService _flightService;
    private readonly IWebHostEnvironment _env;
    private readonly string _csvFilePath;

    public FlightController(FlightService flightService, IWebHostEnvironment env)
    {
        _flightService = flightService;
        _env = env;
        _csvFilePath = Path.Combine(_env.ContentRootPath, "Content", "flights.csv");
    }

    [HttpGet("all")]
    public IActionResult GetAllFlights()
    {
        var flights = _flightService.LoadFlights(_csvFilePath);
        return Ok(flights);
    }

    /// <summary>
    /// Consolidated endpoint that runs all inconsistency checks and returns a logical report.
    /// </summary>
    /// <returns>List of detailed inconsistency reports.</returns>
    [HttpGet("check-inconsistencies")]
    public IActionResult GetInconsistencies()
    {
        var flights = _flightService.LoadFlights(_csvFilePath);

        // Initialize a list to store all the results
        var inconsistencyReport = new List<string>();

        // Check for missing data
        var missingData = _flightService.FindFlightsWithMissingDataDetailed(flights);
        if (missingData.Any())
        {
            inconsistencyReport.Add("Missing Data Inconsistencies:");
            inconsistencyReport.AddRange(missingData);
        }

        // Check for missing flights
        var missingFlights = _flightService.FindMissingFlights(flights);
        if (missingFlights.Any())
        {
            inconsistencyReport.Add("Missing Flight Transitions:");
            inconsistencyReport.AddRange(missingFlights);
        }

        // Check for inconsistent airport transitions
        var inconsistentAirportTransitions = _flightService.FindInconsistentAirportTransitions(flights);
        if (inconsistentAirportTransitions.Any())
        {
            inconsistencyReport.Add("Inconsistent Airport Transitions:");
            inconsistencyReport.AddRange(inconsistentAirportTransitions);
        }

        // Check for inconsistent time sequences
        var inconsistentTimeSequences = _flightService.FindInconsistentTimeSequences(flights);
        if (inconsistentTimeSequences.Any())
        {
            inconsistencyReport.Add("Inconsistent Time Sequences:");
            inconsistencyReport.AddRange(inconsistentTimeSequences);
        }

        // Check for unrealistic turnaround times
        var unrealisticTurnaroundTimes = _flightService.FindUnrealisticTurnaroundTimes(flights, new TimeSpan(2, 0, 0));
        if (unrealisticTurnaroundTimes.Any())
        {
            inconsistencyReport.Add("Unrealistic Turnaround Times:");
            inconsistencyReport.AddRange(unrealisticTurnaroundTimes);
        }

        // Check for flightNumber consistency (e.g., same flightNumber with different aircraft)
        var flightNumberInconsistencies = _flightService.CheckFlightNumberConsistency(flights);
        if (flightNumberInconsistencies.Any())
        {
            inconsistencyReport.Add("Flight Number Inconsistencies:");
            inconsistencyReport.AddRange(flightNumberInconsistencies);
        }

        // If no inconsistencies were found, return a success message
        if (!inconsistencyReport.Any())
        {
            inconsistencyReport.Add("No inconsistencies found in the flight data.");
        }

        // Return the combined inconsistency report
        return Ok(inconsistencyReport);
    }
}
