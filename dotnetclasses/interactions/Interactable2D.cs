using Godot;
using System;

public partial class Interactable2D : Area2D
{
	public class InteracterData
	{
	}

	public Func<bool, String> getInteractionHint;
	public Action<InteracterData> interactionCallback;
	public Func<InteracterData, bool> canInteractCallback;

	public delegate void InteractorClose(InteracterData data);

	public delegate void InteractorAway();

	public event InteractorClose OnInteractorClose;
	public event InteractorAway OnInteractorAway;

	private bool _enabled = true;

	public void SetEnabled(bool enabled)
	{
		_enabled = enabled;
	}

	public bool IsEnabled()
	{
		return _enabled;
	}

	private Variant _interacterType;

	public Variant GetInteracterDataType()
	{
		return _interacterType;
	}

	public Interactable2D SetInteracterDataType(Variant type)
	{
		_interacterType = type;
		return this;
	}


	public bool CanInteract(InteracterData data)
	{
		if (canInteractCallback == null)
			return true;

		return canInteractCallback(data);
	}

	public void Interact(InteracterData data)
	{
		interactionCallback?.Invoke(data);
	}

	public void SignalInteracterClose(InteracterData data)
	{
		OnInteractorClose?.Invoke(data);
	}

	public void SignalInteracterAway()
	{
		OnInteractorAway?.Invoke();
	}
}