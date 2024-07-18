using AirFishLab.ScrollingList;
using AirFishLab.ScrollingList.ContentManagement;
using AirFishLab.ScrollingList.Demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SongItemListBox : ListBox
{
    [SerializeField]
    private Image _albumArt;
    [SerializeField]
    private TextMeshProUGUI _songTitle;
    [SerializeField]
    private TextMeshProUGUI _songArtist;

    protected override void UpdateDisplayContent(IListContent content)
    {
        var data = (Song)content;
        _albumArt.sprite = data.albumArt;
        _songTitle.text = data.title;
        _songArtist.text = data.artist;
    }
}