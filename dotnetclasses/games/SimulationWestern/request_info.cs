using System;
namespace WesternSimGame;

public class RequestInfo
{
    public enum RequestStatus
    {
        Pending,
        Accepted,
        Refused
    }

    public RequestStatus Status { get; set; }
    public string DescriptionIfAccepted { get; set; }
    public string DescriptionIfRefused { get; set; }

    public Action<GameState> CallbackAccepted { get; set; }
    public Action<GameState> CallbackRefused { get; set; }

    public string GetDescription()
    {
        return $"If accepted {DescriptionIfAccepted}, If refused {DescriptionIfRefused}";
    }

	public RequestInfo getDuplicated()
	{
		var newRequest = new RequestInfo();

		newRequest.Status = Status;
		newRequest.DescriptionIfAccepted = DescriptionIfAccepted;
		newRequest.DescriptionIfRefused = DescriptionIfRefused;
		newRequest.CallbackAccepted = CallbackAccepted;
		newRequest.CallbackRefused = CallbackRefused;

		return newRequest;
	}
}