using System.Collections.Generic;

namespace DrawGuess.SignalR
{
    public class GroupDetail
    {
        public bool InfoUpdated { get; set; }
        public bool IsPlaying { get; set; }
        public string GuessingWord { get; set; }
        public string LastUpdateId { get; set; }
        public LineInfo LastInfo { get; set; }
        public List<string> ListConnectionId { get; set; }
        public List<string> ListPlayingId { get; set; }
        public List<string> ListGuessedId { get; set; }

        public GroupDetail()
        {
            InfoUpdated = true;
            IsPlaying = false;
            GuessingWord = null;
            LastUpdateId = null;
            LastInfo = null;
            ListConnectionId = new List<string>();
            ListPlayingId = new List<string>();
            ListGuessedId = new List<string>();
        }
    }
}