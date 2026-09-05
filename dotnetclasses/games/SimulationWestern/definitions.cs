namespace WesternSimGame;

public static class Definitions
{
    public enum ItemType
    {
        Gun,
        Food,
        Pickaxe,
        MedKit,
        Ammo
    }

    public enum PlaceType
    {
        Unclassified,
		Road,
        Saloon,
        Thuroughfare,
        Bank,
        Shop,
        Mine,
        SheriffStation
    }

    public enum CharacterType
    {
        Miner,
        Sheriff,
        SaloonOwner,
        BankOwner,
        Bandit,
        Deputy,
        ShopOwner
    }
}