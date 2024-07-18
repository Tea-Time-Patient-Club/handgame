using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongItem : MonoBehaviour
{
    public Image songImage;
    public TextMeshProUGUI songNameText;
    public TextMeshProUGUI creatorText;
    public Button button;  // Button 컴포넌트 추가

    public void Setup(Sprite image, string songName, string creator)
    {
        if (songImage != null)
        {
            songImage.sprite = image;
        }
        
        if (songNameText != null)
        {
            songNameText.text = songName;
        }
        
        if (creatorText != null)
        {
            creatorText.text = creator;
        }
    }
}
