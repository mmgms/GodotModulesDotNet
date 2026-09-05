using System;
using System.Collections.Generic;

namespace WesternSimGame;

public class CharacterInfo
{
	public delegate void StatsChangedEvent();	
	public event StatsChangedEvent OnStatsChanged;

	public int Id { get; set; }
    public string Name { get; set;}

    public Definitions.CharacterType Type { get; set; }
    private int Hp;//10x2x100x10x[]
    private bool Dead;
    private int Hunger;
    private int Gold;
    private Definitions.PlaceType CurrentPlace;
    private List<ItemInfo> Inventory = new();

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

		Inventory.ForEach(x => newCharacter.Inventory.Add(x.getDuplicated()));

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

    public void ReduceHp()
    {
        Hp = Math.Clamp(Hp - 1, 0, MaxHp);
		OnStatsChanged?.Invoke();
    }

    public void IncreaseHp()
    {
        Hp = Math.Clamp(Hp + 1, 0, MaxHp);
		OnStatsChanged?.Invoke();
    }

    public bool HasItem(Definitions.ItemType type)
    {
        foreach (ItemInfo item in Inventory)
        {
            if (item.ItemData.Type == type)
                return true;
        }

        return false;
    }

    public ItemInfo GetItemOfType(Definitions.ItemType type)
    {
        foreach (ItemInfo item in Inventory)
        {
            if (item.ItemData.Type == type)
                return item;
        }

        return null;
    }

	public ItemInfo GetItemAt(int index)
	{
		return Inventory[index];
	}

    public void RemoveItem(ItemInfo item)
    {
        Inventory.Remove(item);
		OnStatsChanged?.Invoke();
    }

	public void RemoveItem(int index)
    {
        Inventory.RemoveAt(index);
		OnStatsChanged?.Invoke();
    }

    public CharacterInfo AddItem(ItemData itemData)
    {
        ItemInfo itemInfo = new()
        {
            ItemData = itemData
        };

        Inventory.Add(itemInfo);
		OnStatsChanged?.Invoke();
        return this;
    }

    public CharacterInfo AddItemAmount(ItemData itemData, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            AddItem(itemData);
        }

        return this;
    }

    public List<ItemInfo> GetAllItems()
    {
        return Inventory;
    }

	
    public List<ItemData> GetAllItemData()
    {
		var types = new List<ItemData>();
		Inventory.ForEach((x) =>
		{
			if (!types.Contains(x.ItemData))
			{
				types.Add(x.ItemData);
			}
		});
        return types;
    }

	public void AddItemList(List<ItemInfo> items)
    {
        Inventory.AddRange(items);
		OnStatsChanged?.Invoke();
    }
}