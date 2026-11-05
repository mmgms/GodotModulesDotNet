
using Godot;
using System;

public partial class Interactable3D : Area3D
{
	public class InteracterData
	{
	}

	public event Action<InteracterData> InteractorClose;
	public event Action InteractorAway;

	public Action<InteracterData> onInteraction;
	public Func<InteracterData, bool> canInteract;
	public Func<bool, String> getActionDescription;

	private bool _enabled = true;
	private Variant _interacterType;

	public void SetEnabled(bool enabled)
	{
		_enabled = enabled;
	}

	public bool IsEnabled()
	{
		return _enabled;
	}

	public Variant GetInteracterDataType()
	{
		return _interacterType;
	}

	public Interactable3D SetInteracterDataType(Variant type)
	{
		_interacterType = type;
		return this;
	}

	public bool CanInteract(InteracterData data)
	{
		if (canInteract == null)
			return true;

		return canInteract(data);
	}

	public void Interact(InteracterData data)
	{
		onInteraction?.Invoke(data);
	}

	public void SignalInteracterClose(InteracterData data)
	{
		InteractorClose?.Invoke(data);
	}

	public void SignalInteracterAway()
	{
		InteractorAway?.Invoke();
	}
}
