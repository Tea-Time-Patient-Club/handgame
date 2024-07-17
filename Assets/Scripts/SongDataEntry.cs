using System;
using System.Collections.Generic;

[Serializable]
public class SongDataEntry
{
    public string title;
    public string creater;
    public string filePath;
    public int active;
    public string genre;
}

[Serializable]
public class SongDataList
{
    public List<SongDataEntry> songs;
}
