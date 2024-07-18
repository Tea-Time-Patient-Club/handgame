using AirFishLab.ScrollingList;
using AirFishLab.ScrollingList.ContentManagement;
using System;
using UnityEngine;
using UnityEngine.UI;

public class SongItemBank : BaseListBank
{
    [SerializeField]
    private Song[] _songs;

    public override IListContent GetListContent(int index)
    {
        return _songs[index];
    }

    public override int GetContentCount()
    {
        return _songs.Length;
    }
}

[Serializable]
public class Song : IListContent
{
    public Sprite albumArt;
    public string title;
    public string artist;
}