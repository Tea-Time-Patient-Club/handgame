using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;

public class BLEManager : MonoBehaviour
{
    public static BLEManager Instance { get; private set; } // 싱글톤 인스턴스

    public TextMeshProUGUI statusText; // 상태를 표시할 UI 텍스트 컴포넌트
    public Button connectButton; // 연결 버튼 컴포넌트
    public Button readDataButton; // 데이터 읽기 버튼 컴포넌트
    private string deviceName = "Nano33BLE"; // 연결할 장치 이름
    private string serviceUUID = "12345678-1234-5678-1234-56789abcdef0"; // 서비스 UUID
    private string characteristicUUID = "87654321-4321-6789-4321-abcdef012345"; // 특성 UUID
    private bool isConnected = false; // 연결 상태를 추적하기 위한 변수
    private string connectedDeviceAddress; // 연결된 장치 주소
    private bool isScanning = false; // 스캔 상태를 추적하기 위한 변수

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 객체가 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성된 객체를 파괴
        }
    }

    void Start()
    {
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClick); // 연결 버튼 클릭 리스너 추가
        }

        if (readDataButton != null)
        {
            readDataButton.onClick.AddListener(OnReadDataButtonClick); // 데이터 읽기 버튼 클릭 리스너 추가
            readDataButton.interactable = false; // 초기에는 버튼 비활성화
        }

        StartCoroutine(InitializeBLE()); // BLE 초기화 코루틴 시작
    }

    public void OnConnectButtonClick()
    {
        if (!isConnected && !isScanning)
        {
            StartScanning(); // 스캔 시작
        }
        else
        {
            statusText.text = "Already connected or scanning"; // 이미 연결된 상태임을 표시
        }
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

        }, (error) =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialization Error: " + error; // 오류 메시지 설정
        });
    }

    void StartScanning()
    {
        isScanning = true; // 스캔 상태로 변경
        if (statusText != null)
            statusText.text = "Starting Scan..."; // 스캔 시작 메시지 설정

        // 주변 장치를 스캔하여 장치 이름이 일치하면 연결 시작
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
        {
            Debug.Log("Found device: " + name); // 디버그 로그에 장치 이름 출력

            if (statusText != null && !isConnected)
            {
                if (string.IsNullOrEmpty(name))
                {
                    statusText.text = "Scanning: no name";
                }
                else
                {
                    statusText.text = "Scanning: " + name; // 현재 스캔 중인 장치 이름을 표시
                }
            }

            if (name.Contains(deviceName))
            {
                if (statusText != null)
                    statusText.text = "Scanned: " + name;

                BluetoothLEHardwareInterface.StopScan(); // 스캔 중지
                isScanning = false; // 스캔 종료 상태로 변경
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
                GlobalHandler.PlayerTool = 1;
                connectedDeviceAddress = addr; // 연결된 장치 주소 저장
                if (readDataButton != null)
                {
                    readDataButton.interactable = true; // 데이터 읽기 버튼 활성화
                }
            }
        }, (disconnectedAddress) =>
        {
            if (disconnectedAddress == address)
            {
                isConnected = false; // 연결 해제 시 상태 업데이트
                if (statusText != null)
                    statusText.text = "Disconnected from " + address; // 연결 해제 메시지 설정
                if (readDataButton != null)
                {
                    readDataButton.interactable = false; // 데이터 읽기 버튼 비활성화
                }
            }
        });
    }

    public void SubscribeToCharacteristic()
    {
        if (isConnected && !string.IsNullOrEmpty(connectedDeviceAddress))
        {
            BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(
                connectedDeviceAddress, serviceUUID, characteristicUUID,
                (deviceAddress, characteristic) => { }, // Subscription callback
                (deviceAddress, characteristic, bytes) => // Notification callback
                {
                    if (bytes.Length > 0)
                    {
                        string rawData = BitConverter.ToString(bytes); // 바이트 배열을 문자열로 변환
                        Debug.Log("Received data: " + rawData);

                        if (statusText != null)
                        {
                            statusText.text = "Subscribed data: " + rawData; // 수신된 데이터 표시
                        }

                        // 판정 로직을 추가할 수 있습니다.
                    }
                }
            );
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "Not connected to any device"; // 연결되지 않은 상태 메시지 설정
            }
        }
    }

    public void UnsubscribeFromCharacteristic()
    {
        if (isConnected && !string.IsNullOrEmpty(connectedDeviceAddress))
        {
            BluetoothLEHardwareInterface.UnSubscribeCharacteristic(connectedDeviceAddress, serviceUUID, characteristicUUID, null);

            if (statusText != null)
            {
                statusText.text = "Unsubscribed from characteristic"; // 구독 취소 메시지 설정
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "Not connected to any device"; // 연결되지 않은 상태 메시지 설정
            }
        }
    }

    public void OnReadDataButtonClick()
    {
        if (isConnected && !string.IsNullOrEmpty(connectedDeviceAddress))
        {
            ReadDataFromDevice(connectedDeviceAddress, serviceUUID, characteristicUUID); // 데이터 읽기 시작
        }
    }

    void ReadDataFromDevice(string address, string serviceUUID, string characteristicUUID)
    {
        if (statusText != null)
            statusText.text = "Reading data from " + characteristicUUID; // 데이터 읽기 시도 메시지 설정

        // 특성에서 데이터를 읽기
        BluetoothLEHardwareInterface.ReadCharacteristic(address, serviceUUID, characteristicUUID, (characteristic, bytes) =>
        {
            if (bytes.Length > 0)
            {
                string rawData = BitConverter.ToString(bytes); // 바이트 배열을 문자열로 변환
                if (statusText != null)
                    statusText.text = rawData; // 수신된 데이터 표시

                // 판정 로직을 추가할 수 있습니다.
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
