using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WesternSimGame;

public class CharacterInfo
{
	public delegate void StatsChangedEvent();	
	public event StatsChangedEvent OnStatsChanged;

	public short Id { get; set; }
    public string Name { get; set;}

    public Definitions.CharacterType Type { get; set; }
    private int Hp;
    private bool Dead;
    private int Hunger;
    private int Gold;
    private Definitions.PlaceType CurrentPlace;

	private const int MAX_INVENTORY_SIZE = 5;
    private ItemSlotInfo[] Inventory = new ItemSlotInfo[MAX_INVENTORY_SIZE];

    private bool HasReceivedRequest;
    private bool HasSentRequest;
    private int CurrentRequestId = -1;

    public bool IsAimingGun { get; set; }
    public int CharacterAimedId { get; set; }

    public int MaxHungerLevel { get; set; } = 10;
    public int MaxHp { get; set; } = 10;

    public CharacterInfo()
    {
        Hp = MaxHp;
    }

	public CharacterInfo getDuplicated()
	{
		var newCharacter = new CharacterInfo
		{
			Id = Id,
			Name = Name,
			Type = Type,
			Hp = Hp,
			Dead = Dead,
			Gold = Gold,
			Hunger = Hunger,
			CurrentPlace = CurrentPlace,

			HasReceivedRequest = HasReceivedRequest,
			HasSentRequest = HasSentRequest,
			CurrentRequestId = CurrentRequestId,

			IsAimingGun = IsAimingGun,
			CharacterAimedId = CharacterAimedId
		};

		Inventory.CopyTo(newCharacter.Inventory, 0);

		return newCharacter;
	}

	public bool Equals(CharacterInfo obj)
	{
		return Id == obj.Id && Hp == obj.Hp && Dead == obj.Dead && Gold == obj.Gold && Hunger == obj.Hunger && CurrentPlace == obj.CurrentPlace && 
			HasReceivedRequest == obj.HasReceivedRequest && HasSentRequest == obj.HasSentRequest && CurrentRequestId == obj.CurrentRequestId && IsAimingGun == obj.IsAimingGun && CharacterAimedId == obj.CharacterAimedId;
	}

	public int GetHash()
	{
		var hash = new HashCode();
		hash.Add(Id);
		hash.Add(Hp);
		hash.Add(Dead);
		hash.Add(Gold);
		hash.Add(CurrentPlace);
		hash.Add(Hunger);
		hash.Add(HasReceivedRequest);
		hash.Add(HasSentRequest);
		hash.Add(CurrentRequestId);
		hash.Add(IsAimingGun);
		hash.Add(CharacterAimedId);
		return hash.ToHashCode();
	}

	public int getGold()
	{
		return Gold;
	}

	public int getHunger()
	{
		return Hunger;
	}

	public int getHp()
	{
		return Hp;
	}

	public bool GetHasReceivedRequest()
	{
		return HasReceivedRequest;
	}

	public bool GetHasSentRequest()
	{
		return HasSentRequest;
	}

	public int getCurrentRequestId()
	{
		return CurrentRequestId;
	}

	public Definitions.PlaceType getCurrentPlace()
	{
		return CurrentPlace;
	}

	public void moveToPlace(Definitions.PlaceType place)
	{
		CurrentPlace = place;
		OnStatsChanged?.Invoke();
	}

	public String getNameAndType()
	{
		return $"{Name} ({Type.ToString()})";
	}

	public bool isDead()
	{
		return Dead;
	}

    public CharacterInfo SetGold(int gold)
    {
        Gold = gold;
        return this;
    }

	public CharacterInfo SetHunger(int hunger)
    {
        Hunger = hunger;
        return this;
    }

    public CharacterInfo SetPlace(Definitions.PlaceType place)
    {
        CurrentPlace = place;
        return this;
    }

    public void AimGunAt(CharacterInfo character)
    {
        IsAimingGun = true;
        CharacterAimedId = character.Id;
    }

    public void HolsterGun()
    {
        IsAimingGun = false;
    }

    public void SendRequest(CharacterInfo to, int Id)
    {
        HasSentRequest = true;
        CurrentRequestId = Id;

        to.HasReceivedRequest = true;
        to.CurrentRequestId = Id;

    }

    public void AcceptRequest()
    {
        if (!HasReceivedRequest)
            throw new InvalidOperationException("Character has not received a request.");

        HasReceivedRequest = false;
		CurrentRequestId = -1;
    }

