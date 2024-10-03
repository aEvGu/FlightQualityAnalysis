namespace FlightQualityAnalysis.Tests;

[TestClass]
public class FlightServiceTests
{
    private FlightService _flightService;

    [TestInitialize]
    public void Initialize()
    {
        // Initialize the FlightService before each test
        _flightService = new FlightService();
    }

    [TestMethod]
    public void Test_FindFlightsWithMissingData_ShouldReturnCorrectWarnings()
    {
        // Arrange: Create a list of flights with some missing data
        var flights = new List<Flight>
            {
                new Flight { FlightNumber = "AA123", DepartureAirport = null, ArrivalAirport = "LAX", DepartureDatetime = DateTime.Now, ArrivalDatetime = DateTime.Now.AddHours(2) },
                new Flight { FlightNumber = "BB456", DepartureAirport = "JFK", ArrivalAirport = null, DepartureDatetime = DateTime.Now, ArrivalDatetime = DateTime.Now.AddHours(2) }
            };

        // Act: Call the method
        var result = _flightService.FindFlightsWithMissingDataDetailed(flights);

        // Assert: Check if we get the correct warnings
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Contains("Flight AA123: Missing Departure Airport."));
        Assert.IsTrue(result.Contains("Flight BB456: Missing Arrival Airport."));
    }

    [TestMethod]
    public void Test_FindInconsistentAirportTransitions_ShouldReturnCorrectInconsistencies()
    {
        // Arrange: Create flights with inconsistent airport transitions
        var flights = new List<Flight>
            {
                new Flight { AircraftRegistrationNumber = "N123", FlightNumber = "AA123", DepartureAirport = "JFK", ArrivalAirport = "LAX", DepartureDatetime = DateTime.Now, ArrivalDatetime = DateTime.Now.AddHours(2) },
                new Flight { AircraftRegistrationNumber = "N123", FlightNumber = "AA456", DepartureAirport = "SFO", ArrivalAirport = "ORD", DepartureDatetime = DateTime.Now.AddHours(3), ArrivalDatetime = DateTime.Now.AddHours(5) }
            };

        // Act: Call the method
        var result = _flightService.FindInconsistentAirportTransitions(flights);

        // Assert: Check if the inconsistency is detected
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].Contains("Inconsistent Transition: Aircraft N123"));
    }

    [TestMethod]
    public void Test_FindInconsistentTimeSequences_ShouldDetectIncorrectTimeOrder()
    {
        // Arrange: Create flights with inconsistent time sequences (arrival before departure)
        var flights = new List<Flight>
            {
                new Flight { FlightNumber = "AA123", DepartureAirport = "JFK", ArrivalAirport = "LAX", DepartureDatetime = DateTime.Now, ArrivalDatetime = DateTime.Now.AddHours(-1) }
            };

        // Act: Call the method
        var result = _flightService.FindInconsistentTimeSequences(flights);

        // Assert: Verify the result contains the inconsistency
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].Contains("Inconsistent Time Sequence: Flight AA123 arrives earlier than it departs."));
    }

    [TestMethod]
    public void Test_FindUnrealisticTurnaroundTimes_ShouldDetectShortTurnaround()
    {
        // Arrange: Create flights with unrealistic turnaround times
        var flights = new List<Flight>
            {
                new Flight { AircraftRegistrationNumber = "N123", FlightNumber = "AA123", DepartureAirport = "JFK", ArrivalAirport = "LAX", DepartureDatetime = DateTime.Now, ArrivalDatetime = DateTime.Now.AddHours(2) },
                new Flight { AircraftRegistrationNumber = "N123", FlightNumber = "AA456", DepartureAirport = "LAX", ArrivalDatetime = DateTime.Now.AddHours(2), DepartureDatetime = DateTime.Now.AddHours(2).AddMinutes(30) }
            };

        // Act: Call the method with a minimum turnaround time of 2 hours
        var result = _flightService.FindUnrealisticTurnaroundTimes(flights, TimeSpan.FromHours(2));

        // Assert: Check if we detect the unrealistic turnaround time
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].Contains("Unrealistic Turnaround"));
    }

    [TestMethod]
    public void Test_CheckFlightNumberConsistency_ShouldDetectInconsistenciesAcrossAircraft()
    {
        // Arrange: Create flights with the same flight number but different aircraft with inconsistent data
        var flights = new List<Flight>
            {
                new Flight { FlightNumber = "AA123", AircraftRegistrationNumber = "N123", DepartureAirport = "JFK", ArrivalAirport = "LAX", DepartureDatetime = DateTime.Now, ArrivalDatetime = DateTime.Now.AddHours(2) },
                new Flight { FlightNumber = "AA123", AircraftRegistrationNumber = "N456", DepartureAirport = "JFK", ArrivalAirport = "SFO", DepartureDatetime = DateTime.Now.AddHours(3), ArrivalDatetime = DateTime.Now.AddHours(5) }
            };

        // Act: Call the method
        var result = _flightService.CheckFlightNumberConsistency(flights);

        // Assert: Verify the inconsistency in flight numbers is detected
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].Contains("Flight Number AA123 inconsistency"));
    }
}
