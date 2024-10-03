using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

public class FlightService
{
    /// <summary>
    /// Loads flight data from the specified CSV file.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <returns>List of flight records from the CSV file.</returns>
    public List<Flight> LoadFlights(string filePath)
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<FlightMap>();
            var records = csv.GetRecords<Flight>().ToList();
            return records;
        }
    }

    /// <summary>
    /// Finds flights with missing critical data (e.g., missing departure or arrival airport, missing datetime).
    /// </summary>
    /// <param name="flights">List of flight records.</param>
    /// <returns>List of warnings describing which data is missing for each flight.</returns>
    public List<string> FindFlightsWithMissingDataDetailed(List<Flight> flights)
    {
        var warnings = new List<string>();

        foreach (var flight in flights)
        {
            if (string.IsNullOrEmpty(flight.DepartureAirport))
                warnings.Add($"Flight {flight.FlightNumber}: Missing Departure Airport.");

            if (string.IsNullOrEmpty(flight.ArrivalAirport))
                warnings.Add($"Flight {flight.FlightNumber}: Missing Arrival Airport.");

            if (flight.DepartureDatetime == null)
                warnings.Add($"Flight {flight.FlightNumber}: Missing Departure Datetime.");

            if (flight.ArrivalDatetime == null)
                warnings.Add($"Flight {flight.FlightNumber}: Missing Arrival Datetime.");
        }

        return warnings;
    }

    /// <summary>
    /// Checks for inconsistent airport transitions between flights for the same aircraft.
    /// </summary>
    /// <param name="flights">List of flight records.</param>
    /// <returns>List of flights with inconsistent airport transitions.</returns>
    public List<string> FindInconsistentAirportTransitions(List<Flight> flights)
    {
        // Sort flights by aircraft, then by departure time (since aircraft may repeat)
        var orderedFlights = flights.OrderBy(f => f.AircraftRegistrationNumber)
                                    .ThenBy(f => f.DepartureDatetime)
                                    .ToList();

        var inconsistentFlights = new List<string>();

        for (int i = 1; i < orderedFlights.Count; i++)
        {
            var currentFlight = orderedFlights[i];
            var previousFlight = orderedFlights[i - 1];

            // Ensure we're tracking the same aircraft
            if (currentFlight.AircraftRegistrationNumber == previousFlight.AircraftRegistrationNumber)
            {
                // Check if the current flight's departure airport matches the previous flight's arrival airport
                if (currentFlight.DepartureAirport != previousFlight.ArrivalAirport)
                {
                    inconsistentFlights.Add(
                        $"Inconsistent Transition: Aircraft {currentFlight.AircraftRegistrationNumber}, " +
                        $"Flight {currentFlight.FlightNumber} departs from {currentFlight.DepartureAirport} after " +
                        $"arriving at {previousFlight.ArrivalAirport}."
                    );
                }
            }
        }

        return inconsistentFlights;
    }

    /// <summary>
    /// Checks for time sequence inconsistencies (e.g., arrival time earlier than departure time).
    /// This considers repeated flight number, as they are not unique identifiers.
    /// </summary>
    /// <param name="flights">List of flight records.</param>
    /// <returns>List of flights with time sequence inconsistencies.</returns>
    public List<string> FindInconsistentTimeSequences(List<Flight> flights)
    {
        var inconsistentFlights = new List<string>();

        foreach (var flight in flights)
        {
            // Ensure arrival time is not earlier than departure time
            if (flight.ArrivalDatetime < flight.DepartureDatetime)
            {
                inconsistentFlights.Add(
                    $"Inconsistent Time Sequence: Flight {flight.FlightNumber} arrives earlier than it departs."
                );
            }
        }

        return inconsistentFlights;
    }


    /// <summary>
    /// Finds flights with unrealistic turnaround times between consecutive flights for the same aircraft.
    /// This accounts for repeating aircraft registration numbers.
    /// </summary>
    /// <param name="flights">List of flight records.</param>
    /// <param name="minimumTurnaroundTime">The minimum allowed turnaround time.</param>
    /// <returns>List of flights with unrealistic turnaround times.</returns>
    public List<string> FindUnrealisticTurnaroundTimes(List<Flight> flights, TimeSpan minimumTurnaroundTime)
    {
        var orderedFlights = flights.OrderBy(f => f.AircraftRegistrationNumber)
                                    .ThenBy(f => f.DepartureDatetime)
                                    .ToList();

        var inconsistentFlights = new List<string>();

        for (int i = 1; i < orderedFlights.Count; i++)
        {
            var currentFlight = orderedFlights[i];
            var previousFlight = orderedFlights[i - 1];

            // Ensure we are tracking the same aircraft
            if (currentFlight.AircraftRegistrationNumber == previousFlight.AircraftRegistrationNumber)
            {
                // Check if the departure airport matches the previous arrival airport and if the turnaround time is realistic
                if (currentFlight.DepartureAirport == previousFlight.ArrivalAirport)
                {
                    var timeDifference = currentFlight.DepartureDatetime - previousFlight.ArrivalDatetime;
                    if (timeDifference.HasValue && timeDifference.Value < minimumTurnaroundTime)
                    {
                        inconsistentFlights.Add(
                            $"Unrealistic Turnaround: Aircraft {currentFlight.AircraftRegistrationNumber}, " +
                            $"Flight {currentFlight.FlightNumber} departs {timeDifference.Value.TotalMinutes} minutes after arriving at {previousFlight.ArrivalAirport}."
                        );
                    }
                }
            }
        }

        return inconsistentFlights;
    }

    /// <summary>
    /// Checks for inconsistencies in flights with the same flight number but different aircraft.
    /// Ensures that the same flight number has consistent departure and arrival airports across different flights.
    /// </summary>
    /// <param name="flights">List of flight records.</param>
    /// <returns>List of inconsistencies found for flights with the same flight number but different aircraft.</returns>
    public List<string> CheckFlightNumberConsistency(List<Flight> flights)
    {
        var inconsistencies = new List<string>();

        // Group flights by flightNumber
        var flightGroups = flights.GroupBy(f => f.FlightNumber).ToList();

        foreach (var group in flightGroups)
        {
            var groupedFlights = group.OrderBy(f => f.DepartureDatetime).ToList();

            for (int i = 1; i < groupedFlights.Count; i++)
            {
                var currentFlight = groupedFlights[i];
                var previousFlight = groupedFlights[i - 1];

                // Ensure consistency in departure and arrival airports for the same flight number
                if (currentFlight.DepartureAirport != previousFlight.DepartureAirport || currentFlight.ArrivalAirport != previousFlight.ArrivalAirport)
                {
                    inconsistencies.Add(
                        $"Flight Number {currentFlight.FlightNumber} inconsistency: " +
                        $"Departure/Arrival airports differ between flights. " +
                        $"Flight {previousFlight.FlightNumber} from {previousFlight.DepartureAirport} to {previousFlight.ArrivalAirport}, " +
                        $"but Flight {currentFlight.FlightNumber} from {currentFlight.DepartureAirport} to {currentFlight.ArrivalAirport}."
                    );
                }

                // Check for overlapping flight times for the same flight number
                if (currentFlight.DepartureDatetime < previousFlight.ArrivalDatetime)
                {
                    inconsistencies.Add(
                        $"Flight Number {currentFlight.FlightNumber} overlap: " +
                        $"Flight {currentFlight.FlightNumber} departs at {currentFlight.DepartureDatetime} " +
                        $"before previous flight {previousFlight.FlightNumber} arrives at {previousFlight.ArrivalDatetime}."
                    );
                }
            }
        }

        return inconsistencies;
    }

    /// <summary>
    /// Identifies missing flights in the chain based on aircraft registration.
    /// Accounts for repeated aircraft registration numbers, ensuring the arrival airport matches the next departure airport.
    /// </summary>
    /// <param name="flights">List of flight records.</param>
    /// <returns>List of flights that indicate missing flight connections.</returns>
    public List<string> FindMissingFlights(List<Flight> flights)
    {
        var orderedFlights = flights.OrderBy(f => f.AircraftRegistrationNumber)
                                    .ThenBy(f => f.DepartureDatetime)
                                    .ToList();
        var missingFlights = new List<string>();

        for (int i = 1; i < orderedFlights.Count; i++)
        {
            var currentFlight = orderedFlights[i];
            var previousFlight = orderedFlights[i - 1];

            // Ensure we are tracking the same aircraft
            if (currentFlight.AircraftRegistrationNumber == previousFlight.AircraftRegistrationNumber)
            {
                // Ensure the current flight's departure airport matches the previous flight's arrival airport
                if (currentFlight.DepartureAirport != previousFlight.ArrivalAirport)
                {
                    missingFlights.Add(
                        $"Missing Flight: Aircraft {currentFlight.AircraftRegistrationNumber} arrived at {previousFlight.ArrivalAirport}, " +
                        $"but the next flight departs from {currentFlight.DepartureAirport}."
                    );
                }
            }
        }

        return missingFlights;
    }
}

public class FlightMap : ClassMap<Flight>
{
    public FlightMap()
    {
        Map(m => m.Id).Name("id");
        Map(m => m.AircraftRegistrationNumber).Name("aircraft_registration_number");
        Map(m => m.AircraftType).Name("aircraft_type");
        Map(m => m.FlightNumber).Name("flight_number");
        Map(m => m.DepartureAirport).Name("departure_airport");
        Map(m => m.DepartureDatetime).Name("departure_datetime");
        Map(m => m.ArrivalAirport).Name("arrival_airport");
        Map(m => m.ArrivalDatetime).Name("arrival_datetime");
    }
}
