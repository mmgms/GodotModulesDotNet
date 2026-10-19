using Godot;
using System;
using System.Collections.Generic;

public class InteractableDetectorComponent2D
{
	public uint InteractablesMask { get; set; }

	public float DetectionRadius { get; set; }

	public Node2D AgentNode { get; set; }

	private Func<IEnumerable<Area2D>> _customCollectAreaCallback;
	private Func<bool> _confirmInteractionCallback;
	private Func<Variant, bool> _perDataConfirmInteractionCallback;
	private Func<Variant, Interactable2D.InteracterData> _interactionDataCallback;

	private int _limitInteraction = 1;

	public Godot.Collections.Array<Interactable2D> LastAreas { get; private set; } = new();

	public InteractableDetectorComponent2D(Node2D node, uint interactableMask, float detectionRadius)
	{
		AgentNode = node;
		InteractablesMask = interactableMask;
		DetectionRadius = detectionRadius; 
	}

	public InteractableDetectorComponent2D SetCustomCollectAreaCallback(
		Func<Godot.Collections.Array<Area2D>> callback)
	{
		_customCollectAreaCallback = callback;
		return this;
	}

	public InteractableDetectorComponent2D SetConfirmInteractionCallback(
		Func<bool> callback)
	{
		_confirmInteractionCallback = callback;
		return this;
	}

	public InteractableDetectorComponent2D SetPerDataConfirmInteractionCallback(
		Func<Variant, bool> callback)
	{
		_perDataConfirmInteractionCallback = callback;
		return this;
	}

	public InteractableDetectorComponent2D SetInteractionDataCallback(
		Func<Variant, Interactable2D.InteracterData> callback)
	{
		_interactionDataCallback = callback;
		return this;
	}

	public InteractableDetectorComponent2D SetLimitInteraction(int limit)
	{
		_limitInteraction = Math.Max(0, limit);
		return this;
	}

	/// <summary>
	/// Detects nearby interactables. Call this every frame.
	/// </summary>
	public void Detect()
	{
		IEnumerable<Area2D> areas;

		if (_customCollectAreaCallback != null)
		{
			areas = _customCollectAreaCallback();
		}
		else
		{
			if (!GodotObject.IsInstanceValid(AgentNode))
				return;

			areas = PhysicsUtils.Queries.CollectAreasInRadius2D(
				AgentNode.GetWorld2D().DirectSpaceState,
				AgentNode.GlobalPosition,
				InteractablesMask,
				DetectionRadius);
		}

		var interactableAreas = new Godot.Collections.Array<Interactable2D>();

		foreach (Area2D area in areas)
		{
			if (!GodotObject.IsInstanceValid(area))
				continue;

			if (area is not Interactable2D interactable)
				continue;

			if (!interactable.IsEnabled())
				continue;

			interactableAreas.Add(interactable);
		}

		// Notify interactables that are no longer detected.
		var previousAreas = LastAreas.Duplicate();

		foreach (Interactable2D previous in previousAreas)
		{
			if (!GodotObject.IsInstanceValid(previous))
				continue;

			if (!interactableAreas.Contains(previous))
				previous.SignalInteracterAway();
		}

		LastAreas = interactableAreas.Duplicate();

		if (interactableAreas.Count == 0)
			return;

		int interactedCount = 0;

		foreach (Interactable2D interactable in interactableAreas)
		{
			Variant dataType = interactable.GetInteracterDataType();

			Interactable2D.InteracterData data = null;

			if (_interactionDataCallback != null)
				data = _interactionDataCallback(dataType);

			bool canInteract = interactable.CanInteract(data);

			// Notify the interactable that the detector is nearby.
			interactable.SignalInteracterClose(data);

			bool interactionConfirmed;

			if (_perDataConfirmInteractionCallback != null)
			{
				interactionConfirmed =
					_perDataConfirmInteractionCallback(dataType);
			}
			else
			{
				interactionConfirmed =
					_confirmInteractionCallback != null &&
					_confirmInteractionCallback();
			}

			if (!interactionConfirmed || !canInteract)
				continue;

			interactable.Interact(data);
			interactedCount++;

			if (interactedCount >= _limitInteraction)
				return;
		}
	}
}