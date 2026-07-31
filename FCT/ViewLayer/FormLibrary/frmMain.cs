using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.IO.Ports;
using System.Management;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using FCT.ViewLayer.FormLibrary;
using System.Net.NetworkInformation;
using static System.Collections.Specialized.BitVector32;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Data.SqlTypes;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualBasic;
using System.Windows.Automation;
using System.Net.Sockets;
using System.Collections;
using System.Resources;
using Ivi.Visa;

using FCT.Models;
using FCT.Models.Products;
using FCT.Models.Products.Interface;
using FCT.Models.SFC;
using FCT.Models.SFC.Interface;
using FCT.Models.DUT;
using FCT.Models.DUT.Interface;
using FCT.Models.DUT.QBC;
using FCT.Models.DUT.QBC.I2C;
using FCT.Models.DUT.QBC.LED;
using FCT.Models.DUT.QBC.NTC;
using FCT.Models.DUT.PA45;
using FCT.Models.Instrument.RelayCard;
using FCT.Models.Instrument.CaptureCard;


using DUTS = FCT.Models.DUT;
using Prod = FCT.Models.Products;

using USB = FCT.Utilities.USBHelper;
using SMT_SFC = FCT.Utilities.SMT_SFCSHelper;
using SSG_SFC = FCT.Utilities.SFCSHelper;
using INI = FCT.Utilities.IniFileHelper;
using RELAY = FCT.Utilities.RelayHelper;
using PRINTER = FCT.Utilities.LabelPrintHelper;
using REGEX = FCT.Utilities.RegexHelper;
using DataAccess = FCT.Utilities.DataAccess;
using PROC = FCT.Utilities.ProcessHelper;
using WinDevice = FCT.Utilities.WinDeviceHelper;
using NFC = FCT.Utilities.ICardHelper;
using FCT.Models.Instrument;
using System.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace FCT.ViewLayer.FormLibrary
{
    public partial class frmMain : Form
    {
        #region 毫秒延时 界面不会卡死
        public static void Delay(int mm)
        {
            DateTime current = DateTime.Now;
            while (current.AddMilliseconds(mm) > DateTime.Now)
            {
                Application.DoEvents();
            }
            return;
        }
        #endregion

        public event EventHandler<RunTimeLogUpdatedEventArgs> RunTimeLogUpdated;

        public GetStringDelegate GetStringBeforeFlag;
        public GetStringDelegate GetStringAfterFlag;

        private string ConfigFile = Global.CONFIG_FILE;
        private string TestItemFile = Global.TEST_ITEMS_FILE;
        private string WifiMacLabFile = Global.WIFI_MAC_LAB_FILE;
        private string BluetoothMacLabFile = Global.BLUETOOTH_MAC_LAB_FILE;
        private DataTable ProductMatirx = Global.PRODUCT_SETTING_MATRIX;

        private IDUT dut;
        private IProduct product;
        private IProductConfig _productConfig;
        private IProductConfig productConfig
        {
            get { return _productConfig; }

            set
            {
                _productConfig = value;
                ProductConfigChanged?.Invoke(this, new ProductConfigChangedEventArgs(_productConfig));
                Invoke(new Action(() =>
                {
                    lblStation.Text = "[ " + productConfig.STATION_CODE + " ]";
                    lblModel.Text = "[ " + productConfig.MODEL + " ]";
                }));
            }
        }

        public event EventHandler<ProductConfigChangedEventArgs> ProductConfigChanged;

        private SFCS_Setting _smt_sfcs_setting;
        private SFCS_Setting _ssg_sfcs_setting;
        private ISFC sfcInstance;
        private Models.SFC.SMT_SFC _smt_sfc = new Models.SFC.SMT_SFC();
        private Models.SFC.SSG_SFC _ssg_sfc = new Models.SFC.SSG_SFC();

        private SSGRelayCard1 RelayCard1 = SSGRelayCard1.Instance;
        private SSGRelayCard2 RelayCard2 = SSGRelayCard2.Instance;
        private SSGCaptureCard1 CaptureCard1 = SSGCaptureCard1.Instance;
        private SSGCaptureCard2 CaptureCard2 = SSGCaptureCard2.Instance;
        private KEITHLEY_2306_LAN LAN2306;

        private frmRelay relay_form;
        private frmTestItems testitem_form;
        private frmSFCS sfcs_form;
        private frmSerialPort seri_form = new frmSerialPort();
        private frmProductMatrix prod_form;

        private string TESTER_VID;
        private string TESTER_PID;

        private double LSS_SENSOR_VALUE_MEDIA;
        private double LSS_SENSOR_VALUE_NO_MEDIA;
        private double LSS_SENSOR_VALUE_LINER;
        private double LSS_SENSOR_VALUE_DELTA;

        private string MainBarcode = "";

        private readonly string[] USB2ETH_MAC_ARRAY = new string[5];

        private string WIFI_MAC_ADDRESS;
        private string BLUETOOTH_MAC_ADDRESS;

        List<TimeSpan> dura_list = new List<TimeSpan>();

        private string MainboardPartNumber;
        private string MainboardSeriNumber;

        private string PrinterPartNumber;
        private string PrinterSeriNumber;

        private string PartNumber;
        private string SerialNumber;

        private Thread _trigger;

        private readonly StringFormat _headerFormat;
        private readonly Font _headerFont;

        private bool DUT_SERIAL_PORT_STATUS;


        Dictionary<string, string> SensorKeyValueDict = new Dictionary<string, string>();

        private int regular_time_out = 0;

        public frmMain()
        {
            InitializeComponent();

            RunTimeLogUpdated += OnRunTimeLogUpdated;
            ProductConfigChanged += OnProductConfigChanged;
            InitExternalDAQCard();

            GetStringBeforeFlag = new GetStringDelegate(GetStringBeforeFlagInner);
            GetStringAfterFlag = new GetStringDelegate(GetStringAfterFlagInner);

            _smt_sfcs_setting = new SFCS_Setting
                (
                INI.GetValue("SMT_SFC", "Product", ConfigFile),
                INI.GetValue("SMT_SFC", "Station", ConfigFile),
                INI.GetValue("SMT_SFC", "Line", ConfigFile),
                INI.GetValue("SMT_SFC", "testVersion", ConfigFile)
                );

            _ssg_sfcs_setting = new SFCS_Setting
                (
                INI.GetValue("SSG_SFC", "Product", ConfigFile),
                INI.GetValue("SSG_SFC", "Station", ConfigFile),
                INI.GetValue("SSG_SFC", "Line", ConfigFile),
                INI.GetValue("SSG_SFC", "testVersion", ConfigFile)
                );

            //FTDIBUS\COMPORT&VID_0403&PID_6001
            TESTER_VID = INI.GetValue("Device", "VID", ConfigFile);
            TESTER_PID = INI.GetValue("Device", "PID", ConfigFile);

            // 初始化一次即可
            _headerFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center // 垂直居中，之前的代码只设置了水平居中
            };

            _headerFont = new Font("Arial", 11, FontStyle.Bold);

            PROC.StaticProcessStart += OnProcessStart;

            txtPN.Focus();
        }

        #region string process

        public string GetRuntimeStringFlagA(string flag)
        {
            string value = string.Empty;
            Invoke(new Action(() =>
            {
                string tmp = rtbLog.Text;
                value = GetStringAfterFlag.Invoke(tmp, flag);
            }));
            return value;
        }

        public string GetRuntimeStringFlagB(string flag)
        {
            string value = string.Empty;
            this.Invoke(new Action(() => { value = GetStringBeforeFlag.Invoke(rtbLog.Text, flag); }));
            return value;
        }

        public string GetStringBeforeFlagInner(string context, string flag)
        {
            string value = string.Empty;
            this.Invoke(new Action(() => { value = REGEX.GetString(context, flag, true); }));
            return value;
        }

        public string GetStringAfterFlagInner(string context, string flag)
        {
            string value = string.Empty;
            Invoke(new Action(() =>
            {
                value = REGEX.GetString(context, flag, false);
                value = value.Replace(flag, "");
            }));
            return value;
        }
        #endregion

        #region UI opreatation

        private void SetTestEntery(bool status)
        {
            Invoke(new Action(() => { foreach (Control ctrl in groupBox1.Controls) { ctrl.Enabled = status; } }));
        }

        private void ClearLogUI()
        {
            Invoke(new Action(() =>
            {
                ClearLogTextBox();
                lblFinishTime.Text = "";
                lblStartTime.Text = "";
            }));
        }

        private void ClearLogTextBox()
        {
            Invoke(new Action(() => { foreach (Control ctrl in tabControl1.Controls) { if (ctrl is RichTextBox) { (ctrl as RichTextBox).Clear(); } } }));
        }

        private void ClearUI()
        {
            Invoke(new Action(() =>
            {
                txtPN.Clear();
                txtSN.Clear();
                txtSN.Focus();
            }));
        }

        private void UpdateErrorUI(Test_Proc_Error_Code flag)
        {
            Invoke(new Action(() =>
            {
                lblErrorCode.ForeColor = Color.Red;
                lblErrorCode.Text = flag.ToString();
                UpdateResultUI(false);
            }));
        }

        private void UpdateResultUI(bool result)
        {
            Invoke(new Action(() =>
            {
                lblResult.ForeColor = result ? Color.Green : Color.Red;
                lblResult.Text = result ? "PASS" : "FAIL";
            }));
        }

        private void InitializeTestItem(System.Windows.Forms.ListView listview, IDUT dut)
        {
            listview.Items.Clear();
            listview.Columns.Clear();
            listview.View = View.Details;

            listview.Columns.Add("Index", 50, HorizontalAlignment.Left);
            listview.Columns.Add("Name", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Upper Limit", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Lower Limit", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Description", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Value", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Result", 100, HorizontalAlignment.Center);
            listview.Columns.Add("Duration(s)", 100, HorizontalAlignment.Center);

            for (int i = 0; i < dut.TestItems.Count; i++)
            {
                TestItem tItem = dut.TestItems[i];

                ListViewItem lvItem = new ListViewItem((i + 1).ToString());

                lvItem.SubItems.Add(tItem.Name);
                lvItem.SubItems.Add(tItem.UpperLimit != "" ? tItem.UpperLimit : "/");
                lvItem.SubItems.Add(tItem.LowerLimit != "" ? tItem.LowerLimit : "/");
                lvItem.SubItems.Add(tItem.Description);
                lvItem.SubItems.Add("NA");
                lvItem.SubItems.Add("NA");
                lvItem.SubItems.Add("NA");
                listview.Items.Add(lvItem);
            }
        }

        #endregion

        #region  relaycard, capturecard,

        private void InitExternalDAQCard()
        {
            RelayCard1.OpenRelayCard += OnDAQCardOpened;
            RelayCard1.CloseRelayCard += OnDAQCardClosed;
            RelayCard1.RelayOn += OnRelayOn;
            RelayCard1.RelayOff += OnRelayOff;
            RelayCard1.GetDAQCardDI += OnGetDAQCardDI;
            RelayCard1.SetDAQCardDO += OnSetDAQCardDO;
            RelayCard1.OnMeasureVoltage += OnMeasureVoltage;

            RelayCard2.OpenRelayCard += OnDAQCardOpened;
            RelayCard2.CloseRelayCard += OnDAQCardClosed;
            RelayCard2.RelayOn += OnRelayOn;
            RelayCard2.RelayOff += OnRelayOff;
            RelayCard2.GetDAQCardDI += OnGetDAQCardDI;
            RelayCard2.SetDAQCardDO += OnSetDAQCardDO;
            RelayCard2.OnMeasureVoltage += OnMeasureVoltage;

            CaptureCard1.OpenCaptureCard += OnDAQCardOpened;
            CaptureCard1.CloseCaptureCard += OnDAQCardClosed;
            CaptureCard1.OnMeasureCurrent += OnMeasureCurrent;
            CaptureCard1.OnMeasureVoltage += OnMeasureVoltage;
            CaptureCard1.SetDAQCardDO += OnSetDAQCardDO;
            CaptureCard1.GetDAQCardDI += OnGetDAQCardDI;

            CaptureCard2.OpenCaptureCard += OnDAQCardOpened;
            CaptureCard2.CloseCaptureCard += OnDAQCardClosed;
            CaptureCard2.OnMeasureCurrent += OnMeasureCurrent;
            CaptureCard2.OnMeasureVoltage += OnMeasureVoltage;
            CaptureCard2.SetDAQCardDO += OnSetDAQCardDO;
            CaptureCard2.GetDAQCardDI += OnGetDAQCardDI;

        }

        private void OnDAQCardClosed(object sender, DAQCardClosedEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: CloseCard, Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OnDAQCardOpened(object sender, DAQCardOpenedEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: OpenCard, Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OnGetDAQCardDI(object sender, GetDAQCardDIEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: GetDI, Channel: {e.ChannelIndex}, Value: {e.Value}, Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OnSetDAQCardDO(object sender, SetDAQCardDOEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: SetDO, Channel: {e.ChannelIndex}, Value: {e.Value}, Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OnMeasureVoltage(object sender, MeasureVoltageEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: MeasureVoltage, Channel: {e.ChannelIndex}, Value: {e.Value}, Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OnMeasureCurrent(object sender, MeasureCurrentEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: MeasureCurrent, Channel: {e.ChannelIndex}, Value: {e.Value}, Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OnRelayOn(object sender, RelayOnEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: SetRelayOn({e.ChannelIndex}), Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OnRelayOff(object sender, RelayOffEventArgs e)
        {
            string msg = $"InstrmentType: {e.InstrumentType.GetType().Name}, Action: SetRelayOff({e.ChannelIndex}), Result: {e.Result}";
            AppendRuntimeLog(msg);
        }

        private void OpenExternalDAQCard()
        {
            Invoke(new Action(() =>
            {
                btnRelay1.BackColor = RelayCard1.Open() ? Color.Lime : Color.Red;
                btnRelay2.BackColor = RelayCard2.Open() ? Color.Lime : Color.Red;
                btnRelayCap1.BackColor = CaptureCard1.Open() ? Color.Lime : Color.Red;
                btnRelayCap2.BackColor = CaptureCard2.Open() ? Color.Lime : Color.Red;
            }));
        }

        private void CloseAllRelays()
        {
            RelayCard1.SetAllRelayOff();
            RelayCard2.SetAllRelayOff();
        }

        #endregion

        #region frmMain Event

        private void frmMain_Load(object sender, EventArgs e)
        {
            lblStartTime.Text = "";
            lblFinishTime.Text = "";
            lblDuration.Text = "";

            lblStation.Text = "";
            lblModel.Text = "";
            slblPortName.Text = "";
            txtPN.Enabled = false;
            ckbIsPrintLog.Checked = true;
            ckbIsPrintLog.Enabled = false;
            ckbFailStop.Checked = true;
            ckbSFCS.Checked = true;

            dgvProductMatrix.DataSource = ProductMatirx;
            dgvProductMatrix.Refresh();
            dgvProductMatrix.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e) { PROC.StaticProcessStart -= OnProcessStart; }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            if (prod_form == null) { prod_form = new frmProductMatrix(ProductMatirx); }

            DialogResult result = prod_form.ShowDialog();
            if (result == DialogResult.Yes) { productConfig = prod_form.ProductConfig; }
            txtPN.Focus();
        }

        private void txtSN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ClearLogUI();

                pb_test_thread.Value = 0;
                rtbDOS.Clear();
                rtbLog.Clear();
                rtbSerialport.Clear();
                rtbUSB.Clear();

                //PA45L0002D22;26189C0080

                AppendRuntimeLog($"get GUI input{txtSN.Text}");
                if (txtSN.Text.Contains(";"))
                {
                    string[] m_ConfigNums = txtSN.Text.Split(';');

                    if (m_ConfigNums.Length != 2)
                    {
                        MessageBox.Show($"this isn't a good SN or PN.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ClearUI();
                        return;
                    }

                    txtPN.Text = m_ConfigNums[0];
                    txtSN.Text = m_ConfigNums[1];
                }

                SerialNumber = txtSN.Text;
                PartNumber = txtPN.Text;

            GET_PROD_CONFIG:
                if (productConfig == null)
                {
                    MessageBox.Show($"Product Config can't be empty!", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (prod_form != null) { prod_form.ShowDialog(); }
                    productConfig = prod_form.ProductConfig;
                }

                lblResult.Text = "Testing...";
                lblResult.ForeColor = Color.FromArgb(255, 255, 255);
                lblResult.BackColor = Color.FromArgb(255, 255, 0);

                if (productConfig == null) { goto GET_PROD_CONFIG; }

                if (product == null)
                {
                    ClearLogUI();
                    product = Product.CreateProductByProdConfig(SerialNumber, productConfig);
                    product.RunTimeLogUpdated += OnRunTimeLogUpdated;
                }

                // PA45 PN Special

                Prod.PA45 m_PA45 = product as Prod.PA45;

                if (m_PA45 != null)
                {
                    m_PA45.HON_PN = PartNumber;
                }

                if (dut == null)
                {
                    dut = DUT.CreateDutByProduct(product);
                    dut.InitializeTestItem();
                    dut.TestErrorStatusChanged += OnTestErrorStatusChanged;
                    dut.TestMethod = test_and_display_each_item;

                    InitializeTestItem(lvTestItems, dut);
                }

                if (!dut.Product.CheckBarcodeFormat())
                {
                    dut.ErrorCode = Test_Proc_Error_Code.BadSN;
                    dut.Product.Dispose();
                    dut.Product = null;
                    dut.RunTimeLogUpdated -= OnRunTimeLogUpdated;
                    dut.TestErrorStatusChanged -= OnTestErrorStatusChanged;
                    dut = null;

                    product.Dispose();
                    product = null;

                    txtPN.Focus();
                    txtSN.SelectAll();
                    return;
                }

                if (productConfig.STATION_CODE.ToUpper() == "OOBA")
                {
                    ckbSFCS.Checked = false;
                    ckbSFCS.Enabled = false;
                }

                string m_model = "";
                if (productConfig.DATABASE.ToUpper() == "SCANNER")
                {
                    m_model = "Scanner";
                }

                string m_sn = "";
                if (m_PA45 != null)
                {
                    m_sn = m_PA45.HON_PN + ";" + m_PA45.SerialNumber;
                    string m_pcba_seri_num = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE", m_sn);
                    MainBarcode = m_pcba_seri_num;
                    AppendRuntimeLog($"{m_sn} get main barcode is {m_pcba_seri_num}");
                }
                else
                {
                    MainBarcode = dut.Product.SerialNumber;
                }

                if (ckbSFCS.Checked)
                {
                    string sfcLog = _ssg_sfc.CheckStationPass(
                    m_model,
                    productConfig.STATION_CODE,
                    MainBarcode,
                    productConfig.LINE_CODE,
                    "1.0");

                    AppendRuntimeLog($"Product is : {m_model}.");
                    AppendRuntimeLog($"Station is : {productConfig.STATION_CODE}.");
                    AppendRuntimeLog($"Barcode is : {MainBarcode}.");
                    AppendRuntimeLog($"Line code is : { productConfig.LINE_CODE}.");

                    AppendRuntimeLog($"SN: {MainBarcode}, check SFC station pass: {sfcLog}");

                    if (sfcLog != "Y")
                    {
                        MessageBox.Show(
                            $"{SerialNumber} check sfcs station NG!!!" + "\n" + $"result is {sfcLog}",
                            Global.AssemblyName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        UpdateErrorUI(Test_Proc_Error_Code.SfcChkStaNG);
                        return;
                    }
                }

                OpenExternalDAQCard();

                DialogResult bStartTest = MessageBox.Show(
                    "请将打印机和测试电脑连接，并且按下打印机开机按钮，准备测试。\nConnect the printer with PC and press down power button, then start test.",
                    Global.AssemblyName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                slblPortName.Text = "";
                if (bStartTest == DialogResult.Yes)
                {
                    if (spTester.IsOpen) { spTester.Close(); }

                    if (spDutDebugPort.IsOpen) { spDutDebugPort.Close(); }

                    // PA45 PN Special
                    if (product is Prod.PA45_MainBoard mObj &&
                       (product.ProductConfig.STATION_CODE.ToUpper().Contains("FCT") ||
                       product.ProductConfig.STATION_CODE.ToUpper().Contains("FW FLASH")))
                    {
                        InitializeSerialport("0403", "6001", spDutDebugPort);
                    }

                    StartTest();
                }
            }
        }

        private void txtPN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                PartNumber = txtPN.Text;

                if (string.IsNullOrEmpty(PartNumber) || string.IsNullOrWhiteSpace(PartNumber)) { return; }

                txtSN.Focus();
            }
        }

        #endregion

        private ProductConfig CreateProductConfigByStartWith(string SN)
        {
            ProductConfig m_prodConfig = new ProductConfig();
            return m_prodConfig;
        }

        /// <summary>
        /// 带格式追加文本到 RichTextBox
        /// </summary>
        /// <param name="rtb">目标 RichTextBox</param>
        /// <param name="text">要追加的文本</param>
        /// <param name="font">文本字体</param>
        /// <param name="foreColor">文本颜色</param>
        private void AppendTextWithStyle(RichTextBox rtb, string text, Font font, Color foreColor)
        {
            // 1. 记录当前光标位置（避免覆盖原有选区）
            int startIndex = rtb.TextLength;
            // 2. 追加文本
            rtb.AppendText(text);
            // 3. 选中刚追加的文本（从记录位置到文本末尾）
            rtb.Select(startIndex, text.Length);
            // 4. 设置选中文本的字体和颜色
            rtb.SelectionFont = font;
            rtb.SelectionColor = foreColor;
            // 5. 取消选区（可选，避免文本保持高亮）
            rtb.DeselectAll();
            // 6. 光标移到末尾（优化用户体验）
            rtb.SelectionStart = rtb.TextLength;
            rtb.ScrollToCaret();
        }

        private bool GetDutInstance(string vid, string pid)
        {
            bool rlt = false;

            string value = "";

            for (int i = 0; i < 60; i++)
            {
                value = WinDevice.GetSerialportComPort(vid, pid);

                if ((!string.IsNullOrEmpty(value)) && (value != "COM1"))
                {
                    AppendRuntimeLog("detected printer startup OK.");
                    rlt = true;
                    break;
                }

                AppendRuntimeLog("wait to printer startup...");
                Delay(1000);
            }

            return rlt;
        }

        #region  customized event handler

        private void OnProcessStart(ProcessStartEventArgs e) { AppendDOSLog(e.Arguments, false, true); }

        private void OnProductConfigChanged(object sender, ProductConfigChangedEventArgs e) { MessageBox.Show($"Set Product Config OK.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information); }

        private void OnTestErrorStatusChanged(object sender, TestErrorStatusChangedEventArgs e)
        {
            UpdateErrorUI(e.ErrorCode);
        }

        private void OnSfcs_form_MESSettingChanged(object sender, MESSettingChangedEventArgs e)
        {

            _ssg_sfcs_setting = e._sfcs_setting;

            INI.SetValue("SMT_SFC", "Product", _ssg_sfcs_setting.Product, ConfigFile);
            INI.SetValue("SMT_SFC", "PartNumber", _ssg_sfcs_setting.PartNumber, ConfigFile);
            INI.SetValue("SMT_SFC", "Station", _ssg_sfcs_setting.Station, ConfigFile);
            INI.SetValue("SMT_SFC", "Line", _ssg_sfcs_setting.Line, ConfigFile);
            INI.SetValue("SMT_SFC", "testVersion", _ssg_sfcs_setting.testVersion, ConfigFile);

            MessageBox.Show("sfcs setting changed ok!", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnRunTimeLogUpdated(object sender, RunTimeLogUpdatedEventArgs e)
        {
            Invoke(new Action(() =>
            {
                string msg = e.IsAddTimestamp ?
                ("\n" + "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + e.Message + Environment.NewLine) :
                ("\n" + e.Message + Environment.NewLine);

                rtbLog.AppendText(msg);

                rtbLog.Select(rtbLog.TextLength, 0);
                rtbLog.ScrollToCaret();
            }));
        }

        private void AppendRuntimeLog(string message, bool isAddTimestamp = false) { RunTimeLogUpdated?.Invoke(this, new RunTimeLogUpdatedEventArgs(message, isAddTimestamp)); }

        private void AppendUSBLog(string message, bool isAddTimestamp = false)
        {
            Invoke(new Action(() =>
            {
                if (isAddTimestamp)
                    rtbUSB.AppendText("\n" + "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
                else
                    rtbUSB.AppendText("\n" + message + "\r\n");

                rtbUSB.Select(rtbSerialport.TextLength, 0);
                rtbUSB.ScrollToCaret();
            }));
        }

        private void AppendDOSLog(string message, bool isError, bool isAddTimestamp = false)
        {
            Invoke(new Action(() =>
            {
                // 定义自定义字体和颜色（可根据需求调整）
                Font errorFont = new Font("Calibri", 12, FontStyle.Italic);
                Color errorColor = Color.Red;

                Font normalFont = new Font("Calibri", 12, FontStyle.Regular);
                Color normalColor = Color.LimeGreen;

                if (isAddTimestamp)
                {
                    message = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n";
                }
                else
                {
                    message = message + "\r\n";
                }

                if (isError)
                    AppendTextWithStyle(rtbDOS, message, errorFont, errorColor);
                else
                    AppendTextWithStyle(rtbDOS, message, normalFont, normalColor);

                rtbDOS.Select(rtbDOS.TextLength, 0);
                rtbDOS.ScrollToCaret();
            }));
        }

        #endregion

        #region ---QBC Test---
        private bool led_test(LED_ID id, bool status, ref string resultValue)
        {
            bool rlt = false, rltFW = false, rltManu = false, bFlag = false;
            string value = "";
            string command = LED.LedTestCommand(id, status);

            SerialPortWriteLineOnce(command);

            string val_temp = "";
            for (int i = 0; i < 60; i++)
            {
                val_temp = GetRuntimeStringFlagA(command);
                if (!string.IsNullOrEmpty(val_temp) && !string.IsNullOrWhiteSpace(val_temp))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            if (!bFlag)
            {
                value = "TimeOut";
                rltFW = false;
                goto End;
            }

            if (bFlag)
            {
                value = val_temp.Trim();
                rltFW = value.Contains(LED.LedTestResult(id, status));
                value = rltFW ? "PASS" : "FAIL";
            }

            string _status = status ? "ON" : "OFF";

            DialogResult result = MessageBox.Show(
                string.Format($"Please check the LED {id} have {_status}  "),
                Global.AssemblyName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            rltManu = (result == DialogResult.Yes) ? true : false;

        End:
            rlt = rltFW && rltManu;
            resultValue = value;
            return rlt;
        }

        private bool Test_QBC_LED(int index, TestItem item)
        {
            string tmpName = item.Name.Replace("Led_", "L_");
            bool status;
            LED_ID id = LED_ID.L_G1;
            status = tmpName.Contains($"{LED_Status.ledon}") ? true : false;

            foreach (LED_ID led in Enum.GetValues(typeof(LED_ID)))
            {
                if (tmpName.Contains($"{led}"))
                {
                    id = led;
                    break;
                }
            }

            bool rlt = false;
            string value = "";

            rlt = led_test(id, status, ref value);

            item.Value = value;
            return rlt;
        }

        private bool i2c_battery_currentInit_test(I2C_Current currId, out double wCurr, out double rCurr, out string refValue)
        {
            string value = string.Empty;
            string tmp_val = "", w_current = "", r_current = "";
            bool rlt = false, bFlag = false;

            string command = I2C.CurrentInitTestCmd(currId);

            SerialPortWriteLineOnce(command);

            for (int i = 0; i < 60; i++)
            {
                tmp_val = GetRuntimeStringFlagA(command);
                if (!string.IsNullOrEmpty(tmp_val) && !string.IsNullOrWhiteSpace(tmp_val))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            if (!bFlag)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmp_val.Contains("I2C no ACK"))
            {
                value = "I2C_NO_ACK";
                rlt = false;
                goto End;
            }

            List<string> versions = tmp_val.Split('\n').ToList();
            versions.RemoveAll(s => string.IsNullOrEmpty(s));

            foreach (var v in versions)
            {
                if (v.Contains("write current =")) { w_current = v.Replace("write current =", "").Trim(); }
                if (v.Contains("read current =")) { r_current = v.Replace("read current =", "").Trim(); }
            }

            value = w_current + "," + r_current;

        End:
            if (!string.IsNullOrEmpty(w_current) && !string.IsNullOrWhiteSpace(w_current) &&
                !string.IsNullOrEmpty(r_current) && !string.IsNullOrWhiteSpace(r_current))
            {
                rlt = true;
            }
            refValue = value;
            wCurr = Convert.ToDouble(w_current.Replace("mA", ""));
            rCurr = Convert.ToDouble(r_current.Replace("mA", ""));
            return rlt;
        }

        private bool Test_QBC_BAT_CURR(int index, TestItem item)
        {
            string tmp = item.Name.Replace("battery", "BQIC");
            I2C_Current i2cCurr = I2C_Current.BQIC1;

            foreach (I2C_Current curr in Enum.GetValues(typeof(I2C_Current)))
            {
                if (tmp.Contains($"{curr}"))
                {
                    i2cCurr = curr;
                    break;
                }
            }

            bool rlt = false;
            string value = "";
            double wCurr = 0, rCurr = 0;
            rlt = i2c_battery_currentInit_test(i2cCurr, out wCurr, out rCurr, out value);

            item.Value = value;
            rlt =
                wCurr == Convert.ToDouble(item.UpperLimit) &&
                wCurr == Convert.ToDouble(item.LowerLimit) &&
                rCurr == Convert.ToDouble(item.UpperLimit) &&
                rCurr == Convert.ToDouble(item.LowerLimit);

            return rlt;
        }

        private bool i2c_battery_temperature_test(I2C_Battery batteryId, ref string resultValue)
        {
            string value = string.Empty;
            string tmp_val = "";
            bool rlt = false;

            string command = I2C.BatteryTemperatureCmd(batteryId);

            SerialPortWriteLineOnce(command);

            bool bFlag = false;
            this.Invoke(new Action(() =>
            {
                for (int i = 0; i < 60; i++)
                {
                    tmp_val = REGEX.GetString(rtbLog.Text, command, false);
                    if (!string.IsNullOrEmpty(tmp_val) && !string.IsNullOrWhiteSpace(tmp_val))
                    {
                        bFlag = true;
                        break;
                    }

                    Delay(500);
                }
            }));

            if (!bFlag)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmp_val.Contains("I2C no ACK"))
            {
                value = "I2C_NO_ACK";
                rlt = false;
                goto End;
            }

            List<string> values = tmp_val.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s));

            if (batteryId == I2C_Battery.BAT_1) { tmp_val = "Bat1 Temp ="; }
            if (batteryId == I2C_Battery.BAT_2) { tmp_val = "Bat2 Temp ="; }
            if (batteryId == I2C_Battery.BAT_3) { tmp_val = "Bat3 Temp ="; }
            if (batteryId == I2C_Battery.BAT_4) { tmp_val = "Bat4 Temp ="; }

            foreach (var v in values)
            {
                if (v.Contains(tmp_val))
                {
                    value = v.Replace(tmp_val, "").Trim();
                    break;
                }
            }

        End:
            resultValue = value;

            if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                rlt = true;
            }

            return rlt;
        }

        private bool Test_QBC_BAT_TEMP(int index, TestItem item)
        {
            string tmp = item.Name.Replace("battery", "BAT_");
            I2C_Battery i2cBat = I2C_Battery.BAT_1;

            foreach (I2C_Battery bat in Enum.GetValues(typeof(I2C_Battery)))
            {
                if (tmp.Contains($"{bat}"))
                {
                    i2cBat = bat;
                    break;
                }
            }

            bool rlt = false;
            string value = "";

            i2c_battery_temperature_test(i2cBat, ref value);

            item.Value = value;
            double result;
            rlt = double.TryParse(item.Value, out result);

            if (rlt)
            {
                rlt = item.Result = item.IsValueInRange<double>();
            }
            else
            {
                rlt = false;
            }

            return rlt;
        }

        private bool i2c_battery_capacity_test(I2C_Battery batteryId, ref string resultValue)
        {
            string value = string.Empty;
            string tmp_val = "";
            bool rlt = false, bFlag = false;

            string command = I2C.BatteryCapacityCmd(batteryId);

            SerialPortWriteLineOnce(command);

            for (int i = 0; i < 60; i++)
            {
                tmp_val = GetRuntimeStringFlagA(command);
                if (!string.IsNullOrEmpty(tmp_val) && !string.IsNullOrWhiteSpace(tmp_val))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            if (!bFlag)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmp_val.Contains("I2C no ACK"))
            {
                value = "I2C_NO_ACK";
                rlt = false;
                goto End;
            }

            List<string> values = tmp_val.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s));

            if (batteryId == I2C_Battery.BAT_1) { tmp_val = "Bat1 Cap ="; }
            if (batteryId == I2C_Battery.BAT_2) { tmp_val = "Bat2 Cap ="; }
            if (batteryId == I2C_Battery.BAT_3) { tmp_val = "Bat3 Cap ="; }
            if (batteryId == I2C_Battery.BAT_4) { tmp_val = "Bat4 Cap ="; }

            foreach (var v in values)
            {
                if (v.Contains(tmp_val))
                {
                    value = v.Replace(tmp_val, "").Trim();
                    break;
                }
            }

        End:
            resultValue = value;
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                rlt = true;
            }
            return rlt;

        }

        private bool Test_QBC_BAT_CAPA(int index, TestItem item)
        {
            string tmp = item.Name.Replace("battery", "BAT_");
            I2C_Battery i2cBat = I2C_Battery.BAT_1;

            foreach (I2C_Battery bat in Enum.GetValues(typeof(I2C_Battery)))
            {
                if (tmp.Contains($"{bat}"))
                {
                    i2cBat = bat;
                    break;
                }
            }

            bool rlt = false;
            string value = "";

            rlt = i2c_battery_capacity_test(i2cBat, ref value);

            item.Value = value;
            item.Result = item.IsValueInRange<double>();
            return rlt;
        }

        private bool i2c_battery_capVoltage_test(I2C_Battery batteryId, ref string resultValue)
        {
            string value = string.Empty;
            string tmp_val = "";
            bool rlt = false, bFlag = false;

            string command = I2C.BatteryCapVoltageCmd(batteryId);

            SerialPortWriteLineOnce(command);
            for (int i = 0; i < 60; i++)
            {
                tmp_val = REGEX.GetString(rtbLog.Text, command, false);
                if (!string.IsNullOrEmpty(tmp_val) && !string.IsNullOrWhiteSpace(tmp_val))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            if (!bFlag)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmp_val.Contains("I2C no ACK"))
            {
                value = "I2C_NO_ACK";
                rlt = false;
                goto End;
            }
            List<string> values = tmp_val.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s));

            if (batteryId == I2C_Battery.BAT_1) { tmp_val = "Bat1 Vol ="; }
            if (batteryId == I2C_Battery.BAT_2) { tmp_val = "Bat2 Vol ="; }
            if (batteryId == I2C_Battery.BAT_3) { tmp_val = "Bat3 Vol ="; }
            if (batteryId == I2C_Battery.BAT_4) { tmp_val = "Bat4 Vol ="; }

            foreach (var v in values)
            {
                if (v.Contains(tmp_val))
                {
                    value = v.Replace(tmp_val, "").Trim();
                    break;
                }
            }

        End:
            resultValue = value;

            if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                rlt = true;
            }
            return rlt;
        }

        private bool Test_QBC_BAT_CAPVOLT(int index, TestItem item)
        {
            string tmp = item.Name.Replace("battery", "BAT_");
            I2C_Battery i2cBat = I2C_Battery.BAT_1;

            foreach (I2C_Battery bat in Enum.GetValues(typeof(I2C_Battery)))
            {
                if (tmp.Contains($"{bat}"))
                {
                    i2cBat = bat;
                    break;
                }
            }

            bool rlt = false;
            string value = "";

            rlt = i2c_battery_capVoltage_test(i2cBat, ref value);

            item.Value = value;
            item.Result = item.IsValueInRange<double>();
            return rlt;
        }

        private bool ntc_temperature_test(NTC_ID ntcId, ref string resultValue)
        {
            string value = string.Empty, tmp_val = "";
            bool rlt = false, bFlag = false;

            string command = NTC.ReadDeviceTempCmd(ntcId);

            SerialPortWriteLineOnce(command);

            for (int i = 0; i < 60; i++)
            {
                tmp_val = REGEX.GetString(rtbLog.Text, command, false);
                if (!string.IsNullOrEmpty(tmp_val) && !string.IsNullOrWhiteSpace(tmp_val))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            if (!bFlag)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }


            List<string> values = tmp_val.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s));

            foreach (var v in values)
            {
                if (v.Contains("read NTC="))
                {
                    value = v.Replace("read NTC=", "").Trim();
                    break;
                }
            }

        End:
            resultValue = value;

            if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                rlt = true;
            }

            return rlt;
        }

        private bool Test_QBC_NTC_TEMP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            rlt = ntc_temperature_test(NTC_ID.NTC, ref value);
            if (value.Contains("mV"))
            {
                value = value.Replace("mV", "").Trim();
            }
            else
            {
                rlt = false;
                item.Value = value;
                goto End;
            }

            item.Value = value;
            rlt = item.IsValueInRange<double>();
        End:
            return rlt;
        }

        private bool Test_QBC_Enter_Factory(int index, TestItem item)
        {
            string value = string.Empty;
            string tmp_val = "";
            bool rlt = false;

            string command = Models.DUT.QBC.QBC.EnterFactoryModeCmd();

            SerialPortWriteLineOnce(command);

            for (int i = 0; i < 60; i++)
            {
                tmp_val = GetRuntimeStringFlagA(command);
                if (!string.IsNullOrEmpty(tmp_val) && !string.IsNullOrWhiteSpace(tmp_val))
                {
                    if (tmp_val.Contains("Enter Factory mode"))
                    {
                        rlt = true;
                        break;
                    }
                }
                Delay(500);
            }

            value = rlt ? "PASS" : "FAIL";
            item.Value = value;
            return rlt;
        }

        private bool Test_QBC_Exit_Factory(int index, TestItem item)
        {
            string value = string.Empty, tmp_val = "";
            bool rlt = false;

            string command = Models.DUT.QBC.QBC.ExitFactoryModeCmd();

            SerialPortWriteLineOnce(command);

            rlt = true;
            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_QBC_FWVersion(int index, TestItem item)
        {
            string value = string.Empty;
            string tmp_val = "";
            bool rlt = false;

            SerialPortWriteLineOnce(Models.DUT.QBC.QBC.GetFWVersionCmd());

            bool bFlag = false;

            for (int i = 0; i < 60; i++)
            {
                tmp_val = GetRuntimeStringFlagA(Models.DUT.QBC.QBC.GetFWVersionCmd());
                if (!string.IsNullOrEmpty(tmp_val) && !string.IsNullOrWhiteSpace(tmp_val))
                {
                    bFlag = true;
                    break;
                }
                Delay(500);
            }

            if (!bFlag)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            List<string> values = tmp_val.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s));

            foreach (var v in values)
            {
                if (v.Contains("FW Version"))
                {
                    value = v.Replace("FW Version", "").Trim();
                    break;
                }
            }

        End:
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
            {
                rlt = true;
            }
            item.Value = value;
            return rlt;
        }

        #endregion

        #region ---USB2ETH---

        private bool ping_one_bay(string clentIpAddr, string hostIpAddr, ref string output, ref string error, int outTime = 0)
        {
            string args = $"-S {clentIpAddr} {hostIpAddr}";
            PROC.CmdStart(args, ref output, ref error, 0);
            return output.Contains("TTL");
        }

        private bool Test_USB2ETH_Ping_OneBay(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            string clentIpAddr = WinDevice.GetPhysicalEthernetAdaptersIpAddr("");
            string hostIpAddr = WinDevice.GetPhysicalEthernetAdaptersIpAddr("");

            string output = "";
            string error = "";

            rlt = ping_one_bay(clentIpAddr, hostIpAddr, ref output, ref error) ? true : false;

            //get_network_interface();

            AppendDOSLog(output, false, false);
            AppendDOSLog(error, true, false);

            item.Value = value;
            return rlt;
        }

        private bool write_mac_into_lan9500(string mac, ref string output, ref string error)
        {
            string cliPath = Path.Combine(Global.ASSEMBLY_PATH, "9500eepApp_V2.5", "release", "9500eepApp.exe");
            string cliConfigPath = Path.Combine(Global.ASSEMBLY_PATH, "9500eepApp_V2.5", "release", "9500Aeep.ini");
            string args = string.Format($"-i {cliConfigPath} -M {mac} -S {DateTime.Now.Year.ToString("D4")}{DateTime.Now.Month.ToString("D2")}{DateTime.Now.Day.ToString("D2")} -w");

            PROC.ProcessStart(cliPath, args, ref output, ref error, 0);

            return output.Contains("The current device was programmed");
        }

        private bool Test_USB2ETH_WriteMac(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            MessageBox.Show("Please put the USB jig on the dock port.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Delay(3000);
            bool isConneted = false;

            for (int i = 0; i < 60; i++)
            {
                isConneted = WinDevice.IsPhysicalEthernetAdapters("LAN9500A");
                if (isConneted) { break; }
                Delay(1000);
            }

            if (!isConneted)
            {
                rlt = false;
                value = "DeviceNotExist";
                goto End;
            }

            //Write MAC(USB2ETH)2
            int indexMac = Convert.ToInt32(item.Name.Replace("Write MAC(USB2ETH)", ""));
            indexMac = indexMac - 1;

            string mac = USB2ETH_MAC_ARRAY[indexMac];
            string output = "";
            string error = "";
            rlt = write_mac_into_lan9500(mac, ref output, ref error);

            /*
            string lab_file_path = Path.Combine(Global.ASSEMBLY_PATH, "LabFile", "usb2eth_mac.Lab");

            if (File.Exists(lab_file_path))
            {
                PRINTER.PrintLabel(lab_file_path, "mac", mac);
            }
            */


            AppendDOSLog(output, false);
            AppendDOSLog(error, true);

            value = rlt ? "PASS" : "FAIL";
            value = mac + "/" + value;
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_USB2ETH_Ping(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            MessageBox.Show("Please put the USB jig on the dock port.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Delay(3000);
            bool isConneted = false;

            for (int i = 0; i < 60; i++)
            {
                isConneted = WinDevice.IsPhysicalEthernetAdapters("LAN9500A");
                if (isConneted) { break; }
                Delay(1000);
            }

            if (!isConneted)
            {
                rlt = false;
                value = "DeviceNotExist";
                goto End;
            }

            string currMac = WinDevice.GetPhysicalEthernetAdaptersMac("LAN9500A");
            DialogResult bCheckMac = MessageBox.Show(
                $"检测到当前的MAC Address是{currMac},请和产品后盖确认是否一致。",
                Global.AssemblyName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            slblPortName.Text = "";
            if (bCheckMac != DialogResult.Yes)
            {
                rlt = false;
                value = "MAC_ADDRESS_DISMATCH";
                goto End;
            }



            // 2. 设定目标网卡名称（请替换为你电脑上实际显示的名称，如 "以太网" 或 "WLAN"）
            string targetAdapterName = "LAN9500A";

            // 3. 设定新的网络参数
            string newIP = "192.168.1.100";
            string subnetMask = "255.255.255.0";
            string[] gateways = new string[] { "192.168.1.1" };
            string[] dnsServers = new string[] { "8.8.8.8", "114.114.114.114" };

            try
            {
                Console.WriteLine($"正在尝试为 [{targetAdapterName}] 设置IP...");

                string m_adapter_name = WinDevice.GetNetworkAdapters("LAN9500A");
                WinDevice.SetNetworkAdapterIP(m_adapter_name, newIP, subnetMask, gateways, dnsServers);
                Console.WriteLine("✅ 网络配置修改成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 修改失败: {ex.Message}");
            }

            Delay(5000);

            string ip = INI.GetValue("USB2ETH", "IP_Address", ConfigFile);
            string args = $"ping {ip}";
            string output = "", error = "";

            PROC.CmdStart(args, ref output, ref error, 0);
            AppendDOSLog(output, false);
            AppendDOSLog(error, true);

            if (output.Contains("TTL"))
            {
                rlt = true;
            }
            else
            {
                rlt = false;
            }


            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_USB2ETH_ReadMac(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            MessageBox.Show("Please put the USB jig on the dock port.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Delay(3000);

            for (int i = 0; i < 60; i++)
            {
                value = WinDevice.GetPhysicalEthernetAdaptersMac("LAN9500A");
                if (!string.IsNullOrEmpty(value))
                {
                    rlt = true;
                    break;
                }
                Delay(500);
            }

            item.Value = value;
            return rlt;
        }

        private bool Test_USB2ETH_FWVersion(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            value = "ATE_NOT_IMPLEMENT";

            item.Value = value;
            return rlt;
        }

        private bool Test_USB2ETH_GetMacArray(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            string usb2eth_sn = SerialNumber;
            //GET_AUTO_MAC_ADD
            string macs_array_from_sfc = _ssg_sfc.GetSpecialValue("Scanner", "GET_AUTO_MAC_ADD_5", $"910400065200;{usb2eth_sn}");

            List<string> m_mac_array = macs_array_from_sfc.Split(';').ToList();

            if (m_mac_array.Count != 5)
            {
                value = "MAC_COUNT_ERROR";
                rlt = false;
                goto End;
            }

            for (int i = 0; i < m_mac_array.Count; i++) { USB2ETH_MAC_ARRAY[i] = m_mac_array[i]; }

            value = "PASS";
            rlt = true;
        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_USB2ETH_PrintMacArray(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            string[] keys = new string[5];
            keys[0] = "mac1";
            keys[1] = "mac2";
            keys[2] = "mac3";
            keys[3] = "mac4";
            keys[4] = "mac5";

            string lab_file_path = Path.Combine(Global.ASSEMBLY_PATH, "LabFile", "usb2eth_mac.Lab");

            if (!File.Exists(lab_file_path))
            {
                value = "NO_LAB_FILE";
                rlt = false;
                goto End;
            }

            rlt = PRINTER.PrintLabel(lab_file_path, keys, USB2ETH_MAC_ARRAY);

            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value;
            return rlt;
        }

        #endregion

        #region ---Printer Test---

        private bool Test_Printer_LTS(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string media_value = "", no_media_value = "";
            int test_feed_time = 0, test_media_type_time = 0;
            string media_type = "";

            string zh_cn = "", en_us = "", thai = "";

            // PA45 PN Special
            Prod.PA45 m_PA45 = product as Prod.PA45;

            Prod.PA45_MainBoard m_PA45_MB = product as Prod.PA45_MainBoard;
            if ((m_PA45 == null) && (m_PA45_MB == null))
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (m_PA45 != null)
            {
                if (m_PA45.MediaHandling != MediaHandling.Peeler)
                {
                    value = "NO_LTS_ON_PRINTER";
                    rlt = true;
                    goto End;
                }
            }

            if (m_PA45_MB != null)
            {

            }

            zh_cn = $"请把{Media_Type.Gaps.get_Media_Type()},安装在打印机上并且确认有正确安装穿过分纸器。";
            en_us = $"please install {Media_Type.Gaps.get_Media_Type()} into media holder and make it through pleer.";
            zh_cn = $"thai tips unknown.";

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);
            if (!bOpStep)
            {
                value = "OP_STRP_ERROR";
                rlt = false;
                goto End;
            }

        test_feed:
            bool bTestFeed = test_feed_1(Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg"));
            if (!bTestFeed)
            {
                test_feed_time = test_feed_time + 1;
                if (test_feed_time < 4) { goto test_feed; }
                value = "test_feed-1 fail";
                rlt = false;
                goto End;
            }

        test_get_media_type:
            media_type = GetMediaType();

            if (media_type != Media_Type.Gaps.get_Media_Type())
            {
                test_media_type_time = test_media_type_time + 1;
                if (test_media_type_time < 4)
                {
                    test_feed_time = 0;
                    goto test_feed;
                }
                value = "PRINTER_MEDIA_ERROR";
                rlt = false;
                goto End;
            }

            media_value = GetLTSSensorValue();

            bOpStep = false;
            Invoke(new Action(() =>
            {
                frmTips dialog = new frmTips(
                    $"读取纸底的LTS值。",
                    $"",
                    "thai tips (unknown)");
                DialogResult result = dialog.ShowDialog(this);

                bOpStep = result == DialogResult.OK;

            }));

            if (!bOpStep)
            {
                value = "OP_STRP_ERROR";
                rlt = false;
                goto End;
            }

            no_media_value = GetLTSSensorValue();

            value = $"{media_value} / {no_media_value}";

            rlt =
                !string.IsNullOrEmpty(media_value) &&
                !string.IsNullOrWhiteSpace(media_value) &&
                !string.IsNullOrEmpty(no_media_value) &&
                !string.IsNullOrWhiteSpace(no_media_value);

            if (media_value.Trim() == "0" || no_media_value.Trim() == "0")
            {
                rlt = false;
            }

        End:
            item.Value = value;

            /*
            if (item.Value == "NO_LTS_ON_PRINTER")
            {

            }
            else
            {
                rlt = item.IsValueInRange<double>();
            }
            */

            return rlt;
        }

        private bool Test_Printer_SELFSTRIP(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string media_type = "";
            int test_feed_time = 0, test_media_type_time = 0;

            // PA45 PN Special
            //bool isPA45 = product is Prod.PA45 m_PA45;
            Prod.PA45 m_PA45 = product as Prod.PA45;

            Prod.PA45_MainBoard m_PA45_MB = product as Prod.PA45_MainBoard;
            if ((m_PA45 == null) && (m_PA45_MB == null))
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (m_PA45 != null)
            {
                if (m_PA45.MediaHandling != MediaHandling.Peeler)
                {
                    value = "NO_SELFSTRIP_ON_PRINTER";
                    rlt = true;
                    goto End;
                }
            }

            if (m_PA45_MB != null)
            {

            }

            /*

            bool bOpStep = false;
            Invoke(new Action(() =>
            {
                frmTips dialog = new frmTips(
                    $"请将打印纸({media_type_gap})安装到打印机里面，并确认把Gap media穿过分纸机已经安装好。",
                    $"Please install the {media_type_gap} into printer.",
                    "thai tips (unknown)");
                DialogResult result = dialog.ShowDialog(this);

                bOpStep = result == DialogResult.OK;

            }));

            if (!bOpStep)
            {
                value = "OP_STRP_ERROR";
                rlt = false;
                goto End;
            }

        test_feed:
            string pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg");
            bool bTestFeed = false;
            if (File.Exists(pic_path))
            {
                bTestFeed = test_feed_1(pic_path);
            }
            else
            {
                bTestFeed = test_feed_1();
            }
            if (!bTestFeed)
            {
                test_feed_time = test_feed_time + 1;
                if (test_feed_time < 4)
                {
                    goto test_feed;
                }
                value = "test_feed-1 fail";
                rlt = false;
                goto End;
            }

        test_get_media_type:
            media_type = GetMediaType();

            if (media_type != media_type_gap)
            {
                test_media_type_time = test_media_type_time + 1;
                if (test_media_type_time < 4)
                {
                    test_feed_time = 0;
                    goto test_feed;
                }
                value = "PRINTER_MEDIA_ERROR";
                rlt = false;
                goto End;
            }


            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.SELFSTRIP);

            Delay(18000);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.SELFSTRIP);
                tmpValue = tmpValue.Replace(PrinterCommands.SELFSTRIP, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrEmpty(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains(ErrorCode.HW_NOT_INSTALL.ToString()))
            {
                value = ErrorCode.HW_NOT_INSTALL.ToString();
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains("PASS"))
            {
                rlt = true;
            }

            if (tmpValue.Contains("FAIL"))
            {
                rlt = false;
            }
            */

            rlt = true;
            value = "MARGINAL_LTS_VALUE";
        //value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_something(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            //------
            // how to imploment
            //


            item.Value = value;
            return rlt;
        }

        private bool Test_WritePrinterSn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            string printer_PN = "", printer_SN = "", region = "";

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "MODEL_ERROR";
                rlt = false;
                goto End;
            }

            region = m_PA45.RegionalCodeFlag.ToString();
            printer_PN = m_PA45.HON_PN;
            printer_SN = m_PA45.SerialNumber;

            string m_sfc_address = "";

            if (m_PA45.CommunicationOption == CommunicationOptions.USB_only)
            {
                SerialPortWriteLineOnce(PrinterCommands.set_modules_printer(printer_PN, printer_SN, region));
            }
            else if (m_PA45.CommunicationOption == CommunicationOptions.BT_only)
            {
                string bt_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114400_BT_board");
                AppendRuntimeLog($"{MainBarcode} get sfc bt module SN is {bt_module_sn}.");

                if (string.IsNullOrEmpty(bt_module_sn) || string.IsNullOrWhiteSpace(bt_module_sn))
                {
                    value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                    rlt = false;
                    goto End;
                }

                m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GET_AUTO_MAC_ADD", $"AMFIS16S00000056;{bt_module_sn}");

                SerialPortWriteLineOnce(PrinterCommands.set_modules_printer_bt(printer_PN, printer_SN, region, m_sfc_address.ToUpper()));
            }
            else
            {
                // wifi bt
                SerialPortWriteLineOnce(PrinterCommands.set_modules_printer_bt_wifi(printer_PN, printer_SN, region));
            }


            rlt = true;
            value = rlt ? "PASS" : "FAIL";

            if (m_PA45.CommunicationOption == CommunicationOptions.BT_only)
            {
                value = value + " / " + m_sfc_address;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_CheckPcba(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", temp_value = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.modules_cpu);

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "MODEL_ERROR";
                rlt = false;
                goto End;
            }

            string m_mech = m_PA45.HON_PN.Substring(0, 5);
            string m_sn = m_PA45.HON_PN + ";" + m_PA45.SerialNumber;

            

            for (int i = 0; i < 60; i++)
            {
                temp_value = GetRuntimeStringFlagA(PrinterCommands.modules_cpu);
                temp_value = temp_value.Replace(PrinterCommands.modules_cpu, "");
                if (!string.IsNullOrEmpty(temp_value) && !string.IsNullOrWhiteSpace(temp_value))
                {
                    value = temp_value;
                    break;
                }

                Delay(500);
            }

            DUTS.PA45.PA45 dut_PA45 = dut as DUTS.PA45.PA45;
            string pcba_part_num = "", pcba_seri_num = "", pcba_mech = "";

            dut_PA45.get_modules_cpu(temp_value, ref pcba_part_num, ref pcba_seri_num, ref pcba_mech);
            if (string.IsNullOrEmpty(pcba_part_num) ||
                string.IsNullOrEmpty(pcba_seri_num) ||
                string.IsNullOrEmpty(pcba_mech))
            {
                rlt = false;
                value = "PCBA_INFO_NULL";
                goto End;
            }

            if (string.IsNullOrWhiteSpace(pcba_part_num) ||
                string.IsNullOrWhiteSpace(pcba_seri_num) ||
                string.IsNullOrWhiteSpace(pcba_mech))
            {
                rlt = false;
                value = "PCBA_INFO_NULL";
                goto End;
            }

            if (pcba_mech != m_mech)
            {
                rlt = false;
                value = "PRINTER_MODEL_ERROR";
                goto End;
            }

            if (ckbSFCS.Checked)
            {
                string m_pcba_seri_num = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE", m_sn);
                string m_pcba_part_num = _ssg_sfc.GetSpecialValue("Scanner", "GET_SN_MODEL", MainBarcode);

                if (pcba_part_num != m_pcba_part_num)
                {
                    rlt = false;
                    value = "PCBA_PART_NUM_DISMATCH_WITH_SFC";
                    goto End;
                }

                if (pcba_seri_num != MainBarcode)
                {
                    rlt = false;
                    value = "PCBA_PART_NUM_DISMATCH_WITH_SFC";
                    goto End;
                }

                /*
                if (pcba_mech != m_pcba_mech)
                {
                    rlt = false;
                    value = "PCBA_MECH_DISMATCH";
                    goto End;
                }
                */
            }

            rlt = true;
            value = pcba_part_num + " / " + pcba_seri_num + " / " + pcba_mech;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_CheckPrinterSn(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", temp_value = "";
            
            Prod.PA45 prod_PA45 = product as Prod.PA45;

            if (prod_PA45 == null)
            {
                rlt = false;
                value = "PRODUCT_TPYE_ERROR";
                goto End;
            }

            DUTS.PA45.PA45 dut_PA45 = dut as DUTS.PA45.PA45;

            if (dut_PA45 == null)
            {
                value = "PRODUCT_TYPE_ERROR";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.modules_printer);

            string printer_part_num = "", printer_seri_num = "", printer_radio = "", printer_region = ""; ;

            for (int i = 0; i < 60; i++)
            {
                temp_value = GetRuntimeStringFlagA(PrinterCommands.modules_printer);
                temp_value = temp_value.Replace(PrinterCommands.modules_printer, "");
                if (!string.IsNullOrEmpty(temp_value) && !string.IsNullOrWhiteSpace(temp_value))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = true;
                goto End;
            }

            dut_PA45.get_modules_printer(temp_value, ref printer_part_num, ref printer_seri_num, ref printer_radio, ref printer_region);
            PrinterPartNumber = printer_part_num;
            PrinterSeriNumber = printer_seri_num;
           
            if (printer_part_num != prod_PA45.HON_PN)
            {
                rlt = false;
                value = "PRINTER_PN_ERROR";
                goto End;
            }

            if (printer_seri_num != prod_PA45.SerialNumber)
            {
                rlt = false;
                value = "PRINTER_SN_ERROR";
                goto End;
            }

            value = printer_part_num + " / " + printer_seri_num + " / " + printer_radio + " / " + printer_region;
            rlt = true;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MacAddressBurnIn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            string mech = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "MODEL_ERROR";
                rlt = false;
                goto End;
            }

            mech = m_PA45.HON_PN.Substring(0, 5);

            AppendRuntimeLog($"get printer model is {mech}.");
            string m_sn = m_PA45.HON_PN + ";" + m_PA45.SerialNumber;

            string pcba_SN = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE", m_sn);

            if (string.IsNullOrEmpty(pcba_SN) || string.IsNullOrWhiteSpace(pcba_SN))
            {
                value = "PCBA_SN_NULL_ERROR";
                rlt = false;
                goto End;
            }

            string pcba_PN = _ssg_sfc.GetSpecialValue("Scanner", "GET_SN_MODEL", pcba_SN);

            if (string.IsNullOrEmpty(pcba_PN) || string.IsNullOrWhiteSpace(pcba_PN))
            {
                value = "PCBA_PN_NULL_ERROR";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.set_modules_cpu(pcba_PN, pcba_SN, mech));

            rlt = true;
            value = "PASS";

        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_PingWifiRouter(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", temp_value = "";
            string ssid = "", dhcp_ip = "", router_ip = "", pwd = "";

            if (productConfig.STATION_CODE == "OOBA")
            {
                ssid = INI.GetValue("Wifi_Router", "OOBA_SSID", ConfigFile);
                pwd = INI.GetValue("Wifi_Router", "OOBA_PWD ", ConfigFile);
                router_ip = INI.GetValue("Wifi_Router", "OOBA_IP", ConfigFile);
            }
            else
            {
                ssid = INI.GetValue("Wifi_Router", "SSID", ConfigFile);
                pwd = INI.GetValue("Wifi_Router", "PWD", ConfigFile);
                router_ip = INI.GetValue("Wifi_Router", "IP", ConfigFile);
            }

            INI.GetValue("Wifi_Router", "IP", ConfigFile);

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "MODEL_ERROR";
                rlt = false;
                goto End;
            }

            if (m_PA45.CommunicationOption == CommunicationOptions.BT_only)
            {
                value = "BT_ONLY_SKU";
                rlt = true;
                goto End;
            }

            if (m_PA45.CommunicationOption == CommunicationOptions.USB_only)
            {
                value = "USB_ONLY_SKU";
                rlt = true;
                goto End;
            }

            string wifi_setting_script = "", wifi_setting_script_path = "";

            wifi_setting_script_path = Path.Combine(Global.ASSEMBLY_PATH, "wifi_script.txt");

            if (!File.Exists(wifi_setting_script_path))
            {
                AppendRuntimeLog($"can't find {wifi_setting_script_path}.");
                value = "NO_WIFI_SCRIPT";
                rlt = false;
                goto End;
            }

            using (StreamReader sw = new StreamReader(wifi_setting_script_path)) { wifi_setting_script = sw.ReadToEnd(); }

            if (!wifi_setting_script.Contains("NOMAD_WIFI_ROUTER_SSID") || !wifi_setting_script.Contains("NOMAD_WIFI_ROUTER_KEY"))
            {
                value = "BAD_WIFI_SCRIPT";
                rlt = false;
                goto End;
            }

            wifi_setting_script = wifi_setting_script.Replace("NOMAD_WIFI_ROUTER_SSID", ssid);
            wifi_setting_script = wifi_setting_script.Replace("NOMAD_WIFI_ROUTER_KEY", pwd);

            SerialPortWriteLineOnce(wifi_setting_script);

            //WIFI IPADDRESS : 192.168.1.100
            Delay(3000);

            SerialPortWriteLineOnce(PrinterCommands.get_ip_addr_wifi);

            for (int i = 0; i < 60; i++)
            {
                temp_value = GetRuntimeStringFlagA(PrinterCommands.get_ip_addr_wifi);
                temp_value = temp_value.Replace(PrinterCommands.get_ip_addr_wifi, "");
                if (!string.IsNullOrEmpty(temp_value) && !string.IsNullOrWhiteSpace(temp_value))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dhcp_ip = GetDHCP_IP_Address(temp_value);

            if (dhcp_ip == "0.0.0.0")
            {
                value = "PRINTER_NO_IP";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.ping_ip_adderss(router_ip));

            Delay(80000);
            temp_value = "";
            bTimeOut = true;
            for (int i = 0; i < 60; i++)
            {
                temp_value = GetRuntimeStringFlagA(PrinterCommands.ping_ip_adderss(router_ip));
                temp_value = temp_value.Replace(PrinterCommands.ping_ip_adderss(router_ip), "");
                if (temp_value.Contains("5 packets transmitted"))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            //5 packets transmitted, 0 packets received, 100% packet loss
            if (temp_value.Contains("ttl")) { rlt = true; }

            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_FindBluetoothDevice(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            AppendRuntimeLog("checking the local bluetooth adapter...");

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }
            Delay(3000);
            //8CCDFE07BEE9
            string tmp_mac = BLUETOOTH_MAC_ADDRESS.Replace(":", "").Trim();

            if (string.IsNullOrEmpty(BLUETOOTH_MAC_ADDRESS) || string.IsNullOrWhiteSpace(BLUETOOTH_MAC_ADDRESS))
            {
                AppendRuntimeLog("error: Get bluetooth mac is null, please test Bluetooth ID first!!!");
                MessageBox.Show(
                    $"Get bluetooth mac is null, please test Bluetooth ID first!!!",
                    Global.AssemblyName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                value = "PRINTER_MAC_NULL";

                rlt = false;
                goto End;
            }

            // 1. 检查本地是否有蓝牙适配器
            var radios = BluetoothRadio.AllRadios;
            if (radios.Length == 0)
            {
                AppendRuntimeLog("error: not found any local bluetooth adapter.");
                value = "NOT_FOUNF_BT_ADAPTER";
                rlt = false;
                goto End;
            }

            var primaryRadio = BluetoothRadio.PrimaryRadio;
            if (primaryRadio == null)
            {
                AppendRuntimeLog("error: unable to retrieve the main bluetooth adapter.");
                value = "NOT_FOUNF_BT_ADAPTER";
                rlt = false;
                goto End;
            }

            // 2. 确保蓝牙已开启 (RadioMode.Connectable 表示可连接/可见)
            if (primaryRadio.Mode != RadioMode.Connectable)
            {
                AppendRuntimeLog("set bluetooth enable.");
                primaryRadio.Mode = RadioMode.Connectable;
            }

            AppendRuntimeLog("start to research surounding BT device.");

            // 3. 使用 Task.Run 在后台线程执行扫描，避免阻塞主线程
            BluetoothDeviceInfo[] devices;
            using (var client = new BluetoothClient())
            {
                // DiscoverDevices 参数说明：
                // maxDevices: 最大返回设备数量 (255)
                // authenticated: true 表示只返回已配对的设备
                // remembered: true 表示返回已记忆的设备
                // unknown: true 表示返回新发现的未知设备
                devices = client.DiscoverDevices(255, false, true, true);
            }

            // 4. 输出扫描结果
            AppendRuntimeLog($"scanning finish, get {devices.Length} device.");
            bool bFindDevice = false, bIsMacMatch = false;
            foreach (var device in devices)
            {
                AppendRuntimeLog($"device: {device.DeviceName}");
                AppendRuntimeLog($"MAC address: {device.DeviceAddress}");
                AppendRuntimeLog($"is paired: {(device.Authenticated ? "yes" : "no")}");
                AppendRuntimeLog($"is remembered: {(device.Remembered ? "yes" : "no")}");
                AppendRuntimeLog("-----------------------------------");

                if (device.DeviceName == "PA45-" + m_PA45.SerialNumber)
                {
                    bFindDevice = true;
                    AppendRuntimeLog($"get dut device: {device.DeviceName}");
                    if (device.DeviceAddress.ToString() == tmp_mac)
                    {
                        AppendRuntimeLog($"dut device mac match bt found device mac.");
                        bIsMacMatch = true;
                    }
                }
            }

            if (!bFindDevice)
            {
                value = "NOT_FOUND_BT_DUT";
                rlt = false;
                goto End;
            }

            if (!bIsMacMatch)
            {
                value = "MAC_NOT_MATCH";
                rlt = false;
                goto End;
            }

            value = "PASS";
            rlt = true;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_WifiID(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (m_PA45.CommunicationOption == CommunicationOptions.BT_only || m_PA45.CommunicationOption == CommunicationOptions.USB_only)
            {
                value = "this_sku_no_wifi";
                rlt = true;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.WIFI_ID);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.WIFI_ID);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains("HW_NOT_INSTALLED"))
            {
                value = "HW_NOT_INSTALLED";
                rlt = false;
                goto End;
            }

            string part_number = "", seri_number = "", address = "";
            string m_sfc_address = "";
            GetWifiId(tmpValue, ref part_number, ref seri_number, ref address);

            WIFI_MAC_ADDRESS = address;
            if (ckbSFCS.Checked)
            {
                string bt_wifi_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114390_BT_Wifi_board");
                AppendRuntimeLog($"{MainBarcode} get sfc wifi module SN is {bt_wifi_module_sn}.");

                if (string.IsNullOrEmpty(bt_wifi_module_sn) || string.IsNullOrWhiteSpace(bt_wifi_module_sn))
                {
                    value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                    rlt = false;
                    goto End;
                }

                string station = "BT_Wifi_board_Function_Test";
                string target_values = "WiFi_BT_Module Read(Wifi)";
                string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
                m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);

                m_sfc_address = m_sfc_address.ToUpper();
                AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");
            }

            string input_wifi_mac = Interaction.InputBox("please scan the wifi label on printer");

            if (ckbSFCS.Checked)
            {
                rlt = (input_wifi_mac.ToUpper() == address.ToUpper()) && (input_wifi_mac.ToUpper() == m_sfc_address.ToUpper());
                if (rlt)
                {
                    value = address.ToUpper();
                }
                else
                {
                    value = $"{address.ToUpper()} / {input_wifi_mac.ToUpper()} / {m_sfc_address.ToUpper()}";
                }
            }
            else
            {
                rlt = input_wifi_mac.ToUpper() == address.ToUpper();

                if (rlt)
                {
                    value = address.ToUpper();
                }
                else
                {
                    value = $"{address.ToUpper()} / {input_wifi_mac.ToUpper()}";
                }
            }

        End:
            item.Value = value;
            return rlt;
        }

        private string GetMacFromWlan0(string temp_value)
        {
            string value = "";
            string[] lines = temp_value.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string target = "wlan0Linkencap:EthernetHWaddr";
                //wlan0     Link encap:Ethernet  HWaddr 40:D9:5A:FF:B8:EE  
                string line = lines[i].Replace(" ", "");
                if (line.Contains(target))
                {
                    value = line.Replace(target, "");
                    break;
                }
            }

            return value;
        }

        private bool Test_PrintWifiAddress(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!File.Exists(WifiMacLabFile))
            {
                value = "the lab file doesn't exist!!!";
                goto End;
            }
            string key = INI.GetValue("WifiLabelPrint", "Key", ConfigFile);
            Invoke(new Action(() => { PRINTER.PrintLabel(WifiMacLabFile, key, WIFI_MAC_ADDRESS); }));
            value = "PASS";
            rlt = true;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_PrintBluetoothAddress(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!File.Exists(BluetoothMacLabFile))
            {
                value = "the lab file doesn't exist!!!";
                goto End;
            }

            string key = INI.GetValue("BluetoothLabelPrint", "Key", ConfigFile);
            PRINTER.PrintLabel(BluetoothMacLabFile, key, BLUETOOTH_MAC_ADDRESS);
            value = "PASS";
            rlt = true;

        End:
            item.Value = value;
            return rlt;
        }


        /*
        private bool Test_BlueTooth_check(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
        }
        */


        private bool Test_BlueTooth_set(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            string m_sn = m_PA45.SerialNumber;
            m_sn = "PA45-" + m_sn;

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.SetBtName(m_sn));

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.SetBtName(m_sn));
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))

                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains("PASS"))
            {
                rlt = true;
            }

            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_Battery(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = string.Empty, tmpValue = string.Empty;

            DUTS.PA45.PA45 dut_PA45 = dut as DUTS.PA45.PA45;

            if (dut_PA45 == null)
            {
                value = "PRODUCT_TYPE_ERROR";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.BATTERY_TEST);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.BATTERY_TEST);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains($"{Printer_Error_Code.HW_NOT_INSTALLED}"))
            {
                rlt = false;
                value = tmpValue.Trim();
                goto End;
            }

            string status = "", external_power = "", remaining = "", health = "", current = "";

            dut_PA45.get_BATTERY_TEST(tmpValue, ref status, ref external_power, ref remaining, ref health, ref current);

            value = status + " / " + external_power + " / " + remaining + " / " + health + " / " + current;

            if ((!string.IsNullOrEmpty(status)) &&
                (!string.IsNullOrEmpty(external_power)) &&
                (!string.IsNullOrEmpty(remaining)) &&
                (!string.IsNullOrEmpty(health)) &&
                (!string.IsNullOrEmpty(current)))
            {
                rlt = true;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_PaperFeedGetMediaLength(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = string.Empty, tmpValue = string.Empty, labelLen = string.Empty;
            int test_feed_time = 0, test_media_type_time = 0;
            string media_type = "";

            string zh_cn = $"请把{Media_Type.Gaps.get_Media_Type()}放进载纸装置中";
            string en_us = $"please put {Media_Type.Gaps.get_Media_Type()} into media holder.";
            string thai = $"please put {Media_Type.Gaps.get_Media_Type()} into media holder.";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_STRP_ERROR";
                rlt = false;
                goto End;
            }

        test_feed:
            string pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg");
            bool bTestFeed = test_feed_1(pic_path);


            if (!bTestFeed)
            {
                test_feed_time = test_feed_time + 1;
                if (test_feed_time < 4)
                {
                    goto test_feed;
                }
                value = "test_feed-1 fail";
                rlt = false;
                goto End;
            }

        test_get_media_type:
            media_type = GetMediaType();

            if (media_type != Media_Type.Gaps.get_Media_Type())
            {
                test_media_type_time = test_media_type_time + 1;
                if (test_media_type_time < 4)
                {
                    test_feed_time = 0;
                    goto test_feed;
                }
                value = "PRINTER_MEDIA_ERROR";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.FORMFEED);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.FORMFEED);
                tmpValue = tmpValue.Replace(PrinterCommands.FORMFEED, "");

                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            //Label Length: 1259
            //PASS
            string label_len = "";
            if (tmpValue.Contains("!PASS"))
            {
                value = "FORM_FEED_FAIL";
                rlt = false;
                goto End;
            }

            List<string> values = tmpValue.Split('\n').ToList();

            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (String var in values)
            {
                if (var.Contains("Label Length:"))
                {
                    label_len = var.Replace("Label Length:", "");
                    break;
                }
            }

            if (string.IsNullOrEmpty(label_len) || string.IsNullOrWhiteSpace(label_len))
            {
                value = "LABEL_LEN_ERROR";
                rlt = false;
                goto End;
            }

            tmpValue = "";
            bTimeOut = true;
            SerialPortWriteLineOnce(PrinterCommands.GET_MEDIA_LENGTH);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.GET_MEDIA_LENGTH);
                tmpValue = tmpValue.Replace(PrinterCommands.GET_MEDIA_LENGTH, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            List<string> values1 = tmpValue.Split('\n').ToList();

            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (String var in values1)
            {
                string mVar = var.Trim();
                if (Information.IsNumeric(mVar))
                {
                    value = mVar;
                    break;
                }
            }


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Printer_Barcode(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            int printing_retry_times = Convert.ToInt32(INI.GetValue("PrintSetting", "RetryTimes", ConfigFile));
            int current_time = 1, test_feed_time = 0, test_media_type_time = 0;
            string media_type = "", target_media_type = "";
            string zh_cn = "", en_us = "", thai = "";

            Prod.PA45 prod_PA45 = product as Prod.PA45;

            if (prod_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (prod_PA45.MediaHandling == MediaHandling.LinerlessPlatenRoller)
            {
                target_media_type = Media_Type.ContinuousVarLen.get_Media_Type();
            }
            else
            {
                target_media_type = Media_Type.Gaps.get_Media_Type();
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            string currentTime = "";
            AppendRuntimeLog($"SFC checked is {ckbSFCS.Checked}");
            if (ckbSFCS.Checked)
            {
                AppendRuntimeLog($"get current time from SFC...");
                currentTime = SSG_SFC.GetDbNow("AB3");
                AppendRuntimeLog($"get current time from SFC is {currentTime}.");
                if (string.IsNullOrEmpty(currentTime) || string.IsNullOrWhiteSpace(currentTime))
                {
                    value = "CANNOT_GET_SFC_TIME";
                    rlt = false;
                    goto End;
                }
            }
            else
            {
                AppendRuntimeLog($"get current time from local PC...");
                currentTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                AppendRuntimeLog($"get current time from local PC is {currentTime}.");
            }

            if (string.IsNullOrEmpty(PrinterSeriNumber) ||
                string.IsNullOrWhiteSpace(PrinterSeriNumber) ||
                string.IsNullOrEmpty(PrinterPartNumber) ||
                string.IsNullOrWhiteSpace(PrinterPartNumber))
            {
                AppendRuntimeLog($"get printer info...");

                SerialPortWriteLineOnce(PrinterCommands.modules_printer);
                
                for (int i = 0; i < 200; i++)
                {
                    tmpValue = GetRuntimeStringFlagA("get printer info...");
                    tmpValue = tmpValue.Replace(PrinterCommands.modules_printer, "");
                    if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                    {
                        bTimeOut = false;
                        break;
                    }
                    Delay(500);
                }

                if (bTimeOut)
                {
                    value = "TimeOut";
                    rlt = true;
                    goto End;
                }

                DUTS.PA45.PA45 dut_PA45 = dut as DUTS.PA45.PA45;
                string printer_part_num = "", printer_seri_num = "", printer_radio = "", printer_region = "";
                dut_PA45.get_modules_printer(tmpValue, ref printer_part_num, ref printer_seri_num, ref printer_radio, ref printer_region);
            }

            zh_cn = $"请将打印纸({target_media_type})安装到打印机里面，并确认已经安装好。";
            en_us = $"Please install the {target_media_type} into printer.";
            thai = "thai tips (unknown)";

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_STRP_ERROR";
                rlt = false;
                goto End;
            }

        test_feed:
            bool bTestFeed = test_feed_1(Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg"));
            if (!bTestFeed)
            {
                test_feed_time = test_feed_time + 1;
                if (test_feed_time < 4)
                {
                    goto test_feed;
                }
                value = "test_feed-1 fail";
                rlt = false;
                goto End;
            }

        test_get_media_type:
            media_type = GetMediaType();

            if (media_type != target_media_type)
            {
                test_media_type_time = test_media_type_time + 1;
                if (test_media_type_time < 4)
                {
                    test_feed_time = 0;
                    goto test_feed;
                }
                value = "PRINTER_MEDIA_ERROR";
                rlt = false;
                goto End;
            }

            string barcode_script_path = Path.Combine(Global.ASSEMBLY_PATH, "Script", "Nomad_PQ_Barcode.txt");
            string barcode_script;
            using (StreamReader reader = new StreamReader(barcode_script_path)) { barcode_script = reader.ReadToEnd(); }

            barcode_script = barcode_script.Replace("m_printer_pn", PrinterPartNumber);
            barcode_script = barcode_script.Replace("m_printer_sn", PrinterSeriNumber);
            barcode_script = barcode_script.Replace("m_print_time", SSG_SFC.GetDbNow("AB3"));

            Delay(1000);

            Invoke(new Action(() =>
            {
            PRINT:
                SerialPortWriteLineOnce(barcode_script);

                Delay(Convert.ToInt32(INI.GetValue("Delay", "BarcodePrinting", ConfigFile)) * 1000);

                frmTips dialog = new frmTips(
                    "这是第 " + current_time.ToString() + " 打印," + "请确认Barcode Pattern打印品质",
                    "This is the " + current_time.ToString() + " loop" + ",please check printing quality of Chess pattern.",
                    "thai tips (unknown)");
                DialogResult result = dialog.ShowDialog(this);

                if (result == DialogResult.Yes)
                {
                    value = "PASS" + " " + current_time.ToString();
                    rlt = true;
                }
                else
                {
                    if (current_time < printing_retry_times)
                    {
                        current_time++;
                        goto PRINT;
                    }
                    rlt = false;
                }
            }));

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_ChessPattern(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            int printing_retry_times = Convert.ToInt32(INI.GetValue("PrintSetting", "RetryTimes", ConfigFile));
            int current_time = 1, test_feed_time = 0, test_media_type_time = 0;

            string m_cmd = "", pq_tpye = "", media_tpye = "";
            string zh_cn = $"请将打印纸{media_tpye}安装到打印机里面，并确认已经安装好。";
            string en_us = "Please install the media into printer.";
            string thai = "thai tips (unknown)";

            if (item.Name.Contains("Chess"))
            {
                m_cmd = PrinterCommands.testlabel_cmd(testlabel_list.pq_chess);
                pq_tpye = "pq_chess";
                media_tpye = "Continuous Var Len";
            }
            else
            {
                m_cmd = PrinterCommands.testlabel_cmd(testlabel_list.pq_ruler);
                pq_tpye = "pq_ruler";
                media_tpye = "Continuous Var Len";
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_PUT_MEDIA_ERROR";
                rlt = false;
                goto End;
            }

        test_feed:
            bool bTestFeed = test_feed_1(Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg"));

            if (!bTestFeed)
            {
                test_feed_time = test_feed_time + 1;
                if (test_feed_time < 4)
                {
                    goto test_feed;
                }
                value = "test_feed-1 fail";
                rlt = false;
                goto End;
            }

        test_get_media_type:
            string media_type = GetMediaType();

            if (media_type != Media_Type.ContinuousVarLen.get_Media_Type())
            {
                test_media_type_time = test_media_type_time + 1;
                if (test_media_type_time < 4)
                {
                    test_feed_time = 0;
                    goto test_feed;
                }
                value = "PRINTER_MEDIA_ERROR";
                rlt = false;
                goto End;
            }

            Invoke(new Action(() =>
            {
            PRINT:

                SerialPortWriteLineOnce(m_cmd);
                Delay(3000);

                frmTips dialog = new frmTips(
                    $"这是第 { current_time.ToString() } 打印, 请确认{pq_tpye}打印品质",
                    "please check printing quality of Chess pattern.",
                    "thai tips (unknown)");
                DialogResult result = dialog.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    value = "PASS" + " " + current_time.ToString();
                    rlt = true;
                }
                else
                {
                    if (current_time < printing_retry_times)
                    {
                        current_time++;
                        goto PRINT;
                    }
                    rlt = false;
                }
            }));

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_HostUsb(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", temp_value = "", pic_path = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            string zhTip = "", enTip = "", thaiTip = "";

            zhTip = "请将type-C U盘插入第右边的type-C接口中。";
            enTip = "please plug type-c USB pen in the right interface.";
            thaiTip = "thai tips (unknown)";

            pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "printer_usb.png");

            bool b_OpStep = GetOperatorSelection(zhTip, enTip, thaiTip, pic_path);

            if (!b_OpStep)
            {
                value = "OP_PLUG_IN_FAIL";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.USB_HOST);
            Delay(2000);

            for (int i = 0; i < 200; i++)
            {
                temp_value = GetRuntimeStringFlagA(PrinterCommands.USB_HOST);
                temp_value = temp_value.Replace(PrinterCommands.USB_HOST, "");
                if (!string.IsNullOrEmpty(temp_value) && !string.IsNullOrWhiteSpace(temp_value))
                {
                    bTimeOut = false;
                    break;
                }

                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            temp_value = temp_value.Replace("PASS", "").Trim();
            temp_value = temp_value.Replace("Ok", "").Trim();
            //File 1: test.txt
            rlt = temp_value == "File 1: test.txt";

            value = temp_value;

            zhTip = "请将type-C U盘从打印机上移走。";
            enTip = "please remove type-c USB pen from printer.";
            thaiTip = "thai tips (unknown)";
            pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "cover_close_no_media.jpg");
            GetOperatorSelection(zhTip, enTip, thaiTip, pic_path);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_SpeakerBuzzer(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.AUDIO);

            bool b_manual_check = false;
            bool self_check = false;
            Invoke(new Action(() =>
            {
                frmTips dialog = new frmTips("请确认蜂鸣器是否播放音乐", "please check the buzzer have played a audio.", "thai tips (unknown)");
                DialogResult result = dialog.ShowDialog(this);

                b_manual_check = (result == DialogResult.OK) ? true : false;

                for (int i = 0; i < 120; i++)
                {
                    tmpValue = REGEX.GetString(rtbLog.Text, PrinterCommands.AUDIO, false);
                    tmpValue = tmpValue.Replace(PrinterCommands.AUDIO, "");
                    if (!string.IsNullOrWhiteSpace(tmpValue) && !string.IsNullOrEmpty(tmpValue))
                    {
                        bTimeOut = false;
                        break;
                    }
                    Delay(500);
                }
            }));

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (!b_manual_check)
            {
                value = "CANNOT_HEAR_AUDIO";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains(Test_Proc_Error_Code.HW_NOT_INSTALL.ToString()))
            {
                value = $"{Test_Proc_Error_Code.HW_NOT_INSTALL}";
                rlt = false;
                goto End;
            }

            tmpValue = tmpValue.Replace("Ok", "");
            tmpValue = tmpValue.Trim();

            if ((value = tmpValue.Trim()) == "PASS")
            {
                self_check = true;
            }

            rlt = b_manual_check && self_check;

        End:
            item.Value = value;
            return rlt;
        }

        private event KeyButtonTestedEventHanlder KeyButtonTested;

        private bool Test_Printer_KeyButton(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.KEYPAD);

            bool b_time_out = true;
            Invoke(new Action(() =>
            {
                frmKeybutton dialog = new frmKeybutton();
                KeyButtonTested += dialog.OnKeyButtonTested;
                dialog.Show();

                for (int i = 0; i < 120; i++)
                {
                    string temp_value = REGEX.GetString(rtbLog.Text, PrinterCommands.KEYPAD, false);

                    if (productConfig.STATION_CODE == "FCT")
                    {
                        RelayCard2.SetRelayOff(2);
                        Delay(500);
                        RelayCard2.SetRelayOn(2);
                        Delay(500);

                        RelayCard2.SetRelayOff(3);
                        Delay(500);
                        RelayCard2.SetRelayOn(3);
                        Delay(500);

                        RelayCard2.SetRelayOff(4);
                        Delay(500);
                        RelayCard2.SetRelayOn(4);
                        Delay(500);

                        RelayCard2.SetRelayOff(5);
                        Delay(500);
                        RelayCard2.SetRelayOn(5);
                        Delay(500);

                        RelayCard2.SetRelayOff(6);
                        Delay(500);
                        RelayCard2.SetRelayOn(6);
                        Delay(500);
                    }

                    if (!string.IsNullOrEmpty(temp_value))
                    {
                        string[] keyCodes = temp_value.Split('\n');
                        List<string> key_codes = new List<string>();

                        foreach (string code in keyCodes)
                        {
                            if (!string.IsNullOrEmpty(code) && code.Trim().Length == 12)
                                key_codes.Add(code.Replace("Key code:", "").Trim());
                        }

                        KeyButtonTested?.Invoke(this, new KeyButtonTestedEventArgs(key_codes));
                    }

                    if (dialog.GetKeyButtonTestResult())
                    {
                        List<string> temp = dialog.clickedButtons.ToList();

                        for (int j = 0; j < temp.Count; j++)
                        {
                            if (j == 0) value = value + temp[j];
                            else value = value + " / " + temp[j];
                        }

                        Delay(1000);
                        //value = dialog.clickedButtons.ToString();
                        KeyButtonTested -= dialog.OnKeyButtonTested;
                        dialog.Close();
                        dialog.Dispose();
                        b_time_out = false;
                        rlt = true;
                        break;
                    }

                    Delay(500);
                }

                if (b_time_out)
                {
                    List<string> temp = dialog.clickedButtons.ToList();

                    for (int j = 0; j < temp.Count; j++)
                    {
                        if (j == 0) value = value + temp[j];
                        else value = value + " / " + temp[j];
                    }

                    KeyButtonTested -= dialog.OnKeyButtonTested;
                    dialog.Close();
                    dialog.Dispose();
                    rlt = false;
                }
            }));

            SerialPortWriteLineOnce(PrinterCommands.Quit);
            Delay(5000);

            item.Value = value;
            return rlt;
        }


        private bool Test_LssSensor(int index, TestItem item)
        {
            bool rlt = false, bTimeout = true;
            string value = "", tmpValue = "";
            string zh_cn = "", en_us = "", thai = "", pic_path = "";
            bool isDelta = false;
            string lss_test_tpye = "", media_type = "";

            if (item.Name.Contains("No Media"))
            {
                zh_cn = "移除LSS Sensor上面的所有媒介.";
                en_us = "remove all media on the LSS sensor.";
                thai = "null";
                lss_test_tpye = "NO_MEDIA";
            }
            else if (item.Name.Contains("Liner"))
            {
                zh_cn = "请使用打印纸纸低遮挡LSS Sensor.";
                en_us = "please put liner on LSS sensor.";
                thai = "null";
                lss_test_tpye = "LINER";
            }
            else if (item.Name.Contains("Delta"))
            {
                //zh_cn = "Media";
                isDelta = true;
                lss_test_tpye = "DETLA";
            }
            else
            {
                zh_cn = "请把GAP MEDIA 放在打印机.";
                en_us = "please install GAP media into printer.";
                thai = "null";
                lss_test_tpye = "MEDIA";
            }

            if (lss_test_tpye == "MEDIA")
            {
                int retryTimes = 0, retry_test_media_type_times = 0;
            retry_test_feed:
                pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "printer_gap_media.png");

                bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai, pic_path);
                if (!bOpStep)
                {
                    value = "OP_PUT_GAP_MEDIA_FAIL";
                    rlt = false;
                    goto End;
                }

                bool bTestfeed = test_feed_1(Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg"));

                if (!bTestfeed)
                {
                    retryTimes = retryTimes + 1;

                    if (retryTimes < 4)
                    {
                        goto retry_test_feed;
                    }

                    value = "OP_TEST_FEED_FAIL";
                    rlt = false;
                    goto End;
                }

            test_media_type:

                media_type = GetMediaType();

                AppendRuntimeLog($"get current media type is {media_type}.");

                if (media_type != Media_Type.Gaps.get_Media_Type())
                {
                    retry_test_media_type_times = retry_test_media_type_times + 1;
                    if (retry_test_media_type_times < 4)
                    {
                        retryTimes = 0;
                        goto retry_test_feed;
                    }

                    value = "MEDIA_TYPE_ERROR";
                    rlt = false;
                    goto End;
                }
            }

            if (lss_test_tpye == "LINER")
            {
                int retryTimes = 1;

                bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai, pic_path);

                if (!bOpStep)
                {
                    value = "OP_PUT_LINER_FAIL";
                    rlt = false;
                    goto End;
                }
            }

            if (lss_test_tpye == "NO_MEDIA")
            {
                bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai, pic_path);

                if (!bOpStep)
                {
                    value = "OP_PUT_REMOVER_MEDIA_FAIL";
                    rlt = false;
                    goto End;
                }
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.LSS);

            bTimeout = true;
            tmpValue = "";
            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(item.Name);
                tmpValue = tmpValue.Replace(PrinterCommands.LSS, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeout = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeout)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            value = GetSensorValue(tmpValue);

            if (item.Name.Contains("No Media"))
            {
                LSS_SENSOR_VALUE_NO_MEDIA = Convert.ToDouble(value);
            }
            else if (item.Name.Contains("Liner"))
            {
                LSS_SENSOR_VALUE_LINER = Convert.ToDouble(value);
            }
            else
            {
                LSS_SENSOR_VALUE_MEDIA = Convert.ToDouble(value);
            }

            if (isDelta)
            {
                value = Convert.ToString(LSS_SENSOR_VALUE_LINER - LSS_SENSOR_VALUE_MEDIA);
            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }



        private bool Test_Printer_BMSensor(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "", pic_path = "";
            string m_cmd = "", bm_sensor_position = "", media_type = "";
            int test_feed_times = 0, calibrate_media_times = 0;
            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            if (item.Name.Contains("Front") || item.Name.Contains("Top"))
            {
                m_cmd = PrinterCommands.FRONT_BM;
                bm_sensor_position = "F";
            }
            else
            {
                m_cmd = PrinterCommands.BM;
                bm_sensor_position = "B";
            }

            if (productConfig.STATION_CODE.Contains("FCT"))
            {
                AppendRuntimeLog("detected is FCT station, board level test, no need to madie test.");
                goto SendCom;
            }

            if (item.Name.ToUpper().Contains("MB EOL"))
            {
                AppendRuntimeLog("detected is MB EOL station, board level test, no need to madie test.");
                goto SendCom;
            }

            bool bOpStep = false;

            Invoke(new Action(() =>
            {
                string m_tips = "";

                if (bm_sensor_position == "F")
                {
                    m_tips = "请放Front BM测试纸(黑边朝外)测试,进行testfeed -1测试。";
                    pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "printer_black_mark_outside.jpg.");
                }
                else
                {
                    m_tips = "请放Bottem BM测试纸(黑边朝内)测试,进行testfeed -1测试。";
                    pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "printer_black_mark_inside.jpg.");
                }

                frmTips dialog;
                if (File.Exists(pic_path))
                {
                    dialog = new frmTips(pic_path,
                        m_tips,
                        "please check testfeed -1 have finished yet.",
                        "thai tips (unknown)");
                }
                else
                {
                    dialog = new frmTips(m_tips,
                        "please check testfeed -1 have finished yet.",
                        "thai tips (unknown)");
                }

                DialogResult result = dialog.ShowDialog(this);

                bOpStep = result == DialogResult.OK;
            }));

            if (!bOpStep)
            {
                value = "OP_PUT_MEDIA_ERROR";
                rlt = false;
                goto End;
            }

        TestFeed:

            if (bm_sensor_position == "F")
            {
                pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "test_feed_bm_outside.png");
            }
            else
            {
                pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "test_feed_bm_inside.png");
            }

            bool bTestFeed = test_feed_1(pic_path);

            if (!bTestFeed)
            {
                test_feed_times = test_feed_times + 1;

                if (test_feed_times < 4)
                {
                    goto TestFeed;
                }
                value = "testfeed -1 fail";
                rlt = false;
                goto End;
            }

        test_media_type:
            media_type = GetMediaType();

            if (media_type != Media_Type.BlackMark.get_Media_Type())
            {
                calibrate_media_times = calibrate_media_times + 1;

                if (calibrate_media_times < 4)
                {
                    test_feed_times = 0;
                    goto TestFeed;
                }
                value = "MEDIA_TYPE_ERROR";
                rlt = false;
                goto End;
            }

        SendCom:
            SerialPortWriteLineOnce(m_cmd);
            tmpValue = "";
            bTimeOut = true;
            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(m_cmd);

                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            value = GetSensorValue(tmpValue);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Printer_SensorTemp(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.get_printer_sensor_status);
            /*
            if (SensorKeyValueDict == null)
            {
                SensorKeyValueDict = new Dictionary<string, string>();
            }

            if (SensorKeyValueDict.Count == 0)
            {
                SerialPortWriteLineOnce(PrinterCommands.get_printer_sensor_status);

                for (int i = 0; i < 200; i++)
                {

                }
            }
            */

            /*
             *  sensorSelected=0
             *  Rear LSSGain=7
        * Rear LSSDrive=255
        * Front LSSGain=7
        * Front LSSDrive=255
        * Rear LSSValue=795
        * Front LSSValue=795
        * LTSValue=-1
        * LTSThr=40
        * TPHRes=0
        * TPH AD=818 Temp=27
        * MPCB AD=310 Temp=34
        * MOTOR AD=327 Temp=34
        * SCAP VOLT AD=793,SCAP_Voltage(mv)=2555
        * BATTERY AD=425,Battery_Voltage(mv)=8217
        * ACCESSORY AD=1002,ACCESSORY_Voltage(mv)=3251
        */

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(item.Name);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }

                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            string flag1 = "", flag2 = "";
            if (item.Name.ToLower().Contains("motor"))
            {
                flag1 = "MOTORAD=";
                flag2 = "Temp=";
            }
            else if (item.Name.ToLower().Contains("ambient"))
            {
                flag1 = "MPCBAD=";
                flag2 = "Temp=";

            }
            else if (item.Name.ToUpper().Contains("TPH"))
            {
                flag1 = "TPHAD=";
                flag2 = "Temp=";
            }

            string ADC_value = "", temp_value = "";

            GetSensorValues(tmpValue, flag1, flag2, ref ADC_value, ref temp_value);
            value = temp_value;
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_TphResolution(int index, TestItem item)
        {

            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.PRINTHEAD);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.PRINTHEAD);

                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "Time out";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains("FAIL") || tmpValue.Contains("Fail"))
            {
                value = "FAIL";
                rlt = true;
                goto End;
            }

            value = GetValueAfterFlag(tmpValue, "Print Head DPI:");

            rlt = (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue)) ? true : false;
        End:
            item.Value = value;
            return rlt;
        }



        private bool Test_Printer_TPHOhm(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";



        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_GetTphId(int index, TestItem item)
        {
            bool rlt = false, bFlag = false;
            string value = "", tmpValue = "";

            DUTS.PA45.PA45 dut_PA45 = dut as DUTS.PA45.PA45;

            if (dut_PA45 == null)
            {
                value = "PRODUCT_TYPE_ERROR";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.TPH_ID);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.TPH_ID);
                tmpValue = tmpValue.Replace(PrinterCommands.TPH_ID, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            tmpValue = tmpValue.Trim();

            if (!bFlag)
            {
                value = "Time out";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains($"{Printer_Error_Code.HW_NOT_INSTALLED}"))
            {
                value = $"{Printer_Error_Code.HW_NOT_INSTALLED}";
                rlt = false;
                goto End;
            }

            string part = "", seri = "";

            dut_PA45.get_TPH_ID(tmpValue, ref part, ref seri);

            value = $"{part} / {seri}";
            rlt = string.IsNullOrEmpty(part) || string.IsNullOrWhiteSpace(part) || string.IsNullOrEmpty(seri) || string.IsNullOrWhiteSpace(seri);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_SetRtc(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            int time_resource = Convert.ToInt32(INI.GetValue("RTC", "Type", ConfigFile));

            string currentTime = "";
            string msg = "";

            if (productConfig.STATION_CODE.Contains("FCT"))
            {
                currentTime = DateTime.Now.ToString("HH:mm:ss MM/dd/yyyy");
                currentTime = $"{DateTime.Now.Hour.ToString("D2")}:{DateTime.Now.Minute.ToString("D2")}:{DateTime.Now.Second.ToString("D2")} {DateTime.Now.Day.ToString("D2")}/{DateTime.Now.Month.ToString("D2")}/{DateTime.Now.Year.ToString("D4")}";
                msg = $"Get local PC current time is: {currentTime}";
            }
            else
            {
                currentTime = SSG_SFC.GetDbNow("AB3");

                msg = $"Get SFC current time is: {currentTime}";


                string m_sfc_year = "", m_sfc_month = "", m_sfc_day = "";
                string m_sfc_hour = "", m_sfc_minute = "", m_sfc_second = "";
                List<string> values1 = currentTime.Split('T').ToList();
                values1.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

                List<string> date = values1[0].Split('-').ToList();
                m_sfc_year = date[0].ToString();
                m_sfc_month = date[1].ToString();
                m_sfc_day = date[2].ToString();

                List<string> date1 = values1[1].Split(':').ToList();
                m_sfc_hour = date1[0].ToString();
                m_sfc_minute = date1[1].ToString();
                m_sfc_second = date1[2].ToString();

                currentTime = $"{m_sfc_hour}:{m_sfc_minute}:{m_sfc_second} {m_sfc_day}/{m_sfc_month}/{m_sfc_year}";
            }

            AppendRuntimeLog(msg);

            if (string.IsNullOrEmpty(currentTime))
            {
                value = "CANNOT_ACCESS_CURRTIME";
                rlt = false;
                goto End;
            }

            //key in →run "functest RTC hh:mm:ss dd/MM/yyyy"(eg: run "functest RTC 09:33:00 07/03/2023")
            SerialPortWriteLineOnce(PrinterCommands.WriteRTC(currentTime));

            if (productConfig.STATION_CODE.Contains("FCT"))
            {
                value = "PASS";
                rlt = true;
                goto End;
            }

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.WriteRTC(currentTime));
                tmpValue = tmpValue.Replace(PrinterCommands.WriteRTC(currentTime), "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }

                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            tmpValue = tmpValue.Replace("Ok", "");

            value = tmpValue.Trim();


            rlt = value.Contains("PASS") ? true : false;

        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_CheckRtc(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            int time_resource = Convert.ToInt32(INI.GetValue("RTC", "Type", ConfigFile));

            SerialPortWriteLineOnce(PrinterCommands.get_rtc);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.get_rtc);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            //FAIL
            if (tmpValue.Contains("FAIL"))
            {
                value = "FW_RTC_FAIL";
                rlt = false;
                goto End;
            }

            //Date: 01 / 07 / 2026
            //Time: 13:17:28
            //PASS

            string m_printer_year = "", m_printer_month = "", m_printer_day = "";
            string m_printer_hour = "", m_printer_minute = "", m_printer_second = "";
            List<string> values = tmpValue.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {
                if (var.Contains("Date:"))
                {
                    string m_var = var.Replace("Date:", "");
                    List<string> dates = m_var.Split('/').ToList();
                    dates.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));
                    m_printer_day = dates[0].Trim();
                    m_printer_month = dates[1].Trim();
                    m_printer_year = dates[2].Trim();
                }

                if (var.Contains("Time:"))
                {
                    string m_var = var.Replace("Time:", "");
                    List<string> dates = m_var.Split(':').ToList();
                    dates.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));
                    m_printer_hour = dates[0].Trim();
                    m_printer_minute = dates[1].Trim();
                    m_printer_second = dates[2].Trim();
                }
            }

            DateTime printer_datetime = new DateTime(
                Convert.ToInt32(m_printer_year),
                Convert.ToInt32(m_printer_month),
                Convert.ToInt32(m_printer_day),
                Convert.ToInt32(m_printer_hour),
                Convert.ToInt32(m_printer_minute),
                Convert.ToInt32(m_printer_second));

            string current_time = SSG_SFC.GetDbNow("AB3");


            string m_sfc_year = "", m_sfc_month = "", m_sfc_day = "";
            string m_sfc_hour = "", m_sfc_minute = "", m_sfc_second = "";
            List<string> values1 = current_time.Split('T').ToList();
            values1.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            List<string> date = values1[0].Split('-').ToList();
            m_sfc_year = date[0].ToString();
            m_sfc_month = date[1].ToString();
            m_sfc_day = date[2].ToString();

            List<string> date1 = values1[1].Split(':').ToList();
            m_sfc_hour = date1[0].ToString();
            m_sfc_minute = date1[1].ToString();
            m_sfc_second = date1[2].ToString();

            DateTime sfc_datetime = new DateTime(
                Convert.ToInt32(m_sfc_year),
                Convert.ToInt32(m_sfc_month),
                Convert.ToInt32(m_sfc_day),
                Convert.ToInt32(m_sfc_hour),
                Convert.ToInt32(m_sfc_minute),
                Convert.ToInt32(m_sfc_second));

            TimeSpan timeDiff = sfc_datetime - printer_datetime;

            value = Math.Abs(timeDiff.TotalSeconds).ToString("F2");

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }


        private bool Test_Printer_UbootVersion(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.GetUbootVersion());

            string regexPattern = @"U-Boot[\s\S]+";
            string tempValue = "";
            this.Invoke(new Action(() =>
            {
                for (int i = 0; i < 200; i++)
                {
                    tempValue = REGEX.StringMatch(rtbLog.Text, PrinterCommands.GetUbootVersion(), false, regexPattern);
                    if (!string.IsNullOrEmpty(tempValue)) { break; }
                    Delay(500);
                }
            }));

            /*
             * U - Boot SPL 2021.04(Dec 31 2025 - 18:48:48 + 0800)
             * U - Boot     2021.04(Jan 20 2026 - 10:57:03 + 0800)
             */

            List<string> versions = tempValue.Split('\n').ToList();
            versions.RemoveAll(s => string.IsNullOrEmpty(s));

            List<string> upperLimits = item.UpperLimit.Split(';').ToList();
            List<string> lowerLimits = item.LowerLimit.Split(';').ToList();

            bool upper_result = false, lower_result = false;

            foreach (var u in upperLimits)
            {
                if (versions.Contains(u))
                {
                    upper_result = true;
                    break;
                }
            }

            foreach (var l in lowerLimits)
            {
                if (versions.Contains(l))
                {
                    lower_result = true;
                    break;
                }
            }

            /*
            if (versions.Contains(item.UpperLimit) && versions.Contains(item.LowerLimit))
                rlt = true;
            else
                rlt = false;
                */

            rlt = upper_result && lower_result;

            value = string.Join(" / ", versions);
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_BatteryCharingByE_Charging(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "";
            string zh_cn = "", en_us = "", th_ai = "";

            zh_cn = "请把Printer放在Dock上面充电，观察LCD是否提示有在充电。";
            en_us = "Please set printer on the charging dock, and check if is on charging.";
            th_ai = "null";

            rlt = GetOperatorSelection(zh_cn, en_us, th_ai);
            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value.Trim();
            return rlt;
        }

        private bool Test_Printer_TurnOffSleepMode(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.SetUpSleepTimer_0);
            Delay(1000);

            SerialPortWriteLineOnce(PrinterCommands.get_sleep_timer_value);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.get_sleep_timer_value);
                tmpValue = tmpValue.Replace(PrinterCommands.get_sleep_timer_value, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            value = tmpValue.Replace("Ok", "").Trim();
            rlt = true;
        End:
            item.Value = value.Trim();
            rlt = ((item.Value == item.UpperLimit) && (item.Value == item.LowerLimit)) ? true : false;
            return rlt;
        }


        private bool Test_Printer_FwVersion(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.GetFWVersion);
            //string regaxPattern = @"M[\d\.]+N";

            Delay(Convert.ToInt32(INI.GetValue("Delays", "GetFWVersion", ConfigFile)));

            for (int i = 0; i < 200; i++)
            {
                value = GetRuntimeStringFlagA(PrinterCommands.GetFWVersion);
                value = value.Replace(PrinterCommands.GetFWVersion, "");
                if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            value = value.Replace("Ok", "");
        End:
            item.Value = value.Trim();
            rlt = ((item.Value == item.UpperLimit) && (item.Value == item.LowerLimit)) ? true : false;
            return rlt;
        }

        private bool Test_Printer_CoverInSwichSensor(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string tmpLifed = "", tmpLowered = "";
            string zh_ch = "", us_en = "", thai = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);


            if (productConfig.STATION_CODE.Contains("FCT"))
            {
                RelayCard2.SetRelayOff(0);
                Delay(500);
                goto Check_Cover_Lifted;
            }

            zh_ch = "请将载纸装置打开。";
            us_en = "please open the media cover.";
            thai = "thai tips (unknown)";

            bool bOpStep = GetOperatorSelection(zh_ch, us_en, thai);

            if (!bOpStep)
            {
                value = "OP_OPEN_COVER_FAIL";
                rlt = false;
                goto End;
            }

        Check_Cover_Lifted:

            SerialPortWriteLine(PrinterCommands.HEAD_LIFT_SENSOR, 1);
            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.HEAD_LIFT_SENSOR + " " + 1);
                tmpValue = tmpValue.Replace(PrinterCommands.HEAD_LIFT_SENSOR + " " + 1, "");
                tmpValue = tmpValue.Replace(PrinterCommands.HEAD_LIFT_SENSOR, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            tmpValue = tmpValue.Trim();
            List<string> values_lifted = tmpValue.Split('\n').ToList();
            values_lifted.RemoveAll(s => string.IsNullOrEmpty(s));

            foreach (string v in values_lifted)
            {
                if (v.Contains("Print head"))
                {
                    tmpLifed = v.Replace("Print head", "").Trim();
                    break;
                }
            }

            bool bOpStep1 = false;
            if (productConfig.STATION_CODE.Contains("FCT"))
            {
                RelayCard2.SetRelayOn(0);
                Delay(500);
                goto Check_Cover_Lowered;
            }
            else
            {
                Invoke(new Action(() =>
                {
                    frmTips dialog = new frmTips("请将载纸装置关闭", "please open the media cover.", "thai tips (unknown)");
                    DialogResult result = dialog.ShowDialog(this);
                    bOpStep1 = result == DialogResult.OK;
                }));
            }

            if (!bOpStep1)
            {
                value = "OP_CLOSE_COVER_FAIL";
                rlt = false;
                goto End;
            }

        Check_Cover_Lowered:
            SerialPortWriteLine(PrinterCommands.HEAD_LIFT_SENSOR, 2);
            bTimeOut = true;
            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.HEAD_LIFT_SENSOR + " " + 2);
                tmpValue = tmpValue.Replace(PrinterCommands.HEAD_LIFT_SENSOR + " " + 2, "");
                tmpValue = tmpValue.Replace(PrinterCommands.HEAD_LIFT_SENSOR, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            tmpValue = tmpValue.Trim();
            List<string> values_lowered = tmpValue.Split('\n').ToList();
            values_lowered.RemoveAll(s => string.IsNullOrEmpty(s));

            foreach (string v in values_lowered)
            {
                if (v.Contains("Print head"))
                {
                    tmpLowered = v.Replace("Print head", "").Trim();
                    break;
                }
            }

            rlt = (tmpLifed == item.UpperLimit && tmpLowered == item.LowerLimit) ? true : false;
            value = tmpLifed + " / " + tmpLowered;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_SecurityChip(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";


            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.SECURITY_CHIP_OTP);
            Delay(2000);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.SECURITY_CHIP_OTP);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            /*
            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains("PASS"))
            {
                rlt = true;
            }
            else
            {
                rlt = false;
            }
            */


            Delay(5000);

            SerialPortWriteLineOnce(PrinterCommands.SECURITY_CHIP_GEN_KEY);

            SerialPortWriteLineOnce(PrinterCommands.SECURITY_CHIP_GET_DEVICE_ID);

            Delay(5000);

            rlt = true;

            value = rlt ? "PASS" : "FAIL";

        End:

            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_ChargerIC(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.CHARGER);
            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.CHARGER);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            value = GetValueContainFlag(tmpValue, "REG");
            rlt = (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value)) ? true : false;
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_MotionSensor(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.MOTION);
            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.MOTION);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            value = GetValueAfterFlag(tmpValue, "WHO_AM_I=");
            rlt = (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value)) ? true : false;
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_MotionSensor1(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string zh_cn = "请请把打印机倒转，观察LCD屏幕是否是跟着旋转。";
            string en_us = "please turn down the printer and check if the LCD turn down as well";
            string thai = "thai lang unknown.";

            rlt = GetOperatorSelection(zh_cn, en_us, thai);

            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_PaperFeed(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.MOTOR_CLEANFEED_2000);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.MOTOR_CLEANFEED_2000);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            value = tmpValue.Contains("PASS") ? "PASS" : "FAIL";
            rlt = tmpValue.Contains("PASS") ? true : false;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_LCDScreen(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            SerialPortWriteLineOnce(PrinterCommands.LCD_GREEN);
            Delay(5000);
            string zh_cn = "请确认LCD屏幕是否是绿色。";
            string en_us = "please comfirm the LCD screen is green.";
            string thai = "thai tips (unknown)";
            bool bGreen = GetOperatorSelection(zh_cn, en_us, thai);
            if (!bGreen)
            {
                value = "GREEN_LCD_DISPLAG_FIAL";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LCD_BLACK);
            Delay(5000);
            zh_cn = "请确认LCD屏幕是否是黑色。";
            en_us = "please comfirm the LCD screen is black.";
            thai = "thai tips (unknown)";
            bool bBlack = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bBlack)
            {
                value = "BLACK_LCD_DISPLAG_FIAL";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LCD_WHITE);
            Delay(5000);
            zh_cn = "请确认LCD屏幕是否是白色。";
            en_us = "please comfirm the LCD screen is white.";
            thai = "thai tips (unknown)";
            SerialPortWriteLineOnce(PrinterCommands.LCD_WHITE);
            bool bWhite = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bWhite)
            {
                value = "WHITE_LCD_DISPLAG_FIAL";
                rlt = false;
                goto End;
            }

            rlt = (bGreen && bBlack && bWhite) ? true : false;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_LEDLight(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            string zh_cn = "请确认LED灯有橙色灯亮";
            string en_us = "Pleace check the LED is blinking with orange light.";
            string thai = "thai tips (unknown)";
            SerialPortWriteLineOnce(PrinterCommands.LED_ORANGE_ON);
            bool b_orange_led = GetOperatorSelection(zh_cn, en_us, thai);

            if (!b_orange_led)
            {
                value = "LED_ORANGE_FAIL";
                rlt = false;
                goto End;
            }

            Delay(1000);

            zh_cn = "请确认LED灯有绿色灯亮";
            en_us = "Pleace check the LED is blinking with green light.";
            thai = "thai tips (unknown)";
            SerialPortWriteLineOnce(PrinterCommands.LED_GREEN_ON);
            bool b_green_led = GetOperatorSelection(zh_cn, en_us, thai);

            if (!b_green_led)
            {
                value = "LED_GREEN_FAIL";
                rlt = false;
                goto End;
            }

            rlt = true;
            value = "PASS";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_BlueToothID(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string part_number = "", seri_number = "", address = "";
            string m_sfc_address = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (m_PA45.CommunicationOption == CommunicationOptions.USB_only)
            {
                value = "USB_ONLY_SKU";
                rlt = true;
                goto End;
            }

            if (m_PA45.CommunicationOption == CommunicationOptions.BT_only)
            {
                SerialPortWriteLineOnce(PrinterCommands.read_bluetooth_mac_for_bt_only_module);
                Delay(2000);
                for (int i = 0; i < 60; i++)
                {
                    tmpValue = GetRuntimeStringFlagA(PrinterCommands.read_bluetooth_mac_for_bt_only_module);
                    tmpValue = tmpValue.Replace(PrinterCommands.read_bluetooth_mac_for_bt_only_module, "");
                    if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                    {
                        bTimeOut = false;
                        break;
                    }
                    Delay(1000);
                }

                if (bTimeOut)
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

                if (tmpValue.Contains("HW_NOT_INSTALLED"))
                {
                    value = "HW_NOT_INSTALLED";
                    rlt = false;
                    goto End;
                }

                List<string> values = tmpValue.Split('\n').ToList();
                values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

                foreach (string var in values)
                {
                    if (var.Contains("btmac ="))
                    {
                        address = var.Replace("btmac =", "").Trim();
                        break;
                    }
                }

            }
            else
            {
                SerialPortWriteLineOnce(PrinterCommands.BLUETOOTH_ID);

                for (int i = 0; i < 60; i++)
                {
                    tmpValue = GetRuntimeStringFlagA(PrinterCommands.BLUETOOTH_ID);
                    tmpValue = tmpValue.Replace(PrinterCommands.BLUETOOTH_ID, "");
                    if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                    {
                        bTimeOut = false;
                        break;
                    }
                    Delay(1000);
                }

                if (bTimeOut)
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

                if (tmpValue.Contains("HW_NOT_INSTALLED"))
                {
                    value = "HW_NOT_INSTALLED";
                    rlt = false;
                    goto End;
                }

                GetBluetoothId(tmpValue, ref part_number, ref seri_number, ref address);
            }

            AppendRuntimeLog($"detected printer's bluetooth mac is {address}");

            if (string.IsNullOrEmpty(address) || string.IsNullOrWhiteSpace(address))
            {
                value = "MAC_ADDRESS_NULL";
                rlt = false;
                goto End;
            }

            BLUETOOTH_MAC_ADDRESS = address;

            if (ckbSFCS.Checked)
            {

                if (m_PA45.CommunicationOption == CommunicationOptions.BT_only)
                {
                    string bt_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114400_BT_board");

                    if (string.IsNullOrEmpty(bt_module_sn) || string.IsNullOrWhiteSpace(bt_module_sn))
                    {
                        value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                        rlt = false;
                        goto End;
                    }

                    m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GET_AUTO_MAC_ADD", $"AMFIS16S00000056;{bt_module_sn}");

                    AppendRuntimeLog($"{bt_module_sn} get sfc mac is {m_sfc_address}.");

                }
                else
                {
                    string bt_wifi_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114390_BT_Wifi_board");
                    AppendRuntimeLog($"{MainBarcode} get sfc wifi module SN is {bt_wifi_module_sn}.");

                    if (string.IsNullOrEmpty(bt_wifi_module_sn) || string.IsNullOrWhiteSpace(bt_wifi_module_sn))
                    {
                        value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                        rlt = false;
                        goto End;
                    }

                    string station = "BT_Wifi_board_Function_Test";
                    string target_values = "WiFi_BT_Module Read(BT)";
                    string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
                    m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);
                    m_sfc_address = m_sfc_address.ToUpper();
                    AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");
                }
            }

            string input_bt_mac = Interaction.InputBox("please scan the bluetooth label on printer");

            if (ckbSFCS.Checked)
            {
                rlt = (input_bt_mac.ToUpper() == address.ToUpper()) && (input_bt_mac.ToUpper() == m_sfc_address.ToUpper());
                if (rlt)
                {
                    value = address.ToUpper();
                }
                else
                {
                    value = $"{address.ToUpper()} / {input_bt_mac.ToUpper()} / {m_sfc_address.ToUpper()}";
                }
            }
            else
            {
                rlt = input_bt_mac.ToUpper() == address.ToUpper();

                if (rlt)
                {
                    value = address.ToUpper();
                }
                else
                {
                    value = $"{address.ToUpper()} / {input_bt_mac.ToUpper()}";
                }
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_MotorClean2000(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            Invoke(new Action(() =>
            {
                frmTips dialog = new frmTips(
                    "请将打印纸安装到打印机里面，并确认已经安装好。",
                    "Please install the media into printer.",
                    "thai tips (unknown)");
                DialogResult result = dialog.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    SerialPortWriteLineOnce(PrinterCommands.FORMFEED);

                    //peFeed() failed!Error:Printhead lifted
                    //FAIL
                    for (int i = 0; i < 60; i++)
                    {
                        value = GetRuntimeStringFlagA(PrinterCommands.FORMFEED);
                        if (!string.IsNullOrEmpty(value))
                        {
                            bTimeOut = false;
                            break;
                        }
                    }

                    Delay(1000);
                }
                else
                {
                    value = "FAIL";
                    rlt = false;
                }
            }));

            //rlt = string.IsNullOrEmpty(temp_value) ? false : true;

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }


        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_RFID(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            Prod.PA45 m_PA45 = product as Prod.PA45;
            if (m_PA45 == null)
            {
                value = "PRODUCT_ERROR";
                rlt = false;
                goto End;
            }

            if (!m_PA45.CommunicationOption.ToString().ToUpper().Contains("RFID"))
            {
                value = "NO_RFID_CONFIG";
                rlt = true;
                goto End;
            }

        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_CheckLockingStatus(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.chekc_locking_status);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.chekc_locking_status);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            //console=ttymxc0,115200 SecureBoot=Disabled cma=32M root=/dev/mmcblk1p2 rootfstype=ext4 rootwait rw

            List<string> values = tmpValue.Split(' ').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {
                if (var.Contains("SecureBoot"))
                {
                    value = var.Replace("SecureBoot=", "").Trim();
                    break;
                }
            }

            rlt = (value == item.UpperLimit && value == item.LowerLimit) ? true : false;

        End:
            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_RecoverySetting(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.RESTORE_DEFAULT);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.RESTORE_DEFAULT);
                tmpValue = tmpValue.Replace(PrinterCommands.RESTORE_DEFAULT, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (!tmpValue.Contains("PASS"))
            {
                value = "RESTORE_DEFAULT_ERROR";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.recovery_network_config);
            SerialPortWriteLineOnce(PrinterCommands.reboot);

            string zh_cn = "请确认是否重启，待到出现选择语言界面关机。", en_us = "", thai = "";
            string pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "printer_initial_status.jpg");

            rlt = GetOperatorSelection(zh_cn, en_us, thai, pic_path);

        End:
            item.Value = value;
            return rlt;
        }


        #endregion

        #region ---keyboard fct implement---
        private bool Test_Keyboard_PowerOn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            rlt = RelayCard1.SetRelayOn(0);
            value = rlt ? "PASS" : "FAIL";

            item.Value = value;
            return rlt;
        }

        private bool Test_Keyboard_SW1_UiSw1(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            double m_voltage = 0.0;

            if (item.Name.ToUpper().Contains("ON"))
            {
                rlt = RelayCard1.SetRelayOn(2);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(3);
                rlt = RelayCard1.SetRelayOff(2);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(3);
            }

            value = Convert.ToString(2 * m_voltage);
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Keyboard_SW2_EntryKey(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            double m_voltage = 0.0;

            if (item.Name.ToUpper().Contains("ON"))
            {
                RelayCard1.SetRelayOn(3);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(1);
                RelayCard1.SetRelayOff(3);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(1);
            }

            value = Convert.ToString(2 * m_voltage);
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Keyboard_SW3_UiMenuKey(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            double m_voltage = 0.0;

            if (item.Name.ToUpper().Contains("ON"))
            {
                RelayCard1.SetRelayOn(4);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(2);
                RelayCard1.SetRelayOn(4);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(2);
            }

            value = Convert.ToString(2 * m_voltage);
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Keyboard_SW4_FeedSw2(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            double m_voltage = 0.0;
            if (item.Name.ToUpper().Contains("ON"))
            {
                RelayCard1.SetRelayOn(5);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(0);
                RelayCard1.SetRelayOff(5);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(0);
            }

            value = Convert.ToString(2 * m_voltage);
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Keyboard_SW5_DownKey(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            double m_voltage = 0.0;

            if (item.Name.ToUpper().Contains("ON"))
            {
                RelayCard1.SetRelayOn(6);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(4);
                RelayCard1.SetRelayOff(6);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(4);
            }

            value = Convert.ToString(m_voltage);

            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Keyboard_SW6_UpKey(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            double m_voltage = 0.0;

            if (item.Name.ToUpper().Contains("ON"))
            {
                RelayCard1.SetRelayOn(7);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(5);
                RelayCard1.SetRelayOn(7);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(5);
            }

            value = Convert.ToString(m_voltage);
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Keyboard_LedPowerOn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            double m_voltage = 0.0;

            if (item.Name.ToUpper().Contains("RED"))
            {
                RelayCard1.SetRelayOn(1);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(6);
                RelayCard1.SetRelayOn(1);
            }
            else
            {
                RelayCard1.SetRelayOn(1);
                Delay(500);
                m_voltage = RelayCard1.MeasureVoltage(7);
                RelayCard1.SetRelayOn(1);
            }

            value = Convert.ToString(m_voltage);
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        #endregion

        #region ---mainboard FW flash implement ---

        private bool Test_MainBoard_PowerOn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(2))
            {
                rlt = false;
                value = "RELAY1_3_ON-FAIL";
                goto End;
            }
            Delay(500);

            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_1_ON-FAIL";
                goto End;
            }
            Delay(500);

            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_2_ON-FAIL";
                goto End;
            }

            rlt = true;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MainBoard_RePowerOn(int index, TestItem item)
        {
            bool rlt = false, bFlag = false;
            string value = "", tmpValue = "";

            if (!RelayCard1.SetRelayOn(2))
            {
                rlt = false;
                value = "RELAY1_3_ON-FAIL";
                goto End;
            }

            Delay(500);

            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_2_ON-FAIL";
                goto End;
            }

            for (int i = 0; i < 200; i++)
            {
                tmpValue = ReadSerialportDataReceived();

                if (tmpValue.Contains("INTERMEC_$123$ login:"))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            if (!bFlag)
            {
                value = "FIND_DEVICE_TIMEOUT";
                rlt = false;
                goto End;
            }

            rlt = true;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MainBoard_FWFlash(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            string mfg_tool_path = Path.Combine(
                Global.ASSEMBLY_PATH,
                INI.GetValue("FirmwareFlash", "AssemblyPath", ConfigFile),
                INI.GetValue("FirmwareFlash", "AssemblyName", ConfigFile));

            if (!File.Exists(mfg_tool_path))
            {
                value = "NO_FW_TOOL";
                rlt = false;
                goto End;
            }

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = mfg_tool_path;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(mfg_tool_path);
                proc.Start();

                // 等待进程初始化并创建主窗口 (非常重要)
                proc.WaitForInputIdle();
                int pid = proc.Id;

                //get ui reource

            }

            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MainBoard_CheckSum(int index, TestItem item)
        {
            bool rlt = false;
            string value = "MAINBOARD_CHECK_SUM_NOT_IMPELMENT";

            item.Value = value;
            return rlt;
        }

        #endregion

        #region ---Powerboard FCT implement ---

        private bool Test_Powerboard_PowerOn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_1_ON-FAIL";
                goto End;
            }
            Delay(500);

            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_1_ON-FAIL";
                goto End;
            }
            Delay(500);

            if (!RelayCard1.SetRelayOn(2))
            {
                rlt = false;
                value = "RELAY1_2_ON-FAIL";
                goto End;
            }

            rlt = true;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_VBUS_IN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_0_ON-FAIL";
                goto End;
            }
            Delay(500);
            value = Convert.ToString(3.7 * RelayCard1.MeasureVoltage(0));
            RelayCard1.SetRelayOff(0);
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_DC_IN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_1_ON-FAIL";
                goto End;
            }
            Delay(500);
            value = Convert.ToString(1.1 * RelayCard1.MeasureVoltage(1));
            RelayCard1.SetRelayOff(1);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_VBUS_5V(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(2))
            {
                rlt = false;
                value = "RELAY1_2_ON-FAIL";
                goto End;
            }
            value = Convert.ToString(RelayCard1.MeasureVoltage(2));
            RelayCard1.SetRelayOff(2);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_CON1_G1_G4(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(3))
            {
                rlt = false;
                value = "RELAY1_3_ON-FAIL";
                goto End;
            }
            value = Convert.ToString(RelayCard1.MeasureVoltage(3));
            RelayCard1.SetRelayOff(3);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_CON1_G2_G3(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(4))
            {
                rlt = false;
                value = "RELAY1_4_ON-FAIL";
                goto End;
            }
            value = Convert.ToString(RelayCard1.MeasureVoltage(4));
            RelayCard1.SetRelayOff(4);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_CON2_G1_G4(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(5))
            {
                rlt = false;
                value = "RELAY1_5_ON-FAIL";
                goto End;
            }
            value = Convert.ToString(RelayCard1.MeasureVoltage(5));
            RelayCard1.SetRelayOff(5);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_CON2_G2_G3(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(6))
            {
                rlt = false;
                value = "RELAY1_6_ON-FAIL";
                goto End;
            }
            value = Convert.ToString(RelayCard1.MeasureVoltage(6));
            RelayCard1.SetRelayOff(6);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_TP307(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(7))
            {
                rlt = false;
                value = "RELAY1_7_ON-FAIL";
                goto End;
            }
            value = Convert.ToString(RelayCard1.MeasureVoltage(7));
            RelayCard1.SetRelayOff(7);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Powerboard_CC2O(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(0);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(0) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(0);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(0) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_CC1O(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(1);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(1) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(1);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(1) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB_OTG1_DP_A(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(2);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(2) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(2);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(2) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB_OTG1_DM_A(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(3);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(3) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(3);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(3) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }


        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_CC1H(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(5);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(5) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(5);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(5) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_CC2H(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(4);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(4) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(4);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(4) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_HOST_DPTest_Powerboard_CC1H(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(5);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(5) ? "1" : "0";
                rlt = value == "1" ? true : false;

            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(5);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(5) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_HOST_DP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(6);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(6) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(6);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(6) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_HOST_DM(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(7);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(7) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(7);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(7) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_VEHICLE_MOUNT_DET(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(8);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(8) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(8);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(8) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB2_DOCK_DM(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(8);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(8) ? "1" : "0";
                rlt = value == "1" ? true : false;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(8);
                Delay(500);
                value = CaptureCard1.GetCaptureCardDI(8) ? "0" : "1";
                rlt = value == "0" ? true : false;
            }

        End:
            item.Value = value;
            return rlt;
        }



        #endregion

        #region---top sensor implement---

        private bool Test_TopSensor_PowerOn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_0_ON-FAIL";
                goto End;
            }

            rlt = true;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_TopSensor_VF(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            value = Convert.ToString(RelayCard1.MeasureVoltage(0));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_TopSensor_VCE(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            string zn_ch_tips = "", en_us_tips = "", thai_tips = "";
            if (item.Name.ToUpper().Contains("BLACK") || item.Name.ToUpper().Contains("DARK"))
            {
                zn_ch_tips = "黑色";
                en_us_tips = "black";
                thai_tips = "null";
            }
            else
            {
                zn_ch_tips = "白色";
                en_us_tips = "white";
                thai_tips = "null";
            }

            Invoke(new Action(() =>
            {
                frmTips dialog = new frmTips(zn_ch_tips, en_us_tips, thai_tips);
                DialogResult result = dialog.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    Delay(500);
                    value = Convert.ToString(RelayCard1.MeasureVoltage(1));
                }
                else
                {
                    value = "-1";
                }
            }));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        #endregion

        #region ---WiFi_BT_Module,  BT_Module ---

        private bool Test_WiFiBtModule_PowerOn(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "";
            string devicePortName = "";

            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_0_ON_NG";
                goto End;
            }

            Delay(3000);

            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_1_ON_NG";
                goto End;
            }

            bool is_wifi_bt = false, is_bt = false;

            Prod.WiFi_BT_Module m_product = product as Prod.WiFi_BT_Module;

            if (m_product == null)
            {
                is_wifi_bt = false;
                //value = "PRODUCT_TPYE_ERROR";
                AppendRuntimeLog($"this DUT is not WIFI_BT_module.");
                goto check_is_bt_only;
            }
            else
            {
                is_wifi_bt = true;
                AppendRuntimeLog($"this DUT is WIFI_BT_module.");
                goto StartTest;
            }


        check_is_bt_only:
            Prod.BT_Module m_product1 = product as Prod.BT_Module;

            if (m_product1 == null)
            {
                is_bt = false;
                AppendRuntimeLog($"this DUT is not BT_module.");
                value = "PRODUCT_TPYE_ERROR";
                goto End;
            }
            else
            {
                is_bt = true;
                AppendRuntimeLog($"this DUT is BT_module.");
            }

        StartTest:


            if (!is_wifi_bt && !is_bt)
            {
                rlt = false;
                value = "PRODUCT_TPYE_ERROR";
                goto End;
            }

            string m_vid, m_pid = "";

            if (is_wifi_bt)
            {
                DUTS.WiFi_BT_Module.WiFi_BT_Module m_dut = dut as DUTS.WiFi_BT_Module.WiFi_BT_Module;
                m_vid = m_dut.VID;
                m_pid = m_dut.PID;
            }
            else
            {
                DUTS.BT_Module.BT_Module m_dut = dut as DUTS.BT_Module.BT_Module;
                m_vid = m_dut.VID;
                m_pid = m_dut.PID;
            }

            for (int i = 0; i < 200; i++)
            {
                devicePortName = WinDevice.GetSerialportComPort(m_vid, m_pid);
                if (devicePortName != "COM1" &&
                    !string.IsNullOrEmpty(devicePortName) &&
                    !string.IsNullOrWhiteSpace(devicePortName))
                {
                    AppendRuntimeLog($"count: {i}, detected device: {devicePortName} [vid({m_vid}), pid({m_pid})].");

                    rlt = true;
                    bTimeOut = false;
                    break;
                }

                AppendRuntimeLog($"count: {i}, detecting device [vid({m_vid}), pid({m_pid})].");
                Delay(1000);
            }

            if (bTimeOut)
            {
                rlt = false;
                value = "TimeOut";
                goto End;
            }

            Delay(40000);

            value = rlt ? $"PASS / {devicePortName}" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_WiFiBtModule_BtWifiRead(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true, bFinddevice = false;
            string value = "", tmpValue = "";
            string devicePortName = "";

            Prod.WiFi_BT_Module m_product = product as Prod.WiFi_BT_Module;

            if (m_product == null)
            {
                rlt = false;
                value = "PRODUCT_TPYE_ERROR";
                goto End;
            }

            Type m_type = dut.GetType();

            if (m_type.Name != "WiFi_BT_Module")
            {
                rlt = false;
                value = "DUT_TPYE_ERROR";
                goto End;
            }

            DUTS.WiFi_BT_Module.WiFi_BT_Module m_dut = dut as DUTS.WiFi_BT_Module.WiFi_BT_Module;

            for (int i = 0; i < 200; i++)
            {
                devicePortName = WinDevice.GetSerialportComPort(m_dut.VID, m_dut.PID);
                if (devicePortName != "COM1" && !string.IsNullOrEmpty(devicePortName) && !string.IsNullOrWhiteSpace(devicePortName))
                {
                    AppendRuntimeLog($"current times is{i}, detected device: {devicePortName} [vid({m_dut.VID}), pid({m_dut.PID})].");
                    rlt = true;
                    bFinddevice = true;
                    break;
                }
                AppendRuntimeLog($"current times is{i}, detecting device [vid({m_dut.VID}), pid({m_dut.PID})].");
                Delay(1000);
            }

            if (!bFinddevice)
            {
                rlt = false;
                value = "CANNOT_FIND_DEVICE";
                goto End;
            }

            if (!InitializeSerialport(m_dut.VID, m_dut.PID, spTester))
            {
                rlt = false;
                value = "CANNOT_OPEN_DEVICE";
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            string m_cmd = string.Empty;
            if (item.Name.ToUpper().Contains("(WIFI)")) { m_cmd = PrinterCommands.WIFI_ID; }
            else { m_cmd = PrinterCommands.BLUETOOTH_ID; }

            SerialPortWriteLineOnce(m_cmd);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(m_cmd);
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(1000);
            }

            if (bTimeOut)
            {
                rlt = false;
                value = "TimeOut";
                goto End;
            }

            if (tmpValue.Contains(Test_Proc_Error_Code.HW_NOT_INSTALL.ToString()))
            {
                rlt = false;
                value = $"{Test_Proc_Error_Code.HW_NOT_INSTALL}";
                goto End;
            }

            if (item.Name.ToUpper().Contains("(WIFI)"))
            {
                value = GetSensorValue(tmpValue, "MAC Address:");
                WIFI_MAC_ADDRESS = value;
            }
            else
            {
                value = GetSensorValue(tmpValue, "BD Address:");
                BLUETOOTH_MAC_ADDRESS = value;
            }

            rlt = (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(value)) ? true : false;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_WiFiBtModule_PrintMAC(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true, bFinddevice = false;
            string value = "", tmpValue = "";
            string LabFile = "", LabFilePath = "";
            string mac = "";

            if (item.Name.ToUpper().Contains("(WIFI)"))
            {
                mac = WIFI_MAC_ADDRESS;
                LabFile = INI.GetValue("PrintMACSetting", "WifiLabFile", ConfigFile);
            }
            else
            {
                mac = BLUETOOTH_MAC_ADDRESS;
                LabFile = INI.GetValue("PrintMACSetting", "BluetoothLabFile", ConfigFile);
            }

            LabFilePath = Path.Combine(Global.ASSEMBLY_PATH, "LabFile", LabFile);

            if (string.IsNullOrEmpty(mac) || string.IsNullOrWhiteSpace(mac))
            {
                value = "MAC_CONTENT_IS_NULL";
                rlt = false;
                goto End;
            }

            AppendRuntimeLog($"LabFile is :{LabFilePath}");

            if (Convert.ToBoolean(INI.GetValue("PrintMACSetting", "IsPrint", ConfigFile)))
            {
                rlt = PRINTER.PrintLabel(LabFilePath, INI.GetValue("PrintMACSetting", "KeyName", ConfigFile), mac.ToUpper()) ? true : false;
                AppendRuntimeLog($"Is Print enable is :{LabFilePath}");
            }
            else
            {
                rlt = true;
            }

            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_WiFiBtModule_FindBTDevice(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true, bFinddevice = false;
            string value = "", tmpValue = "";
            string mac = "";


        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_BtModule_WriteMAC(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true, bFinddevice = false;
            string value = "", tmpValue = "";
            string LabFile = "", LabFilePath = "";
            string mac = "";


            DUTS.BT_Module.BT_Module m_dut = dut as DUTS.BT_Module.BT_Module;

            if (m_dut == null)
            {
                value = "DUT_TYPE_ERROR";
                rlt = false;
                goto End;
            }

            if (!InitializeSerialport(m_dut.VID, m_dut.PID, spTester))
            {
                rlt = false;
                value = "CANNOT_OPEN_DEVICE";
                goto End;
            }

            string mac_assigned_by_sfc = _ssg_sfc.GetSpecialValue("Scanner", "GET_AUTO_MAC_ADD", $"AMFIS16S00000056;{dut.SerialNumber}");

            AppendRuntimeLog($"DUT'SN is {mac_assigned_by_sfc}, assigned mac by SFC is {mac_assigned_by_sfc}");
            if (string.IsNullOrEmpty(mac_assigned_by_sfc) || string.IsNullOrWhiteSpace(mac_assigned_by_sfc))
            {
                value = "SFC_ASSIGNED_MAC_ERROR";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.write_bluetooth_mac_for_bt_only_module(mac_assigned_by_sfc));

            Delay(3000);

            SerialPortWriteLineOnce(PrinterCommands.read_bluetooth_mac_for_bt_only_module);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.read_bluetooth_mac_for_bt_only_module);
                tmpValue = tmpValue.Replace(PrinterCommands.read_bluetooth_mac_for_bt_only_module, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = true;
                goto End;
            }

            List<string> values = tmpValue.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string var in values)
            {
                if (var.Contains("btmac ="))
                {
                    value = var.Replace("btmac =", "").Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
            {
                value = "PRINTER_BT_MAC_NULL";
                rlt = false;
                goto End;
            }
            BLUETOOTH_MAC_ADDRESS = value;
            rlt = value == mac_assigned_by_sfc;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_NFCTag_Read(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            value = "ATE_NOT_IMPELMENT";

            value = NFC.ReadNFCTag(1);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_NFCTag_Write(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            //step1, find the NFC reader device;
            byte[] devno = new byte[4];
            if (NFC.pcdgetdevicenumber(devno) == 0)
            {
                NFC.pcdbeep(50);
                //MessageBox.Show("设备的序列号为：" + System.Convert.ToString(devno[0]) + "-" + System.Convert.ToString(devno[1]) + "-" + System.Convert.ToString(devno[2]) + "-" + System.Convert.ToString(devno[3]), "示例程序");
                //ShowMessage(IntToStr(devno[0]) + "-" + IntToStr(devno[1]) + "-" + IntToStr(devno[2]) + "-" + IntToStr(devno[3]));
                AppendRuntimeLog($"NFC device number: {Convert.ToString(devno[0])} - {Convert.ToString(devno[1])} - {Convert.ToString(devno[2])} - {Convert.ToString(devno[3])}");
            }
            else
            {
                AppendRuntimeLog("did find NFC reader");
                value = "No_NFC_Reader_Device";
                rlt = false;
                goto End;
            }

            //step1, get BT mac
            string blueteethMAC = BLUETOOTH_MAC_ADDRESS;
            string model = "PA45";

            //step2, write data into card.
            string statusCode = string.Empty;
            string str1 = NFC.WriteCardSector(NFC.NFC_Data(1, blueteethMAC), 1, NFC.Key_B, ref statusCode);

            AppendRuntimeLog($"operation result: {str1}, data content: {str1}");

            /*
            if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrEmpty(str1) || str1.Length > 2)
            {
                value = "NFC_Sector1_Write_Fail";
                rlt = false;
                goto End;
            }
            */

            string statusCode1 = string.Empty;
            string str2 = NFC.WriteCardSector(NFC.NFC_Data(2, blueteethMAC), 2, NFC.Key_B, ref statusCode1);
            AppendRuntimeLog($"operation result: {str2}, data content: {str2}");

            /*
            if (string.IsNullOrWhiteSpace(str2) || string.IsNullOrEmpty(str2) || str2.Length > 2)
            {
                value = "NFC_Sector2_Write_Fail";
                rlt = false;
                goto End;
            }
            */

            value = "PASS";
            rlt = true;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_NFCTag_SetCardNDEF(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            string statusCode = string.Empty;
            //set NFC-tag as NDEF format
            // 0,1 sector, and the 3 blocks of them.

            // 0-sector, 1 block
            if (!NFC.WriteCardSectorBlock(NFC.NDEF_FORMAT_DATA(0), 1, 0, NFC.Key_Default, ref statusCode))
            {
                rlt = false;
                value = statusCode;
                goto End;
            }
            AppendRuntimeLog($"0-sector, 1 block NDEF_FORMAT OK.");

            // 0-sector, 2 block
            if (!NFC.WriteCardSectorBlock(NFC.NDEF_FORMAT_DATA(1), 2, 0, NFC.Key_Default, ref statusCode))
            {
                rlt = false;
                value = statusCode;
                goto End;
            }
            AppendRuntimeLog($"0-sector, 2 block NDEF_FORMAT OK.");

            // 0-sector, 3 block
            if (!NFC.WriteCardSectorBlock(NFC.NDEF_FORMAT_DATA(2), 3, 0, NFC.Key_Default, ref statusCode))
            {
                rlt = false;
                value = statusCode;
                goto End;
            }
            AppendRuntimeLog($"0-sector, 3 block NDEF_FORMAT OK.");

            // 1-sector, 1 block
            if (!NFC.WriteCardSectorBlock(NFC.NDEF_FORMAT_DATA(3), 0, 1, NFC.Key_Default, ref statusCode))
            {
                rlt = false;
                value = statusCode;
                goto End;
            }
            AppendRuntimeLog($"1-sector, 1 block NDEF_FORMAT OK.");

            // 1-sector, 2 block
            if (!NFC.WriteCardSectorBlock(NFC.NDEF_FORMAT_DATA(4), 3, 1, NFC.Key_Default, ref statusCode))
            {
                rlt = false;
                value = statusCode;
                goto End;
            }
            AppendRuntimeLog($"1-sector, 2 block NDEF_FORMAT OK.");

            // 1-sector, 3 block
            if (!NFC.WriteCardSectorBlock(NFC.NDEF_FORMAT_DATA(4), 3, 2, NFC.Key_Default, ref statusCode))
            {
                rlt = false;
                value = statusCode;
                goto End;
            }
            AppendRuntimeLog($"1-sector, 3 block NDEF_FORMAT OK.");

            value = (statusCode == "OK") ? "PASS" : "FAIL";
            rlt = (statusCode == "OK") ? true : false;

        End:

            item.Value = value;
            return rlt;
        }

        private bool Test_NFCTag_CheckNFCContent(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (m_PA45.CommunicationOption == CommunicationOptions.USB_only)
            {
                value = "NO_NFC_CONFIG";
                rlt = true;
                goto End;
            }

            if (string.IsNullOrEmpty(BLUETOOTH_MAC_ADDRESS) || string.IsNullOrWhiteSpace(BLUETOOTH_MAC_ADDRESS))
            {
                value = "MAC_CONTENT_IS_NULL";
                rlt = false;
                goto End;
            }

            string zh_cn = "请把NFC读卡器靠近NFC标签";
            string en_us = "please put NFC reader near NFC-Tag.";
            string thai = "null";

            bool bOpStep = false;
            Invoke(new Action(() =>
            {
                frmTips dialog = new frmTips(zh_cn, en_us, thai);
                DialogResult result = dialog.ShowDialog(this);
                bOpStep = (result == DialogResult.OK);
            }));

            if (!bOpStep)
            {
                value = "OP_PUT_LINER_FAIL";
                rlt = false;
                goto End;
            }


            byte[] devno = new byte[4];

            if (NFC.pcdgetdevicenumber(devno) == 0)
            {
                NFC.pcdbeep(50);
                AppendRuntimeLog($"NFC device number: {Convert.ToString(devno[0])} - {Convert.ToString(devno[1])} - {Convert.ToString(devno[2])} - {Convert.ToString(devno[3])}");
            }
            else
            {
                AppendRuntimeLog("did find NFC reader");
                value = "No_NFC_Reader_Device";
                rlt = false;
                goto End;
            }
            string str1 = "", str2 = "", str3 = "";
            string statusCode = "";
            bool rootState, bmpState;

            str1 = NFC.ReadCard(0, NFC.Key(1), ref statusCode);
            if (!string.IsNullOrWhiteSpace(str1))
            {
                if (str1.Length > 2)
                {
                    AppendRuntimeLog("读取Sector0数据为：\r\n" + str1);
                    rootState = true;
                }
            }

            str2 = NFC.ReadCard(1, NFC.Key(2), ref statusCode);
            if (!string.IsNullOrWhiteSpace(str2))
            {
                if (str2.Length > 2)
                {
                    AppendRuntimeLog("读取Sector1数据为：\r\n" + str2);
                    bmpState = true;
                }
            }

            str3 = NFC.ReadCard(2, NFC.Key(2), ref statusCode);
            if (!string.IsNullOrWhiteSpace(str3))
            {
                if (str3.Length > 2)
                {
                    AppendRuntimeLog("读取Sector2数据为：\r\n" + str3);
                }
            }

            if (str1 == "")
            {
                value = "NFC_SECTOR_O_EMPTY";
                rlt = false;
                goto End;
            }

            if (str2 == "")
            {
                value = "NFC_SECTOR_1_EMPTY";
                rlt = false;
                goto End;
            }

            if (str3 == "")
            {
                value = "NFC_SECTOR_2_EMPTY";
                rlt = false;
                goto End;
            }

            string strTemp = str1.Replace(" ", "").Replace("\r", "").Replace("\n", "") + str2.Replace(" ", "").Replace("\r", "").Replace("\n", "") + str3.Replace(" ", "").Replace("\r", "").Replace("\n", "");

            if (strTemp.Length != 288)
            {
                value = "NFC_CONTENT_LENGTH_ERROR";
                rlt = false;
                goto End;
            }

            string macNfc = strTemp.Substring(178, 12);        //获取固定位置的mac(需反转)

            macNfc = reserveMac(macNfc);        //反转后加冒号:

            if (macNfc != BLUETOOTH_MAC_ADDRESS)
            {
                rlt = false;
                value = $"MAC_DISMATCH({BLUETOOTH_MAC_ADDRESS}/{macNfc})";
                goto End;
            }

            rlt = true;
            value = BLUETOOTH_MAC_ADDRESS;

        End:
            item.Value = value;
            return rlt;
        }

        #endregion

        #region ---lts sensor implement---

        private bool Test_LTSSensor_PowerOn(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "";
            string devicePortName = "";

            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_0_ON_NG";
                goto End;
            }

            Delay(3000);

            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_1_ON_NG";
                goto End;
            }

            Prod.LTSSensor m_product = product as Prod.LTSSensor;

            if (m_product == null)
            {
                rlt = false;
                value = "PRODUCT_TPYE_ERROR";
                goto End;
            }

            Type m_type = dut.GetType();

            if (m_type.Name != "LTSSensor")
            {
                rlt = false;
                value = "DUT_TPYE_ERROR";
                goto End;
            }

            DUTS.LTSSensor.LTSSensor m_dut = dut as DUTS.LTSSensor.LTSSensor;

            for (int i = 0; i < 200; i++)
            {
                devicePortName = WinDevice.GetSerialportComPort(m_dut.VID, m_dut.PID);
                if (devicePortName != "COM1" &&
                    !string.IsNullOrEmpty(devicePortName) &&
                    !string.IsNullOrWhiteSpace(devicePortName))
                {
                    AppendRuntimeLog($"count: {i}, detected device: {devicePortName} [vid({m_dut.VID}), pid({m_dut.PID})].");

                    rlt = true;
                    bTimeOut = false;
                    break;
                }

                AppendRuntimeLog($"count: {i}, detecting device [vid({m_dut.VID}), pid({m_dut.PID})].");
                Delay(1000);
            }

            if (bTimeOut)
            {
                rlt = false;
                value = "TimeOut";
                goto End;
            }

            Delay(20000);

            value = rlt ? $"PASS / {devicePortName}" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_LTSSensor_Func(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true, bFinddevice = false;
            string value = "", devicePortName = "";
            string val_temp = "";
            string zh_cn = "", en_us = "", thai = "";

            DUTS.LTSSensor.LTSSensor m_dut = dut as DUTS.LTSSensor.LTSSensor;

            for (int i = 0; i < 200; i++)
            {
                devicePortName = WinDevice.GetSerialportComPort(m_dut.VID, m_dut.PID);
                if (devicePortName != "COM1" &&
                    !string.IsNullOrEmpty(devicePortName) &&
                    !string.IsNullOrWhiteSpace(devicePortName))
                {
                    AppendRuntimeLog($"current times is{i}, detected device: {devicePortName} [vid({m_dut.VID}), pid({m_dut.PID})].");

                    //rlt = true;
                    bFinddevice = true;
                    break;
                }
                AppendRuntimeLog($"current times is{i}, detecting device [vid({m_dut.VID}), pid({m_dut.PID})].");
                Delay(1000);
            }

            if (!bFinddevice)
            {
                rlt = false;
                value = "CANNOT_FIND_DEVICE";
                goto End;
            }

            if (!InitializeSerialport(m_dut.VID, m_dut.PID, spTester))
            {
                rlt = false;
                value = "CANNOT_OPEN_DEVICE";
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            if (item.Name.Contains("No Media"))
            {
                zh_cn = "移除LTS Sensor上面的所有媒介.";
                en_us = "remove all media on the LSS sensor.";
                thai = "null";
            }
            else
            {
                zh_cn = "请在LTS Sensor上面放置媒介.";
                en_us = "put media on the LSS sensor.";
                thai = "null";
            }

            this.Invoke(new Action(() =>
            {
                frmTips dialog = new frmTips(zh_cn, en_us, thai);
                DialogResult result = dialog.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    SerialPortWriteLineOnce(PrinterCommands.LTS);
                    for (int i = 0; i < 200; i++)
                    {
                        val_temp = GetRuntimeStringFlagA(item.Name);
                        if (!string.IsNullOrEmpty(val_temp) && !string.IsNullOrWhiteSpace(val_temp))
                        {
                            bTimeOut = false;
                            break;
                        }

                        Delay(500);
                    }

                    if (bTimeOut)
                    {
                        value = "TimeOut";
                        rlt = false;
                    }
                    else
                    {
                        if (val_temp.Contains(Test_Proc_Error_Code.HW_NOT_INSTALL.ToString()))
                        {
                            value = $"{Test_Proc_Error_Code.HW_NOT_INSTALL}";
                            rlt = false;
                        }
                        else
                        {
                            value = GetSensorValue(val_temp);
                            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                            {
                                value = "-1";
                            }
                        }
                    }
                }
                else
                {
                    rlt = false;
                    value = "FAIL";
                }
            }));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        #endregion

        #region---mainboard fct---

        private bool Test_MB_FCT_PowerOn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            if (!RelayCard1.SetRelayOn(0))
            {
                value = "REALAY1_0_ON_FAIL";
                rlt = false;
                goto End;
            }

            Delay(500);

            if (!RelayCard1.SetRelayOn(1))
            {
                value = "REALAY1_1_ON_FAIL";
                rlt = false;
                goto End;
            }

            rlt = true;
            value = "PASS";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_SystemReady(int index, TestItem item)
        {
            bool rlt = false, bFlag = false, bLoginStatus = false;
            string value = "", tmpValue = "";

            for (int i = 0; i < 200; i++)
            {
                tmpValue = ReadSerialportDataReceived();

                if (tmpValue.Contains("INTERMEC_$123$ login:"))
                {
                    bFlag = true;
                    break;
                }

                Delay(500);
            }

            if (!bFlag)
            {
                value = "FIND_DEVICE_TIMEOUT";
                rlt = false;
                goto End;
            }

            /*
            Delay(Convert.ToInt32(INI.GetValue("Delays", "root", ConfigFile)));

            SerialPortWriteLineOnce1(spDutDebugPort, "root");
            SerialPortWriteLineOnce1(spDutDebugPort, "f!j!r0cks");

            for (int i = 0; i < 200; i++)
            {
                tmpValue = ReadSerialportDataReceived();

                if (tmpValue.Contains("root@INTERMEC_$123$ ~$"))
                {
                    bLoginStatus = true;
                    break;
                }

                Delay(500);
            }

            if (!bLoginStatus)
            {
                value = "LOGIN_SYSTEM_TIMEOUT";
                rlt = false;
                goto End;
            }
            */

            Delay(Convert.ToInt32(INI.GetValue("BootupTime", "Time", ConfigFile)));
            //SerialPortWriteLineOnce1(spDutDebugPort, PrinterCommands.SetUpSleepTimer_0);
            DUTS.PA45_MainBoard.PA45_MainBoard m_dut = dut as DUTS.PA45_MainBoard.PA45_MainBoard;

            if (m_dut == null)
            {
                value = "ERROR_DUT_TYPE";
                rlt = false;
                goto End;
            }

            if (!InitializeSerialport(m_dut.VID, m_dut.PID, spTester))
            {
                value = "COMMUNICATE_COMPORT_ERROR";
                rlt = false;
                goto End;
            }

            rlt = true;
            value = "PASS";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_CoinBattery(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(0));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VSYS_CHG(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (!item.Name.Contains("Voltage Test"))
            {
                RelayCard1.SetRelayOn(2);
                Delay(100);
                value = Convert.ToString(RelayCard1.MeasureVoltage(0) * 5.6);
                RelayCard1.SetRelayOff(2);
            }
            else
            {
                Delay(100);
                value = Convert.ToString(RelayCard1.MeasureVoltage(0));
            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_MCU(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(1));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_MAIN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(2));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VDD_SNV3_CAP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(3));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_INT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(4));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_DRAM_1V35(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(5));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V1P8_INT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(6));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V1P35_SOC(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (item.Name.Contains("Normal"))
            {
                value = Convert.ToString(CaptureCard1.MeasureVoltage(7) / 0.95);

            }
            else
            {
                value = Convert.ToString(CaptureCard1.MeasureVoltage(8));

            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }


        private bool Test_MB_FCT_VDD_HIGH_CAP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(9) * 2.29);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_NVCC_PLL_OUT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(10));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }


        private bool Test_MB_FCT_V5P0(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard1.MeasureVoltage(10) * 1.39);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_AUX(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(11));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_LTS(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(12));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_LPT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(13) * 1.29);
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_WIFI(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(14));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V1P8_WIFI(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(15));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V5P0_WWAN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard1.SetRelayOn(3);
            Delay(100);
            value = Convert.ToString(CaptureCard1.MeasureVoltage(15));
            RelayCard1.SetRelayOff(3);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_AP_READY_1V8(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (item.Name.Contains("H"))
            {
                //H

            }
            else
            {
                //L
            }


            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(0));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P8_WWAN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(1));


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Cellular(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(CaptureCard1.MeasureVoltage(1));


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_DRAM_VREF(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(2));


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LED1(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(3));


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LED2(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(4));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LED3(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(5));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LED4_FullCharged(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(RelayCard1.MeasureVoltage(3));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_DC_IN_ChargerCurrent(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(3));


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_RctSet(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string currentTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            SerialPortWriteLineOnce($"functest RTC {currentTime}");

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_LCD(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard2.MeasureVoltage(7));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_J19_LCD_LEDA(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard2.MeasureVoltage(8) * 2.42);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VBATT1_SENSE(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard2.MeasureVoltage(7));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Buzzer_Control(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard2.MeasureVoltage(9));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_USB_HUB_HOST_J5(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard2.MeasureVoltage(10));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LTPPWR_TPH(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (item.Name.ToUpper().Contains("ON"))
            {
                RelayCard2.SetRelayOn(3);
                RelayCard2.SetRelayOn(4);
                Delay(1000);
                value = Convert.ToString(RelayCard1.MeasureVoltage(5));
                RelayCard2.SetRelayOff(3);
                RelayCard2.SetRelayOff(4);
            }
            else
            {
                RelayCard2.SetRelayOn(5);
                Delay(1000);
                value = Convert.ToString(RelayCard1.MeasureVoltage(5));
                RelayCard2.SetRelayOff(5);
            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }


        private bool Test_MB_FCT_RFID_Power(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = "-1";

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V24P0_MOTOR(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard1.MeasureVoltage(6));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }



        private bool Test_MB_FCT_PLATEN_MOTOR(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (item.Name.Contains("J14"))
            {
                RelayCard2.SetRelayOn(7);
                Delay(1000);
                value = Convert.ToString(-1);
                RelayCard2.SetRelayOff(7);
            }
            else
            {
                RelayCard2.SetRelayOn(6);
                Delay(1000);
                value = Convert.ToString(-1);
                RelayCard2.SetRelayOff(6);
            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }
        private bool Test_MB_FCT_STM_VREF(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard2.MeasureVoltage(11));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_AMBIENT_TEMP_SENSE(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(-1);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }


        private bool Test_MB_FCT_MOTOR_TEMP_SENSE(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(-1);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_PoweroffVBATT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard1.MeasureVoltage(7));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VBUS_IN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard2.MeasureVoltage(0) * 5);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LOWPWR_IRQ_N(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (item.Name.Contains("=6.5"))
            {
                value = Convert.ToString(RelayCard2.MeasureVoltage(1));
            }
            else
            {
                value = Convert.ToString(RelayCard2.MeasureVoltage(2));
            }


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_N17945044(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard2.MeasureVoltage(2));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_BatteryPowerSupply(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard2.MeasureVoltage(3));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_SUPER_CAP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard2.MeasureVoltage(3));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }


        private bool Test_MB_FCT_HOTSWAP_V5P2_CAP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard2.MeasureVoltage(2));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        #endregion

        private bool default_placeholder_function(int index, TestItem item)
        {
            bool rlt = false;
            string value = "ATE_NOT_IMPLEMENT_YET";
            item.Value = value;
            return rlt;
        }

        #region trigger ATE test thread

        private void StartTest()
        {
            if (_trigger != null && _trigger.IsAlive)
            {
                MessageBox.Show("_trigger thread is already running.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var start = new ThreadStart(delegate { go_through_testing_process(); });
            _trigger = new Thread(start);
            _trigger.Start();
        }

        private void OnTested(object sender, TestedEventArgs e)
        {
            string message = string.Format($"{e._testItem.Name} test value is {e._testItem.Value} ( {e._testItem.Description} )");
            AppendRuntimeLog(message);
        }

        private void OnTestFinished(object sender, TestFinishedEventArgs e)
        {
            string message = string.Format($"{e._testItem.Name} test result is {e._testItem.Result} ");
            AppendRuntimeLog(message);
        }

        private void go_through_testing_process()
        {
            try
            {
                DUT_SERIAL_PORT_STATUS = false;
                SetTestEntery(false);
                Invoke(new Action(() => { lblStartTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); }));

                Type m_type = dut.GetType();

                if (m_type.Name == "USB2ETH" ||
                    m_type.Name == "PA45_KeyBoard" ||
                    m_type.Name == "BT_Module" ||
                    m_type.Name == "WiFi_BT_Module" ||
                    m_type.Name == "TopSensor" ||
                    m_type.Name == "LTSSensor" ||
                    m_type.Name == "PA45_PowerBoard")
                {
                    DUT_SERIAL_PORT_STATUS = true;

                    if ((m_type.Name == "PA45_PowerBoard") && productConfig.STATION_CODE == "FCT")
                    {
                        // open KEITHLEY_2306_LAN
                    }
                    goto TEST;
                }

                if (m_type.Name == "PA45_MainBoard" && productConfig.STATION_CODE.Contains("FW Flash"))
                {
                    DUT_SERIAL_PORT_STATUS = true;
                    goto TEST;
                }

                if (m_type.Name == "PA45_MainBoard" && productConfig.STATION_CODE.Contains("FCT"))
                {
                    DUT_SERIAL_PORT_STATUS = true;
                    goto TEST;
                }

                if (!DUT_SERIAL_PORT_STATUS)
                {
                    if (m_type.Name == "PA45")
                    {
                        DUTS.PA45.PA45 m_PA45 = dut as DUTS.PA45.PA45;
                        DUT_SERIAL_PORT_STATUS = InitializeSerialport(m_PA45.VID, m_PA45.PID, spTester);
                    }

                    else if (m_type.Name == "PA45_MainBoard")
                    {
                        DUTS.PA45_MainBoard.PA45_MainBoard m_PA45_MainBoard = dut as DUTS.PA45_MainBoard.PA45_MainBoard;
                        DUT_SERIAL_PORT_STATUS = InitializeSerialport(m_PA45_MainBoard.VID, m_PA45_MainBoard.PID, spTester);
                    }

                    else if (m_type.Name == "QBC")
                    {
                        DUTS.QBC.QBC m_QBC = dut as DUTS.QBC.QBC;
                        DUT_SERIAL_PORT_STATUS = InitializeSerialport(m_QBC.VID, m_QBC.PID, spTester);
                    }
                    else if (m_type.Name == "USB2ETH")
                    {
                        DUTS.USB2ETH.USB2ETH m_USB2ETH = dut as DUTS.USB2ETH.USB2ETH;
                        DUT_SERIAL_PORT_STATUS = InitializeSerialport(m_USB2ETH.VID, m_USB2ETH.PID, spTester);
                    }
                    else if (m_type.Name == "CUP")
                    {
                        DUTS.QBC.QBC m_QBC = dut as DUTS.QBC.QBC;
                        DUT_SERIAL_PORT_STATUS = InitializeSerialport(m_QBC.VID, m_QBC.PID, spTester);
                    }
                    else
                    {
                        // connect again.
                        DUT_SERIAL_PORT_STATUS = InitializeSerialport(TESTER_VID, TESTER_PID, spTester);
                    }
                }

            TEST:
                if (DUT_SERIAL_PORT_STATUS)
                {
                    dura_list.Clear();

                    //自动丢弃小数点部分
                    int process_bar_unit = 1000 / dut.TestItems.Count;

                    Invoke(new Action(() => { pb_test_thread.Maximum = 1000; }));

                    for (int i = 0; i < dut.TestItems.Count; i++)
                    {
                        TestItem tmpItem = dut.TestItems[i];
                        tmpItem.Tested += OnTested;
                        tmpItem.TestFinished += OnTestFinished;

                        if (dut.TestMethod != null) { dut.TestItems[i] = dut.TestMethod.Invoke(i, tmpItem); }

                        Invoke(new Action(() => { pb_test_thread.Value = (i == dut.TestItems.Count - 1) ? 1000 : process_bar_unit * (i + 1); }));

                        //deviceUnderTestInstance.TestItems[i] = test_and_display_each_item(i, temp_item);
                        dura_list.Add(tmpItem.Duration);
                        tmpItem.Tested -= OnTested;
                        tmpItem.TestFinished -= OnTestFinished;

                        if (ckbFailStop.Checked)
                        {
                            if (!dut.TestItems[i].Result) { break; }
                        }
                    }
                }
                else
                {
                    UpdateErrorUI(Test_Proc_Error_Code.NoSeriPort);
                }

                Invoke(new Action(() => { lblFinishTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); }));
            }

            catch (Exception e)
            {
                Invoke(new Action(() =>
                {
                    lblFinishTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    Status_Label.Text = " ` " + e.Message + " ` ";
                    AppendRuntimeLog(string.Format($"catch exception in FUNC [ {MethodBase.GetCurrentMethod().Name} ] as : \n") + e.Message);
                }));
                //return;
            }

            finally
            {
                Invoke(new Action(() =>
                {
                    UpdateResultUI(dut.FinalResult);

                    if (dura_list != null)
                    {
                        TimeSpan total = dura_list.Aggregate(TimeSpan.Zero, (sum, next) => sum + next);
                        lblDuration.Text = $"{total.Hours.ToString("D2")} : {total.Minutes.ToString("D2")} : {total.Seconds.ToString("D2")}";
                    }

                    DataAccess.SaveTxtLog(dut, rtbLog, rtbUSB, rtbSerialport);
                    DataAccess.SaveCsvLog(dut, MainboardPartNumber);

                    if (ckbSFCS.Checked)
                    {
                        Prod.PA45 m_PA45 = product as Prod.PA45;
                        string m_sn = "";
                        string m_sn1 = "";
                        if (m_PA45 != null)
                        {
                            m_sn = m_PA45.HON_PN + ";" + m_PA45.SerialNumber;
                            string m_pcba_seri_num = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE", m_sn);
                            m_sn1 = m_pcba_seri_num;
                            AppendRuntimeLog($"{m_sn} get main barcode is {m_pcba_seri_num}");
                            m_sn = m_pcba_seri_num;
                        }
                        else
                        {
                            m_sn = dut.Product.SerialNumber;
                        }


                        string columnList = "Line_Code,Station_Code,PROCESS_NO,BARCODE,Result,Defect_Code,MODEL_NO,PC_NAME";
                        string valueList = $"{dut.Product.ProductConfig.LINE_CODE},{dut.Product.ProductConfig.STATION_CODE},{dut.Product.ProductConfig.STATION_CODE},{MainBarcode},{lblResult.Text},{""},{dut.Product.ProductConfig.MODEL_NO},{Dns.GetHostName()}";

                        for (int i = 0; i < dut.TestItems.Count; i++)
                        {
                            columnList = columnList + "," + dut.TestItems[i].Name;
                            valueList = valueList + "," + dut.TestItems[i].Value;
                        }

                        Type m_type = dut.GetType();
                        string mModel = "";

                        if (productConfig.DATABASE.ToUpper() == "SCANNER") { mModel = "Scanner"; }

                        string rltSFC = _ssg_sfc.InsertIntoTable(
                            mModel,
                            dut.Product.ProductConfig.TABLE,
                            columnList,
                            valueList);
                        AppendRuntimeLog($"SN: {m_sn}, insertintotable: {rltSFC}");
                        if (rltSFC != "Y")
                        {
                            MessageBox.Show($"Exception: {rltSFC} ", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            UpdateErrorUI(Test_Proc_Error_Code.SfcInsertNG);
                        }
                        AppendRuntimeLog("");
                    }

                    ClearUI();

                    WIFI_MAC_ADDRESS = "";
                    BLUETOOTH_MAC_ADDRESS = "";
                    PrinterPartNumber = "";
                    PrinterSeriNumber = "";

                    MainboardPartNumber = "";
                    MainboardSeriNumber = "";

                    spTester.Close();
                    spDutDebugPort.Close();

                    CloseAllRelays();

                    product.RunTimeLogUpdated -= OnRunTimeLogUpdated;
                    product = null;

                    dut.Product.RunTimeLogUpdated -= OnRunTimeLogUpdated;
                    dut.Product = null;
                    dut.RunTimeLogUpdated -= OnRunTimeLogUpdated;
                    dut = null;

                    _trigger = null;

                    SetTestEntery(true);
                    GC.Collect();
                }));
            }
        }

        private TestItem test_and_display_each_item(int index, TestItem item)
        {
            Invoke(new Action(() => { lvTestItems.Items[index].BackColor = Color.LightBlue; }));
            DateTime dt_start = DateTime.Now;
            AppendRuntimeLog(item.Name, true);
            switch (item.Name)
            {
                #region --- Printer ---

                case "Battery Charing by E-Charging":
                    item.Result = Test_Printer_BatteryCharingByE_Charging(index, item);
                    break;

                case "Turn Off Sleep Mode":
                    item.Result = Test_Printer_TurnOffSleepMode(index, item);
                    break;

                case "FW Version":
                    item.Result = Test_Printer_FwVersion(index, item);
                    break;

                case "U-Boot Version":
                    item.Result = Test_Printer_UbootVersion(index, item);
                    break;

                case "Set RTC":
                    item.Result = Test_Printer_SetRtc(index, item);
                    break;

                case "Check RTC":
                    item.Result = Test_Printer_CheckRtc(index, item);
                    break;

                case "Mac Address Burn-in":
                    item.Result = Test_MacAddressBurnIn(index, item);
                    break;

                case "Write Printer SN":
                    item.Result = Test_WritePrinterSn(index, item);
                    break;

                case "Get TPH ID":
                    item.Result = Test_GetTphId(index, item);
                    break;

                case "TPH Resolution":
                    item.Result = Test_TphResolution(index, item);
                    break;

                case "TPH Temperature":
                    item.Result = Test_Printer_SensorTemp(index, item);
                    break;

                case "TPH Ohm":
                    item.Result = Test_Printer_TPHOhm(index, item);
                    break;

                case "Motor Temperature":
                    item.Result = Test_Printer_SensorTemp(index, item);
                    break;

                case "Ambient Temperature":
                    item.Result = Test_Printer_SensorTemp(index, item);
                    break;

                case "LCD":
                    item.Result = Test_Printer_LCDScreen(index, item);
                    break;

                case "Cover-in Switch Sensor":
                    item.Result = Test_Printer_CoverInSwichSensor(index, item);
                    break;

                case "Top BM Sensor Test":
                    item.Result = Test_Printer_BMSensor(index, item);
                    break;

                case "Bottom BM Sensor Test":
                    item.Result = Test_Printer_BMSensor(index, item);
                    break;

                case "LSS Sensor Test(Media)":
                    item.Result = Test_LssSensor(index, item);
                    break;

                case "LSS Sensor Test(Liner)":
                    item.Result = Test_LssSensor(index, item);
                    break;

                case "LSS Sensor Test(No Media)":
                    item.Result = Test_LssSensor(index, item);
                    break;

                case "LSS Sensor Delta":
                    item.Result = Test_LssSensor(index, item);
                    break;

                case "Key Button":
                    item.Result = Test_Printer_KeyButton(index, item);
                    break;

                case "Speaker(Buzzer) Test":
                    item.Result = Test_Printer_SpeakerBuzzer(index, item);
                    break;

                case "Host USB":
                    item.Result = Test_HostUsb(index, item);
                    break;

                case "Host USB1":
                    item.Result = Test_HostUsb(index, item);
                    break;

                case "LTS (Cutter Sensor)":
                    item.Result = Test_Printer_LTS(index, item);
                    break;

                case "SELFSTRIP(Peeler)":
                    item.Result = Test_Printer_SELFSTRIP(index, item);
                    break;

                case "BlueTooth ID":
                    item.Result = Test_Printer_BlueToothID(index, item);
                    break;

                case "BlueTooth(Set Device Name)":
                    item.Result = Test_BlueTooth_set(index, item);
                    break;

                /*
            case "BlueTooth(Check Device Name)":
                item.Result = Test_BlueTooth_check(index, item);
                break;
                */


                case "Wifi(Ping Router)":
                    item.Result = Test_Printer_PingWifiRouter(index, item);
                    break;

                case "Find Printer(Bluetooth)":
                    item.Result = Test_Printer_FindBluetoothDevice(index, item);
                    break;

                case "RFID":
                    item.Result = Test_Printer_RFID(index, item);
                    break;

                case "Blue Tooth(Print)":
                    item.Result = Test_PrintBluetoothAddress(index, item);
                    break;

                case "Wifi ID":
                    item.Result = Test_Printer_WifiID(index, item);
                    break;

                case "Wifi(Print)":
                    item.Result = Test_PrintWifiAddress(index, item);
                    break;

                case "Chess Test(Pattern)":
                    item.Result = Test_ChessPattern(index, item);
                    break;

                case "Ruler Test(Pattern)":
                    item.Result = Test_ChessPattern(index, item);
                    break;

                case "Barcode Test":
                    item.Result = Test_Printer_Barcode(index, item);
                    break;

                case "Motion Sensor":
                    item.Result = Test_Printer_MotionSensor1(index, item);
                    break;

                case "Paper Feed and Get Media Length":
                    item.Result = Test_Printer_PaperFeedGetMediaLength(index, item);
                    break;

                case "Battery Test":
                    item.Result = Test_Printer_Battery(index, item);
                    break;

                case "Check Printer SN":
                    item.Result = Test_CheckPrinterSn(index, item);
                    break;

                case "Check Mainboard":
                    item.Result = Test_CheckPcba(index, item);
                    break;

                case "Recovery Setting":
                    item.Result = Test_Printer_RecoverySetting(index, item);
                    break;

                case "Check Device Locking Status":
                    item.Result = Test_Printer_CheckLockingStatus(index, item);
                    break;

                #endregion

                #region --- Mainboard FCT ---

                case "Power On(MB FCT)":
                    item.Result = Test_MB_FCT_PowerOn(index, item);
                    break;

                case "FW Version(MB FCT)":
                    item.Result = Test_Printer_FwVersion(index, item);
                    break;

                case "System Ready(MB FCT)":
                    item.Result = Test_MB_FCT_SystemReady(index, item);
                    break;

                case "Comm J1":
                    //item.Result = Test_MB_FCT_CommJ1(index, item);
                    break;

                case "Coin Battery":
                    item.Result = Test_MB_FCT_CoinBattery(index, item);
                    break;

                case "RTC Battery":
                    item.Result = Test_MB_FCT_CoinBattery(index, item);
                    break;

                case "VSYS_CHG":
                    item.Result = Test_MB_FCT_VSYS_CHG(index, item);
                    break;

                case "VSYS_CHG(Voltage Test)":
                    item.Result = Test_MB_FCT_VSYS_CHG(index, item);
                    break;

                case "V3P3_MCU":
                    item.Result = Test_MB_FCT_V3P3_MCU(index, item);
                    break;

                case "V3P3_MAIN":
                    item.Result = Test_MB_FCT_V3P3_MAIN(index, item);
                    break;

                case "VDD_SNV3_CAP":
                    item.Result = Test_MB_FCT_VDD_SNV3_CAP(index, item);
                    break;

                case "V3P3_INT":
                    item.Result = Test_MB_FCT_V3P3_INT(index, item);
                    break;

                case "DRAM_1V35":
                    item.Result = Test_MB_FCT_DRAM_1V35(index, item);
                    break;

                case "V1P8_INT":
                    item.Result = Test_MB_FCT_V1P8_INT(index, item);
                    break;

                case "V1P35_SOC":
                    item.Result = Test_MB_FCT_V1P35_SOC(index, item);
                    break;

                case "V1P35_SOC  Normal":
                    item.Result = Test_MB_FCT_V1P35_SOC(index, item);
                    break;

                case "VDD_HIGH_CAP":
                    item.Result = Test_MB_FCT_VDD_HIGH_CAP(index, item);
                    break;

                case "NVCC_PLL_OUT":
                    item.Result = Test_MB_FCT_NVCC_PLL_OUT(index, item);
                    break;

                case "V5P0":
                    item.Result = Test_MB_FCT_V5P0(index, item);
                    break;

                case "V3P3_AUX":
                    item.Result = Test_MB_FCT_V3P3_AUX(index, item);
                    break;

                case "V3P3_LTS":
                    item.Result = Test_MB_FCT_V3P3_LTS(index, item);
                    break;

                case "V3P3_LPT":
                    item.Result = Test_MB_FCT_V3P3_LPT(index, item);
                    break;

                case "V3P3_WIFI":
                    item.Result = Test_MB_FCT_V3P3_WIFI(index, item);
                    break;

                case "V1P8_WIFI":
                    item.Result = Test_MB_FCT_V1P8_WIFI(index, item);
                    break;

                case "V5P0_WWAN":
                    item.Result = Test_MB_FCT_V5P0_WWAN(index, item);
                    break;

                case "AP_READY_1V8(L)":
                    item.Result = Test_MB_FCT_AP_READY_1V8(index, item);
                    break;

                case "AP_READY_1V8(H)":
                    item.Result = Test_MB_FCT_AP_READY_1V8(index, item);
                    break;

                case "V3P8_WWAN":
                    item.Result = Test_MB_FCT_V3P8_WWAN(index, item);
                    break;

                case "DRAM_VREF":
                    item.Result = Test_MB_FCT_DRAM_VREF(index, item);
                    break;

                case "LED1(SOC_DEBUG_LED)":
                    item.Result = Test_MB_FCT_LED1(index, item);
                    break;

                case "LED2(MCU P13)":
                    item.Result = Test_MB_FCT_LED2(index, item);
                    break;

                case "LED3(LED VF)":
                    item.Result = Test_MB_FCT_LED3(index, item);
                    break;

                case "DC_IN Charger Current":
                    item.Result = Test_MB_FCT_DC_IN_ChargerCurrent(index, item);
                    break;

                case "LED4(charging)":
                    item.Result = Test_MB_FCT_LED4_FullCharged(index, item);
                    break;

                case "LED4(full charged)":
                    item.Result = Test_MB_FCT_LED4_FullCharged(index, item);
                    break;

                case "RTC Test(MB FCT)":
                    item.Result = Test_Printer_SetRtc(index, item);
                    break;

                case "VBATT1 SENSE":
                    item.Result = Test_MB_FCT_VBATT1_SENSE(index, item);
                    break;

                case "AMBIENT TEMP SENSE":
                    item.Result = Test_MB_FCT_AMBIENT_TEMP_SENSE(index, item);
                    break;

                case "MOTOR TEMP SENSE":
                    item.Result = Test_MB_FCT_MOTOR_TEMP_SENSE(index, item);
                    break;

                case "V3P3_LCD":
                    item.Result = Test_MB_FCT_V3P3_LCD(index, item);
                    break;

                case "LCD(J19)(LEDA)":
                    item.Result = Test_MB_FCT_J19_LCD_LEDA(index, item);
                    break;

                case "Buzzer Control":
                    item.Result = Test_MB_FCT_Buzzer_Control(index, item);
                    break;

                case "RFID(Power)":
                    item.Result = Test_MB_FCT_RFID_Power(index, item);
                    break;

                case "USB HUB (USB HOST)(J5)":
                    item.Result = Test_MB_FCT_USB_HUB_HOST_J5(index, item);
                    break;

                case "LTPPWR(TPH)(ON)":
                    item.Result = Test_MB_FCT_LTPPWR_TPH(index, item);
                    break;

                case "LTPPWR(TPH)(OFF)":
                    item.Result = Test_MB_FCT_LTPPWR_TPH(index, item);
                    break;

                case "V24P0_MOTOR":
                    item.Result = Test_MB_FCT_V24P0_MOTOR(index, item);
                    break;

                case "PLATEN MOTOR":
                    item.Result = Test_MB_FCT_PLATEN_MOTOR(index, item);
                    break;

                case "PLATEN MOTOR(J14)":
                    item.Result = Test_MB_FCT_PLATEN_MOTOR(index, item);
                    break;

                case "STM VREF":
                    item.Result = Test_MB_FCT_STM_VREF(index, item);
                    break;

                case "Power off(VBATT)":
                    item.Result = Test_MB_FCT_PoweroffVBATT(index, item);
                    break;

                case "VBUS_IN(MB FCT)(voltage test)":
                    item.Result = Test_MB_FCT_VBUS_IN(index, item);
                    break;

                case "LOWPWR_IRQ_N(=6.5V)":
                    item.Result = Test_MB_FCT_LOWPWR_IRQ_N(index, item);
                    break;

                case "LOWPWR_IRQ_N(<6.5V)":
                    item.Result = Test_MB_FCT_LOWPWR_IRQ_N(index, item);
                    break;

                case "N17945044":
                    item.Result = Test_MB_FCT_N17945044(index, item);
                    break;

                case "Battery(power supply)(VSYS)":
                    item.Result = Test_MB_FCT_BatteryPowerSupply(index, item);
                    break;

                case "SUPER CAP(VSYS)":
                    item.Result = Test_MB_FCT_SUPER_CAP(index, item);
                    break;

                case "Hotswap(V5P2_CAP)":
                    item.Result = Test_MB_FCT_HOTSWAP_V5P2_CAP(index, item);
                    break;

                case "Uboot Version(MB FCT)":
                    item.Result = Test_Printer_FwVersion(index, item);
                    break;



                case "LTS Sensor Funcrion":
                    item.Result = Test_Printer_LTS(index, item);
                    break;


                case "Battery Function":
                    item.Result = Test_Printer_Battery(index, item);
                    break;



                case "Security Chip":
                    item.Result = Test_Printer_SecurityChip(index, item);
                    break;

                case "Paper Feeding":
                    item.Result = Test_Printer_PaperFeed(index, item);
                    break;

                case "PrintHead Function":
                    item.Result = Test_TphResolution(index, item);
                    break;

                case "Gap Media Length":
                    item.Result = Test_Printer_PaperFeedGetMediaLength(index, item);
                    break;

                case "USB Print Function":
                    item.Result = Test_Printer_Barcode(index, item);
                    break;

                #endregion

                #region--- MB EOL ---

                case "UBoot Version(MB EOL)":
                    item.Result = Test_Printer_UbootVersion(index, item);
                    break;

                case "FW Version(MB EOL)":
                    item.Result = Test_Printer_FwVersion(index, item);
                    break;

                case "BM Sensor ADC Value(MB EOL)":
                    item.Result = Test_Printer_BMSensor(index, item);
                    break;

                case "BM Sensor ADC Value(Front)(MB EOL)":
                    item.Result = Test_Printer_BMSensor(index, item);
                    break;

                case "LTS sensor function(MB EOL)":
                    item.Result = Test_Printer_LTS(index, item);
                    break;

                case "PrintCover Sensor(MB EOL)":
                    item.Result = Test_Printer_CoverInSwichSensor(index, item);
                    break;

                case "Battery Function(MB EOL)":
                    item.Result = Test_Printer_Battery(index, item);
                    break;

                case "Wifi Module ID(MB EOL)":
                    item.Result = Test_Printer_WifiID(index, item);
                    break;

                case "BT Module ID(MB EOL)":
                    item.Result = Test_Printer_BlueToothID(index, item);
                    break;

                case "RTC Function(MB EOL)":
                    item.Result = Test_Printer_SetRtc(index, item);
                    break;

                case "AUDIO Function(MB EOL)":
                    item.Result = Test_Printer_SpeakerBuzzer(index, item);
                    break;

                case "LED Function(MB EOL)":
                    item.Result = Test_Printer_LEDLight(index, item);
                    break;

                case "LED":
                    item.Result = Test_Printer_LEDLight(index, item);
                    break;

                case "Keypad Function(MB EOL)":
                    item.Result = Test_Printer_KeyButton(index, item);
                    break;

                case "Motion Sensor Function(MB EOL)":
                    item.Result = Test_Printer_MotionSensor(index, item);
                    break;

                case "Charger IC Function(MB EOL)":
                    item.Result = Test_Printer_ChargerIC(index, item);
                    break;

                case "LCD Function(MB EOL)":
                    item.Result = Test_Printer_LCDScreen(index, item);
                    break;

                case "Paper Feed(MB EOL)":
                    item.Result = Test_Printer_PaperFeed(index, item);
                    break;

                case "Print Cover Sensor Function":
                    item.Result = Test_Printer_CoverInSwichSensor(index, item);
                    break;


                #endregion

                #region --- Mainboard FW Flash ---

                case "Power On(mianboard FW Flash)":
                    item.Result = Test_MainBoard_PowerOn(index, item);
                    break;

                case "RePower On(mianboard FW Flash)":
                    item.Result = Test_MainBoard_RePowerOn(index, item);
                    break;

                case "FW Flash":
                    item.Result = Test_MainBoard_FWFlash(index, item);
                    break;

                case "Check Sum":
                    item.Result = Test_MainBoard_CheckSum(index, item);
                    break;

                #endregion

                #region--- Powerboard FCT ---

                case "Power On(Powerboard)":
                    item.Result = Test_Powerboard_PowerOn(index, item);
                    break;

                case "VBUS_IN(Powerboard)":
                    item.Result = Test_Powerboard_VBUS_IN(index, item);
                    break;

                case "DC_IN(Powerboard)":
                    item.Result = Test_Powerboard_DC_IN(index, item);
                    break;

                case "VBUS_5V(Powerboard)":
                    item.Result = Test_Powerboard_VBUS_5V(index, item);
                    break;

                case "CON1_G1_G4":
                    item.Result = Test_Powerboard_CON1_G1_G4(index, item);
                    break;

                case "CON1_G2_G3":
                    item.Result = Test_Powerboard_CON1_G2_G3(index, item);
                    break;

                case "CON2_G1_G4":
                    item.Result = Test_Powerboard_CON2_G1_G4(index, item);
                    break;

                case "CON2_G2_G3":
                    item.Result = Test_Powerboard_CON2_G2_G3(index, item);
                    break;

                case "TP307":
                    item.Result = Test_Powerboard_TP307(index, item);
                    break;

                case "CC2O(high)":
                    item.Result = Test_Powerboard_CC2O(index, item);
                    break;

                case "CC2O(low)":
                    item.Result = Test_Powerboard_CC2O(index, item);
                    break;

                case "CC1O(high)":
                    item.Result = Test_Powerboard_CC1O(index, item);
                    break;

                case "CC1O(low)":
                    item.Result = Test_Powerboard_CC1O(index, item);
                    break;

                case "USB_OTG1_DP_A(high)":
                    item.Result = Test_Powerboard_USB_OTG1_DP_A(index, item);
                    break;

                case "USB_OTG1_DP_A(low)":
                    item.Result = Test_Powerboard_USB_OTG1_DP_A(index, item);
                    break;

                case "USB_OTG1_DM_A(high)":
                    item.Result = Test_Powerboard_USB_OTG1_DM_A(index, item);
                    break;

                case "USB_OTG1_DM_A(low)":
                    item.Result = Test_Powerboard_USB_OTG1_DM_A(index, item);
                    break;

                case "CC2H(high)":
                    item.Result = Test_Powerboard_CC2H(index, item);
                    break;

                case "CC2H(low)":
                    item.Result = Test_Powerboard_CC2H(index, item);
                    break;

                case "CC1H(high)":
                    item.Result = Test_Powerboard_CC1H(index, item);
                    break;

                case "CC1H(low)":
                    item.Result = Test_Powerboard_CC1H(index, item);
                    break;

                case "HOST_DP(high)":
                    item.Result = Test_Powerboard_HOST_DP(index, item);
                    break;

                case "HOST_DP(low)":
                    item.Result = Test_Powerboard_HOST_DP(index, item);
                    break;

                case "HOST_DM(high)":
                    item.Result = Test_Powerboard_HOST_DM(index, item);
                    break;

                case "HOST_DM(low)":
                    item.Result = Test_Powerboard_HOST_DM(index, item);
                    break;

                case "VEHICLE_MOUNT_DET(high)":
                    item.Result = Test_Powerboard_VEHICLE_MOUNT_DET(index, item);
                    break;

                case "VEHICLE_MOUNT_DET(low)":
                    item.Result = Test_Powerboard_VEHICLE_MOUNT_DET(index, item);
                    break;

                case "USB2_DOCK_DM(high)":
                    item.Result = Test_Powerboard_USB2_DOCK_DM(index, item);
                    break;

                case "USB2_DOCK_DM(low)":
                    item.Result = Test_Powerboard_USB2_DOCK_DM(index, item);
                    break;

                #endregion

                #region --- USB2ETH ---
                case "FW Version(USB2ETH)":
                    item.Result = Test_USB2ETH_FWVersion(index, item);
                    break;

                case "Get MAC Array":
                    item.Result = Test_USB2ETH_GetMacArray(index, item);
                    break;

                case "Print MAC Array":
                    item.Result = Test_USB2ETH_PrintMacArray(index, item);
                    break;

                case "Write MAC(USB2ETH)1":
                    item.Result = Test_USB2ETH_WriteMac(index, item);
                    break;

                case "Write MAC(USB2ETH)2":
                    item.Result = Test_USB2ETH_WriteMac(index, item);
                    break;

                case "Write MAC(USB2ETH)3":
                    item.Result = Test_USB2ETH_WriteMac(index, item);
                    break;

                case "Write MAC(USB2ETH)4":
                    item.Result = Test_USB2ETH_WriteMac(index, item);
                    break;

                case "Write MAC(USB2ETH)5":
                    item.Result = Test_USB2ETH_WriteMac(index, item);
                    break;

                case "Ping Test(USB2ETH)1":
                    item.Result = Test_USB2ETH_Ping(index, item);
                    break;

                case "Ping Test(USB2ETH)2":
                    item.Result = Test_USB2ETH_Ping(index, item);
                    break;

                case "Ping Test(USB2ETH)3":
                    item.Result = Test_USB2ETH_Ping(index, item);
                    break;

                case "Ping Test(USB2ETH)4":
                    item.Result = Test_USB2ETH_Ping(index, item);
                    break;

                case "Ping Test(USB2ETH)5":
                    item.Result = Test_USB2ETH_Ping(index, item);
                    break;

                case "Read MAC(USB2ETH)1":
                    item.Result = Test_USB2ETH_ReadMac(index, item);
                    break;

                case "Read MAC(USB2ETH)2":
                    item.Result = Test_USB2ETH_ReadMac(index, item);
                    break;

                case "Read MAC(USB2ETH)3":
                    item.Result = Test_USB2ETH_ReadMac(index, item);
                    break;

                case "Read MAC(USB2ETH)4":
                    item.Result = Test_USB2ETH_ReadMac(index, item);
                    break;

                case "Read MAC(USB2ETH)5":
                    item.Result = Test_USB2ETH_ReadMac(index, item);
                    break;

                #endregion

                #region --- QBC ---
                case "Led_R1(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_R2(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_R3(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_R4(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_R1(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_R2(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_R3(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_R4(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G1(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G2(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G3(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G4(ledon)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G1(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G2(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G3(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "Led_G4(ledoff)":
                    item.Result = Test_QBC_LED(index, item);
                    break;

                case "battery1 current":
                    item.Result = Test_QBC_BAT_CURR(index, item);
                    break;

                case "battery2 current":
                    item.Result = Test_QBC_BAT_CURR(index, item);
                    break;

                case "battery3 current":
                    item.Result = Test_QBC_BAT_CURR(index, item);
                    break;

                case "battery4 current":
                    item.Result = Test_QBC_BAT_CURR(index, item);
                    break;

                case "battery1 temperature":
                    item.Result = Test_QBC_BAT_TEMP(index, item);
                    break;

                case "battery2 temperature":
                    item.Result = Test_QBC_BAT_TEMP(index, item);
                    break;

                case "battery3 temperature":
                    item.Result = Test_QBC_BAT_TEMP(index, item);
                    break;

                case "battery4 temperature":
                    item.Result = Test_QBC_BAT_TEMP(index, item);
                    break;

                case "battery1 capacity":
                    item.Result = Test_QBC_BAT_CAPA(index, item);
                    break;

                case "battery2 capacity":
                    item.Result = Test_QBC_BAT_CAPA(index, item);
                    break;

                case "battery3 capacity":
                    item.Result = Test_QBC_BAT_CAPA(index, item);
                    break;

                case "battery4 capacity":
                    item.Result = Test_QBC_BAT_CAPA(index, item);
                    break;

                case "battery1 cap Voltage":
                    item.Result = Test_QBC_BAT_CAPVOLT(index, item);
                    break;

                case "battery2 cap Voltage":
                    item.Result = Test_QBC_BAT_CAPVOLT(index, item);
                    break;

                case "battery3 cap Voltage":
                    item.Result = Test_QBC_BAT_CAPVOLT(index, item);
                    break;

                case "battery4 cap Voltage":
                    item.Result = Test_QBC_BAT_CAPVOLT(index, item);
                    break;

                case "ntc temperature":
                    item.Result = Test_QBC_NTC_TEMP(index, item);
                    break;

                case "Enter Factory Mode":
                    item.Result = Test_QBC_Enter_Factory(index, item);
                    break;

                case "Exit Factory Mode":
                    item.Result = Test_QBC_Exit_Factory(index, item);
                    break;

                case "QBC FW Version":
                    item.Result = Test_QBC_FWVersion(index, item);
                    break;
                #endregion

                #region --- keyboard FCT--- 
                case "PowerOn(Keyboard)":
                    item.Result = Test_Keyboard_PowerOn(index, item);
                    break;

                case "UI_SW1-OFF":
                    item.Result = Test_Keyboard_SW1_UiSw1(index, item);
                    break;

                case "UI_SW1-ON":
                    item.Result = Test_Keyboard_SW1_UiSw1(index, item);
                    break;

                case "UI_MENU_KEY-OFF":
                    item.Result = Test_Keyboard_SW3_UiMenuKey(index, item);
                    break;

                case "UI_MENU_KEY-ON":
                    item.Result = Test_Keyboard_SW3_UiMenuKey(index, item);
                    break;

                case "UI_DOWN_KEY-OFF":
                    item.Result = Test_Keyboard_SW5_DownKey(index, item);
                    break;

                case "UI_DOWN_KEY-ON":
                    item.Result = Test_Keyboard_SW5_DownKey(index, item);
                    break;

                case "UI_ENTRY_KEY-OFF":
                    item.Result = Test_Keyboard_SW2_EntryKey(index, item);
                    break;

                case "UI_ENTRY_KEY-ON":
                    item.Result = Test_Keyboard_SW2_EntryKey(index, item);
                    break;

                case "UI_FEED_SW2-OFF":
                    item.Result = Test_Keyboard_SW4_FeedSw2(index, item);
                    break;

                case "UI_FEED_SW2-ON":
                    item.Result = Test_Keyboard_SW4_FeedSw2(index, item);
                    break;

                case "UI_UP_KEY-OFF":
                    item.Result = Test_Keyboard_SW6_UpKey(index, item);
                    break;

                case "UI_UP_KEY-ON":
                    item.Result = Test_Keyboard_SW6_UpKey(index, item);
                    break;

                case "LED_POWR_RED-ON":
                    item.Result = Test_Keyboard_LedPowerOn(index, item);
                    break;

                case "LED_POWR_GRN-ON":
                    item.Result = Test_Keyboard_LedPowerOn(index, item);
                    break;

                #endregion

                #region --- NFC module ---
                case "Power On(WifiBT)":
                    item.Result = Test_WiFiBtModule_PowerOn(index, item);
                    break;

                case "Power On(BT)":
                    item.Result = Test_WiFiBtModule_PowerOn(index, item);
                    break;

                case "BT_Module Write(BT)":
                    item.Result = Test_BtModule_WriteMAC(index, item);
                    break;

                case "BT_Module Read(BT)":
                    item.Result = Test_WiFiBtModule_BtWifiRead(index, item);
                    break;

                case "WiFi_BT_Module Read(Wifi)":
                    item.Result = Test_WiFiBtModule_BtWifiRead(index, item);
                    break;

                case "WiFi_BT_Module Read(BT)":
                    item.Result = Test_WiFiBtModule_BtWifiRead(index, item);
                    break;

                case "Print MAC(BT)":
                    item.Result = Test_WiFiBtModule_PrintMAC(index, item);
                    break;

                case "Print MAC(Wifi)":
                    item.Result = Test_WiFiBtModule_PrintMAC(index, item);
                    break;

                case "NFC Tag Read":
                    item.Result = Test_NFCTag_Read(index, item);
                    break;

                case "NFC Tag Write":
                    item.Result = Test_NFCTag_Write(index, item);
                    break;

                case "Set NFCTag NDEF":
                    item.Result = Test_NFCTag_SetCardNDEF(index, item);
                    break;

                case "Check NFC Content":
                    item.Result = Test_NFCTag_CheckNFCContent(index, item);
                    break;

                #endregion

                #region --- Top Sensor ---
                case "Power On(Top Sensor)":
                    item.Result = Test_TopSensor_PowerOn(index, item);
                    break;

                case "VF voltage test":
                    item.Result = Test_TopSensor_VF(index, item);
                    break;

                case "VCE(SAT) voltage test":
                    item.Result = Test_TopSensor_VCE(index, item);
                    break;

                case "VCE(DARK) voltage test":
                    item.Result = Test_TopSensor_VCE(index, item);
                    break;


                #endregion

                #region--- LTS Sensor ---

                case "Power On(LTS Sensor)":
                    item.Result = Test_LTSSensor_PowerOn(index, item);
                    break;

                case "LTS Sensor test(Media)":
                    item.Result = Test_LTSSensor_Func(index, item);
                    break;

                case "LTS Sensor test(No Media)":
                    item.Result = Test_LTSSensor_Func(index, item);
                    break;


                #endregion

                // 默认分支：所有case都不匹配时执行
                default:
                    item.Result = default_placeholder_function(index, item);
                    break; // 可选：default在最后时break可省略，但建议保留，规范
            }

            DateTime dt_end = DateTime.Now;
            item.Duration = dt_end - dt_start;
            Invoke(new Action(() =>
            {
                lvTestItems.Items[index].SubItems[5].Text = item.Value;
                lvTestItems.Items[index].SubItems[6].Text = item.Result ? "PASS" : "FAIL";
                lvTestItems.Items[index].SubItems[7].Text = item.Duration.TotalSeconds.ToString("F2");
                lvTestItems.Items[index].BackColor = item.Result ? Color.FromArgb(204, 232, 207) : Color.FromArgb(245, 203, 203);
                lvTestItems.Items[index].ForeColor = Color.FromArgb(60, 60, 60);

                lvTestItems.EnsureVisible(index);
            }));

            return item;
        }

        #endregion

        private bool test_feed_1(string pic_path = "")
        {
            SerialPortWriteLineOnce(PrinterCommands.testfeed_1);
            string zh_cn = "请确认testfeed -1 是否完成。";
            string en_us = "please check testfeed -1 have finished yet.";
            string thai = "thai tips (unknown)";

            return GetOperatorSelection(zh_cn, en_us, thai);
        }

        private bool GetOperatorSelection(string zh_cn, string us_en, String thai, string pic_path = "")
        {
            bool bOpSelection = false;
            Invoke(new Action(() =>
            {
                frmTips dialog;
                if (File.Exists(pic_path)) { dialog = new frmTips(pic_path, zh_cn, us_en, thai); }
                else { dialog = new frmTips(zh_cn, us_en, thai); }
                DialogResult result = dialog.ShowDialog(this);

                bOpSelection = result == DialogResult.Yes;
            }));

            return bOpSelection;
        }

        private string GetMediaType()
        {
            string media_type = "", tmpValue = "";
            bool bTimeOut = true;
            string m_tag = Guid.NewGuid().ToString("N");
            AppendRuntimeLog(m_tag);
            SerialPortWriteLineOnce(PrinterCommands.get_media_type);
            SerialPortWriteLineOnce(PrinterCommands.get_media_type_value);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(m_tag);
                tmpValue = tmpValue.Replace(PrinterCommands.get_media_type, "");
                tmpValue = tmpValue.Replace(PrinterCommands.get_media_type_value, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                media_type = "GetMediaTypeTimeOut";
                goto End;
            }

            media_type = tmpValue.Replace("Ok", "").Trim();

        End:
            return media_type;
        }

        private string GetLTSSensorValue()
        {
            string value = "", tmpValue = "";
            bool bTimeOut = true;
            string m_tag = Guid.NewGuid().ToString("N");
            AppendRuntimeLog(m_tag);

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);
            SerialPortWriteLineOnce(PrinterCommands.LTS);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(m_tag);
                tmpValue = tmpValue.Replace(PrinterCommands.LTS, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrEmpty(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                goto End;
            }

            value = GetSensorValue(tmpValue);
        End:
            return value;
        }

        private string GetHEAD_LIFT_SENSOR()
        {
            string value = "", tmpValue = "";
            bool bTimeOut = true;

            DUTS.PA45.PA45 dut_PA45 = dut as DUTS.PA45.PA45;

            if (dut_PA45 == null)
            {
                value = "PRODUCT_TYPE_ERROR";
                goto End;
            }

            string m_tag = Guid.NewGuid().ToString("N");
            AppendRuntimeLog(m_tag);

            SerialPortWriteLineOnce(PrinterCommands.HEAD_LIFT_SENSOR);
            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(m_tag);
                tmpValue = tmpValue.Replace(PrinterCommands.HEAD_LIFT_SENSOR, "");
                if (!string.IsNullOrEmpty(tmpValue) && !string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                goto End;
            }



        End:
            return value;
        }

        public string reserveMac(string temp)
        {
            string mac = "";
            if (temp != "" && temp.Length == 12)
            {
                /*
                 * 反转的另一种方法
                 string strMac = "";
                for (int i = 10; i >= 0; i -= 2)
                {

                    strMac += string.Format("{0:X}", mac.Substring(i, 2));
                }
                mac = strMac;
                */

                /*****反转的第二种方法*****/
                ArrayList macList = new ArrayList();
                for (int i = 0; i < 12; i += 2)
                {
                    macList.Add(temp.Substring(i, 2));
                }
                macList.Reverse();

                foreach (string t in macList)
                {
                    mac += t;
                }

                mac = System.Text.RegularExpressions.Regex.Replace(mac, @"(\w{2})", "$1:").Trim(':'); ;

            }

            return mac;
        }


        private string GetSensorValue(string text)
        {
            string value = "";
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("Sensor Value:"))
                {
                    value = v.Replace("Sensor Value:", "").Trim();
                    break;
                }
            }
            return value;
        }


        private string GetDHCP_IP_Address(string text)
        {
            string value = "";
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains("WIFI IPADDRESS :"))
                {
                    value = v.Replace("WIFI IPADDRESS :", "").Trim();
                    break;
                }
            }
            return value;
        }

        private string GetSensorValue(string text, string flag)
        {
            string value = "";
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains(flag))
                {
                    value = v.Replace(flag, "").Trim();
                    break;
                }
            }
            return value;
        }

        private void GetSensorValues(string text, string flag1, string flag2, ref string ADCValue, ref string tempValue)
        {
            string value = "";
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (string.IsNullOrEmpty(flag2) || string.IsNullOrWhiteSpace(flag2))
                {
                    string m_buff = v.Replace(" ", "");
                    if (m_buff.Contains(flag1))
                    {
                        value = m_buff.Replace(flag1, "");
                        break;
                    }
                }
                else
                {
                    string m_buff = v.Replace(" ", "");

                    if (m_buff.Contains(flag1) && m_buff.Contains(flag2))
                    {
                        int index1 = m_buff.IndexOf(flag1);
                        int index2 = m_buff.IndexOf(flag2);
                        ADCValue = m_buff.Substring(index1 + flag1.Length, index2 - index1 - flag1.Length);
                        tempValue = m_buff.Substring(index2 + flag2.Length, m_buff.Length - index2 - flag2.Length);
                        break;
                    }
                }
            }
        }

        private string GetValueAfterFlag(string text, string flag)
        {
            string value = "";
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains(flag))
                {
                    value = v.Replace(flag, "").Trim();
                    break;
                }
            }

            return value;
        }

        private string GetValueContainFlag(string text, string flag)
        {
            string value = "";
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values)
            {
                if (v.Contains(flag))
                {
                    value = v.Trim();
                    break;
                }
            }

            return value;
        }

        private void GetValuesWithFlags(string text, ref Dictionary<string, string> dict)
        {
            List<string> values = text.Split('\n').ToList();
            values.RemoveAll(s => string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

            foreach (string v in values) { foreach (string key in dict.Keys) { if (v.Contains(key)) { dict[key] = v.Replace(key, "").Trim(); } } }
        }

        private void GetBluetoothId(string text, ref string pn, ref string sn, ref string addr)
        {
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Part Number:")) pn = lines[i].Replace("Part Number:", "").Trim();
                if (lines[i].Contains("Serial Number:")) sn = lines[i].Replace("Serial Number:", "").Trim();
                if (lines[i].Contains("BD Address:")) addr = lines[i].Replace("BD Address:", "").Trim();
            }
        }

        private void GetWifiId(string text, ref string pn, ref string sn, ref string addr)
        {

            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Part Number:")) pn = lines[i].Replace("Part Number:", "").Trim();
                if (lines[i].Contains("Serial Number:")) sn = lines[i].Replace("Serial Number:", "").Trim();
                if (lines[i].Contains("MAC Address:")) addr = lines[i].Replace("MAC Address:", "").Trim();
            }
        }

        private void GetLabelLength(string text, ref string labelLen)
        {
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Label Length:"))
                {
                    labelLen = lines[i].Replace("Label Length:", "").Trim();
                    break;
                }
            }
        }

        private void GetCpuPcbaId(string text, ref string pn, ref string sn, ref string addr)
        {
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Part Num = ")) pn = lines[i].Replace("Part Num = ", "").Trim();
                if (lines[i].Contains("Serial Num = ")) sn = lines[i].Replace("Serial Num = ", "").Trim();
                if (lines[i].Contains("Mech = ")) addr = lines[i].Replace("Mech =", "").Trim();
            }
        }
        
        private string ReadSerialportDataReceived() { return Convert.ToString(Invoke(new Func<string>(() => { return rtbSerialport.Text; }))); }

        private void ckbSFCS_CheckedChanged(object sender, EventArgs e) { MessageBox.Show("ckbSFCS_CheckedChanged"); }

        private void ckbSFCS_Click(object sender, EventArgs e) { MessageBox.Show("ckbSFCS_Click"); }

        #region serialport
        private bool InitializeSerialport(string vid, string pid, SerialPort serialport)
        {
            bool rlt = false;
            try
            {
                if (serialport.IsOpen)
                    serialport.Close();

                int temp_seri_baudreta = 57600;

                Type m_type = dut.GetType();

                if (m_type.Name == "QBC" || m_type.Name == "USB2ETH")
                {
                    temp_seri_baudreta = 57600;
                }

                if (m_type.Name == "PA45" ||
                    m_type.Name == "BT_Module" ||
                    m_type.Name == "WiFi_BT_Module" ||
                    m_type.Name == "PA45_MainBoard")
                {
                    temp_seri_baudreta = 115200;
                }

                if (!string.IsNullOrEmpty(seri_form.SeriBoudRete) &&
                    !string.IsNullOrWhiteSpace(seri_form.SeriBoudRete))
                {
                    temp_seri_baudreta = Convert.ToInt32(seri_form.SeriBoudRete);
                }

                // 1. 基础参数配置（可根据你的设备调整）
                serialport.PortName = WinDevice.GetSerialportComPort(vid, pid);
                serialport.BaudRate = temp_seri_baudreta;          // 波特率（常用：9600、115200）
                serialport.DataBits = 8;               // 数据位
                serialport.StopBits = StopBits.One;    // 停止位
                serialport.Parity = Parity.None;       // 校验位（无校验）
                serialport.Handshake = Handshake.None; // 握手协议（无）
                serialport.DtrEnable = true;
                serialport.RtsEnable = true;

                /*
                // 2. 超时设置（防止读取阻塞）
                spLowerComputer.ReadTimeout = 500;          // 读取超时500ms
                spLowerComputer.WriteTimeout = 500;         // 写入超时500ms
                */

                // 3. 关闭自动换行（可选，根据设备需求）
                //serialport.NewLine = "\r\n";

                Invoke(new Action(() => { slblPortName.Text = slblPortName.Text + string.Format($"{serialport.PortName} : {serialport.BaudRate} [vid:{vid}, pid:{pid}] "); }));
                serialport.Open();
                rlt = true;
            }
            catch (Exception e)
            {
                AppendRuntimeLog(string.Format($"catch exception in FUNC [ {MethodBase.GetCurrentMethod().Name} ] as : ") + e.Message);
                MessageBox.Show(e.Message, string.Format($"FUNC [ {MethodBase.GetCurrentMethod().Name} ]"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateErrorUI(Test_Proc_Error_Code.NoSeriPort);
                rlt = false;
            }

            return rlt;
        }

        private bool InitializeSerialport1(SerialPort serialport, string portname)
        {
            bool rlt = false;
            try
            {
                if (serialport.IsOpen)
                    spTester.Close();

                // 1. 基础参数配置（可根据你的设备调整）
                serialport.PortName = portname;          // 串口名（默认，可通过下拉框选择）
                serialport.BaudRate = 115200;            // 波特率（常用：9600、115200）
                serialport.DataBits = 8;               // 数据位
                serialport.StopBits = StopBits.One;    // 停止位
                serialport.Parity = Parity.None;       // 校验位（无校验）
                serialport.Handshake = Handshake.None; // 握手协议（无）
                serialport.DtrEnable = true;
                serialport.RtsEnable = true;

                /*
                // 2. 超时设置（防止读取阻塞）
                spLowerComputer.ReadTimeout = 500;          // 读取超时500ms
                spLowerComputer.WriteTimeout = 500;         // 写入超时500ms
                */

                // 3. 关闭自动换行（可选，根据设备需求）
                //spLowerComputer.NewLine = "\r\n";
                Invoke(new Action(() => { slblPortName.Text = serialport.PortName; }));

                serialport.Open();
                rlt = true;
            }
            catch (Exception e)
            {
                AppendRuntimeLog(string.Format($"catch exception in FUNC [ {MethodBase.GetCurrentMethod().Name} ] as : {Environment.NewLine}") + e.Message);
                MessageBox.Show(e.Message, string.Format($"FUNC [ {MethodBase.GetCurrentMethod().Name} ]"), MessageBoxButtons.OK, MessageBoxIcon.Error);

                rlt = false;
            }

            return rlt;
        }

        private void SerialPortWriteLine(string text, int retryTimes, int milliSecond = 1000)
        {
            if (!spTester.IsOpen) spTester.Open();

            spTester.WriteLine(text);

            Invoke(new Action(() =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    rtbLog.AppendText("\n" + text + string.Format($" {retryTimes} ") + "\n");
                    rtbLog.Select(rtbLog.TextLength, 0);
                    rtbLog.ScrollToCaret();
                }
            }));

            Delay(milliSecond);
        }

        private void SerialPortWriteLineOnce(string text, int milliSecond = 1000)
        {
            if (!spTester.IsOpen) spTester.Open();

            spTester.WriteLine(text);

            if (ckbIsPrintLog.Checked)
            {
                Invoke(new Action(() =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        rtbLog.AppendText("\n" + text + "\n");
                        rtbLog.Select(rtbLog.TextLength, 0);
                        rtbLog.ScrollToCaret();
                    }
                }));
            }

            Delay(milliSecond);
        }
        
        private string ReadSerialportCommand()
        {
            Delay(1000);
            string value = spTester.ReadExisting();

            Invoke(new Action(() =>
            {
                if (!string.IsNullOrEmpty(value))
                {
                    rtbLog.AppendText("\n" + value + "\n");
                    rtbLog.Select(rtbLog.TextLength, 0);
                    rtbLog.ScrollToCaret();
                }
            }));
            return value;
        }

        private void spTester_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            /*
            int count = spTester.BytesToRead;
            byte[] buffer = new byte[count];
            spTester.Read(buffer, 0, count);
            string recv = Encoding.GetEncoding("ASCII").GetString(buffer);
            Invoke(new Action(() => { rtbLog.AppendText(recv); }));
            */
        }

        private void spTester_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Delay(500);
            int count = spTester.BytesToRead;
            byte[] buffer = new byte[count];
            spTester.Read(buffer, 0, count);
            string recv = Encoding.GetEncoding("ASCII").GetString(buffer);
            Invoke(new Action(() =>
            {
                rtbLog.AppendText(recv);
            }));
        }

        private void spTester_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            /*
            int count = spTester.BytesToRead;
            byte[] buffer = new byte[count];
            spTester.Read(buffer, 0, count);
            string recv = Encoding.GetEncoding("ASCII").GetString(buffer);
            Invoke(new Action(() =>
            {
                rtbSerialport.AppendText(recv);
                rtbSerialport.Select(rtbLog.TextLength, 0);
                rtbSerialport.ScrollToCaret();
            }));
            */
        }

        private void spDutDebugPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int count = spDutDebugPort.BytesToRead;
            byte[] buffer = new byte[count];
            spDutDebugPort.Read(buffer, 0, count);
            string recv = Encoding.GetEncoding("ASCII").GetString(buffer);
            Invoke(new Action(() => { rtbSerialport.AppendText(recv); }));
        }

        private void spDutDebugPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            int count = spDutDebugPort.BytesToRead;
            byte[] buffer = new byte[count];
            spDutDebugPort.Read(buffer, 0, count);
            string recv = Encoding.GetEncoding("ASCII").GetString(buffer);
            Invoke(new Action(() =>
            {
                rtbSerialport.AppendText(recv);
                rtbSerialport.Select(rtbSerialport.TextLength, 0);
                rtbSerialport.ScrollToCaret();
            }));
        }

        private void spDutDebugPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            int count = spDutDebugPort.BytesToRead;
            byte[] buffer = new byte[count];
            spDutDebugPort.Read(buffer, 0, count);
            string recv = Encoding.GetEncoding("ASCII").GetString(buffer);
            Invoke(new Action(() =>
            {
                rtbSerialport.AppendText(recv);
                rtbSerialport.Select(rtbSerialport.TextLength, 0);
                rtbSerialport.ScrollToCaret();
            }));
        }

        #endregion

        #region menustrip

        private void releaseNoteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmReleaseNote form = new frmReleaseNote();
            form.Show();
        }

        private void serialPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialog_result = seri_form.ShowDialog();
            if (dialog_result == DialogResult.Yes) { }
        }

        private void MenuItem_Testitem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(productConfig.TEST_ITEMS))
            {
                MessageBox.Show(
                           $"Please select a production config.",
                           Global.AssemblyName,
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Error);
            }

            string m_testitemPath = Path.Combine(Global.ASSEMBLY_PATH, productConfig.TEST_ITEMS);

            if ((testitem_form == null) || testitem_form.IsDisposed) { testitem_form = new frmTestItems(m_testitemPath); }

            testitem_form.Show();
        }

        private void MenuItem_Relays_Click(object sender, EventArgs e)
        {
            frmRelay form = frmRelay.Instance;
            frmRelay.ShowInstance();
        }

        private void MenuItem_SFCS_Click(object sender, EventArgs e)
        {
            if ((sfcs_form == null) || sfcs_form.IsDisposed)
            {
                sfcs_form = new frmSFCS(_ssg_sfcs_setting);
                sfcs_form.MESSettingChanged += OnSfcs_form_MESSettingChanged;
            }

            sfcs_form.Show();
        }

        private void aCCLabelPrintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmACCPrint form = new frmACCPrint();
            form.Show();
        }

        #endregion

        private void button1_Click_1(object sender, EventArgs e)
        {
            /*

            // 1. 先列出当前所有可用的网卡名称，方便你确认要修改的网卡叫什么
            Console.WriteLine("--- 当前可用的网卡列表 ---");
            WinDevice.ListNetworkAdapters();
            Console.WriteLine("--------------------------\n");

            // 2. 设定目标网卡名称（请替换为你电脑上实际显示的名称，如 "以太网" 或 "WLAN"）
            string targetAdapterName = "LAN9500A";

            // 3. 设定新的网络参数
            string newIP = "192.168.1.100";
            string subnetMask = "255.255.255.0";
            string[] gateways = new string[] { "192.168.1.1" };
            string[] dnsServers = new string[] { "8.8.8.8", "114.114.114.114" };

            try
            {
                Console.WriteLine($"正在尝试为 [{targetAdapterName}] 设置IP...");

                string m_adapter_name = WinDevice.GetNetworkAdapters("LAN9500A");
                WinDevice.SetNetworkAdapterIP(m_adapter_name, newIP, subnetMask, gateways, dnsServers);
                Console.WriteLine("✅ 网络配置修改成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 修改失败: {ex.Message}");
            }

            Console.ReadLine();
            */

            /*
            // 桌面根元素，全局所有窗口所有控件
            AutomationElement desktop = AutomationElement.RootElement;

            // 筛选条件：只找按钮
            PropertyCondition buttonCond = new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Button
            );

            // 递归查找整个桌面所有按钮（所有进程、所有软件）
            AutomationElementCollection allButtons = desktop.FindAll(
                TreeScope.Descendants,
                buttonCond
            );
            
            Console.WriteLine($"共找到 {allButtons.Count} 个按钮");

            foreach (AutomationElement btn in allButtons)
            {
                try
                {
                    var cur = btn.Current;
                    // 按钮核心信息
                    string btnName = cur.Name;                // 按钮显示文字
                    int pid = cur.ProcessId;                  // 所属软件进程ID
                    System.Windows.Rect rect = cur.BoundingRectangle;        // 屏幕坐标
                    string autoId = cur.AutomationId;         // 控件唯一ID（最稳定定位）
                    string className = cur.ClassName;         // 窗口类名

                    Console.WriteLine("=====================================");
                    Console.WriteLine($"按钮文本：{btnName}");
                    Console.WriteLine($"AutomationId：{autoId}");
                    Console.WriteLine($"所属进程PID：{pid}");
                    Console.WriteLine($"屏幕位置：X={rect.X}, Y={rect.Y}, W={rect.Width}, H={rect.Height}");
                    Console.WriteLine($"控件类名：{className}");

                    // 判断按钮是否可点击
                    bool isClickable = false;
                    if (btn.TryGetCurrentPattern(InvokePattern.Pattern, out object patternObj))
                    {
                        isClickable = true;
                    }
                    Console.WriteLine($"是否可点击：{isClickable}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"读取按钮异常：{ex.Message}");
                }
            }
            */

            /*
            string ipAddress = "192.168.0.3"; // 替换为仪器实际IP
            int port = 5025;                    // Keithley LAN 默认 Raw Socket 端口
            using (TcpClient client = new TcpClient())
            {
                //try
                {
                    // 1. 连接仪器
                    Console.WriteLine("正在连接仪器...");
                    client.Connect(ipAddress, port);
                    client.ReceiveTimeout = 3000; // 设置读取超时 5秒
                    client.SendTimeout = 3000;
                    NetworkStream stream = client.GetStream();

                    // 2. 发送查询指令 (*IDN?)
                    string command = "*IDN?\n"; // 注意：SCPI指令通常必须以 \n (换行符) 结尾
                    byte[] sendData = Encoding.ASCII.GetBytes(command);
                    stream.Write(sendData, 0, sendData.Length);

                    // 3. 读取返回值
                    byte[] buffer = new byte[256];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                    Console.WriteLine("仪器身份: " + response);

                    // 2. 发送查询指令 (*IDN?)
                    string command1 = "*IDN?\n"; // 注意：SCPI指令通常必须以 \n (换行符) 结尾
                    byte[] sendData1 = Encoding.ASCII.GetBytes(command1);
                    stream.Write(sendData1, 0, sendData1.Length);

                    // 3. 读取返回值
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                    Console.WriteLine("仪器身份: " + response);
                    
                    // 4. 发送控制指令示例 (设置通道1电压为 5.0V)
                    // 通道1 设置7.4V，打开输出
                    SendCommand(stream, "SOUR1:CURR 3.0");//SOUR2:CURR 3.0
                    SendCommand(stream, "SOUR1:VOLT 7.4");
                    SendCommand(stream, "OUTP1 ON");

                    Delay(1000);
                    // 通道2 设置5.0V，打开输出
                    SendCommand(stream, "SOUR2:CURR 3.0");
                    SendCommand(stream, "SOUR2:VOLT 12.0");
                    // channel2, current limit (<3A)
                    SendCommand(stream, "OUTP2 ON");

                    SendCommand(stream, "INST:SEL CH1");
                    SendCommand(stream, "MEAS:VOLT?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("VOLT CH1: " + response);

                    SendCommand(stream, "INST:SEL CH1");
                    SendCommand(stream, "MEAS:CURR?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("CURR CH1: " + response);

                    SendCommand(stream, "INST:SEL CH2");
                    SendCommand(stream, "MEAS:VOLT?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("VOLT CH2: " + response);

                    SendCommand(stream, "INST:SEL CH2");
                    SendCommand(stream, "MEAS:CURR?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("CURR CH2: " + response);

                    Console.WriteLine("指令发送成功！");
                }
            }
            */

        }

        public AutomationElement GetElementByPid(int processId)
        {
            // 1. 获取桌面根元素
            AutomationElement rootElement = AutomationElement.RootElement;

            // 2. 创建查找条件：ProcessId 等于传入的 processId
            Condition condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);

            // 3. 在桌面的直接子元素（TreeScope.Children）中查找第一个匹配的元素
            AutomationElement targetElement = rootElement.FindFirst(TreeScope.Children, condition);

            return targetElement;
        }

        // 封装一个发送指令的辅助方法（不需要返回值的指令）
        static void SendCommand(NetworkStream stream, string command)
        {
            byte[] data = Encoding.ASCII.GetBytes(command + "\n");
            stream.Write(data, 0, data.Length);
            stream.Flush();
            Thread.Sleep(100); // 稍微延时，防止指令发送过快导致仪器缓冲区溢出
        }

        private void btn2306_Click(object sender, EventArgs e)
        {
            string ipAddress = "192.168.0.3"; // 替换为仪器实际IP
            int port = 5025;                    // Keithley LAN 默认 Raw Socket 端口
            using (TcpClient client = new TcpClient())
            {
                //try
                {
                    // 1. 连接仪器
                    Console.WriteLine("正在连接仪器...");
                    client.Connect(ipAddress, port);
                    client.ReceiveTimeout = 3000; // 设置读取超时 5秒
                    client.SendTimeout = 3000;
                    NetworkStream stream = client.GetStream();

                    // 2. 发送查询指令 (*IDN?)
                    string command = "*IDN?\n"; // 注意：SCPI指令通常必须以 \n (换行符) 结尾
                    byte[] sendData = Encoding.ASCII.GetBytes(command);
                    stream.Write(sendData, 0, sendData.Length);

                    // 3. 读取返回值
                    byte[] buffer = new byte[256];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                    Console.WriteLine("仪器身份: " + response);

                    // 2. 发送查询指令 (*IDN?)
                    string command1 = "*IDN?\n"; // 注意：SCPI指令通常必须以 \n (换行符) 结尾
                    byte[] sendData1 = Encoding.ASCII.GetBytes(command1);
                    stream.Write(sendData1, 0, sendData1.Length);

                    // 3. 读取返回值
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                    Console.WriteLine("仪器身份: " + response);

                    // 4. 发送控制指令示例 (设置通道1电压为 5.0V)
                    // 通道1 设置7.4V，打开输出
                    SendCommand(stream, "SOUR1:CURR 3.0");//SOUR2:CURR 3.0
                    SendCommand(stream, "SOUR1:VOLT 7.4");
                    SendCommand(stream, "OUTP1 ON");

                    Delay(1000);
                    // 通道2 设置5.0V，打开输出
                    SendCommand(stream, "SOUR2:CURR 3.0");
                    SendCommand(stream, "SOUR2:VOLT 12.0");
                    // channel2, current limit (<3A)
                    SendCommand(stream, "OUTP2 ON");

                    SendCommand(stream, "INST:SEL CH1");
                    SendCommand(stream, "MEAS:VOLT?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("VOLT CH1: " + response);

                    SendCommand(stream, "INST:SEL CH1");
                    SendCommand(stream, "MEAS:CURR?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("CURR CH1: " + response);

                    SendCommand(stream, "INST:SEL CH2");
                    SendCommand(stream, "MEAS:VOLT?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("VOLT CH2: " + response);

                    SendCommand(stream, "INST:SEL CH2");
                    SendCommand(stream, "MEAS:CURR?");
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("CURR CH2: " + response);

                    Console.WriteLine("指令发送成功！");
                }
            }

            MessageBox.Show("仪器设定成功");
        }

    }
}