    public void RefuseRequest()
    {
        if (!HasReceivedRequest)
            throw new InvalidOperationException("Character has not received a request.");

        HasReceivedRequest = false;
		CurrentRequestId = -1;
    }

    public void ProcessSentRequest()
    {
        if (!HasSentRequest)
            throw new InvalidOperationException("Character has not sent a request.");

        HasSentRequest = false;
		CurrentRequestId = -1;
    }

    public void Kill()
    {
        Dead = true;
		OnStatsChanged?.Invoke();
    }

    public void ReduceHunger()
    {
        Hunger = Math.Clamp(Hunger - 1, 0, MaxHungerLevel);
		OnStatsChanged?.Invoke();
    }

    public void IncreaseHunger()
    {
        Hunger = Math.Clamp(Hunger + 1, 0, MaxHungerLevel);
		OnStatsChanged?.Invoke();
    }

    public void ReduceGold(int amount)
    {
        Gold = Math.Clamp(Gold - amount, 0, int.MaxValue);
		OnStatsChanged?.Invoke();
    }

    public void IncreaseGold(int amount)
    {
        Gold = Gold + amount;
		OnStatsChanged?.Invoke();
    }

    public void ReduceHp(int amount=1)
    {
        Hp = Math.Clamp(Hp - amount, 0, MaxHp);
		OnStatsChanged?.Invoke();
    }

    public void IncreaseHp(int amount=1)
    {
        Hp = Math.Clamp(Hp + amount, 0, MaxHp);
		OnStatsChanged?.Invoke();
    }

    public bool HasItem(Definitions.ItemType type)
    {
        foreach (ItemSlotInfo item in Inventory)
        {
            if (item.ItemData.Type == type)
                return true;
        }

        return false;
    }

    public int GetIndexForType(Definitions.ItemType type)
    {
        for (int i = 0; i < Inventory.Length; i++)
        {
			var itemSlot = Inventory[i];
			if (!itemSlot.used)
			{
				continue;
			}
            if (itemSlot.ItemData.Type == type)
                return i;
        }

        return -1;
    }

	public IEnumerable<Definitions.ItemType> getAllTypes()
	{
		for (int i = 0; i < Inventory.Length; i++)
		{
			var itemSlot = Inventory[i];

			if (itemSlot.used)
			{
				yield return itemSlot.ItemData.Type;
			}
		}
	}

	public ItemSlotInfo GetItemAt(int index)
	{
		return Inventory[index];
	}


	public int GetItemTurnsUsed(int index)
	{
		return Inventory[index].TurnsUsed;
	}

	
	public void IncreseItemTurnUsed(int index)
	{	
		var item  = Inventory[index];
		Inventory[index] = item with {TurnsUsed = item.TurnsUsed +1};
	}
	
	
	public void IncreseItemTurnUsedAndRemoveOnEmpty(int index)
	{	
		var item  = Inventory[index];
		IncreseItemTurnUsed(index);
		if (GetItemTurnsUsed(index) > item.ItemData.MaxUses)
		{
			Inventory[index].TurnsUsed = 0;
			RemoveItem(index);
		}
	}
	public void SetItemAt(int index, ItemSlotInfo info)
	{
		Inventory[index] = info; 
	}

	public void RemoveItem(int index)
    {
		Debug.Assert(Inventory[index].used);
		Inventory[index].used =false;

		OnStatsChanged?.Invoke();
    }

    public CharacterInfo AddItem(ItemData itemData, int amount=1)
    {
		foreach (var j in Enumerable.Range(0, amount))
		{
			
			var idxToAdd = -1;
			for (int i = 0; i < Inventory.Length; i++)
			{
				var itemSlot = Inventory[i];
				if (!itemSlot.used)
				{
					idxToAdd = i;
					break;
				}
			}
		
			if (idxToAdd >= 0)
			{
				var itemSlot = Inventory[idxToAdd];
				Inventory[idxToAdd] = itemSlot with {used = true, ItemData = itemData};
				OnStatsChanged?.Invoke();
			}
		}

        return this;
    }

    public IEnumerable<ItemSlotInfo> GetAllItems()
    {
        return Inventory;
    }

	public void AddItemList(IEnumerable<ItemSlotInfo> items)
    {

		foreach (var itemSlot in items)
		{
			if (!itemSlot.used)
			{
				continue;
			}
			AddItem(itemSlot.ItemData);
		}
		OnStatsChanged?.Invoke();
    }
}