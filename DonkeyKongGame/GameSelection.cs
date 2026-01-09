namespace DonkeyKongGame
{
    public enum LevelId
    {
        Map1 = 1,
        Map2 = 2,
        Map3 = 3
    }

    public class GameSelection
    {
        public LevelId Level { get; set; } = LevelId.Map1;
        public int CharacterId { get; set; } = 1; // 1,2,3
    }
}
