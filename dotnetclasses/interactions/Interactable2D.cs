using Godot;
using System;

public partial class Interactable2D : Area2D
{
	public class InteracterData
	{
	}

	private Action<InteracterData> _interactionCallback;
	private Func<InteracterData, bool> _canInteractCallback;

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

	public Interactable2D SetInteractionCallback(Action<InteracterData> interactionCallback)
	{
		_interactionCallback = interactionCallback;
		return this;
	}

	public Interactable2D SetCanInteractCallback(Func<InteracterData, bool> canInteractCallback)
	{
		_canInteractCallback = canInteractCallback;
		return this;
	}

	public bool CanInteract(InteracterData data)
	{
		if (_canInteractCallback == null)
			return true;

		return _canInteractCallback(data);
	}

	public void Interact(InteracterData data)
	{
		_interactionCallback?.Invoke(data);
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