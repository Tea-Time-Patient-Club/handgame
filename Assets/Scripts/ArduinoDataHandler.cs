using UnityEngine;
using TMPro;

public class ArduinoDataHandler : MonoBehaviour
{
    private BLEManager bleManager;
    public TextMeshProUGUI arduinoText;
    private int dataReceivedCount = 0;

    // GameHandler���� ������ �� �ִ� ������Ƽ
    public string LastReceivedData { get; private set; }
    public int DataReceivedCount => dataReceivedCount;

    private void Start()
    {
        InitializeBLEManager();
    }

    private void InitializeBLEManager()
    {
        bleManager = FindObjectOfType<BLEManager>();
        if (bleManager == null)
        {
            Debug.LogError("BLEManager not found in the scene!");
        }
        else
        {
            bleManager.OnDataReceived += HandleArduinoData;
            Debug.Log("Successfully subscribed to BLEManager's OnDataReceived event.");
        }
    }

    private void OnDisable()
    {
        if (bleManager != null)
        {
            bleManager.OnDataReceived -= HandleArduinoData;
        }
    }

    private void HandleArduinoData(byte[] data)
    {
        dataReceivedCount++;
        string receivedData = System.Text.Encoding.UTF8.GetString(data);
        LastReceivedData = receivedData;

        // UI ������Ʈ�� ���� �����忡�� ����
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (arduinoText != null)
            {
                arduinoText.text = $"{dataReceivedCount} + {receivedData}";
            }
        });

        // GameHandler�� �˸�
        GameHandler gameHandler = FindObjectOfType<GameHandler>();
        if (gameHandler != null)
        {
            gameHandler.OnArduinoDataReceived(receivedData);
        }
    }

    // GameHandler���� ȣ���� �� �ִ� �޼���
    public void ResetDataCount()
    {
        dataReceivedCount = 0;
    }
}