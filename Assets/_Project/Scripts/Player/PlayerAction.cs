public struct PlayerAction
{
    public enum Kind { None, CardClicked, TileClicked, EndTurn }

    public Kind Type;
    public CardView CardView;
    public Tile TileTarget;

    public static PlayerAction MakeEndTurn() => new PlayerAction { Type = Kind.EndTurn };
    public static PlayerAction MakeCard(CardView v) => new PlayerAction { Type = Kind.CardClicked, CardView = v };
    public static PlayerAction MakeTile(Tile t) => new PlayerAction { Type = Kind.TileClicked, TileTarget = t };
}