using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;
using Assets.SimpleSpinner;

public class BLEManager : MonoBehaviour
{
    public static BLEManager Instance { get; private set; } // 싱글톤 인스턴스

    public Sprite circle; //스피너 원
    public Sprite checkLogo; //연결 완료!
    public Sprite errorLogo; //오류
    public Sprite infoLogo; //대기 상태

    private string deviceName = "Nano33BLE"; // 연결할 장치 이름
    private string serviceUUID = "12345678-1234-5678-1234-56789abcdef0"; // 서비스 UUID
    private string characteristicUUID = "87654321-4321-6789-4321-abcdef012345"; // 특성 UUID
    private bool isConnected = false; // 연결 상태를 추적하기 위한 변수
    private string connectedDeviceAddress; // 연결된 장치 주소
    private bool isScanning = false; // 스캔 상태를 추적하기 위한 변수
    int i = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 객체가 파괴되지 않도록 설정
            Debug.Log("DDestory");
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성된 객체를 파괴
        }
    }

    public IEnumerator InitializeBLE(TextMeshProUGUI statusText, SimpleSpinner spinner, GameObject statusLogo)
    {
        if (statusText != null)
            statusText.text = "Initializing Bluetooth LE\n(1/5)"; // 상태 텍스트 설정

        yield return new WaitForSeconds(1f); // 1초 대기

        // BLE 초기화
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {
            if (statusText != null)
                statusText.text = "BLE Initialized\n'Connect' to connect (2/5)"; // 초기화 완료 메시지 설정
                spinner.Rotation = false;
                spinner.gameObject.SetActive(false);
                statusLogo.SetActive(true);
                statusLogo.GetComponent<Image>().sprite = infoLogo;
        }, (error) =>
        {
            if (statusText != null)
                statusText.text = $"BLE Initialization Error: {error}"; // 오류 메시지 설정
            if (spinner != null && statusLogo != null)
            {
                spinner.Rotation = false;
                spinner.gameObject.SetActive(false);
                statusLogo.SetActive(true);
                statusLogo.GetComponent<Image>().sprite = errorLogo;
            }
        });
    }

    public void StartScanning(TextMeshProUGUI statusText, SimpleSpinner spinner, GameObject statusLogo, Button readDataButton)
    {
        if (!isConnected && !isScanning)
        {
            isScanning = true; // 스캔 상태로 변경
            if (statusText != null)
                statusText.text = "Starting Scan...\n(3/5)"; // 스캔 시작 메시지 설정

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
                    ConnectToDevice(address, statusText, spinner, statusLogo, readDataButton); // 장치에 연결
                }
            }, null);
        } else
        {
            statusText.text = "Already connected or scanning"; // 이미 연결된 상태임을 표시
        }
    }

    void ConnectToDevice(string address, TextMeshProUGUI statusText, SimpleSpinner spinner, GameObject statusLogo, Button readDataButton)
    {
        if (statusText != null)
            statusText.text = $"Connecting to {address}\n(4/5)"; // 연결 시도 메시지 설정

        // 장치에 연결
        BluetoothLEHardwareInterface.ConnectToPeripheral(address, null, null, (addr, serviceUUID, charUUID) =>
        {
            if (statusText != null)
                statusText.text = $"Connected to {address}\n(5/5)"; // 연결 완료 메시지 설정

            if (spinner)
            {
                spinner.Rotation = false;
                spinner.gameObject.SetActive(false);
                statusLogo.SetActive(true);
                statusLogo.GetComponent<Image>().sprite = checkLogo;
            }

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
                   
                if (spinner)
                {
                    spinner.Rotation = false;
                    spinner.gameObject.SetActive(false);
                    statusLogo.SetActive(true);
                    statusLogo.GetComponent<Image>().sprite = errorLogo;
                }
                   

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

                        //if (statusText != null)
                        //{
                            if(rawData.ToString().Contains("31"))
                                GameHandler.ArduinoHit = 1;
                            //statusText.text = rawData; // 수신된 데이터 표시
                        //}
                    }
                }
            );
        }
        else
        {
            //if (statusText != null)
            //{
            //    statusText.text = "Not connected to any device"; // 연결되지 않은 상태 메시지 설정
            //}
        }
    }

    public void UnsubscribeFromCharacteristic()
    {
        if (isConnected && !string.IsNullOrEmpty(connectedDeviceAddress))
        {
            BluetoothLEHardwareInterface.UnSubscribeCharacteristic(connectedDeviceAddress, serviceUUID, characteristicUUID, null);

            //if (statusText != null)
            //{
            //    statusText.text = "Unsubscribed from characteristic"; // 구독 취소 메시지 설정
            //}
        }
        else
        {
            //if (statusText != null)
            //{
            //    statusText.text = "Not connected to any device"; // 연결되지 않은 상태 메시지 설정
            //}
        }
    }

    public void ReadDataFromDevice(TextMeshProUGUI statusText)
    {
        if (isConnected && !string.IsNullOrEmpty(connectedDeviceAddress))
        {
            if (statusText != null)
                statusText.text = "Reading data from " + characteristicUUID; // 데이터 읽기 시도 메시지 설정

            // 특성에서 데이터를 읽기
            BluetoothLEHardwareInterface.ReadCharacteristic(connectedDeviceAddress, serviceUUID, characteristicUUID, (characteristic, bytes) =>
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
    }

    void OnDisable()
    {
        // 연결 해제 및 BLE 비활성화
        if (isConnected)
        {
            GlobalHandler.PlayerTool = 0;
            BluetoothLEHardwareInterface.DisconnectAll();
        }
        BluetoothLEHardwareInterface.DeInitialize(() => { });
    }
}
