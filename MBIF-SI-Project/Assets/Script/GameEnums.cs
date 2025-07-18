// File: GameEnums.cs

// Enum ini tidak perlu diubah.
public enum HelpCardEffect
{
    AdministrativePenalties,
    NegativeEquity,
    TaxEvasion,
    MarketPrediction,
    EyeOfTruth,
    MarketStabilization,
    CardSwap,
    ForcedPurchase
}

public enum MarketPredictionType 
{ 
    Rise, 
    Fall 
}

public enum GamePhase
{
    WaitingForPlayers,
    TicketSelection,
    CardDrafting,
    HelpCard,
    Selling,
    Rumor,
    Resolution,
    GameOver
}

public enum IPOState 
{ 
    Normal, 
    Ascend, 
    Advanced 
}
