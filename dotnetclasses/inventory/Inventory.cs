using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace Inventories;
public class LimitedSizeInventory<T>
{
	public delegate void InventoryChanged();
	public event InventoryChanged OnInventoryChagend;
	private List<ItemSlot<T>> slots;
	public class ItemSlot<TItem> 
	{
		public bool used;
		public TItem item;
		public int stackAmount;
		public int timesUsed;

		public ItemSlot<TItem> clone()
		{
			return new ItemSlot<TItem>
			{
				used = this.used,
				item = this.item,
				stackAmount = this.stackAmount,
				timesUsed = this.timesUsed
			};
		}
	}

	private EqualityComparer<T> comparer;
	private Func<T, int> getDurability;
	private Func<T, int> getMaxStackAmount;

	public LimitedSizeInventory(int capacity, EqualityComparer<T> comparer, Func<T, int> getDurability, Func<T, int> getMaxStackAmount, List<T> initialInventory=null)
	{
		Debug.Assert(capacity > 0);
		slots = new List<ItemSlot<T>>(capacity);
		foreach (var i in Enumerable.Range(0, capacity))
		{
			slots.Add(new ItemSlot<T>
			{
				used = false
			});
		}
		this.comparer = comparer;
		this.getDurability = getDurability;
		this.getMaxStackAmount = getMaxStackAmount;
		if (initialInventory != null)
		{
			foreach (var item in initialInventory)
			{
				addItem(item, getFirstAvailableSlot(item));
			}
		}
	}

	public LimitedSizeInventory<T> getClone()
	{
		var newInventory = new LimitedSizeInventory<T>(slots.Capacity, this.comparer, this.getDurability, this.getMaxStackAmount);
		Enumerable.Range(0, slots.Count-1).Where((i) => slots[i].used ).Select((i) =>
		{
			var thisSlot = slots[i];
			var newSlot = newInventory.slots[i];
			newSlot.used = true;
			newSlot.item = thisSlot.item;
			newSlot.stackAmount = thisSlot.stackAmount;
			newSlot.timesUsed = thisSlot.timesUsed;
			return 0;
		});

		return newInventory;
	}

	public int getCapacity()
	{
		return slots.Count;
	}

	public bool hasItem(T item)
	{
		return slots.Any((x) => x.used && comparer.Equals(item, x.item));
	}

	public T getItem(int id)
	{
		return slots[id].item;
	}

	public IEnumerable<ItemSlot<T>> getAllSlots()
	{
		for (var i=0; i < slots.Count; i++)
		{
			var slot = slots[i];
			yield return slot;
		}
	}
	public IEnumerable<int> getSlotIndices(Func<T, bool> filter)
	{
		for (var i=0; i < slots.Count; i++)
		{
			var slot = slots[i];
			if (slot.used && filter(slot.item))
			{
				yield return i;
			}
		}
	}

	public IEnumerable<int> getSlotIdxForItem(T item)
	{
		for (var i=0; i < slots.Count; i++)
		{
			var slot = slots[i];
			if (slot.used && comparer.Equals(item, slot.item))
			{
				yield return i;
			}
		}
	}

	public bool isSlotUsed(int i)
	{
		return slots[i].used;
	}

	public void move(int fromIdx, int toIdx)
	{
		var temp = slots[toIdx];
		slots[toIdx] = slots[fromIdx];
		slots[fromIdx] = temp;
		OnInventoryChagend?.Invoke();
	}

	public IEnumerable<T> getAllAvailableItems()
	{
		for (var i = 0; i < slots.Count; i++)
		{
			if (!slots[i].used)
			{
				continue;
			}
			if(!slots.Take(i).Any((x) => comparer.Equals(x.item, slots[i].item)))
			{
				yield return slots[i].item;
			}
		}
	}

	public int getStackAmountPerSlot(int i)
	{
		return slots[i].stackAmount;
	}

	public int getTimesUsedPerSlot(int i)
	{
		return slots[i].timesUsed;
	}

	public bool canBeStacked(T item)
	{
		return getMaxStackAmount(item) > 1 && !(getDurability(item) > 1);
	}

	public int getFirstAvailableSlot(T item)
	{ 
		var firstEmptySlot = Enumerable.Range(0, slots.Count-1).Where((i) => !slots[i].used).FirstOrDefault(-1);
		if (!canBeStacked(item))
		{
			return firstEmptySlot;
		}
		
		var availableIdx = getSlotIdxForItem(item).Where((i) => getStackAmountPerSlot(i) < getMaxStackAmount(item)).FirstOrDefault(-1); 
		if (availableIdx == -1)
		{
			return firstEmptySlot;
		}
		return availableIdx;
	}

	public void addItem(T item, int idx, int amount=1)
	{
		var slot = slots[idx];
		var stackable = canBeStacked(item);
		Debug.Assert(slot.used == false || (comparer.Equals(slot.item, item) && stackable));
		if (stackable && slot.used)
		{
			Debug.Assert((slot.stackAmount + amount ) <= getMaxStackAmount(item));
		}
		if (!stackable)
		{
			Debug.Assert(amount == 1);
		}
		slot.used = true;
		slot.item = item;
		slot.timesUsed = 0;
		slot.stackAmount += amount;
		OnInventoryChagend?.Invoke();
	}

	public void removeItem(int idx)
	{
		var slot = slots[idx];
		slot.timesUsed = 0;
		slot.stackAmount -= 1;
		if (slot.stackAmount <= 0)
		{
			slot.stackAmount = 0;
			slot.used = false;
		}
		OnInventoryChagend?.Invoke();
	}

	public bool increaseTimesUsed(int idx)
	{
		var slot = slots[idx];
		Debug.Assert(slot.used = true);
		slot.timesUsed += 1;
		OnInventoryChagend?.Invoke();
		return slot.timesUsed > getDurability(slot.item);
	}






}
