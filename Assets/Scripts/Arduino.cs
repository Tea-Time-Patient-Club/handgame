using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class Blemanager : MonoBehaviour
{
    public TextMeshProUGUI statusText; // ���¸� ǥ���� UI �ؽ�Ʈ ������Ʈ
    private string deviceName = "Nano33BLE"; // ������ ��ġ �̸�
    private string serviceUUID = "12345678-1234-5678-1234-56789abcdef0"; // ���� UUID
    private string characteristicUUID = "87654321-4321-6789-4321-abcdef012345"; // Ư�� UUID
    private bool isConnected = false; // ���� ���¸� �����ϱ� ���� ����
    public Button connet;

    // Start is called before the first frame update
    void Start()
    {
        if (connet != null) connet.onClick.AddListener(connetting);
    }
    public void connetting()
    {
        StartCoroutine(InitializeBLE()); // BLE �ʱ�ȭ �ڷ�ƾ ����
    }

    IEnumerator InitializeBLE()
    {
        if (statusText != null)
            statusText.text = "Initializing BLE..."; // ���� �ؽ�Ʈ ����

        yield return new WaitForSeconds(1f); // 1�� ���

        // BLE �ʱ�ȭ
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialized"; // �ʱ�ȭ �Ϸ� �޽��� ����

            StartScanning(); // ��ĵ ����
        }, (error) =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialization Error: " + error; // ���� �޽��� ����
        });
    }

    void StartScanning()
    {
        if (statusText != null)
            statusText.text = "Starting Scan..."; // ��ĵ ���� �޽��� ����

        // �ֺ� ��ġ�� ��ĵ�Ͽ� ��ġ �̸��� ��ġ�ϸ� ���� ����
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
        {
            Debug.Log("Found device: " + name); // ����� �α׿� ��ġ �̸� ���

            if (statusText != null)
                statusText.text = "Scanning: " + name; // ���� ��ĵ ���� ��ġ �̸��� ǥ��

            if (name.Contains(deviceName))
            {
                if (statusText != null)
                    statusText.text = "Scaned: " + name;

                BluetoothLEHardwareInterface.StopScan(); // ��ĵ ����
                ConnectToDevice(address); // ��ġ�� ����
            }
        }, null);
    }



    void ConnectToDevice(string address)
    {
        if (statusText != null)
            statusText.text = "Connecting to " + address; // ���� �õ� �޽��� ����

        // ��ġ�� ����
        BluetoothLEHardwareInterface.ConnectToPeripheral(address, null, null, (addr, serviceUUID, charUUID) =>
        {
            if (statusText != null)
                statusText.text = "Connected to " + address; // ���� �Ϸ� �޽��� ����

            if (serviceUUID == this.serviceUUID && charUUID == this.characteristicUUID)
            {
                isConnected = true; // ���� ���� ������Ʈ
                SubscribeToCharacteristic(addr, serviceUUID, charUUID); // Ư�� ���� ����
                statusText.text = "Connected and Subscribed";
            }
        }, (disconnectedAddress) =>
        {
            if (disconnectedAddress == address)
            {
                isConnected = false; // ���� ���� �� ���� ������Ʈ
                if (statusText != null)
                    statusText.text = "Disconnected from " + address; // ���� ���� �޽��� ����
            }
        });
    }


    void SubscribeToCharacteristic(string address, string serviceUUID, string characteristicUUID)
    {
        if (statusText != null)
            statusText.text = "Subscribing to " + characteristicUUID; // ���� �õ� �޽��� ����

        // Ư���� �����Ͽ� �����͸� ����
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(address, serviceUUID, characteristicUUID, null, (addr, charUUID, bytes) =>
        {
            if (bytes.Length > 0)
            {
                string rawData = System.BitConverter.ToString(bytes); // ����Ʈ �迭�� ���ڿ��� ��ȯ
                if (statusText != null)
                    statusText.text = "Received Data: " + rawData; // ���ŵ� ������ ǥ��
            }
        });
    }



    void OnDisable()
    {
        // ���� ���� �� BLE ��Ȱ��ȭ
        if (isConnected)
        {
            BluetoothLEHardwareInterface.DisconnectAll();
        }
        BluetoothLEHardwareInterface.DeInitialize(() => { });
    }
}