namespace WesternSimGame;

public class ItemInfo
{
    public ItemData ItemData { get; set; }
    public int TurnsUsed { get; set; }

	public ItemInfo getDuplicated()
	{
		var newItem = new ItemInfo();
		newItem.TurnsUsed = TurnsUsed;
		newItem.ItemData = ItemData;

		return newItem;
	}
}