# Flight Quality Analysis

## Overview

The **Flight Quality Analysis** project is a .NET 7.0 Web API that reads flight data from a CSV file, detects inconsistencies, and provides an API to retrieve and analyze flight data. The API checks for missing data, inconsistencies in airport transitions, and issues like unrealistic turnaround times.

---

## Features

- Load flight data from a CSV file.
- Detect missing flight information (departure/arrival airport and time).
- Identify inconsistent airport transitions for the same aircraft.
- Detect flights with unrealistic turnaround times.
- Ensure flights are in logical time sequence (no arrivals before departure).
- Handle repeated aircraft registration numbers and flight numbers appropriately for real-world scenarios.

---

## Prerequisites

- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- IDE or editor (e.g., Visual Studio Code, Visual Studio)

---

## Setup

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/FlightQualityAnalysis.git
cd FlightQualityAnalysis

### 2. Restore Dependencies

Restore the required dependencies by running:

dotnet restore

### 3. Build the Project

To build the solution, run:

dotnet build

### 4. Run the Application

Start the application by running:

dotnet run

The API should be running at https://localhost:5118.

### API Endpoints
Base URL: //api/flight
GET	/all	(Get all flight data)
GET	/check-inconsistencies	(Check for inconsistencies (missing data, unrealistic turnaround, inconsistent time sequences, and airport transitions))

curl -X GET "http://localhost:5118/api/flight/all"
curl -X GET "http://localhost:5118/api/flight/check-inconsistencies"

### Testing

Unit tests are written using MSTest and cover the following:

    Detecting missing flight data (departure/arrival airport, times).
    Checking inconsistent airport transitions.
    Detecting unrealistic turnaround times.
    Checking for incorrect time sequences.
    
To run the tests, use the following command:

dotnet test