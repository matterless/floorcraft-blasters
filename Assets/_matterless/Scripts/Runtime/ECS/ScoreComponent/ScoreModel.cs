namespace Matterless.Floorcraft
{
    public struct ScoreModel
    {
        public ScoreModel(int score)
        {
            this.m_Score = score;
        }
        private int m_Score;
        public int score => m_Score;
    }
}

