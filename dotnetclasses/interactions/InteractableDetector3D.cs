using Godot;
using System;
using System.Collections.Generic;

public class InteractableDetectorComponent3D
{
	public enum InteractableDetectionMode
	{
		Radius,
		Raycast
	}

	public uint InteractablesMask { get; set; }

	public uint StaticMask { get; set; }

	public InteractableDetectionMode Mode { get; set; }

	public float DetectionRadius { get; set; } = 1.0f;

	public Node3D AgentNode { get; set; }

	public float RaycastLength { get; set; } = 2.0f;

	private Func<IEnumerable<Area3D>> _customCollectAreaCallback;

	private Func<bool> _confirmInteractionCallback;

	private Func<Variant, bool> _perDataConfirmInteractionCallback;

	private Func<Variant, Interactable3D.InteracterData>
		_interactionDataCallback;

	private int _limitInteraction = 1;

	public List<Interactable3D> LastAreas { get; private set; } = new();

	private Func<(Vector3, Vector3)> getRaycastStartAndDirection;
	public InteractableDetectorComponent3D(Node3D agentNode, uint interactableMask, InteractableDetectionMode mode, float detectionRadius, Func<(Vector3, Vector3)> getRaycastStartAndDirection, float raycastLength)
	{
		this.AgentNode = agentNode;
		this.InteractablesMask = interactableMask;
		this.DetectionRadius = detectionRadius;
		this.Mode = mode;
		this.RaycastLength = raycastLength;
		this.getRaycastStartAndDirection = getRaycastStartAndDirection;
	}

	public InteractableDetectorComponent3D SetCustomCollectAreaCallback(
		Func<IEnumerable<Area3D>> callback)
	{
		_customCollectAreaCallback = callback;
		return this;
	}

	public InteractableDetectorComponent3D SetConfirmInteractionCallback(
		Func<bool> callback)
	{
		_confirmInteractionCallback = callback;
		return this;
	}

	public InteractableDetectorComponent3D SetPerDataConfirmInteractionCallback(
		Func<Variant, bool> callback)
	{
		_perDataConfirmInteractionCallback = callback;
		return this;
	}

	public InteractableDetectorComponent3D SetInteractionDataCallback(
		Func<Variant, Interactable3D.InteracterData> callback)
	{
		_interactionDataCallback = callback;
		return this;
	}

	public InteractableDetectorComponent3D SetLimitInteraction(int limit)
	{
		_limitInteraction = limit;
		return this;
	}

	public void Detect()
	{
		var areas = new List<Area3D>();

		if (_customCollectAreaCallback != null)
		{
			IEnumerable<Area3D> collected =
				_customCollectAreaCallback();

			if (collected != null)
				areas.AddRange(collected);
		}
		else if (Mode == InteractableDetectionMode.Radius)
		{
			var collected = PhysicsUtils.Queries.CollectAreasInRadius3D(
				AgentNode.GetWorld3D().DirectSpaceState,
				AgentNode.GlobalPosition,
				InteractablesMask,
				DetectionRadius);

			if (collected != null)
			{
				foreach (Area3D area in collected)
					areas.Add(area);
			}
		}
		else
		{
			var (from, dir) = getRaycastStartAndDirection();
			Area3D area = PhysicsUtils.Queries.CheckAreaRaycastAvoidStatic3D(
				AgentNode.GetWorld3D().DirectSpaceState,
				StaticMask,
				InteractablesMask,
				from,
				from + dir * RaycastLength);

			if (area != null)
				areas.Add(area);
		}

		var interactableAreas = new List<Interactable3D>();

		foreach (Area3D area in areas)
		{
			if (GodotObject.IsInstanceValid(area)
				&& area is Interactable3D interactable
				&& interactable.IsEnabled())
			{
				interactableAreas.Add(interactable);
			}
		}

		// Notify interactables that are no longer detected.
		for (int i = LastAreas.Count - 1; i >= 0; i--)
		{
			Interactable3D previous = LastAreas[i];

			if (!GodotObject.IsInstanceValid(previous))
			{
				LastAreas.RemoveAt(i);
				continue;
			}

			if (!interactableAreas.Contains(previous))
			{
				previous.SignalInteracterAway();
				LastAreas.RemoveAt(i);
			}
		}

		if (interactableAreas.Count == 0)
		{
			LastAreas.Clear();
			return;
		}

		LastAreas = new List<Interactable3D>(interactableAreas);

		int interacted = 0;

		foreach (Interactable3D interactable in interactableAreas)
		{
			Variant dataType = interactable.GetInteracterDataType();

			Interactable3D.InteracterData data = null;

			if (_interactionDataCallback != null)
				data = _interactionDataCallback(dataType);

			bool canInteract = interactable.CanInteract(data);

			interactable.SignalInteracterClose(data);

			bool confirmed;

			if (_perDataConfirmInteractionCallback != null)
			{
				confirmed = _perDataConfirmInteractionCallback(dataType);
			}
			else
			{
				confirmed = _confirmInteractionCallback != null
					&& _confirmInteractionCallback();
			}

			if (confirmed && canInteract)
			{
				interactable.Interact(data);
				interacted++;

				if (interacted >= _limitInteraction)
					return;
			}
		}
	}
}
