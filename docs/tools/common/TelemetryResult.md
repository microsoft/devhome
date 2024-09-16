# TelemetryResult
Used to extract data from a `ProviderOperationResult` object returned by an SDK method or wrapper class. This can also be used as a general class to store error information that can be sent in a telemetry payload.

## Properties
| Property | Type | Description |
| -------- | -------- | -------- |
| HResult | Integer | HRESULT code returned by an operation to Dev Home or an extension.
| Status | ProviderOperationStatus | Enum used to dictate whether an  operation succeeded or failed.  |
| DisplayMessage | String? | Optional display message returned to the user from an interaction with Dev Home. This may be localized. |
| DiagnosticText | String? | Optional diagnostic text return by an operation. This can be used to gain insights into errors that occur in an extension, after an operation is initiated by Dev Home. |

## Usage
#### ComputeSystemViewModel.cs
```C#
// Using the parameterless constructor. This is optional. Most events in Dev Home only care about the result of the operation and not that the operation started.
var startLaunchEvent = new EnvironmentLaunchEvent(
    ComputeSystemProviderId,
    EnvironmentsTelemetryStatus.Started,
    new TelemetryResult());

// Send the telemetry stating that the operation has started.
TelemetryFactory.Get<ITelemetry>().Log(
    "Environment_Launch_Event",
    LogLevel.Critical,
    startLaunchEvent);

// It's good practise to wrap the out of proc COM object in a wrapper class and return the associated SDK result. Exceptions caught by the wrapper can then be used to return an SDK result that indicates that the operation failed.
var launchResponse = await ComputeSystemWrapper.ConnectAsync(string.Empty);

// ... Handle any post operation logic..

// Use the TelemetryResult to extract data from the ProviderOperationResult, so it can be used in an Event payload.
var endLaunchEvent = new EnvironmentLaunchEvent(
    ComputeSystemProviderId,
    telemetryStatusBasedOnResponse,
    new TelemetryResult(launchResponse?.Result));

// Operation is completed and the payload for ending the event can be sent.
TelemetryFactory.Get<ITelemetry>().Log(
    "Environment_Launch_Event",
    LogLevel.Critical,
    endLaunchEvent);
```