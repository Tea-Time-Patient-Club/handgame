using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class DataManager
{
    private const string DATA_FOLDER_NAME = "GameData";
    private const string ALL_GAME_DATA_FILE = "AllGameData.json";

    public static string GetDataPath(string fileName)
    {
        string dataDirectory = Path.Combine(Application.persistentDataPath, DATA_FOLDER_NAME);
        
        // GameData 디렉토리가 없으면 생성
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        string filePath = Path.Combine(dataDirectory, fileName);
        Debug.Log($"Data file path: {filePath}");
        return filePath;
    }

    public static void SaveGameData(GameData gameData)
    {
        try
        {
            string filePath = GetDataPath(ALL_GAME_DATA_FILE);
            GameDataList gameDataList;

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                gameDataList = JsonUtility.FromJson<GameDataList>(json);
            }
            else
            {
                gameDataList = new GameDataList();
            }

            gameDataList.Games.Add(gameData);

            string updatedJson = JsonUtility.ToJson(gameDataList, true);
            File.WriteAllText(filePath, updatedJson);
            Debug.Log($"Game data saved to {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    public static GameDataList LoadAllGameData()
    {
        string filePath = GetDataPath(ALL_GAME_DATA_FILE);
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Debug.Log($"Loaded JSON: {json}");
                GameDataList data = JsonUtility.FromJson<GameDataList>(json);
                if (data != null && data.Games != null)
                {
                    Debug.Log($"Successfully loaded {data.Games.Count} game records.");
                    return data;
                }
                else
                {
                    Debug.LogWarning("Loaded data is null or empty.");
                }
            }
            else
            {
                Debug.LogWarning($"File not found: {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading game data: {e.Message}\nStack Trace: {e.StackTrace}");
        }
        return new GameDataList();
    }

    public static async Task SaveGameDataAsync(GameData gameData)
    {
        try
        {
            string filePath = GetDataPath(ALL_GAME_DATA_FILE);
            GameDataList gameDataList;

            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                gameDataList = JsonUtility.FromJson<GameDataList>(json);
            }
            else
            {
                gameDataList = new GameDataList();
            }

            gameDataList.Games.Add(gameData);

            string updatedJson = JsonUtility.ToJson(gameDataList, true);
            await File.WriteAllTextAsync(filePath, updatedJson);
            Debug.Log($"Game data saved asynchronously to {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data asynchronously: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    public static async Task<GameDataList> LoadAllGameDataAsync()
    {
        string filePath = GetDataPath(ALL_GAME_DATA_FILE);
        try
        {
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                Debug.Log($"Loaded JSON: {json}");
                GameDataList data = JsonUtility.FromJson<GameDataList>(json);
                if (data != null && data.Games != null)
                {
                    Debug.Log($"Successfully loaded {data.Games.Count} game records asynchronously.");
                    return data;
                }
                else
                {
                    Debug.LogWarning("Loaded data is null or empty.");
                }
            }
            else
            {
                Debug.LogWarning($"File not found: {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading game data asynchronously: {e.Message}\nStack Trace: {e.StackTrace}");
        }
        return new GameDataList();
    }

    public static List<GameData> LoadRecentGameData(int days)
    {
        GameDataList allData = LoadAllGameData();
        List<GameData> recentData = new List<GameData>();
        DateTime cutoffDate = DateTime.Now.AddDays(-days);

        foreach (var game in allData.Games)
        {
            if (DateTime.TryParse(game.PlayDate, out DateTime playDate) && playDate >= cutoffDate)
            {
                recentData.Add(game);
            }
        }

        return recentData;
    }

    public static void DeleteAllData()
    {
        string filePath = GetDataPath(ALL_GAME_DATA_FILE);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"All game data deleted from {filePath}");
        }
        else
        {
            Debug.LogWarning($"No data file found to delete at {filePath}");
        }
    }
}