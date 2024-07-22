using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AllHandHandler : MonoBehaviour
{
    public TextMeshProUGUI outputTextBox;

    private const string DATA_DIRECTORY_PATH = "Resources/GameData";

    void Start()
    {
        LoadAndDisplayAllHandData();
    }

    void LoadAndDisplayAllHandData()
    {
        GameDataList allGameData = DataManager.LoadAllGameData();
        Dictionary<string, Dictionary<string, int[]>> groupedData = new Dictionary<string, Dictionary<string, int[]>>();

        foreach (var gameData in allGameData.Games)
        {
            if (!string.IsNullOrEmpty(gameData.PlayDate))
            {
                string dateKey = DateTime.Parse(gameData.PlayDate).ToString("MM-dd");
                string handKey = gameData.SelectedHand.ToLower();

                if (!groupedData.ContainsKey(dateKey))
                {
                    groupedData[dateKey] = new Dictionary<string, int[]>
                    {
                        { "right", new int[5] },
                        { "left", new int[5] }
                    };
                }

                for (int i = 0; i < 5; i++)
                {
                    groupedData[dateKey][handKey][i] += gameData.HitCounts[i + 1];
                }
            }
        }

        string output = "";
        string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
        foreach (var dateEntry in groupedData.OrderByDescending(x => x.Key))
        {
            foreach (var handEntry in dateEntry.Value)
            {
                output += $"{dateEntry.Key}: {handEntry.Key} ";
                for (int i = 0; i < 5; i++)
                {
                    output += $"{fingerNames[i]}: {handEntry.Value[i]} ";
                }
                output += "\n\n";
            }
        }

        outputTextBox.text = output;
    }
}