using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class Blemanager : MonoBehaviour
{
    public TextMeshProUGUI statusText; // 상태를 표시할 UI 텍스트 컴포넌트
    private string deviceName = "Nano33BLE"; // 연결할 장치 이름
    private string serviceUUID = "12345678-1234-5678-1234-56789abcdef0"; // 서비스 UUID
    private string characteristicUUID = "87654321-4321-6789-4321-abcdef012345"; // 특성 UUID
    private bool isConnected = false; // 연결 상태를 추적하기 위한 변수
    public Button connet;

    // Start is called before the first frame update
    void Start()
    {
        if (connet != null) connet.onClick.AddListener(connetting);
    }
    public void connetting()
    {
        StartCoroutine(InitializeBLE()); // BLE 초기화 코루틴 시작
    }

    IEnumerator InitializeBLE()
    {
        if (statusText != null)
            statusText.text = "Initializing BLE..."; // 상태 텍스트 설정

        yield return new WaitForSeconds(1f); // 1초 대기

        // BLE 초기화
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialized"; // 초기화 완료 메시지 설정

            StartScanning(); // 스캔 시작
        }, (error) =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialization Error: " + error; // 오류 메시지 설정
        });
    }

    void StartScanning()
    {
        if (statusText != null)
            statusText.text = "Starting Scan..."; // 스캔 시작 메시지 설정

        // 주변 장치를 스캔하여 장치 이름이 일치하면 연결 시작
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
        {
            Debug.Log("Found device: " + name); // 디버그 로그에 장치 이름 출력

            if (statusText != null)
                statusText.text = "Scanning: " + name; // 현재 스캔 중인 장치 이름을 표시

            if (name.Contains(deviceName))
            {
                if (statusText != null)
                    statusText.text = "Scaned: " + name;

                BluetoothLEHardwareInterface.StopScan(); // 스캔 중지
                ConnectToDevice(address); // 장치에 연결
            }
        }, null);
    }



    void ConnectToDevice(string address)
    {
        if (statusText != null)
            statusText.text = "Connecting to " + address; // 연결 시도 메시지 설정

        // 장치에 연결
        BluetoothLEHardwareInterface.ConnectToPeripheral(address, null, null, (addr, serviceUUID, charUUID) =>
        {
            if (statusText != null)
                statusText.text = "Connected to " + address; // 연결 완료 메시지 설정

            if (serviceUUID == this.serviceUUID && charUUID == this.characteristicUUID)
            {
                isConnected = true; // 연결 상태 업데이트
                SubscribeToCharacteristic(addr, serviceUUID, charUUID); // 특성 구독 시작
                statusText.text = "Connected and Subscribed";
            }
        }, (disconnectedAddress) =>
        {
            if (disconnectedAddress == address)
            {
                isConnected = false; // 연결 해제 시 상태 업데이트
                if (statusText != null)
                    statusText.text = "Disconnected from " + address; // 연결 해제 메시지 설정
            }
        });
    }


    void SubscribeToCharacteristic(string address, string serviceUUID, string characteristicUUID)
    {
        if (statusText != null)
            statusText.text = "Subscribing to " + characteristicUUID; // 구독 시도 메시지 설정

        // 특성에 구독하여 데이터를 수신
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(address, serviceUUID, characteristicUUID, null, (addr, charUUID, bytes) =>
        {
            if (bytes.Length > 0)
            {
                string rawData = System.BitConverter.ToString(bytes); // 바이트 배열을 문자열로 변환
                if (statusText != null)
                    statusText.text = "Received Data: " + rawData; // 수신된 데이터 표시
            }
        });
    }



    void OnDisable()
    {
        // 연결 해제 및 BLE 비활성화
        if (isConnected)
        {
            BluetoothLEHardwareInterface.DisconnectAll();
        }
        BluetoothLEHardwareInterface.DeInitialize(() => { });
    }
}