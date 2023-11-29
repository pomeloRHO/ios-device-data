using System.Text.RegularExpressions;
using System;
using UnityEngine;
using System.Linq;
using SystemInfo = UnityEngine.Device.SystemInfo;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IOSDeviceData {
    const string SEPARATOR = ",";
    const string PATTERN = SEPARATOR + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
    const string DEVICE_NAME = "Device Name";
    const string MODEL_NAMES = "Model Names";
    const string NOTCH_HEIGHT = "Notch Height";

    
    string deviceModel;
    string deviceName;
    int notchHeight;
    bool deviceSupported;

    static IOSDeviceData instance;
    public static IOSDeviceData Instance {
        get {
            if (instance == null) {
                instance = new IOSDeviceData();
            }
            return instance;
        }
    }
    public static string Name => Instance.deviceName;
    public static int NotchHeight => Instance.notchHeight;
    public static bool DeviceSupported => Instance.deviceSupported;

    public IOSDeviceData() {
        LoadData();
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
#endif
    }

#if UNITY_EDITOR
    private void OnPlayModeChanged(PlayModeStateChange change) {
        // Reset when exiting play mode
        if (change != PlayModeStateChange.ExitingPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.update -= Update;
        instance = null;
    }
#endif

    void Update() {
        // Device model changed in simulator
        if (SystemInfo.deviceModel != deviceModel) {
            LoadData();
        }
    }

    void LoadData() {
        deviceModel = SystemInfo.deviceModel;
        notchHeight = 0;
        // If the device is not on the list, mark it as unsopported,
        // in that case the data given by unity should be used instead
        deviceSupported = false;

        var textAsset = Resources.Load<TextAsset>("iOSDeviceData/DeviceData");
        var csvParser = new Regex(PATTERN);
        var rows = textAsset.text.Split('\n', '\r');
        
        // Find indices for titles
        var titles = csvParser.Split(rows[0]);
        var deviceNameInd = 0;
        var modelNamesInd= 1;
        var notchHeightInd = 2;
        for (int i = 0; i < titles.Length; i++) {
            var value = titles[i];
            switch (value) {
                case DEVICE_NAME:
                    deviceNameInd = i;
                    break;
                case MODEL_NAMES:
                    modelNamesInd = i;
                    break;
                case NOTCH_HEIGHT:
                    notchHeightInd = i;
                    break;
            }
        }
        var maxIndex = Mathf.Max(deviceNameInd, modelNamesInd, notchHeightInd);

        // Look for current model
        for (int r = 1; r < rows.Length; r++) {
            if (!csvParser.IsMatch(rows[r])) continue;
            var fields = csvParser.Split(rows[r]);
            if (fields.Length <= maxIndex) continue;
            var models = fields[modelNamesInd].Trim('"').Split(';');
            if (models.Contains(SystemInfo.deviceModel)) {
                deviceName = fields[deviceNameInd];
                int.TryParse(fields[notchHeightInd], out notchHeight);
                deviceSupported = true;
#if UNITY_EDITOR
                Debug.Log($"Device model: {deviceName} ({SystemInfo.deviceModel})");
#endif
                break;
            }
        }

        // Unload asset
        Resources.UnloadAsset(textAsset);
    }
}
