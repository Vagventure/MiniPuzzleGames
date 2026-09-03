namespace PuzzleGame.Core
{
    /// <summary>
    /// Identifies which mini-game a level belongs to.
    /// Add new games here (and a matching chapter in the LevelCatalog) to extend the sequence.
    /// </summary>
    public enum GameId
    {
        None = 0,
        ConnectBalls = 1,
        FindTheHole = 2,
        RollingMaze = 3,
    }
}
