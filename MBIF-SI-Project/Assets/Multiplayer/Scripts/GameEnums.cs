// File: GameEnums.cs

// Enum ini digunakan oleh mode single-player dan multiplayer.
public enum HelpCardEffect
{
    ExtraFinpoints,
    BoostRandomIPO,
    AdministrativePenalties,
    NegativeEquity,
    FreeCardPurchase,
    TaxEvasion,
    MarketPrediction,
    EyeOfTruth,
    MarketStabilization,
    CardSwap,        // <-- TAMBAHKAN INI
    ForcedPurchase   // <-- TAMBAHKAN INI
}

// Anda juga bisa memindahkan enum lain ke sini jika ada.
// Contoh: public enum MarketPredictionType { Rise, Fall }
public enum MarketPredictionType 
{ 
    Rise, 
    Fall 
}