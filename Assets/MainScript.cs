using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainScript : MonoBehaviour {
    // Simple classes to serialize to json
    [System.Serializable]
    public class DataJson {
        public List<DataLine> data = new List<DataLine>();
    }

    [System.Serializable]
    public class DataLine {
        public List<double> rawData = new List<double>();
        public List<double> conversionFactors = new List<double>();
        public List<double> parsedData = new List<double>();
    }

    // Serial port
    SerialPort port = new SerialPort();
    string delimeter = "";
    bool startParsing = false;

    // Data
    DataJson data = new DataJson();
    int dataBufferSize = 30;
    List<double> conversionFactors = new List<double>();

    // Game Objects
    TMP_Dropdown serialPortDp;
    TMP_Dropdown baudRateDp;
    TMP_InputField delimeterInp;
    Button startParsingButton;
    TextMeshProUGUI startParsingButtonLabel;
    TextMeshProUGUI parsedDataText;
    TMP_InputField filenameInp;
    TMP_Dropdown fileTypeDp;
    TMP_Dropdown conversionFactorDp;
    TMP_InputField conversionFactorInp;


    // Start is called before the first frame update
    void Start() {
        // Initializes all the game objects
        serialPortDp = GameObject.Find("SerialPort Dropdown").GetComponent<TMP_Dropdown>();
        baudRateDp = GameObject.Find("BaudRate Dropdown").GetComponent<TMP_Dropdown>();
        delimeterInp = GameObject.Find("Delimeter Input").GetComponent<TMP_InputField>();
        startParsingButton = GameObject.Find("StartParsing Button").GetComponent<Button>();
        startParsingButtonLabel = GameObject.Find("StartParsing Button Label").GetComponent<TextMeshProUGUI>();
        parsedDataText = GameObject.Find("Content").GetComponent<TextMeshProUGUI>();
        filenameInp = GameObject.Find("Filename Input").GetComponent<TMP_InputField>();
        fileTypeDp = GameObject.Find("FileType Dropdown").GetComponent<TMP_Dropdown>();
        conversionFactorDp = GameObject.Find("ConversionFactor Dropdown").GetComponent<TMP_Dropdown>();
        conversionFactorInp = GameObject.Find("ConversionFactor Input").GetComponent<TMP_InputField>();

        // Adds the available serial ports in the list of options
        // in the serial port dropdown
        List<string> serialPortNameList = new List<string>(SerialPort.GetPortNames());
        foreach (string serialPortName in serialPortNameList) {
            serialPortDp.options.Add(new TMP_Dropdown.OptionData() { text = serialPortName });
        }
    }

    // Update is called once per frame
    void Update() {
        // Returns if we are not parsing the data
        if (!startParsing) return;

        // Parses the data
        string token = "";
        if (port.IsOpen) {
            try {
                string str = port.ReadLine();
                DataLine dataList = new DataLine();

                // Parses through the line
                foreach (char ch in str) {
                    token += ch;
                    if (token.Contains(delimeter)) {
                        token = token.Substring(0, token.Length - delimeter.Length);
                        try {
                            dataList.rawData.Add(Double.Parse(token));
                        } catch (FormatException) { }
                        token = "";
                    }
                }

                // Converts all the raw data
                while (conversionFactors.Count < dataList.rawData.Count) fixConversionFactors(true);
                while (conversionFactors.Count > dataList.rawData.Count) fixConversionFactors(false);
                for (int i = 0; i < dataList.rawData.Count; ++i) {
                    dataList.conversionFactors.Add(conversionFactors[i]);
                    dataList.parsedData.Add(dataList.rawData[i] * dataList.conversionFactors[i]);
                }

                // Adds the line to the data
                data.data.Add(dataList);
            } catch (TimeoutException) { }
        }

        // Updates the parsed data text
        parsedDataText.text = "";
        for (int i = (data.data.Count > dataBufferSize) ? (data.data.Count - dataBufferSize) : 0; i < data.data.Count; ++i) {
            parsedDataText.text += String.Join(", ", data.data[i].parsedData.ToArray()) + '\n';
        }
    }

    // Initializes the serial port
    private void initSerialPort(string name) {
        // Closes the serial port if its already open
        if (port.IsOpen) port.Close();

        // Gets the baud rate
        string baudrateStr = baudRateDp.options[baudRateDp.value].text;
        int baudrate = Int32.Parse(baudrateStr.Substring(0, baudrateStr.IndexOf(' ')));

        // Initializes the serial port
        port = new SerialPort(name, baudrate);
        port.ReadTimeout = 20;
        port.Parity = Parity.None;
        port.StopBits = StopBits.One;
        port.DataBits = 8;
        port.Handshake = Handshake.None;
        port.NewLine = "\n";
        port.Open();
    }

    // Initializes the conversion factors
    private void initConversionFactors() {
        // Parses through a line in order to calculate
        // the number of inputs on one line
        int size = 0;
        string token = "";

        try {
            string str = port.ReadLine();
            foreach (char ch in str) {
                if (ch == '\n') break;
                token += ch;
                if (token.Contains(delimeter)) {
                    size++;
                    token = "";
                }
            }
            print(str);
        } catch (TimeoutException) { }

        // Adds the options to the dropdown
        for (int i = 1; i < size + 1; ++i) {
            conversionFactorDp.options.Add(new TMP_Dropdown.OptionData() { text = "Input " + i.ToString() });
            conversionFactors.Add(1.0);
        }

        conversionFactorDp.interactable = true;
        conversionFactorInp.interactable = true;
    }

    // Fixes the conversion factors
    private void fixConversionFactors(bool add) {
        if (add) {
            conversionFactorDp.options.Add(new TMP_Dropdown.OptionData() { text = "Input " + conversionFactorDp.options.Count.ToString() });
            conversionFactors.Add(1.0);
        } else {
            conversionFactorDp.options.RemoveAt(conversionFactorDp.options.Count - 1);
            conversionFactors.RemoveAt(conversionFactors.Count - 1);
        }
    }

    // Activated when a serial port is selected
    public void onSerialPortDropdown() {
        // Opens the selected serial port
        string portName = serialPortDp.options[serialPortDp.value].text;
        if (portName == "") return;
        initSerialPort(portName);

        // Sets the delimeter input to be interactable
        delimeterInp.interactable = true;
    }

    // Activated when the baud rate is changed
    public void onBaudRateDropdown() {
        // Gets the baud rate and sets it
        string baudrateStr = baudRateDp.options[baudRateDp.value].text;
        int baudrate = Int32.Parse(baudrateStr.Substring(0, baudrateStr.IndexOf(' ')));
        port.BaudRate = baudrate;
    }

    // Activated when a delimeter is entered
    public void onDelimeterInput() {
        delimeter = delimeterInp.text;
        startParsingButton.interactable = true;

        initConversionFactors();
    }

    // Activated when the start parsing button is pressed
    public void onStartParsingButton() {
        // Toggles if we are parsing data or not
        startParsing = !startParsing;
        startParsingButtonLabel.text = startParsing ? "Stop Parsing" : "Start Parsing";

        // Discards any data that was previously in the serial port
        port.DiscardInBuffer();
    }

    // Activated when the save data in file button is pressed
    public void onFileSaveButton() {
        // The data that will be written onto the file
        string dataToWrite = "";

        // Gets the filename and extension
        string filename = filenameInp.text;
        string fileType = fileTypeDp.options[fileTypeDp.value].text;

        // Formats the data based on the file type
        switch (fileType) {
        case ".txt":
        case ".csv":
            foreach (DataLine dl in data.data) {
                dataToWrite += String.Join(", ", dl.parsedData.ToArray()) + '\n';
            }
            break;
        case ".json":
            dataToWrite = JsonUtility.ToJson(data);
            break;
        }

        // Writes to the file
        File.WriteAllTextAsync(filename + fileType, dataToWrite);
    }

    // Activated when user finishes entering conversion factor
    public void onConversionFactorEnter() {
        if (conversionFactorDp.value != 0) {
            try {
                conversionFactors[conversionFactorDp.value - 1] = Double.Parse(conversionFactorInp.text);
            } catch (FormatException) { }
        }
    }

    // Exits the application when the user presses the quit button
    public void onQuitButton() {
        Application.Quit();
    }
}
