namespace SeaWolfBlazorVersion.Engine.Models.Enums;

public enum ShipType
{
    Destroyer,   // medium size, medium speed
    PtBoat,      // small, fast
    Cargo,       // large, slow — takes two hits
    Cruiser,     // large warship, medium speed — takes two hits
    FishingBoat, // small, slow civilian — one hit
    Tanker,      // huge, very slow — takes two hits, high value
    Carrier      // enormous, very slow — takes two hits, highest value
}
