using System;
namespace WesternSimGame;

public struct RequestInfo
{
    public enum RequestStatus
    {
		Unused,
        Pending,
        Accepted,
        Refused
    }

    public RequestStatus Status { get; set; }
    public string DescriptionIfAccepted { get; set; }
    public string DescriptionIfRefused { get; set; }

    public Action<GameState, GameState.CharacterActionExecution> CallbackAccepted { get; set; }
    public Action<GameState, GameState.CharacterActionExecution> CallbackRefused { get; set; }

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