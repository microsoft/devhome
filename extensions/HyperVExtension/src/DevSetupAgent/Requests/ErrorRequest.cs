// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle invalid requests (for example an exception while parsing request JSON).
/// It creates an error response to send back to the client.
/// </summary>
internal class ErrorRequest : IHostRequest
{
    public ErrorRequest(IRequestMessage requestMessage, Exception? ex = null)
    {
        Timestamp = DateTime.UtcNow;
        RequestId = requestMessage.CommunicationId!;
        Error = ex;
    }

    public virtual uint Version { get; set; } = 1;

    public virtual bool IsStatusRequest => true;

    public virtual string RequestId { get; }

    public virtual string RequestType => "ErrorNoData";

    public DateTime Timestamp { get; }

    public virtual IHostResponse Execute(IProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        return new ErrorResponse(RequestId, Error);
    }

    private Exception? Error { get; }
}
