using System;
using System.Collections.Generic;

[Serializable]
public class SongDataEntry
{
    public string title;
    public string file;
    public string genre;
    public string creater;
    public int active;
}

public static class SongData
{
    public static List<SongDataEntry> GetSongList()
    {
        return new List<SongDataEntry>
        {
            new SongDataEntry
            {
                title = "Sample",
                file = "Sample",
                creater = "Kevin",
                genre = "Trot",
                active = 1
            },
            new SongDataEntry
            {
                title = "Picnic Party", 
                file = "Picnic Party",
                creater = "SunoAI",
                genre = "POP",
                active = 1
            },
            new SongDataEntry
            {
                title = "Warm Up Groove",
                file = "Warm Up Groove", 
                creater = "Kevin MacLeod",
                genre = "Trot",
                active = 1
            },
            new SongDataEntry
            {
                title = "Smile",
                file = "Smile", 
                creater = "SunoAI",
                genre = "POP",
                active = 1
            },
        };
    }
}