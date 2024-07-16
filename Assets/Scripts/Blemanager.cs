using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class BLEManager : MonoBehaviour
{
    public static BLEManager Instance { get; private set; }

    public TextMeshProUGUI statusText; // 상태를 표시할 UI 텍스트 컴포넌트
    private string deviceName = "Nano33BLE"; // 연결할 장치 이름
    private string serviceUUID = "12345678-1234-5678-1234-56789abcdef0"; // 서비스 UUID
    private string characteristicUUID = "87654321-4321-6789-4321-abcdef012345"; // 특성 UUID
    private bool isConnected = false; // 연결 상태를 추적하기 위한 변수
    private string connectedAddress; // 연결된 장치의 주소

    public delegate void DataReceivedHandler(byte[] data);
    public event DataReceivedHandler OnDataReceived;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새로운 씬이 로드될 때마다 호출됩니다.
        StartCoroutine(InitializeOrReconnect());
    }

    IEnumerator InitializeOrReconnect()
    {
        yield return new WaitForSeconds(0.5f); // 씬이 완전히 로드될 때까지 잠시 대기

        statusText = FindObjectOfType<TextMeshProUGUI>(); // 새 씬에서 상태 텍스트 찾기

        if (!isConnected)
        {
            StartCoroutine(InitializeBLE());
        }
        else
        {
            if (statusText != null)
                statusText.text = "Already connected to " + connectedAddress;
        }
    }

    IEnumerator InitializeBLE()
    {
        if (statusText != null)
            statusText.text = "Initializing BLE...";

        yield return new WaitForSeconds(1f);

        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialized";

            StartScanning();
        }, (error) =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialization Error: " + error;
        });
    }

    void StartScanning()
    {
        if (statusText != null)
            statusText.text = "Starting Scan...";

        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
        {
            Debug.Log("Found device: " + name);

            if (statusText != null)
                statusText.text = "Scanning: " + name;

            if (name.Contains(deviceName))
            {
                if (statusText != null)
                    statusText.text = "Scanned: " + name;

                BluetoothLEHardwareInterface.StopScan();
                ConnectToDevice(address);
            }
        }, null);
    }

    void ConnectToDevice(string address)
    {
        if (statusText != null)
            statusText.text = "Connecting to " + address;

        BluetoothLEHardwareInterface.ConnectToPeripheral(address, null, null, (addr, serviceUUID, charUUID) =>
        {
            if (statusText != null)
                statusText.text = "Connected to " + address;

            if (serviceUUID == this.serviceUUID && charUUID == this.characteristicUUID)
            {
                isConnected = true;
                connectedAddress = addr;
                SubscribeToCharacteristic(addr, serviceUUID, charUUID);
                GlobalHandler.PlayerTool = 1;
                statusText.text = "Connected and Subscribed";
            }
        }, (disconnectedAddress) =>
        {
            if (disconnectedAddress == connectedAddress)
            {
                isConnected = false;
                if (statusText != null)
                    statusText.text = "Disconnected from " + disconnectedAddress;
            }
        });
    }

    void SubscribeToCharacteristic(string address, string serviceUUID, string characteristicUUID)
    {
        if (statusText != null)
            statusText.text = "Subscribing to " + characteristicUUID;

        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(address, serviceUUID, characteristicUUID, null, (addr, charUUID, bytes) =>
        {
            if (bytes.Length > 0)
            {
                OnDataReceived?.Invoke(bytes);
                if (statusText != null)
                    statusText.text = "Connected Device";
            }
        });
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnApplicationQuit()
    {
        if (isConnected)
        {
            BluetoothLEHardwareInterface.DisconnectPeripheral(connectedAddress, (address) => {
                Debug.Log("Disconnected from " + address);
            });
        }
        BluetoothLEHardwareInterface.DeInitialize(() => { });
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    public void Reconnect()
    {
        if (!isConnected)
        {
            StartScanning();
        }
    }
}