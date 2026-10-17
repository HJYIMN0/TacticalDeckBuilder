public enum Faction
{
    Player,
    Enemy,
    Neutral
}

public enum SlopeDirection { None, North, East, South, West }

public enum PlayerTurnPhase
{
    Idle,                 // nessuna carta selezionata
    SelectingExecutor,    // carta scelta, in attesa dell'unità esecutrice
    SelectingTargetUnit,  // carta UnitOnly: in attesa dell'unità target/esecutrice
    SelectingTile,        // esecutore (o nessuno) scelto, in attesa della tile
    AwaitingConfirm,      // tutto scelto, in attesa di conferma
    Resolving             // carte in esecuzione
}