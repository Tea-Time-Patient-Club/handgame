using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class AduinoHandler : MonoBehaviour
{
    public Button Back;
    void Start()
    {
        Back.onClick.AddListener(() => BackButtonClick());
    }
    public void BackButtonClick()
    {
        SceneManager.LoadScene("Controll");
        Debug.Log("Controll");
    }
}
