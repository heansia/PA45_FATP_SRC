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
using System.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
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
using FCT.Models.Instrument;
using FCT.Models.Instrument.RelayCard;
using FCT.Models.Instrument.CaptureCard;
using FCT.Service.DUT;
using FCT.Service.DUT.Service;
using DUTS = FCT.Models.DUT;
using Prod = FCT.Models.Products;
using USB = FCT.Util.USBHelper;
using SMT_SFC = FCT.Util.SMT_SFCSHelper;
using SSG_SFC = FCT.Util.SFCSHelper;
using INI = FCT.Util.INI_Parser;
using RELAY = FCT.Util.RelayHelper;
using PRINTER = FCT.Util.LabelPrintHelper;
using REGEX = FCT.Util.RegexHelper;
using DataAccess = FCT.Util.DataAccess;
using PROC = FCT.Util.ProcessHelper;
using WinDevice = FCT.Util.WinDeviceHelper;
using NFC = FCT.Util.ICardHelper;
using WinUI = FCT.Util.WinUIHelper;
using FCT.Models.DUT.PA45_MainBoard;
using FCT.Extension;
using FCT.Models.Instrument.Interface;

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

                    if (
                    _productConfig.LINE_CODE.ToUpper() == "YIFENG" ||
                    _productConfig.LINE_CODE.ToUpper() == "FLYAUDIO")
                    {
                        ckbFailStop.Checked = true;
                        ckbSFCS.Checked = false;
                    }
                    else
                    {
                        _ssg_sfc = new Models.SFC.SSG_SFC();
                    }

                    if (productConfig.STATION_CODE.ToUpper() == "OOBA")
                    {
                        ckbSFCS.Checked = false;
                        ckbSFCS.Enabled = false;
                    }
                }));
            }
        }

        public event EventHandler<ProductConfigChangedEventArgs> ProductConfigChanged;

        private SFCS_Setting _smt_sfcs_setting;
        private SFCS_Setting _ssg_sfcs_setting;
        private ISFC sfcInstance;
        private Models.SFC.SMT_SFC _smt_sfc = new Models.SFC.SMT_SFC();
        //private Models.SFC.SSG_SFC _ssg_sfc = new Models.SFC.SSG_SFC();
        private Models.SFC.SSG_SFC _ssg_sfc;

        private SSGRelayCard1 RelayCard1 = SSGRelayCard1.Instance;
        private SSGRelayCard2 RelayCard2 = SSGRelayCard2.Instance;
        private SSGRelayCard3 RelayCard3 = SSGRelayCard3.Instance;
        private SSGCaptureCard1 CaptureCard1 = SSGCaptureCard1.Instance;
        private SSGCaptureCard2 CaptureCard2 = SSGCaptureCard2.Instance;
        private KEITHLEY_2306_LAN Keithy2306LAN;

        private frmRelay relay_form;
        private frmTestItems testitem_form;
        private frmSFCS sfcs_form;
        private frmSerialPort seri_form = new frmSerialPort();
        private frmProductMatrix prod_form;
        private frmSFC_Get_Special get_sfc_data_form;

        private string TESTER_VID;
        private string TESTER_PID;

        private string MainBarcode = "";

        private readonly string[] USB2ETH_MAC_ARRAY = new string[5];

        string ssid = "", pwd = "", router_ip = "", dhcp_ip = "";
        bool have_send_wifi_script = false;
        private string WIFI_MAC_ADDRESS;
        private string BLUETOOTH_MAC_ADDRESS;

        List<TimeSpan> dura_list = new List<TimeSpan>();

        private string MainboardPartNumber;
        private string MainboardSeriNumber;

        private string PrinterPartNumber;
        private string PrinterSeriNumber;

        private string PartNumber;
        private string SeriNumber;

        private Thread _trigger;

        private readonly StringFormat _headerFormat;
        private readonly Font _headerFont;

        private bool DUT_SERIAL_PORT_STATUS;

        Dictionary<string, string> SensorKeyValueDict = new Dictionary<string, string>();

        private int RegularTimeOutCount = 60;

        private static Media_Type CurrentMediaType = Media_Type.Error;

        public frmMain()
        {
            InitializeComponent();

            RunTimeLogUpdated += OnRunTimeLogUpdated;
            ProductConfigChanged += OnProductConfigChanged;
            InitExternalDAQCard();

            GetStringBeforeFlag = new GetStringDelegate(GetStringBeforeFlagInner);
            GetStringAfterFlag = new GetStringDelegate(GetStringAfterFlagInner);

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
            Keithy2306LAN = new KEITHLEY_2306_LAN("192.168.0.2");
            Keithy2306LAN.UpdateStatus += OnRunTimeLogUpdated;
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

        public string GetUARTStringFlagA(string flag)
        {
            string value = string.Empty;
            Invoke(new Action(() =>
            {
                string tmp = rtbSerialport.Text;
                value = GetStringAfterFlag.Invoke(tmp, flag);
            }));
            return value;
        }

        public string GetUARTStringFlagB(string flag)
        {
            string value = string.Empty;
            this.Invoke(new Action(() => { value = GetStringBeforeFlag.Invoke(rtbSerialport.Text, flag); }));
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
                if (value != null)
                {
                    value = value.Replace(flag, "");
                }
            }));
            return value;
        }
        #endregion

        #region UI opreatation

        private void SetTestEntery(bool status) { Invoke(new Action(() => { foreach (Control ctrl in groupBox1.Controls) { ctrl.Enabled = status; } })); }

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
                lblResultWin.ForeColor = result ? Color.Green : Color.Red;
                lblResultWin.Text = result ? "PASS" : "FAIL";
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

            RelayCard3.OpenRelayCard += OnDAQCardOpened;
            RelayCard3.CloseRelayCard += OnDAQCardClosed;
            RelayCard3.RelayOn += OnRelayOn;
            RelayCard3.RelayOff += OnRelayOff;
            RelayCard3.GetDAQCardDI += OnGetDAQCardDI;
            RelayCard3.SetDAQCardDO += OnSetDAQCardDO;
            RelayCard3.OnMeasureVoltage += OnMeasureVoltage;

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
                btnRelay3.BackColor = RelayCard3.Open() ? Color.Lime : Color.Red;
                btnRelayCap1.BackColor = CaptureCard1.Open() ? Color.Lime : Color.Red;
                btnRelayCap2.BackColor = CaptureCard2.Open() ? Color.Lime : Color.Red;
            }));
        }

        private void CloseAllRelays()
        {
            RelayCard1.SetAllRelayOff();
            RelayCard2.SetAllRelayOff();
            RelayCard3.SetAllRelayOff();
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
                {
                    ClearLogUI();
                    pb_test_thread.Value = 0;
                    rtbDOS.Clear();
                    rtbLog.Clear();
                    rtbSerialport.Clear();
                    rtbUSB.Clear();

                    lblErrorCode.ForeColor = Color.DimGray;
                    lblErrorCode.Text = "error code";
                }

                //PA45L0002D22;26189C0080
                AppendRuntimeLog($"get GUI input {txtSN.Text}");
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

                SeriNumber = txtSN.Text;
                PartNumber = txtPN.Text;

            GET_PROD_CONFIG:
                if (productConfig == null)
                {
                    MessageBox.Show($"Product Config can't be empty!", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (prod_form != null) { prod_form.ShowDialog(); }
                    productConfig = prod_form.ProductConfig;
                }

                lblResultWin.Text = "Testing...";
                lblResultWin.ForeColor = Color.FromArgb(255, 127, 0);

                if (productConfig == null) { goto GET_PROD_CONFIG; }

                if (product == null)
                {
                    ClearLogUI();
                    product = Product.CreateProductByProdConfig(SeriNumber, productConfig);
                    product.RunTimeLogUpdated += OnRunTimeLogUpdated;
                }

                Prod.PA45 m_PA45 = product as Prod.PA45;

                if (m_PA45 != null) { m_PA45.HON_PN = PartNumber; }

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

                string m_model = "";
                if (productConfig.DATABASE.ToUpper() == "SCANNER") { m_model = "Scanner"; }

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
                        UpdateErrorUI(Test_Proc_Error_Code.SfcChkStaNG);

                        MessageBox.Show(
                                $"{MainBarcode} check sfcs station NG!!!" + "\n" + $"result is {sfcLog}",
                                Global.AssemblyName,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                        //return;
                    }
                }

                Task.Run(() => { OpenExternalDAQCard(); });

                DialogResult bStartTest = MessageBox.Show(
                    "请将打印机和测试电脑连接，并且按下打印机开机按钮，准备测试。\nConnect the printer with PC and press down power button, then start test.",
                    Global.AssemblyName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                slblPortName.Text = "";
                if (bStartTest == DialogResult.Yes)
                {
                    if (spTester.IsOpen) { spTester.Close(); }

                    if (spUART.IsOpen) { spUART.Close(); }

                    //FTDIBUS\COMPORT&VID_0403&PID_6001
                    // PA45 PN Special
                    bool needUart = false;
                    if (product is Prod.PA45_MainBoard mObj)
                    {
                        if (product.ProductConfig.STATION_CODE.ToUpper().Contains("FCT") ||
                            product.ProductConfig.STATION_CODE.ToUpper().Contains("FW FLASH"))
                        {
                            needUart = true;
                        }
                    }

                    if (product is Prod.LTSSensor mObj1) { needUart = true; }

                    if (needUart)
                    {
                        if (!InitializeSerialport("0403", "6001", spUART)) { return; }
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

                if (string.IsNullOrWhiteSpace(PartNumber)) { return; }

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

        #region  customized event handler

        private void OnProcessStart(ProcessStartEventArgs e) { AppendDOSLog(e.Arguments, false, true); }

        private void OnProductConfigChanged(object sender, ProductConfigChangedEventArgs e) { MessageBox.Show($"Set Product Config OK.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information); }

        private void OnTestErrorStatusChanged(object sender, TestErrorStatusChangedEventArgs e) { UpdateErrorUI(e.ErrorCode); }

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

                rtbUSB.Select(rtbDOS.TextLength, 0);
                rtbUSB.ScrollToCaret();
            }));
        }

        private void AppendUSBLog1(string message, bool isAddTimestamp = false)
        {
            Invoke(new Action(() =>
            {
                if (isAddTimestamp)
                    rtbUSB.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
                else
                    rtbUSB.AppendText(message + "\r\n");

                rtbUSB.Select(rtbDOS.TextLength, 0);
                rtbUSB.ScrollToCaret();
            }));
        }


        private void AppendSeriPortLog(string message, bool isAddTimestamp = false)
        {
            Invoke(new Action(() =>
            {
                if (isAddTimestamp)
                    rtbSerialport.AppendText("\n" + "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
                else
                    rtbSerialport.AppendText("\n" + message + "\r\n");

                rtbSerialport.Select(rtbSerialport.TextLength, 0);
                rtbSerialport.ScrollToCaret();
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
                if (!string.IsNullOrWhiteSpace(val_temp))
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
                if (!string.IsNullOrWhiteSpace(tmp_val))
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
            versions.RemoveAll(s => string.IsNullOrWhiteSpace(s));

            foreach (var v in versions)
            {
                if (v.Contains("write current =")) { w_current = v.Replace("write current =", "").Trim(); }
                if (v.Contains("read current =")) { r_current = v.Replace("read current =", "").Trim(); }
            }

            value = w_current + "," + r_current;

        End:
            if (!string.IsNullOrWhiteSpace(w_current) && !string.IsNullOrWhiteSpace(r_current))
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
                    if (!string.IsNullOrWhiteSpace(tmp_val))
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
            values.RemoveAll(s => string.IsNullOrWhiteSpace(s));

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

            if (!string.IsNullOrWhiteSpace(value))
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
                if (!string.IsNullOrWhiteSpace(tmp_val))
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
            values.RemoveAll(s => string.IsNullOrWhiteSpace(s));

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
            if (!string.IsNullOrWhiteSpace(value))
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
            PROC.Cmd_Start(args, ref output, ref error, 0);
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

            PROC.Process_Start(cliPath, args, ref output, ref error, 0);

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

            PROC.Cmd_Start(args, ref output, ref error, 0);
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

            string usb2eth_sn = SeriNumber;
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
            Media_Type media_type = Media_Type.Error;

            string zh_cn = "", en_us = "", thai = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (media_type != Media_Type.Gaps)
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

            media_value = GetLTSSensorValue(Communicate_Type.VirtualComPort);

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

            media_value = GetLTSSensorValue(Communicate_Type.VirtualComPort);

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
            string ltsVal_media = "", ltsVal_media1 = "", ltsVal_noMedia = "";
            Media_Type curr_media_type = Media_Type.Error, target_media_type = Media_Type.Gaps;
            int test_feed_time = 0, test_media_type_time = 0;
            string zh_cn = "", en_us = "", thai = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            // PA45 PN Special
            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (m_PA45.MediaHandling != MediaHandling.Peeler)
            {
                value = "NO_SELFSTRIP_ON_PRINTER";
                rlt = true;
                goto End;
            }

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.LTS);
            dut.get_LTS(tmpValue, ref ltsVal_noMedia);

            zh_cn = $"请将打印纸({target_media_type})安装到打印机里面，并确认把Gap media穿过分纸机已经安装好。";
            en_us = $"Please install the {target_media_type} into printer.";
            thai = "thai tips (unknown)";

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_PUT_MEIDA_FAIL";
                rlt = false;
                goto End;
            }

        test_feed:
            string pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg");

            bool bTestFeed = test_feed_1(pic_path);

            if (!bTestFeed)
            {
                test_feed_time = test_feed_time + 1;
                if (test_feed_time < 6)
                {
                    test_feed_time = 0;
                    goto test_feed;
                }

                value = "TEST_FEED_FAIL";
                rlt = false;
                goto End;
            }

        test_get_media_type:
            curr_media_type = GetMediaType();

            if (curr_media_type != target_media_type)
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

            zh_cn = $"请确认打印纸有贴附在Selfstrip上，并且打印纸正对LTS。";
            en_us = "";
            thai = "thai tips (unknown)";

            int test_time = 0;

        Test_LTS_No_Media:

            GetOperatorSelection(zh_cn, en_us, thai);

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.LTS);

            dut.get_LTS(tmpValue, ref ltsVal_media);

            bool rlt2 = Convert.ToDouble(ltsVal_media) > 100 && Convert.ToDouble(ltsVal_media) <= 250;
            test_time = test_time + 1;
            if (!rlt2)
            {
                if (test_time < 4)
                {
                    goto Test_LTS_No_Media;
                }
            }

            zh_cn = $"请将打印纸从LTS上移走，但是保留纸底。";
            en_us = $"Please remove media on the LTS.";
            thai = "thai tips (unknown)";

            bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_REMOVE_MEDIA_FAIL";
                rlt = false;
                goto End;
            }

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.LTS);

            dut.get_LTS(tmpValue, ref ltsVal_media1);

            bool rlt0 = Convert.ToDouble(ltsVal_noMedia) >= 0 && Convert.ToDouble(ltsVal_noMedia) <= 60;

            //60~100
            bool rlt1 =
                Convert.ToDouble(ltsVal_media1) <= Convert.ToDouble(item.UpperLimit) &&
                Convert.ToDouble(ltsVal_media1) > Convert.ToDouble(item.LowerLimit);



            rlt = rlt0 && rlt1 && rlt2;

            value = $"{ltsVal_noMedia} / {ltsVal_media} / {ltsVal_media1}";
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            string printer_PN = "", printer_SN = "", region = "";

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "MODEL_ERROR";
                rlt = false;
                goto End;
            }

            //region = m_PA45.RegionalCodeFlag.ToString();
            region = "ROW";
            printer_PN = m_PA45.HON_PN;
            printer_SN = m_PA45.SerialNumber;

            string m_sfc_address = "";

            if (Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY)
            {
                SerialPortWriteLineOnce(PrinterCommands.set_modules_printer(printer_PN, printer_SN, region));
            }
            else if (Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
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

            if (Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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
                    value = "PCBA_SERI_NUM_DISMATCH_WITH_SFC";
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (string.IsNullOrWhiteSpace(pcba_SN))
            {
                value = "PCBA_SN_NULL_ERROR";
                rlt = false;
                goto End;
            }

            string pcba_PN = _ssg_sfc.GetSpecialValue("Scanner", "GET_SN_MODEL", pcba_SN);

            if (string.IsNullOrWhiteSpace(pcba_PN))
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
            //string ssid = "", dhcp_ip = "", router_ip = "", pwd = "";
            string dutType = dut.GetType().Name;


            if (dutType == "WiFi_BT_Module")
            {

            }
            else if (dutType == "PA45")
            {
                Prod.PA45 m_PA45 = product as Prod.PA45;

                if (m_PA45 == null ||
                    Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY ||
                    Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY
                    )
                {
                    value = "NO_WIFI_SKU";
                    rlt = true;
                    goto End;
                }

            }

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }


            //WIFI IPADDRESS : 192.168.1.100
            Delay(1000);

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

            dut.get_WIFI_IPADDRESS(temp_value, ref dhcp_ip);

            AppendRuntimeLog($"get DHCP ip as {dhcp_ip}");
            if (dhcp_ip == "0.0.0.0")
            {
                value = $"PRINTER_NO_IP : {dhcp_ip}";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.ping_ip_adderss(router_ip));

            Delay(7000);
            temp_value = "";
            bTimeOut = true;
            for (int i = 0; i < 60; i++)
            {
                temp_value = GetRuntimeStringFlagA(PrinterCommands.ping_ip_adderss(router_ip).Trim());
                temp_value = temp_value.Replace(PrinterCommands.ping_ip_adderss(router_ip).Trim(), "");

                if (!string.IsNullOrWhiteSpace(temp_value))
                {
                    if (temp_value.Contains("Network is unreachable"))
                    {
                        value = "Network is unreachable";
                        rlt = false;
                        goto End;
                    }

                    if (temp_value.Contains("packets transmitted") && temp_value.Contains("packets received") && temp_value.Contains("packet loss"))
                    {
                        bTimeOut = false;
                        break;
                    }
                }

                Delay(500);
            }

            if (bTimeOut)
            {
                value = "PingTimeOut";
                rlt = false;
                goto End;
            }

            //5 packets transmitted, 0 packets received, 100% packet loss
            if (temp_value.Contains("ttl")) { rlt = true; }

            value = dhcp_ip;

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

            Prod.BT_Module m_BtModule = product as Prod.BT_Module;

            Prod.WiFi_BT_Module m_WfBtModule = product as Prod.WiFi_BT_Module;

            if (m_PA45 == null && m_WfBtModule == null && m_BtModule == null)
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
                value = "NOT_FOUND_BT_ADAPTER";
                rlt = false;
                goto End;
            }

            var primaryRadio = BluetoothRadio.PrimaryRadio;
            if (primaryRadio == null)
            {
                AppendRuntimeLog("error: unable to retrieve the main bluetooth adapter.");
                value = "NOT_FOUND_BT_ADAPTER";
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

                if (device.DeviceName.ToUpper().Contains("PA45") && device.DeviceName.ToUpper().Contains(m_PA45.SerialNumber) ||
                   device.DeviceAddress.ToString() == tmp_mac ||
                   device.DeviceName == tmp_mac)
                {
                    bFindDevice = true;
                    AppendRuntimeLog($"get dut device: {device.DeviceName}");
                    AppendRuntimeLog($"get dut device mac: {device.DeviceAddress}");
                    if (device.DeviceAddress.ToString() == tmp_mac || device.DeviceAddress.ToString() == BLUETOOTH_MAC_ADDRESS)
                    {
                        AppendRuntimeLog($"dut device mac match bt found device mac.");
                        bIsMacMatch = true;
                        break;
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            Prod.PA45 prod_PA45 = product as Prod.PA45;

            if (prod_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY || Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
            {
                value = "this_sku_no_wifi";
                rlt = true;
                goto End;
            }

            DUTS.PA45.PA45 dut_PA45 = dut as DUTS.PA45.PA45;

            if (dut_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.WIFI_ID);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.WIFI_ID);
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

            if (tmpValue.Contains("HW_NOT_INSTALLED"))
            {
                value = "HW_NOT_INSTALLED";
                rlt = false;
                goto End;
            }

            string m_sfc_address = "";
            string part_number = "", seri_number = "", address = "";
            dut_PA45.get_WIFI_ID(tmpValue, ref part_number, ref seri_number, ref address);

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
                string target_values = "WIFI MAC";
                string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
                m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);

                m_sfc_address = m_sfc_address.ToUpper();
                AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");
            }

            string input_wifi_mac = Interaction.InputBox("please scan the WIFI MAC ADDRESS on printer");

            if (ckbSFCS.Checked)
            {
                rlt = (input_wifi_mac.ToUpper() == address.ToUpper()) && (input_wifi_mac.ToUpper() == m_sfc_address.ToUpper());
                if (rlt)
                {
                    value = address.ToUpper();
                }
                else
                {
                    value = $"MAC_DISMATCH({address.ToUpper()} / {input_wifi_mac.ToUpper()} / {m_sfc_address.ToUpper()})";
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

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_ERROR";
                rlt = false;
                goto End;
            }

            string m_SN = m_PA45.HON_PN + ";" + m_PA45.SerialNumber;

            if (Prod.PA45.CommHWOpt == CommHWOpt.ERROR)
            {
                value = "PRODUCT_COMM_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY)
            {
                value = "SKU_NO_MAC";
                rlt = true;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
            {
                value = "BT_ONLY_SKU";
                rlt = true;
                goto End;
            }

            string bt_wifi_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + $";{Prod.WiFi_BT_Module.PartName}");
            AppendRuntimeLog($"{MainBarcode} get sfc wifi module SN is {bt_wifi_module_sn}.");

            if (string.IsNullOrWhiteSpace(bt_wifi_module_sn))
            {
                value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                rlt = false;
                goto End;
            }

            string station = "BT_Wifi_board_Function_Test", target_values = "WIFI MAC";
            string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
            string m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);

            m_sfc_address = m_sfc_address.ToUpper();
            AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");

            if (string.IsNullOrWhiteSpace(m_sfc_address))
            {
                value = "MAC_IS_EMPTY";
                rlt = false;
                goto End;
            }
            WIFI_MAC_ADDRESS = m_sfc_address;
            value = m_sfc_address;

            if (!File.Exists(WifiMacLabFile))
            {
                value = "LABFILE_ERROR";
                rlt = false;
                goto End;
            }

            string key = INI.GetValue("PrintMACSetting", "Wifi_KeyName ", ConfigFile);
            Invoke(new Action(() => { rlt = PRINTER.PrintLabel(WifiMacLabFile, key, WIFI_MAC_ADDRESS); }));
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_PrintBluetoothAddress(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_ERROR";
                rlt = false;
                goto End;
            }

            string m_SN = m_PA45.HON_PN + ";" + m_PA45.SerialNumber;

            if (Prod.PA45.CommHWOpt == CommHWOpt.ERROR)
            {
                value = "PRODUCT_COMM_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY)
            {
                value = "SKU_NO_MAC";
                rlt = true;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
            {
                string bt_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114400_BT_board");

                if (string.IsNullOrWhiteSpace(bt_module_sn))
                {
                    value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                    rlt = false;
                    goto End;
                }

                string m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GET_AUTO_MAC_ADD", $"{Prod.BT_Module.Dongguan_SFC_MAC_Resource};{bt_module_sn}").ToUpper();


                AppendRuntimeLog($"{bt_module_sn} get sfc mac is {m_sfc_address}.");
                if (string.IsNullOrWhiteSpace(m_sfc_address))
                {
                    value = "MAC_IS_EMPTY";
                    rlt = false;
                    goto End;
                }
                BLUETOOTH_MAC_ADDRESS = m_sfc_address;
                value = m_sfc_address;
            }
            else
            {
                string bt_wifi_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + $";{Prod.WiFi_BT_Module.PartName}");
                AppendRuntimeLog($"{MainBarcode} get sfc wifi module SN is {bt_wifi_module_sn}.");

                if (string.IsNullOrWhiteSpace(bt_wifi_module_sn))
                {
                    value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                    rlt = false;
                    goto End;
                }

                string station = "BT_Wifi_board_Function_Test", target_values = "BT MAC";
                string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
                string m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);

                m_sfc_address = m_sfc_address.ToUpper();
                AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");

                if (string.IsNullOrWhiteSpace(m_sfc_address))
                {
                    value = "MAC_IS_EMPTY";
                    rlt = false;
                    goto End;
                }
                BLUETOOTH_MAC_ADDRESS = m_sfc_address;
                value = m_sfc_address;
            }

            if (!File.Exists(WifiMacLabFile))
            {
                value = "LABFILE_ERROR";
                rlt = false;
                goto End;
            }

            string key = INI.GetValue("PrintMACSetting", "BT_KeyName", ConfigFile);
            Invoke(new Action(() => { rlt = PRINTER.PrintLabel(BluetoothMacLabFile, key, BLUETOOTH_MAC_ADDRESS); }));
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

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
            m_sn = $"{m_PA45.HON_PN.Substring(0, 5)}-{m_sn}";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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
            Prod.PA45 prod_PA45 = product as Prod.PA45;
            Media_Type media_type = Media_Type.Error, target_media_type = Media_Type.Error;
            if (prod_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (prod_PA45.MediaHandling == MediaHandling.LinerlessPlatenRoller)
            {
                target_media_type = Media_Type.ContinuousVarLen;
            }
            else
            {
                target_media_type = Media_Type.Gaps;
            }

            string zh_cn = $"请把{Media_Type.Gaps.get_Media_Type()}放进载纸装置中";
            string en_us = $"please put {Media_Type.Gaps.get_Media_Type()} into media holder.";
            string thai = $"please put {Media_Type.Gaps.get_Media_Type()} into media holder.";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_PUT_MEDIA_FAIL";
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
            string sCmd = PrinterCommands.GET_MEDIA_LENGTH;

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd);

            value = tmpValue.Replace("Ok", "").Trim();

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
            string barcode_script_path = Path.Combine(Global.ASSEMBLY_PATH, "Script", "Nomad_PQ_Barcode.txt");
            string zh_cn = "", en_us = "", thai = "";

            Prod.PA45 prod_PA45 = product as Prod.PA45;
            Media_Type media_type = Media_Type.Error, target_media_type = Media_Type.Error;
            if (prod_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (prod_PA45.MediaHandling == MediaHandling.LinerlessPlatenRoller)
            {
                target_media_type = Media_Type.ContinuousVarLen;
                barcode_script_path = Path.Combine(Global.ASSEMBLY_PATH, "Script", "Nomad_PQ_Barcode_Linerless.txt");
            }
            else
            {
                target_media_type = Media_Type.Gaps;
                barcode_script_path = Path.Combine(Global.ASSEMBLY_PATH, "Script", "Nomad_PQ_Barcode.txt");
            }

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (string.IsNullOrWhiteSpace(PrinterSeriNumber) || string.IsNullOrWhiteSpace(PrinterPartNumber))
            {
                AppendRuntimeLog($"get printer info...");

                SerialPortWriteLineOnce(PrinterCommands.modules_printer);

                for (int i = 0; i < 200; i++)
                {
                    tmpValue = GetRuntimeStringFlagA("get printer info...");
                    tmpValue = tmpValue.Replace(PrinterCommands.modules_printer, "");
                    if (!string.IsNullOrWhiteSpace(tmpValue))
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

            /*
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

            */


            string barcode_script;
            using (StreamReader reader = new StreamReader(barcode_script_path)) { barcode_script = reader.ReadToEnd(); }

            PrinterSeriNumber = prod_PA45.SerialNumber;

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

            string m_cmd = "", pq_type = "";
            Media_Type media_type = Media_Type.Error;

            if (item.Name.Contains("Chess"))
            {
                m_cmd = PrinterCommands.testlabel_cmd(testlabel_list.pq_chess);
                pq_type = "pq_chess";
                media_type = Media_Type.ContinuousVarLen;
            }
            else
            {
                m_cmd = PrinterCommands.testlabel_cmd(testlabel_list.pq_ruler);
                pq_type = "pq_ruler";
                media_type = Media_Type.ContinuousVarLen;
            }

            SerialPortWriteLineOnce(PrinterCommands.setup_printer_speed(125));
            SerialPortWriteLineOnce(PrinterCommands.setup_printer_quality_darkness(10));

            string zh_cn = $"请将打印纸{media_type}安装到打印机里面，并确认已经安装好。";
            string en_us = "Please install the media into printer.";
            string thai = "thai tips (unknown)";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_PUT_MEDIA_ERROR";
                rlt = false;
                goto End;
            }

            if (CurrentMediaType != Media_Type.ContinuousVarLen)
            {
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

                if (media_type != Media_Type.ContinuousVarLen)
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
            }

            Invoke(new Action(() =>
            {
            PRINT:
                SerialPortWriteLineOnce(m_cmd);
                Delay(3000);

                frmTips dialog = new frmTips(
                    $"这是第 { current_time.ToString() } 打印, 请确认{pq_type}打印品质",
                    "please check printing quality of Chess pattern.",
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

        private bool Test_HostUsb(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "", pic_path = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }
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

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.USB_HOST);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            tmpValue = tmpValue.Replace("PASS", "").Trim();
            tmpValue = tmpValue.Replace("Ok", "").Trim();
            //File 1: test.txt
            rlt = tmpValue == "File 1: test.txt";

            value = tmpValue;

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

            string sCmd = "";
            string type_name = dut.GetType().Name;
            if (type_name == "PA45")
            {
                sCmd = PrinterCommands.AUDIO;

                if (!check_unlock_os_cmds())
                {
                    value = "RUN_OS_CMDS_DISABLE";
                    rlt = false;
                    goto End;
                }

                SerialPortWriteLineOnce(sCmd);
            }
            else
            {
                sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.AUDIO);
                SeriPortWriteLine(spUART, sCmd);
            }

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                if (type_name == "PA45") { tmpValue = GetRuntimeStringFlagA(sCmd); }
                else { tmpValue = GetUARTStringFlagA(sCmd); }

                tmpValue = tmpValue.Replace(sCmd, "");
                if (!string.IsNullOrWhiteSpace(tmpValue))
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

            if (tmpValue.Contains(Printer_Error_Code.HW_NOT_INSTALL.ToString()))
            {
                value = $"{Printer_Error_Code.HW_NOT_INSTALL}";
                rlt = false;
                goto End;
            }

            tmpValue = tmpValue.Replace("Ok", "");
            tmpValue = tmpValue.Trim();

            bool bSelfChk = ((value = tmpValue.Trim()) == "PASS");

            string zh_cn = "请确认蜂鸣器是否播放音乐。";
            string en_us = "please check the buzzer have played a audio.。";
            string thai = "thai tips (unknown)";

            bool bOpHeared = GetOperatorSelection(zh_cn, en_us, thai);
            if (!bOpHeared)
            {
                value = "CANNOT_HEAR_AUDIO";
                rlt = false;
                goto End;
            }

            rlt = bOpHeared;
            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value;
            return rlt;
        }

        private event KeyButtonTestedEventHanlder KeyButtonTested;

        private bool Test_Printer_KeyButton(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            string m_tag = Guid.NewGuid().ToString("N");
            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            if (dut.GetType().Name == "PA45")
            {
                AppendRuntimeLog(m_tag);
                SerialPortWriteLineOnce(PrinterCommands.KEYPAD);
            }
            else if (dut.GetType().Name == "PA45_MainBoard")
            {
                AppendSeriPortLog(m_tag);
                SeriPortWriteLine(spUART, PA45_FCT_Commands.functest(SUB_COMMAND.KEYPAD));
            }
            else
            {
                value = "DUT_TYPE_ERROR";
                rlt = false;
                goto End;
            }

            bool b_time_out = true;
            Invoke(new Action(() =>
            {
                frmKeybutton dialog = new frmKeybutton();
                KeyButtonTested += dialog.OnKeyButtonTested;
                dialog.Show();

                for (int i = 0; i < 120; i++)
                {
                    string tmpValue = "";

                    if (dut.GetType().Name == "PA45")
                    {
                        tmpValue = GetRuntimeStringFlagA(m_tag);
                    }

                    if (dut.GetType().Name == "PA45_MainBoard")
                    {
                        tmpValue = GetUARTStringFlagA(m_tag);
                    }

                    if (productConfig.STATION_CODE == "FCT")
                    {
                        /*
                         * Relay1-5
                         * Relay1-6
                         * Relay1-7
                         * Relay1-8
                         * Relay2-1
                         */

                        RelayCard1.SetRelayOn(4);
                        Delay(500);
                        RelayCard1.SetRelayOff(4);
                        Delay(500);

                        RelayCard1.SetRelayOn(5);
                        Delay(500);
                        RelayCard1.SetRelayOff(5);
                        Delay(500);

                        RelayCard1.SetRelayOn(6);
                        Delay(500);
                        RelayCard1.SetRelayOff(6);
                        Delay(500);

                        RelayCard1.SetRelayOn(7);
                        Delay(500);
                        RelayCard1.SetRelayOff(7);
                        Delay(500);

                        RelayCard2.SetRelayOn(0);
                        Delay(500);
                        RelayCard2.SetRelayOff(0);
                        Delay(500);

                    }

                    if (!string.IsNullOrEmpty(tmpValue))
                    {
                        string[] keyCodes = tmpValue.Split('\n');
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

            Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.Quit);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_LssSensor(int index, TestItem item)
        {
            bool rlt = false, bTimeout = true;
            string value = "", tmpValue = "";
            string zh_cn = "", en_us = "", thai = "", pic_path = "";
            bool isDelta = false;
            string lss_test_tpye = "";
            Media_Type media_type = Media_Type.Error;

            SerialPortWriteLineOnce(PrinterCommands.set_rfid_disable);

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

                if (media_type != Media_Type.Gaps)
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LSS);

            bTimeout = true;
            tmpValue = "";
            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(item.Name);
                tmpValue = tmpValue.Replace(PrinterCommands.LSS, "");
                if (!string.IsNullOrWhiteSpace(tmpValue))
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
            string drive = "", gain = "";

            dut.get_LSS(tmpValue, ref value, ref drive, ref gain);

            if (string.IsNullOrWhiteSpace(value))
            {
                value = "LSS_SENSOR_ADC_VALUE_IS_NULL";
                rlt = false;
                goto End;
            }

            if (item.Name.Contains("No Media")) { DUTS.PA45.PA45.LSS_SENSOR_VALUE_NO_MEDIA = Convert.ToDouble(value); }
            else if (item.Name.Contains("Liner")) { DUTS.PA45.PA45.LSS_SENSOR_VALUE_LINER = Convert.ToDouble(value); }
            else { DUTS.PA45.PA45.LSS_SENSOR_VALUE_MEDIA = Convert.ToDouble(value); }

            if (isDelta)
            {
                value = Convert.ToString(DUTS.PA45.PA45.LSS_SENSOR_VALUE_LINER - DUTS.PA45.PA45.LSS_SENSOR_VALUE_MEDIA);
            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Printer_LssSensor_230(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = PrinterCommands.LSS_7_230;

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }
            SerialPortWriteLineOnce(sCmd);
            sCmd = PrinterCommands.LSS;
            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }
            string drive = "", gain = "";

            dut.get_LSS(tmpValue, ref value, ref drive, ref gain);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_BMSensor(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "", pic_path = "";
            string sCmd = "", bm_sensor_position = "";
            int test_feed_times = 0, calibrate_media_times = 0;
            string zh_cn = "", en_us = "", thai = "";
            Media_Type media_type = Media_Type.Error;
            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            if (item.Name.Contains("Front") || item.Name.Contains("Top"))
            {
                sCmd = PrinterCommands.FRONT_BM;
                bm_sensor_position = "F";
            }
            else
            {
                sCmd = PrinterCommands.BM;
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

            if (Prod.PA45.CommHWOpt == CommHWOpt.WIFI_BLUETOOTH_RFID)

                if (bm_sensor_position == "F")
                {
                    zh_cn = "请放Front BM测试纸(黑边朝外)测试,进行testfeed -1测试。";
                    pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "printer_black_mark_outside.jpg.");
                }
                else
                {
                    zh_cn = "请放Bottem BM测试纸(黑边朝内)测试,进行testfeed -1测试。";
                    pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "printer_black_mark_inside.jpg.");
                }

            en_us = "please check testfeed -1 have finished yet.";
            thai = "thai tips (unknown)";
            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai, pic_path);

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

            if (media_type != Media_Type.BlackMark)
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
            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd);

            if (String.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }
            string drive = "", gain = "";
            dut.get_BM(tmpValue, ref value, ref drive, ref gain);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_Printer_SensorTemp(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            string sCmd = PrinterCommands.PRINTHEAD;
            tmpValue = Write_Read_SeriPort_6000(Communicate_Type.VirtualComPort, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            if (tmpValue.Contains("FAIL") || tmpValue.Contains("Fail"))
            {
                value = "FAIL";
                rlt = true;
                goto End;
            }

            dut.get_PRINTHEAD(tmpValue, ref value);

            rlt = (value == item.UpperLimit && value == item.LowerLimit);

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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
            rlt = !string.IsNullOrWhiteSpace(part);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_SetRtc(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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
            string value = "", tmpValue = "";
            string sCmd = "";
            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            sCmd = PrinterCommands.GetUbootVersion();
            SerialPortWriteLineOnce(sCmd);

            /*
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
            */

            /*
             * U - Boot SPL 2021.04(Dec 31 2025 - 18:48:48 + 0800)
             * U - Boot     2021.04(Jan 20 2026 - 10:57:03 + 0800)
             */

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd);

            List<string> versions = tmpValue.Split('\n').ToList();
            versions.RemoveAll(s => string.IsNullOrEmpty(s));

            List<string> upperLimits = item.UpperLimit.Split(';').ToList();
            List<string> lowerLimits = item.LowerLimit.Split(';').ToList();

            bool upper_result = false, lower_result = false;

            /*
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
            */

            /*
            if (versions.Contains(item.UpperLimit) && versions.Contains(item.LowerLimit))
                rlt = true;
            else
                rlt = false;
                */


            foreach (string ver in versions)
            {
                if (ver.Contains(item.UpperLimit))
                {
                    lower_result = true;
                    break;
                }
            }

            foreach (string ver in versions)
            {
                if (ver.Contains(item.LowerLimit))
                {
                    upper_result = true;
                    break;
                }
            }

            rlt = upper_result && lower_result;

            value = string.Join(" / ", versions);
        End:
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.HEAD_LIFT_SENSOR);

            if (String.IsNullOrWhiteSpace(tmpValue))
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

            if (productConfig.STATION_CODE.Contains("FCT"))
            {
                RelayCard2.SetRelayOn(0);
                Delay(500);
                goto Check_Cover_Lowered;
            }

            bOpStep = GetOperatorSelection("请将载纸装置关闭", "please open the media cover.", "thai tips (unknown)");

            if (!bOpStep)
            {
                value = "OP_CLOSE_COVER_FAIL";
                rlt = false;
                goto End;
            }

        Check_Cover_Lowered:

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.HEAD_LIFT_SENSOR);

            if (String.IsNullOrWhiteSpace(tmpValue))
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

        private bool Test_Printer_MFI_Detect(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string sCmd = PrinterCommands.MFI_INFO;
            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd);

            rlt = tmpValue.Contains("PASS");
            value = rlt ? "PASS" : "FAIL";

        End:

            item.Value = value;
            return rlt;
        }


        private bool Test_Printer_SecurityChip(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }
            AppendRuntimeLog("step1: to initialize chip...");
            tmpValue = Write_Read_SeriPort_Delay(Communicate_Type.VirtualComPort, PrinterCommands.SECURITY_CHIP_OTP, 3000);

            if (tmpValue.Contains("PASS"))
            {
                AppendRuntimeLog("this is the frist time to OTP.");
            }
            else
            {
                AppendRuntimeLog("this isn't the frist time to OTP.");
            }

            AppendRuntimeLog("step2: to generate key...");
            tmpValue = Write_Read_SeriPort_Delay(Communicate_Type.VirtualComPort, PrinterCommands.SECURITY_CHIP_GEN_KEY, 3000);

            if (tmpValue.Contains("PASS"))
            {
                AppendRuntimeLog("this is the frist time to GEN_KEY.");
            }
            else
            {
                AppendRuntimeLog("this isn't the frist time to GEN_KEY.");
            }

            AppendRuntimeLog("step3: to vry key...");
            tmpValue = Write_Read_SeriPort_Delay(Communicate_Type.VirtualComPort, PrinterCommands.SECURITY_CHIP_VRF_KEY, 3000);

            if (tmpValue.Contains("PASS"))
            {
                AppendRuntimeLog("this is the frist time to GEN_KEY.");
            }
            else
            {
                AppendRuntimeLog("this isn't the frist time to GEN_KEY.");
            }


            AppendRuntimeLog("step4: get key...");
            tmpValue = Write_Read_SeriPort_Delay(Communicate_Type.VirtualComPort, PrinterCommands.SECURITY_CHIP_GET_DEVICE_ID, 3000);

            if (tmpValue.Contains("PASS"))
            {
                AppendRuntimeLog("this is the frist time to GEN_KEY.");
            }
            else
            {
                AppendRuntimeLog("this isn't the frist time to GEN_KEY.");
            }

            string model_id = "", seri_num = "", pub_key = "";

            dut.get_public_key(tmpValue, ref model_id, ref seri_num, ref pub_key);

            value = pub_key;
            rlt = !string.IsNullOrWhiteSpace(value);

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
                if (!string.IsNullOrWhiteSpace(tmpValue))
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

            dut.get_CHARGER(tmpValue, ref value);
            rlt = !string.IsNullOrWhiteSpace(value);
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
                if (!string.IsNullOrWhiteSpace(tmpValue))
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

            dut.get_MOTION(tmpValue, ref value);
            rlt = !string.IsNullOrWhiteSpace(value);
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_MotionSensor1(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string sCmd = PrinterCommands.enable_rotation;

            SerialPortWriteLineOnce(sCmd);
            Delay(1000);
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.LCD_GREEN);

            string zh_cn = "请确认LCD屏幕是否是绿色。";
            string en_us = "please comfirm the LCD screen is green.";
            string thai = "thai tips (unknown)";
            bool bGreen = GetOperatorSelection(zh_cn, en_us, thai);
            if (!bGreen)
            {
                value = "GREEN_LCD_DISPLAY_FIAL";
                rlt = false;
                goto End;
            }

            Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.Quit);
            Delay(1000);
            SerialPortWriteLineOnce(PrinterCommands.LCD_BLACK);
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

            Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.Quit);

            Delay(1000);
            SerialPortWriteLineOnce(PrinterCommands.LCD_WHITE);
            zh_cn = "请确认LCD屏幕是否是白色。";
            en_us = "please comfirm the LCD screen is white.";
            thai = "thai tips (unknown)";
            bool bWhite = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bWhite)
            {
                value = "WHITE_LCD_DISPLAG_FIAL";
                rlt = false;
                goto End;
            }

            Write_Read_SeriPort(Communicate_Type.VirtualComPort, PrinterCommands.Quit);

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.ERROR)
            {
                value = "COMM_OPTION_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY)
            {
                value = "USB_ONLY_SKU";
                rlt = true;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
            {
                SerialPortWriteLineOnce(PrinterCommands.read_bluetooth_mac_for_bt_only_module);
                Delay(2000);
                for (int i = 0; i < 60; i++)
                {
                    tmpValue = GetRuntimeStringFlagA(PrinterCommands.read_bluetooth_mac_for_bt_only_module);
                    tmpValue = tmpValue.Replace(PrinterCommands.read_bluetooth_mac_for_bt_only_module, "");
                    if (!string.IsNullOrWhiteSpace(tmpValue))
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
                values.RemoveAll(s => string.IsNullOrWhiteSpace(s));

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
                    if (!string.IsNullOrWhiteSpace(tmpValue))
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

                dut.get_BLUETOOTH_ID(tmpValue, ref part_number, ref seri_number, ref address);
            }

            AppendRuntimeLog($"detected printer's bluetooth mac is {address}");

            if (string.IsNullOrWhiteSpace(address))
            {
                value = "MAC_ADDRESS_NULL";
                rlt = false;
                goto End;
            }

            BLUETOOTH_MAC_ADDRESS = address;

            if (ckbSFCS.Checked)
            {
                if (Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
                {
                    string bt_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114400_BT_board");

                    if (string.IsNullOrWhiteSpace(bt_module_sn))
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

                    if (string.IsNullOrWhiteSpace(bt_wifi_module_sn))
                    {
                        value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                        rlt = false;
                        goto End;
                    }

                    string station = "BT_Wifi_board_Function_Test";
                    string target_values = "BT MAC";
                    string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
                    m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);
                    m_sfc_address = m_sfc_address.ToUpper();
                    AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");
                }
            }

            string input_bt_mac = Interaction.InputBox("please scan the BT MAC ADDRESS on printer");

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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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
            string value = "", sCmd = "";

            string zh_cn = "", en_us = "", thai = "";
            Prod.PA45 m_PA45 = product as Prod.PA45;
            if (m_PA45 == null)
            {
                value = "PRODUCT_ERROR";
                rlt = false;
                goto End;
            }

            if (!m_PA45.CommunicationOption.GetDescription().ToUpper().Contains("RFID"))
            {
                value = "NO_RFID_CONFIG";
                rlt = true;
                goto End;
            }

            zh_cn = "请把RFID标签纸装好在打印机。";
            en_us = "Please set the RFID media tag into media holder.";
            thai = "unknown";

            SerialPortWriteLineOnce(PrinterCommands.set_rfid_enable);
            int test_time = 0;

        test_rfid:
            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_PUT_RFID_TAG_FAIL";
                rlt = false;
                goto End;
            }

            string offset = Interaction.InputBox("PLS input RFID OFFSET");

            /*
            zh_cn = "请手动校准RFID，选择Intensive Calibration，并且确认是否校准成功。";
            en_us = "Please do RDIF Intensive Calibration manually.";
            thai = "unknown";

            bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_RFID_CALIB_FAIL";
                rlt = false;
                goto End;
            }

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }
            */

            sCmd = $"ff{offset}";
            SerialPortWriteLineOnce(sCmd);

            sCmd = PrinterCommands.rfid_functest;
            value = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd);

            if (!value.Contains("PASS"))
            {
                test_time = test_time + 1;
                if (test_time < 5)
                {
                    goto test_rfid;
                }
            }

            rlt = (value.Contains("PASS"));

            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_SetRFIDRegionCode(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string sCmd = "";
            Prod.PA45 m_PA45 = product as Prod.PA45;
            if (m_PA45 == null)
            {
                value = "PRODUCT_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.ERROR)
            {
                value = "PRODUCT_COMM_OPT_ERROR";
                rlt = false;
                goto End;
            }

            if (!Prod.PA45.CommHWOpt.ToString().ToUpper().Contains("RFID"))
            {
                value = "NO_RFID_CONFIG";
                rlt = true;
                goto End;
            }

            string m_tag = Guid.NewGuid().ToString("N");
            AppendRuntimeLog(m_tag);

            sCmd = PrinterCommands.cat_rfid;

            string rfid_rgc = m_PA45.CommunicationOption.GetRIFDRegionAttribute().GetDescription();
            string rfid_hw = "IM21A2010";

            string sScript = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "Script", "RFID_Country_freq_table_23June2026.txt"));
            sScript = sScript.Replace("PA45_RFID_Country_Code_Placeholder", rfid_rgc);
            sScript = sScript.Replace("PA45_RFID_cfgnum_Placeholder", rfid_hw);

            SerialPortWriteLineOnce(sScript);
            spTester.BaseStream.Flush(); // 等待驱动把这块推到硬件TX FIFO

            Delay(8000);

            tmpValue = GetRuntimeStringFlagA(m_tag);

            rlt = true;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_CheckRFIDRegionCode(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "", sCmd = "";

            Prod.PA45 m_PA45 = product as Prod.PA45;
            if (m_PA45 == null)
            {
                value = "PRODUCT_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.ERROR)
            {
                value = "PRODUCT_COMM_OPT_ERROR";
                rlt = false;
                goto End;
            }

            if (!Prod.PA45.CommHWOpt.ToString().ToUpper().Contains("RFID"))
            {
                value = "NO_RFID_CONFIG";
                rlt = true;
                goto End;
            }

            RFID_CC rfid_cc = m_PA45.CommunicationOption.GetRIFDRegionAttribute();
            string rfid_rgc_spec = rfid_cc.GetDescription();
            sCmd = PrinterCommands.rfid_version;
            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_RFID_CC(tmpValue, ref value);
            rlt = value == rfid_rgc_spec;
            SerialPortWriteLineOnce(PrinterCommands.set_rfid_disable);
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_CheckLockingStatus(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            SerialPortWriteLineOnce(PrinterCommands.chekc_locking_status);

            for (int i = 0; i < 60; i++)
            {
                tmpValue = GetRuntimeStringFlagA(PrinterCommands.chekc_locking_status);
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

            dut.get_SecureBoot(tmpValue, ref value);

            rlt = value == item.UpperLimit && value == item.LowerLimit;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_RecoverySetting(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

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

        private bool Test_Printer_PrintMACLabel(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string LabFile = "", LabFilePath = "";
            if (item.Name.ToUpper().Contains("WIFI")) { LabFile = WifiMacLabFile; }
            else { LabFile = BluetoothMacLabFile; }

            LabFilePath = Path.Combine(Global.ASSEMBLY_PATH, "LabFile", LabFile);

            Prod.PA45 m_PA45 = product as Prod.PA45;

            if (m_PA45 == null)
            {
                value = "PRODUCT_CONFIG_ERROR";
                rlt = false;
                goto End;
            }

            string printerSN = m_PA45.HON_PN + ";" + m_PA45.SerialNumber;
            if (Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY)
            {
                value = "USB_ONLY_SKU";
                rlt = true;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.ERROR)
            {
                value = "COMM_OPTION_ERROR";
                rlt = false;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY)
            {
                string m_ItemName = item.Name.ToUpper();
                if (m_ItemName.Contains("WIFI"))
                {
                    value = "NO_WIFI_CONFIG";
                    rlt = true;
                    goto End;
                }

                string bt_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114400_BT_board");
                AppendRuntimeLog($"{MainBarcode} get sfc bt module SN is {bt_module_sn}.");

                if (string.IsNullOrEmpty(bt_module_sn) || string.IsNullOrWhiteSpace(bt_module_sn))
                {
                    value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                    rlt = false;
                    goto End;
                }

                string m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GET_AUTO_MAC_ADD", $"AMFIS16S00000056;{bt_module_sn}");
                value = m_sfc_address;
                BLUETOOTH_MAC_ADDRESS = value;
            }
            else
            {
                string m_ItemName = item.Name.ToUpper();
                if (m_ItemName.Contains("WIFI"))
                {
                    string bt_wifi_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114390_BT_Wifi_board");
                    AppendRuntimeLog($"{MainBarcode} get sfc wifi module SN is {bt_wifi_module_sn}.");

                    if (string.IsNullOrEmpty(bt_wifi_module_sn) || string.IsNullOrWhiteSpace(bt_wifi_module_sn))
                    {
                        value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                        rlt = false;
                        goto End;
                    }

                    string station = "BT_Wifi_board_Function_Test", target_values = "WIFI MAC";
                    string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
                    string m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);

                    m_sfc_address = m_sfc_address.ToUpper();
                    AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");
                    value = m_sfc_address;
                    WIFI_MAC_ADDRESS = value;
                }
                else
                {
                    string bt_wifi_module_sn = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114390_BT_Wifi_board");
                    AppendRuntimeLog($"{MainBarcode} get sfc wifi module SN is {bt_wifi_module_sn}.");

                    if (string.IsNullOrWhiteSpace(bt_wifi_module_sn))
                    {
                        value = "CANNOT_GET_WIFI_SN_FROM_SFC";
                        rlt = false;
                        goto End;
                    }

                    string station = "BT_Wifi_board_Function_Test", target_values = "BT MAC";
                    string param = station + ";" + bt_wifi_module_sn + ";" + target_values;
                    string m_sfc_address = _ssg_sfc.GetSpecialValue("Scanner", "GetTableLastValues", param);
                    m_sfc_address = m_sfc_address.ToUpper();
                    AppendRuntimeLog($"{bt_wifi_module_sn} get sfc mac is {m_sfc_address}.");
                    value = m_sfc_address;
                    BLUETOOTH_MAC_ADDRESS = value;
                }
            }



            if (Convert.ToBoolean(INI.GetValue("PrintMACSetting", "IsPrint", ConfigFile)))
            {
                rlt = PRINTER.PrintLabel(LabFilePath, INI.GetValue("PrintMACSetting", "KeyName", ConfigFile), value.ToUpper()) ? true : false;
                AppendRuntimeLog($"Is Print enable is :{LabFilePath}");
            }
            else
            {
                rlt = true;
            }

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_RFIDFunc(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";



            rlt = true;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Printer_SetRFIDRegion(int index, TestItem item)
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

        private bool Test_Printer_CheckRFIDRegion(int index, TestItem item)
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
                m_voltage = RelayCard1.MeasureVoltage(0);
                rlt = RelayCard1.SetRelayOff(2);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(0);
            }

            value = Convert.ToString(m_voltage);
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

            value = Convert.ToString(m_voltage);
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
                RelayCard1.SetRelayOff(4);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(2);
            }

            value = Convert.ToString(m_voltage);
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
                m_voltage = RelayCard1.MeasureVoltage(3);
                RelayCard1.SetRelayOff(5);
            }
            else
            {
                m_voltage = RelayCard1.MeasureVoltage(3);
            }

            value = Convert.ToString(m_voltage);
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
                RelayCard1.SetRelayOff(7);
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

        private bool Test_Keyboard_JigAirPressure(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            DI_Status channel0 = RelayCard1.GetRelayCardDI(0);
            Delay(100);
            DI_Status channel1 = RelayCard1.GetRelayCardDI(1);

            value = $"{(int)channel0} / {(int)channel1}";

            if ((int)channel0 == 0 && (int)channel1 == 0)
            {
                MessageBox.Show("Jig Air Pressure is too high", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);

                value = $"{value} (Jig Air Pressure is too high)";
                rlt = false;
            }

            if ((int)channel0 == 1 && (int)channel1 == 1)
            {
                MessageBox.Show("Jig Air Pressure is too low", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Information);

                value = $"{value} (Jig Air Pressure is too low)";
                rlt = false;
            }

            if ((int)channel0 == 1 && (int)channel1 == 0) { rlt = true; }

            item.Value = value;
            //rlt = item.IsValueInRange<double>();
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

            CloseAllRelays();
            Delay(3000);

            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_2_ON-FAIL";
                goto End;
            }

            for (int i = 0; i < 200; i++)
            {
                tmpValue = ReadSerialportDataReceived();

                if (tmpValue.Contains(PA45_FCT_Commands.none_login))
                {
                    bFlag = true;
                    break;
                }

                Delay(1000);
            }

            if (!bFlag)
            {
                value = "FIND_DEVICE_TIMEOUT";
                rlt = false;
                goto End;
            }

            Delay(1000);
            rlt = true;
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MainBoard_FWFlash(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
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

            string mfg2_tmp_path = "";
            string mfg2_OK_Upper = Path.Combine(Global.ASSEMBLY_PATH, "Res", "MfgTool_OK.PNG");
            string mfg2_OK_Lower = Path.Combine(Global.ASSEMBLY_PATH, "Res", "MfgTool_OK.png");

            if (!File.Exists(mfg2_OK_Upper) && !File.Exists(mfg2_OK_Lower))
            {
                value = "NO_FW_TOOL";
                rlt = false;
                goto End;
            }

            if (File.Exists(mfg2_OK_Upper)) { mfg2_tmp_path = mfg2_OK_Upper; }
            if (File.Exists(mfg2_OK_Lower)) { mfg2_tmp_path = mfg2_OK_Lower; }

            Delay(3000);
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = mfg_tool_path;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(mfg_tool_path);
                proc.Start();

                // 等待进程初始化并创建主窗口 (非常重要)
                proc.WaitForInputIdle();
                Delay(1000);

                PropertyCondition procCond = new PropertyCondition(AutomationElement.ProcessIdProperty, proc.Id);
                AutomationElementCollection windows = AutomationElement.RootElement.FindAll(TreeScope.Children, procCond);

                AutomationElement targetWindow = null;
                for (int i = 0; i < windows.Count; i++)
                {
                    AutomationElement tmpUI = windows[i];
                    string name = tmpUI.Current.Name;

                    if (name.IndexOf("MfgTool") >= 0)
                    {
                        targetWindow = tmpUI;
                        break;
                    }
                }

                if (targetWindow == null)
                {
                    proc.Kill();
                    proc.Close();
                    proc.Dispose();
                    value = "CANNOT_GET_TARGET_WINDOW";
                    rlt = false;
                    goto End;
                }

                IntPtr hwnd = (IntPtr)targetWindow.Current.NativeWindowHandle;
                //WinUI.ForceBringWindowToFront(hwnd);

                Condition start_cdtn = new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.NameProperty, "Start"));

                Condition exit_cdtn = new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.NameProperty, "Exit"));

                Condition ProgressBarCondition =
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ProgressBar);


                AutomationElement flashBtn = targetWindow.FindFirst(TreeScope.Descendants, start_cdtn);
                AutomationElement exitBtn = targetWindow.FindFirst(TreeScope.Descendants, exit_cdtn);

                AutomationElementCollection progressBars = targetWindow.FindAll(TreeScope.Descendants, ProgressBarCondition);

                if (flashBtn == null || exitBtn == null)
                {
                    proc.Kill();
                    proc.Close();
                    proc.Dispose();
                    value = "CANNOT_START_FLASH_FW";
                    rlt = false;
                    goto End;
                }

                AppendRuntimeLog("find Start / Exit button successfully.");
                if (progressBars.Count == 0)
                {
                    proc.Kill();
                    proc.Close();
                    proc.Dispose();
                    value = "CANNOT_FIND_STATUSBAR";
                    rlt = false;
                    goto End;
                }
                AppendRuntimeLog("find the two flashing status progressbar successfully.");

                //4. 获取 RangeValuePattern 模式
                object barObj0, barObj1;
                if (!progressBars[0].TryGetCurrentPattern(RangeValuePattern.Pattern, out barObj0) ||
                    !progressBars[1].TryGetCurrentPattern(RangeValuePattern.Pattern, out barObj1))
                {
                    proc.Kill();
                    proc.Close();
                    proc.Dispose();
                    value = "STATUSBAR_ERROR";
                    rlt = false;
                    goto End;
                }

                RangeValuePattern bar0 = barObj0 as RangeValuePattern;
                RangeValuePattern bar1 = barObj1 as RangeValuePattern;

                InvokePattern StartFlashProcess = flashBtn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                StartFlashProcess?.Invoke();

                Emgu.CV.Image<Bgr, byte> template = new Image<Bgr, byte>(mfg2_tmp_path);
                Emgu.CV.Image<Bgr, byte> ScreenImage;

                double maxVal = 0, minVal = 0;
                Point minLoc = new Point(0, 0);
                Point maxLoc = new Point(0, 0);
                for (int i = 0; i < 60; i++)
                {
                    /*
                    double bar0_value = ((bar0.Current.Value - bar0.Current.Minimum) / (bar0.Current.Maximum - bar0.Current.Minimum)) * 100.0;
                    double bar1_value = ((bar1.Current.Value - bar1.Current.Minimum) / (bar1.Current.Maximum - bar1.Current.Minimum)) * 100.0;

                    AppendRuntimeLog($"progress-bar0 value is {bar0_value.ToString("F2")}%.");
                    AppendRuntimeLog($"progress-bar1 value is {bar1_value.ToString("F2")}%.");
                    */

                    WinUI.ForceBringWindowToFront(hwnd);
                    Bitmap bmpScreen = WinUI.CapturePrimaryScreen();
                    ScreenImage = new Image<Bgr, byte>(bmpScreen);

                    Image<Gray, float> result = ScreenImage.MatchTemplate(template, TemplateMatchingType.CcoeffNormed);

                    CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    AppendRuntimeLog($"MaxVal:{maxVal}, MaxLoc:({maxLoc.X},{maxLoc.Y})");

                    if (maxVal > 0.9999)
                    {
                        rlt = true;
                        bTimeOut = false;
                        value = "PASS";
                        bmpScreen.Dispose();
                        break;
                    }

                    bmpScreen.Dispose();
                    result?.Dispose();
                    ScreenImage?.Dispose();
                    Delay(5000);
                }

                InvokePattern StopFlashProcess = flashBtn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                StopFlashProcess?.Invoke();

                InvokePattern ExitApp = exitBtn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                ExitApp?.Invoke();
            }

            if (bTimeOut)
            {
                value = "TimeOut";
                rlt = false;
                goto End;
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
            value = Convert.ToString(2.68 * RelayCard1.MeasureVoltage(0));
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
            value = Convert.ToString(1.2 * RelayCard1.MeasureVoltage(1));
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
            Delay(500);
            value = Convert.ToString(1.2 * RelayCard1.MeasureVoltage(2));
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
            Delay(500);
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
            Delay(500);
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
            Delay(500);
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
            Delay(500);
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
            DI_Status mDI, specDI;
            if (item.Name.Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(0);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(0);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(0);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(0);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_CC1O(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;
            if (item.Name.Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(1);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(1);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(1);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(1);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB_OTG1_DP_A(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(2);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(2);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(2);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(2);
                specDI = DI_Status.Low;
            }

            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB_OTG1_DM_A(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(3);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(3);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(3);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(3);
                specDI = DI_Status.Low;
            }

            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_CC1H(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(5);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(5);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(5);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(5);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_CC2H(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;
            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(4);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(4);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(4);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(4);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_HOST_DPTest_Powerboard_CC1H(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;
            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(5);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(5);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(5);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(5);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_HOST_DP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;
            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(6);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(6);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(6);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(6);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_HOST_DM(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;
            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(7);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(7);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(7);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(7);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB_CC_INT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            if (item.Name.ToLower().Contains("high"))
            {
                regAddr = "0x020AC000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                char cVal = '\0';
                int sIndex = 7;

                dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
                int nib = cVal.ascii_2_nibble();

                rlt = nib.check_bit_0_or_1(BitPosition.second);
                value = Convert.ToString(nib, 2).PadLeft(4, '0');

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                regAddr = "0x02290010";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);

                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020AC000"; regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                char cVal = '\0';
                int sIndex = 7;

                dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
                int nib = cVal.ascii_2_nibble();

                rlt = !nib.check_bit_0_or_1(BitPosition.second);
                value = Convert.ToString(nib, 2).PadLeft(4, '0');
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }


        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_VEHICLE_MOUNT_DET(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            DI_Status mDI, specDI;

            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(8);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(8);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(8);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(8);
                specDI = DI_Status.Low;
            }

            value = $"{(int)mDI}";
            rlt = mDI == specDI;

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB2_DOCK_DM(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;
            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(8);
                Delay(500);

                mDI = CaptureCard1.GetCaptureCardDI(8);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(8);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(8);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_Powerboard_USB2_DOCK_DP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";
            DI_Status mDI, specDI;
            if (item.Name.ToLower().Contains("high"))
            {
                CaptureCard1.SetCaptureCardDOAsHigh(9);
                Delay(500);

                mDI = CaptureCard1.GetCaptureCardDI(9);
                specDI = DI_Status.High;
            }
            else
            {
                CaptureCard1.SetCaptureCardDOAsLow(9);
                Delay(500);
                mDI = CaptureCard1.GetCaptureCardDI(9);
                specDI = DI_Status.Low;
            }
            rlt = mDI == specDI;
            value = $"{(int)mDI}";
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
            string imgFilePath = "";
            if (item.Name.ToUpper().Contains("BLACK") || item.Name.ToUpper().Contains("DARK"))
            {
                zn_ch_tips = "黑色";
                en_us_tips = "black";
                thai_tips = "null";
                imgFilePath = Path.Combine(Global.ASSEMBLY_PATH, "Res", "topsensor_black.png");
            }
            else
            {
                zn_ch_tips = "白色";
                en_us_tips = "white";
                thai_tips = "null";
                imgFilePath = Path.Combine(Global.ASSEMBLY_PATH, "Res", "topsensor_white.png");
            }

            if (!GetOperatorSelection(zn_ch_tips, en_us_tips, thai_tips, imgFilePath))
            {
                value = "OP_PUT_MEDIA_NG";
                rlt = false;
                goto End;
            }

            Delay(500);
            value = Convert.ToString(RelayCard1.MeasureVoltage(1));

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

            //set powerSupply as 12v

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
            is_bt = true;
            AppendRuntimeLog($"this DUT is BT_module.");

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
                if (devicePortName != "COM1" && !string.IsNullOrEmpty(devicePortName) && !string.IsNullOrWhiteSpace(devicePortName))
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

            Delay(Convert.ToInt32(INI.GetValue("Delay", "WaitUI", ConfigFile)) * 1000);
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

            DUTS.WiFi_BT_Module.WiFi_BT_Module m_dut = dut as DUTS.WiFi_BT_Module.WiFi_BT_Module;

            for (int i = 0; i < 200; i++)
            {
                devicePortName = WinDevice.GetSerialportComPort(m_dut.VID, m_dut.PID);
                if (devicePortName != "COM1" && !string.IsNullOrWhiteSpace(devicePortName))
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
                rlt = false;
                goto End;
            }

            string sCmd = string.Empty;
            if (item.Name.ToUpper().Contains("WIFI")) { sCmd = PrinterCommands.WIFI_ID; }
            else if (item.Name.ToUpper().Contains("BT")) { sCmd = PrinterCommands.BLUETOOTH_ID; }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            tmpValue = Write_Read_SeriPort(Communicate_Type.VirtualComPort, sCmd, 2000);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                rlt = false;
                value = "TimeOut";
                goto End;
            }

            if (tmpValue.Contains(Printer_Error_Code.HW_NOT_INSTALL.ToString()))
            {
                rlt = false;
                value = $"{Printer_Error_Code.HW_NOT_INSTALL}";
                goto End;
            }

            string part_number = "", seri_number = "", address = "";
            if (item.Name.ToUpper().Contains("WIFI"))
            {
                m_dut.get_WIFI_ID(tmpValue, ref part_number, ref seri_number, ref address);
                value = address;
                WIFI_MAC_ADDRESS = value;
            }
            else
            {
                m_dut.get_BLUETOOTH_ID(tmpValue, ref part_number, ref seri_number, ref address);
                value = address;
                BLUETOOTH_MAC_ADDRESS = value;
            }

            rlt = (!string.IsNullOrWhiteSpace(value)) ? true : false;

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

            if (string.IsNullOrWhiteSpace(mac))
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

            if (!check_unlock_os_cmds())
            {
                value = "RUN_OS_CMDS_DISABLE";
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
                if (!string.IsNullOrWhiteSpace(tmpValue))
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

        private bool Test_WiFiBtModule_RePowerOn(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "";
            string devicePortName = "";

            CloseAllRelays();
            Delay(3000);
            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_0_ON_NG";
                goto End;
            }

            //set powerSupply as 12v

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
            is_bt = true;
            AppendRuntimeLog($"this DUT is BT_module.");

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
                if (devicePortName != "COM1" && !string.IsNullOrEmpty(devicePortName) && !string.IsNullOrWhiteSpace(devicePortName))
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

            Delay(Convert.ToInt32(INI.GetValue("Delay", "WaitUI", ConfigFile)) * 1000);
            value = rlt ? $"PASS / {devicePortName}" : "FAIL";

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

            if (Prod.PA45.CommHWOpt == CommHWOpt.ERROR)
            {
                value = "COMM_OPTION_ERROR";
                rlt = true;
                goto End;
            }

            if (Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY)
            {
                value = "NO_NFC_CONFIG";
                rlt = true;
                goto End;
            }

            if (string.IsNullOrWhiteSpace(BLUETOOTH_MAC_ADDRESS))
            {
                value = "MAC_CONTENT_IS_NULL";
                rlt = false;
                goto End;
            }

            string zh_cn = "请把NFC读卡器靠近NFC标签";
            string en_us = "please put NFC reader near NFC-Tag.";
            string thai = "null";

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

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
            string value = "", tmpValue = "";

            if (!RelayCard1.SetRelayOn(0))
            {
                rlt = false;
                value = "RELAY1_0_ON_NG";
                goto End;
            }
            Delay(1000);
            if (!RelayCard1.SetRelayOn(1))
            {
                rlt = false;
                value = "RELAY1_1_ON_NG";
                goto End;
            }

            rlt = true;

            value = rlt ? $"PASS" : "FAIL";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_LTSSensor_Func(int index, TestItem item)
        {
            bool rlt = false, bFinddevice = false, bRoot = false;
            string value = "", tmpValue = "", devicePortName = "";
            string zh_cn = "", en_us = "", thai = "";
            string sCmd = "";
            DUTS.LTSSensor.LTSSensor m_dut = dut as DUTS.LTSSensor.LTSSensor;

            if (m_dut == null)
            {
                value = "DUT_TYPE_ERROR";
                rlt = false;
                goto End;
            }

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                tmpValue = ReadSerialportDataReceived();

                if (tmpValue.Contains(PA45_FCT_Commands.ConsoleLoginReminder))
                {
                    bFinddevice = true;

                    break;
                }

                Delay(1000);
            }

            if (!bFinddevice)
            {
                rlt = false;
                value = "CANNOT_FIND_DEVICE";
                goto End;
            }

            SeriPortWriteLine(spUART, PA45_FCT_Commands.Username);
            SeriPortWriteLine(spUART, PA45_FCT_Commands.Password);

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                tmpValue = GetUARTStringFlagA(PA45_FCT_Commands.ConsoleLoginReminder);

                if (tmpValue.Contains(PA45_FCT_Commands.root_OK_ConsoleTag))
                {
                    bRoot = true;
                    break;
                }

                Delay(1000);
            }

            if (!bRoot)
            {
                rlt = false;
                value = "ROOT_LOGIN_FAIL";
                goto End;
            }

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

            bool bOpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!bOpStep)
            {
                value = "OP_PUT_LTS_FAIL";
                rlt = false;
                goto End;
            }

            value = GetLTSSensorValue(Communicate_Type.UART);

            if (value.Contains($"{Printer_Error_Code.HW_NOT_INSTALL}"))
            {
                rlt = false;
            }

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

            /*
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
            */

            if (!RelayCard1.SetRelayOn(2))
            {
                value = "REALAY1_2_ON_FAIL";
                rlt = false;
                goto End;
            }

            Delay(500);

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

                if (tmpValue.Contains(PA45_FCT_Commands.none_login))
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
            Delay(1000);
            SeriPortWriteLine(spUART, PrinterCommands.username);
            SeriPortWriteLine(spUART, PrinterCommands.password1);
            Delay(1000);

            for (int i = 0; i < 200; i++)
            {
                tmpValue = ReadSerialportDataReceived();

                if (tmpValue.Contains(PA45_FCT_Commands.none_root_ok))
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

            SeriPortWriteLine(spUART, PA45_FCT_Commands.unlock_memtool_cmd1);
            SeriPortWriteLine(spUART, PA45_FCT_Commands.unlock_memtool_cmd2);


            rlt = true;
            value = "PASS";

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_GotoFctMode(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            DUTS.PA45_MainBoard.PA45_MainBoard m_dut = dut as DUTS.PA45_MainBoard.PA45_MainBoard;

            if (m_dut == null)
            {
                value = "ERROR_DUT_TYPE";
                rlt = false;
                goto End;
            }

            string regAddr = "0x020E0048", regVal = "0x00000005";
            string sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);

            SeriPortWriteLine(spUART, sCmd);
            regAddr = "0x0209C000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            string tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 5;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0x0209C000:  30006`1`9C ( 0b`0`001 )
            rlt = !nib.check_bit_0_or_1(BitPosition.third);

            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_Curr_PowerOn(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //step2, K-2306 power supply
            //Keithy2306LAN.Source(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1, 7.0);
            Keithy2306LAN.Connect();
            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 12);
            //step3, Power-Key turn on test, measure TP294
            Delay(1000);
            value = Keithy2306LAN.Measure_scpi(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH2);
        //step4, turn off K-2306
        //Keithy2306LAN.SourceOff(KEITHLEY_2306_Channel.CH2);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_CoinBattery(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard2.SetRelayOn(3);//TPH NTC 30k turn on;

            //relay3-5 rtc power supply

            RelayCard3.SetRelayOn(5);
            Delay(1000);
            value = Convert.ToString(CaptureCard1.MeasureVoltage(0));
            RelayCard3.SetRelayOff(5);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_CommJ1(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            //set some relay to make sure communitcate well.

            RelayCard3.SetRelayOn(0);
            RelayCard3.SetRelayOff(0);

            rlt = true;
            value = "PASS";
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_BootMode(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "";

            string regAddr = "0x020AC000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            string tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 5;

            //00000[A]C3 ( 0x[10]10 )
            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();
            AppendRuntimeLog($"Nibble = {nib}, 4bit binary = {Convert.ToString(nib, 2).PadLeft(4, '0')}");

            rlt = nib.check_bit_0_or_1(BitPosition.third) && !nib.check_bit_0_or_1(BitPosition.second);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_DC_InCurrentWhilePowerOn(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", sTmp = "";

            //step1, supply DC/IN 12v/3a, 2306

            Keithy2306LAN.Connect();
            //step2, relay??? 5V

            //step3, 
            string regAddr = "0x020E01A0", regVal = "00000005";
            string sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            value = Keithy2306LAN.Measure(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1);


        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_ConfigHW(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //step3, 
            string regAddr = "0x020A8004", regVal = "0x0E418400";
            string sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A8000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 5;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes =
                !nib.check_bit_0_or_1(BitPosition.third) &&
                nib.check_bit_0_or_1(BitPosition.second) &&
                !nib.check_bit_0_or_1(BitPosition.first);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0'); ;

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_FwVersion(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = PA45_FCT_Commands.GetFWVersion;

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_FW_Version(tmpValue, ref value);

            rlt = ((value == item.UpperLimit) && (value == item.LowerLimit));
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_AutoPowerOn5V(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard1.SetRelayOn(2);
            Delay(1000);

            //7v~8v
            value = Convert.ToString(RelayCard1.MeasureVoltage(0) * 2);
            RelayCard1.SetRelayOff(2);
            Delay(1000);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_AutoPowerOn12V(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //7v~8v
            Keithy2306LAN.Connect();
            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 12);
            Keithy2306LAN.Source(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH2, 3);
            Delay(1000);

            value = Convert.ToString(RelayCard1.MeasureVoltage(0) * 2);
            Keithy2306LAN.SourceOff(KEITHLEY_2306_Channel.CH2);
            Delay(1000);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_PowerOn7p4V(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //7v~8v
            Keithy2306LAN.Connect();
            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 7.4);
            Keithy2306LAN.Source(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1, 4);
            Delay(1000);
            value = Convert.ToString(Keithy2306LAN.Measure_scpi(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }





        private bool Test_MB_FCT_PowerOn7p4V_battery(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(1000);
            value = Convert.ToString(Keithy2306LAN.Measure_scpi(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_PowerKeyPress(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            rlt = RelayCard3.SetRelayOn(4);
            Delay(100);
            rlt = RelayCard3.SetRelayOff(4);
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VSYS_CHG(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //VBUS_IN供電 (使用PD Adapter 5~20V/?A),
            Delay(100);
            value = Convert.ToString(RelayCard1.MeasureVoltage(0) * 2);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_MCU(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

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

            //MCU_P01:H
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
            Delay(500);
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
            Delay(500);
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
            string sCmd = "";

            if (item.Name.ToLower().Contains("normal"))
            {
                value = Convert.ToString(CaptureCard1.MeasureVoltage(7));

            }
            else
            {
                sCmd = "date -s " + "\"" + $"{DateTime.Now.ToString("2026-01-01 12:00:00")}";
                SeriPortWriteLine(spUART, sCmd);
                SeriPortWriteLine(spUART, "hwclock -w -f /dev/rtc0");
                SeriPortWriteLine(spUART, "echo 0 > /sys/class/rtc/rtc0/wakealarm");
                SeriPortWriteLine(spUART, "echo +5 > /sys/class/rtc/rtc0/wakealarm");
                SeriPortWriteLine(spUART, "echo mem > /sys/power/state");

                value = Convert.ToString(CaptureCard1.MeasureVoltage(7));
                Delay(5000);
            }

            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VDD_HIGH_CAP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(8));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_NVCC_PLL_OUT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(9));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V5P0(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //string sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(0, MCU_DO_Status.High);
            //SeriPortWriteLine(spUART, sCmd);
            value = Convert.ToString(RelayCard1.MeasureVoltage(1) * 2);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_AUX(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = "";

            if (item.Name.ToLower().Contains("low"))
            {
                sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(2, MCU_DO_Status.Low);
            }
            else if (item.Name.ToLower().Contains("high"))
            {
                sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(2, MCU_DO_Status.High);
            }
            else
            {

            }

            SeriPortWriteLine(spUART, sCmd);
            value = Convert.ToString(CaptureCard1.MeasureVoltage(10));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_LTS(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(11));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_LPT(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue;

            string regAddr = "0x020A4000", regVal = "0x00005331";
            string sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            /*
            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                sTmp = GetUARTStringFlagA(sCmd);
                if (!string.IsNullOrWhiteSpace(sTmp))
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
            */

            value = Convert.ToString(CaptureCard1.MeasureVoltage(12));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V3P3_WIFI(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = "";

            //high
            if (item.Name.ToUpper().Contains("HIGH"))
            {
                sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(39, MCU_DO_Status.High);
            }
            else if (item.Name.ToUpper().Contains("LOW"))
            {
                sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(39, MCU_DO_Status.Low);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            SeriPortWriteLine(spUART, sCmd);
            Delay(1000);
            value = Convert.ToString(CaptureCard1.MeasureVoltage(13));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V1P8_WIFI(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(14));
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V5P0_WWAN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //Set GPIO03 io08 H
            /*
             * 設定 GPIO3_IO08 為 High,
            memtool -32 0x020A4000=0x00001331
            設定 GPIO3_IO08 為 Output,
            memtool -32 0x020A4004=0x1E7F6307
             */
            string regAddr = "", regVal = "", sCmd = "";

            regAddr = "0x020A4004";
            regVal = "0x00001331";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A4000";
            regVal = "0x00001331";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //RelayCard1.SetRelayOn(3);
            Delay(1000);
            value = Convert.ToString(RelayCard1.MeasureVoltage(2) * 2);
        //RelayCard1.SetRelayOff(3);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_AP_READY_1V8(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string regAddr = "", regVal = "", sCmd = "";

            if (item.Name.Contains("H"))
            {
                regAddr = "0x020E01AC";
                regVal = "0x5";

                //H
                regAddr = "0x020A8000";
                regVal = "0x065FA400";

                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A8004";
                regVal = "0x0241A400";

                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.Contains("L"))
            {
                //L
                regAddr = "0x020A8000";
                regVal = "0x065F8400";

                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A8004";
                regVal = "0x0241A400";

                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }

            Delay(100);
            value = Convert.ToString(CaptureCard2.MeasureVoltage(5));

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

        private bool Test_MB_FCT_PowerLED(int index, TestItem item)
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

        private bool Test_MB_FCT_LED4_Charging(int index, TestItem item)
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

        private bool Test_MB_FCT_LED4_FullCharged(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            Delay(100);
            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 8.4);
            Keithy2306LAN.Source(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1, 3);
            value = Convert.ToString(RelayCard1.MeasureVoltage(3) * 2);

            Delay(1000);

            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 7.4);
            Keithy2306LAN.Source(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1, 4);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_SetRtc(int index, TestItem item)
        {

            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = PA45_FCT_Commands.i2c_rtc_0x00;

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            rlt = dut.get_i2c_rtc_result(tmpValue, ref value);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_CheckRtc(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.RTC);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
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

            TimeSpan timeDiff = DateTime.Now - printer_datetime;

            value = Math.Abs(timeDiff.TotalSeconds).ToString("F2");

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
            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 12);
            Delay(2000);
            value = Convert.ToString(Keithy2306LAN.Measure_scpi(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH2));

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

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, $"functest RTC {currentTime}");
            rlt = (tmpValue.Contains("PASS"));
            value = rlt ? "PASS" : "FAIL";
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp130ShortTp110_H(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //step3, 
            string regAddr = "0x02290020", regVal = "0x00000005";
            string sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020AC000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 6;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.second);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp128ShortTp112_L(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = "";

            sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(4, MCU_DO_Status.Low);

            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = " 0x02290024", regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020AC000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 6;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.third);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0'); ;

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp111_L(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", sTmp = "";

            string sCmd = "";

            DI_Status specDI = DI_Status.High;
            DI_Status mDI;
            SeriPortWriteLine(spUART, sCmd);

            /*
             * memtool -32 0x020E012C=0x00000005
memtool -32 0x020A4000=0x00001331
memtool -32 0x020A4004=0x1E7F 6707
             */

            //step3, 
            string regAddr = "0x020E012C", regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A4000";
            regVal = "0x00001331";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A4004";
            regVal = "0x1E7F6707";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            mDI = CaptureCard1.GetCaptureCardDI(0);

            rlt = mDI == specDI;
            // CaptureCard get DI
            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp122ShortTp126_L(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01D8", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A8000";
            regVal = "0x065BA400";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A8004";
            regVal = "0x02458400";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020E01D4";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);


            regAddr = "0x020A8000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 3;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.first);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp116ShortTp114_H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E00A4", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x0209C000";
            regVal = "0x3100619C";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x0209C004";
            regVal = "0x01080233";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E00A8";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x0209C000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 1;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.first);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp115ShortTp113_L(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E00AC", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x0209C000";
            regVal = "0x3000619C";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x0209C004";
            regVal = "0x04080233";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E00B0";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x0209C000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 1;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.third);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp117ShortTp118_H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01C0", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A0000";
            regVal = "0x000287FF";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A0004";
            regVal = "0x00028800";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E01C4";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A0000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }


            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 3;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.second);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp121ShortTp123_L(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01C8", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A0000";
            regVal = "0x000087FF";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A0004";
            regVal = "0x00088800";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E01CC";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A0000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 2;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.zeroth);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp127ShortTp129H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01D0", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A0000";
            regVal = "0x002087FF";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A0004";
            regVal = "0x00208800";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E01BC";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A0000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 3;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.zeroth);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp130ShortTp110_1_L(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            //step3, 
            string regAddr = "0x020A4000", regVal = "0x00001131";
            string sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x02290020";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020AC000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 6;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.second);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp128ShortTp112_H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";

            sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(4, MCU_DO_Status.High);

            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = " 0x02290024", regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020AC000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 6;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.third);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0'); ;

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp111_H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            DI_Status specDI = DI_Status.Low;
            DI_Status mDI;
            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E012C", regVal = "0x00000005";//set this pin as GPIO PIN: LCD_DATA05
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN: LCD_DATA05 as High
            regAddr = "0x020A4000";
            regVal = "0x00001731";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A4004";
            regVal = "0x1E7F6707";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            // need this di as 1.
            mDI = CaptureCard1.GetCaptureCardDI(0);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp122ShortTp126_H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01D8", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A8000";
            regVal = "0x065FA400";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A8004";
            regVal = "0x02458400";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020E01D4";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);


            regAddr = "0x020A8000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }
            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 3;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.first);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp116ShortTp114_L(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E00A4", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x0209C000";
            regVal = "0x3000619C";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x0209C004";
            regVal = "0x01080233";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E00A8";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x0209C000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 1;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.first);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp115ShortTp113_H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E00AC", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x0209C000";
            regVal = "0x3400619C";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x0209C004";
            regVal = "0x04080233";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E00B0";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x0209C000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 1;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.third);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp117ShortTp118_L(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01C0", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A0000";
            regVal = "0x000087FF";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A0004";
            regVal = "0x00028800";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E01C4";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A0000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 3;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.second);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp121ShortTp123_H(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01C8", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A0000";
            regVal = "0x000887FF";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A0004";
            regVal = "0x00088800";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E01CC";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A0000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 2;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = nib.check_bit_0_or_1(BitPosition.zeroth);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Tp127ShortTp129_L(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";
            SeriPortWriteLine(spUART, sCmd);

            //step3, 
            string regAddr = "0x020E01D0", regVal = "0x00000005";//set this pin as GPIO.
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set this pin as GPIO PIN as low
            regAddr = "0x020A0000";
            regVal = "0x000087FF";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set output
            regAddr = "0x020A0004";
            regVal = "0x00208800";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            //set anther pin as GPIO
            regAddr = "0x020E01BC";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A0000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 3;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            //0`4`5FA400  ( 0x`010`0 )
            bool tmpRes = !nib.check_bit_0_or_1(BitPosition.zeroth);

            rlt = tmpRes;
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_UP_IRQ_N(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                //memtool -32 0x020A0000=0x00008DFF
                //send -> h, get low;
                regAddr = "0x020A0000";
                regVal = "0x00008DFF";

                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                //send -> l, get high;
                //memtool -32 0x020A0000=0x000085FF
                regAddr = "0x020A0000";
                regVal = "0x000085FF";

                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            SeriPortWriteLine(spUART, sCmd);
            sCmd = PA45_FCT_Commands.Get_MCU_Pin_DI(40);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);

            if (specDI == DI_Status.High)
            {
                rlt = Convert.ToInt32(value) == 0;
            }
            if (specDI == DI_Status.Low)
            {
                rlt = Convert.ToInt32(value) == 1;
            }

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LssSensorADC(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = "";

            if (item.Name.ToLower().Contains("low"))
            {
                sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.LSS);
            }
            else if (item.Name.ToLower().Contains("top"))
            {
                sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.FRONT_LSS);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
            }

            string sensorVal = "", drive = "", gain = "";
            dut.get_LSS(tmpValue, ref sensorVal, ref drive, ref gain);

            value = sensorVal;

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LssSensorDI(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = "";

            if (item.Name.ToLower().Contains("low"))
            {
                // h:
                sCmd = PA45_FCT_Commands.Set_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                sCmd = PA45_FCT_Commands.Set_Front_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                Delay(100);
                DI_Status hDI = DI_Status.Low;
                for (int i = 0; i < 20; i++)
                {
                    hDI = CaptureCard1.GetCaptureCardDI(0);

                    if (hDI == DI_Status.High)
                    {
                        break;
                    }

                    Delay(100);
                }


                sCmd = PA45_FCT_Commands.Set_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                sCmd = PA45_FCT_Commands.Set_Front_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                Delay(500);
                DI_Status lDI = CaptureCard1.GetCaptureCardDI(0);

                value = $"{(int)hDI} / {(int)lDI}";

                rlt =
                    (hDI == DI_Status.High) &&
                    (lDI == DI_Status.Low);

            }
            else if (item.Name.ToLower().Contains("top"))
            {
                // h:
                sCmd = PA45_FCT_Commands.Set_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                sCmd = PA45_FCT_Commands.Set_Front_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                Delay(100);
                DI_Status hDI = DI_Status.Low;
                for (int i = 0; i < 20; i++)
                {
                    hDI = CaptureCard1.GetCaptureCardDI(0);

                    if (hDI == DI_Status.High)
                    {
                        break;
                    }

                    Delay(100);
                }

                sCmd = PA45_FCT_Commands.Set_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                sCmd = PA45_FCT_Commands.Set_Front_BM_High;
                SeriPortWriteLine(spUART, sCmd);

                Delay(500);
                DI_Status lDI = CaptureCard1.GetCaptureCardDI(1);

                value = $"{(int)hDI} / {(int)lDI}";

                rlt =
                    (hDI == DI_Status.High) &&
                    (lDI == DI_Status.Low);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_COVER_SENSOR(int index, TestItem item)
        {
            bool rlt = false, rlt_h = false, rlt_l = false;
            string value = "", tmpValue = "", nib_high = "", nib_low = "";
            string sCmd = "", regAddr = "", regVal = "";

            //memtool -32 0x020AC000 1
            //read result = 0x020AC000:  00000BC3(0x1011)
            regAddr = "0x020AC000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);

            char c = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 5, ref c);

            int nib = c.ascii_2_nibble();
            rlt_h = nib.check_bit_0_or_1(BitPosition.zeroth);
            nib_high = Convert.ToString(nib, 2).PadLeft(4, '0');

            RelayCard3.SetRelayOn(2);
            Delay(1000);

            regAddr = "0x02290028";
            regVal = "0x00000005";

            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020AC000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            c = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 5, ref c);

            nib = c.ascii_2_nibble();
            rlt_l = !nib.check_bit_0_or_1(BitPosition.zeroth);
            nib_low = Convert.ToString(nib, 2).PadLeft(4, '0');

            rlt = rlt_h && rlt_l;

            value = $"{nib_high} / {nib_low}";
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
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

            string sCmd = "", regAddr = "", regVal = "";

            if (item.Name.ToLower().Contains("high"))
            {
                regAddr = "0x020E006C";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C004";
                regVal = "0x00080233";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C000";
                regVal = "0x3000619C";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                regAddr = "0x020E006C";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C000";
                regVal = "0x3000618C";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            value = Convert.ToString(CaptureCard2.MeasureVoltage(8));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_BuzzerControlSOC(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = "", regAddr = "", regVal = "";

            if (item.Name.ToUpper().Contains("high"))
            {
                regAddr = "0x020E0164";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4000";
                regVal = "0x01001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4004";
                regVal = "0x1F7F6307";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                value = Convert.ToString(CaptureCard2.MeasureVoltage(8));

                regAddr = "0x020E0164";
                regVal = "0x00000001";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4004";
                regVal = "0x1E7F6307";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }

            if (item.Name.ToUpper().Contains("low"))
            {
                regAddr = "0x020E0164";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4004";
                regVal = "0x1F7F6307";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                value = Convert.ToString(CaptureCard2.MeasureVoltage(8));

                regAddr = "0x020E0164";
                regVal = "0x00000001";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4004";
                regVal = "0x1E7F6307";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }

            value = CaptureCard2.MeasureVoltage(9).ToString();

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_BuzzerControlMCU(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = "", regAddr = "", regVal = "";

            if (item.Name.ToUpper().Contains("high"))
            {
                sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(19, MCU_DO_Status.High);
            }

            if (item.Name.ToUpper().Contains("low"))
            {
                sCmd = PA45_FCT_Commands.Set_MCU_Pin_DO(19, MCU_DO_Status.Low);
            }

            SeriPortWriteLine(spUART, sCmd);
            value = CaptureCard2.MeasureVoltage(9).ToString();

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_J20_KeySOC(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            string sCmd = "", regAddr = "", regVal = "", regAddr1 = "";
            int relayIndex = -1, byteIndex = -1;
            BitPosition bitIndex;
            char cVal = '\0';
            IRelayCard relay;

            if (item.Name.ToUpper().Contains("TP191"))
            {
                relay = RelayCard3;
                relayIndex = 4;
                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                byteIndex = 7;
                bitIndex = BitPosition.first;
            }
            else if (item.Name.ToUpper().Contains("TP193"))
            {
                relay = RelayCard1;
                relayIndex = 4;
                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                byteIndex = 7;
                bitIndex = BitPosition.first;
            }
            else if (item.Name.ToUpper().Contains("TP197"))
            {
                relay = RelayCard1;
                relayIndex = 6;
                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                byteIndex = 7;
                bitIndex = BitPosition.first;
            }
            else if (item.Name.ToUpper().Contains("TP199"))
            {
                relay = RelayCard1;
                relayIndex = 7;
                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                byteIndex = 7;
                bitIndex = BitPosition.first;
            }
            else if (item.Name.ToUpper().Contains("TP200"))
            {
                relay = RelayCard1;
                relayIndex = 8;
                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                byteIndex = 7;
                bitIndex = BitPosition.first;
            }
            else if (item.Name.ToUpper().Contains("TP201"))
            {
                relay = RelayCard2;
                relayIndex = 0;
                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                byteIndex = 7;
                bitIndex = BitPosition.first;
            }
            else
            {
                value = "TEST_POINT_ERROR";
                rlt = false;
                goto End;
            }

            relay.SetRelayOn(relayIndex);
            Delay(500);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            dut.get_Memtool_Register_Char_Value(regVal, byteIndex, ref cVal);

            int nib = cVal.ascii_2_nibble();
            rlt = nib.check_bit_0_or_1(bitIndex);

            value = Convert.ToString(nib, 2).PadLeft(4, '0');

            relay.SetRelayOff(relayIndex);
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_BMSensor_ADCValue(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string sCmd = "";

            if (item.Name.Contains("J15"))
            {
                sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.FRONT_LSS);
            }
            else if (item.Name.Contains("J18"))
            {
                sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.LSS);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            SeriPortWriteLine(spUART, sCmd);
            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                tmpValue = GetUARTStringFlagA(sCmd);
                if (!string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }

                Delay(1000);
            }

            if (bTimeOut)
            {
                value = $"{Test_Proc_Error_Code.TimeOut}";
                rlt = false;
                goto End;
            }

            string sensorVal = "", drive = "", gain = "";
            dut.get_LSS(tmpValue, ref sensorVal, ref drive, ref gain);
            value = sensorVal;

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_BMSensor_DI(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard2.MeasureVoltage(7));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_SelfStripSensor_DI(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            char cVal = '\0';
            int chIndex = 4;
            if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                RelayCard3.SetRelayOn(3);
                Delay(1000);
            }
            else
            {
                specDI = DI_Status.High;
            }

            regAddr = "0x020E0134";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A4000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            SeriPortWriteLine(spUART, sCmd);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrEmpty(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            dut.get_Memtool_Register_Char_Value(regVal, chIndex, ref cVal);

            int nib = cVal.ascii_2_nibble();

            if (specDI == DI_Status.High)
            {
                rlt = nib.check_bit_0_or_1(BitPosition.zeroth);
            }

            if (specDI == DI_Status.Low)
            {
                rlt = !nib.check_bit_0_or_1(BitPosition.zeroth);
                RelayCard3.SetRelayOff(3);
            }

            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_SelfStripSensor(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.LTS);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }
            dut.get_LTS(tmpValue, ref value);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LCD_CLK(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = -1;
            BitPosition bitPosi = BitPosition.error;
            char cVal = '\0';
            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;

                chIndex = 4;
                bitPosi = BitPosition.zeroth;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020E00F4";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                if (string.IsNullOrEmpty(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;

                chIndex = 4;
                bitPosi = BitPosition.zeroth;
                regAddr = "0x020A4000";
                regVal = "0x00001330";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020E00F4";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                if (string.IsNullOrEmpty(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            dut.get_Memtool_Register_Char_Value(regVal, chIndex, ref cVal);

            int nib = cVal.ascii_2_nibble();

            if (specDI == DI_Status.High)
            {
                rlt = nib.check_bit_0_or_1(bitPosi);
            }

            if (specDI == DI_Status.Low)
            {
                rlt = !nib.check_bit_0_or_1(bitPosi);
            }

            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;

        }

        private bool Test_MB_FCT_LCD_TX(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = -1;
            BitPosition bitPosi = BitPosition.error;
            char cVal = '\0';
            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;

                chIndex = 4;
                bitPosi = BitPosition.third;
                regAddr = "0x020E00F8";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0000";
                regVal = "0x0000A7FF";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0004";
                regVal = "0x00002800";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                if (string.IsNullOrEmpty(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;

                chIndex = 4;
                bitPosi = BitPosition.third;
                regAddr = "0x020E00F8";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0000";
                regVal = "0x000087FF";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0004";
                regVal = "0x00002800";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A0000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                if (string.IsNullOrEmpty(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            dut.get_Memtool_Register_Char_Value(regVal, chIndex, ref cVal);

            int nib = cVal.ascii_2_nibble();

            if (specDI == DI_Status.High)
            {
                rlt = nib.check_bit_0_or_1(bitPosi);
            }

            if (specDI == DI_Status.Low)
            {
                rlt = !nib.check_bit_0_or_1(bitPosi);
            }

            value = Convert.ToString(nib, 2).PadLeft(4, '0');

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;

        }

        private bool Test_MB_FCT_TPH_TP151(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 2;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020A4000";
                regVal = "0x00011331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_TP152(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 3;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020A4000";
                regVal = "0x00021331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            Delay(500);
            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_TP153(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 4;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020A4000";
                regVal = "0x00041331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_TP157(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 5;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020A4000";
                regVal = "0x00081331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_TP158(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 6;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020A4000";
                regVal = "0x00101331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_TP159(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 7;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020A4000";
                regVal = "0x00201331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_TP160(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 8;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020A4000";
                regVal = "0x00401331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_TP161(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            DI_Status mDI, specDI;
            int chIndex = 9;

            if (item.Name.ToLower().Contains("high"))
            {
                specDI = DI_Status.High;
                regAddr = "0x020E01EC";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A8000";
                regVal = "0x06DFA400";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A8004";
                regVal = "0x02C18400";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                /*
                memtool -32 0x020E01EC=0x00000005
                memtool -32 0x020A8000=0x06DFA400
                memtool -32 0x020A8004=0x02C18400
                    */

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                specDI = DI_Status.Low;
                regAddr = "0x020E01EC";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                /*
                memtool - 32 0x020E01EC = 0x00000005
                memtool - 32 0x020A8000 = 0x065FA400)
                memtool - 32 0x020A8004 = 0x02C18400
                */
                regAddr = "0x020A8000";
                regVal = "0x065FA400";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A8004";
                regVal = "0x02C18400";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            mDI = CaptureCard1.GetCaptureCardDI(chIndex);
            rlt = mDI == specDI;

            value = $"{(int)mDI}";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
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

        private bool Test_MB_FCT_Cellular_Modem(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //just one usb device
            string sCmd = PA45_FCT_Commands.ls_media;
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            List<string> deviList = new List<string>();
            dut.get_usb_device(tmpValue, ref deviList);

            for (int i = 0; i < deviList.Count; i++)
            {
                AppendRuntimeLog($"{deviList[i]}");
                value = value + " / " + value;
            }

            rlt = deviList.Count == 2;
            value = $"{deviList.Count}";
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_USB_HUB(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //just one usb device
            string sCmd = PA45_FCT_Commands.ls_media;
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            List<string> deviList = new List<string>();
            dut.get_usb_device(tmpValue, ref deviList);

            for (int i = 0; i < deviList.Count; i++)
            {
                AppendRuntimeLog($"{deviList[i]}");
                value = value + " / " + value;
            }

            rlt = deviList.Count == 2;
            value = $"{deviList.Count}";
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_USB_HUB_HOST_J5(int index, TestItem item)
        {
            bool rlt = false, rltIO = false, rltVolt = false;
            string value = "", tmpValue = "", ioVal = "", voltVal = "";
            string regAddr = "", regVal = "", sCmd = "";
            List<string> deviList = new List<string>();
            int relayIdx = -1;
            if (item.Name.ToUpper().Contains("CC1"))
            {
                relayIdx = 2;
                RelayCard2.SetRelayOn(relayIdx);
            }
            else if (item.Name.ToUpper().Contains("CC2"))
            {
                relayIdx = 1;
                RelayCard2.SetRelayOn(relayIdx);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            //memtool - 32 0x020A4000 = 
            regAddr = "0x020A4000";
            regVal = "0x00001331";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);
            Delay(500);

            //memtool - 32 0x020A4004 = 0x1E7F6B07

            regAddr = "0x02290010";
            regVal = "0x00000005";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);
            Delay(500);

            regAddr = "0x020AC000";
            sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 7;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rltIO = !nib.check_bit_0_or_1(BitPosition.first);
            ioVal = Convert.ToString(nib, 2).PadLeft(4, '0');

            voltVal = CaptureCard2.MeasureVoltage(10).ToString();

            Delay(2000);
            //just one usb device
            sCmd = PA45_FCT_Commands.ls_media;
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_usb_device(tmpValue, ref deviList);

            for (int i = 0; i < deviList.Count; i++)
            {
                AppendRuntimeLog($"{deviList[i]}");
                value = value + " / " + value;
            }

            rlt = deviList.Count == 3;
            RelayCard2.SetRelayOn(relayIdx);
        End:
            item.Value = voltVal;
            rltVolt = item.IsValueInRange<double>();
            item.Value = $"{deviList.Count} / {ioVal} / {voltVal}";
            return rlt && rltVolt;
        }

        private bool Test_MB_FCT_VEHICLE_MOUNT_DET(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "0x0209C000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 5;

            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rlt = nib.check_bit_0_or_1(BitPosition.zeroth);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');
        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_MFI_Chip(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = PA45_FCT_Commands.i2c_mfi_0x10;

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            rlt = dut.get_i2c_mfi_result(tmpValue, ref value);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_SecurityIC(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = PA45_FCT_Commands.provCmd_security_ic;

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            rlt = dut.get_i2c_security_result(tmpValue, ref value);

        End:
            item.Value = value;
            return rlt;
        }

        private bool Test_MB_FCT_ChargerIC(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = PA45_FCT_Commands.i2c_charger;

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            rlt = dut.get_i2c_charger_result(tmpValue, ref value);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LTPPWR_TPH(int index, TestItem item)
        {
            bool rlt = false, rlt1 = false, fstRlt = false;
            string value = "", tmpValue = "", adcVal = "", fstVal = "";
            string regAddr = "", regVal = "", sCmd = "";
            double specUpTemp = -1, specDownTemp = -1;
            if (item.Name.ToUpper().Contains("ON"))
            {
                fstVal = Convert.ToString(RelayCard1.MeasureVoltage(5) * 2);
                double fstDouVal = Convert.ToDouble(fstVal);

                if (fstDouVal > 0.5)
                {
                    fstRlt = false;
                }
                else
                {
                    fstRlt = true;
                }

                RelayCard2.SetRelayOn(3);
                Delay(1000);

                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        regAddr = "0x020A4000";
                        regVal = "0x00001333";
                        sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                        SeriPortWriteLine(spUART, sCmd, 1);

                        regAddr = "0x020A4004";
                        regVal = "0x1E7F6307";
                        sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                        SeriPortWriteLine(spUART, sCmd, 1);
                    }

                    value = Convert.ToString(RelayCard1.MeasureVoltage(5) * 2);

                    if (Convert.ToDouble(value) > 1)
                    {
                        break;
                    }

                    Delay(100);
                }



                for (int i = 0; i < 10; i++)
                {
                    regAddr = "0x020A4000";
                    regVal = "0x00001333";
                    sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                    SeriPortWriteLine(spUART, sCmd, 1);

                    regAddr = "0x020A4004";
                    regVal = "0x1E7F6307";
                    sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                    SeriPortWriteLine(spUART, sCmd, 1);

                }

                sCmd = PA45_FCT_Commands.Get_MCU_Pin_ADC(7);
                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                if (string.IsNullOrWhiteSpace(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

                dut.get_uart_mcu_cat(tmpValue, ref value);

                specUpTemp = Convert.ToDouble(INI.GetValue("Specs", "TPH_Norm_Temp_Spec_Up", ConfigFile));
                specDownTemp = Convert.ToDouble(INI.GetValue("Specs", "TPH_Norm_Temp_Spec_Low ", ConfigFile));

                adcVal = value.d2a(10, 3.3).ToString("F2");

                /*
                if (!(Convert.ToDouble(adcVal) <= specUpTemp && Convert.ToDouble(adcVal) > specUpTemp))
                {
                    value = "TPH_TEMP_OUT_OF_SPEC";
                    rlt = false;
                    goto End;
                }
                */

                value = Convert.ToString(RelayCard1.MeasureVoltage(5) * 2);
                RelayCard2.SetRelayOff(3);
            }
            else if (item.Name.ToUpper().Contains("OFF"))
            {
                RelayCard2.SetRelayOn(5);
                Delay(1000);

                sCmd = PA45_FCT_Commands.Get_MCU_Pin_ADC(7);
                tmpValue = Write_Read_SeriPort_Delay(Communicate_Type.UART, sCmd, 3000);

                if (string.IsNullOrWhiteSpace(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

                dut.get_uart_mcu_cat(tmpValue, ref value);

                specUpTemp = Convert.ToDouble(INI.GetValue("Specs", "TPH_Over_Temp_Spec_Up", ConfigFile));
                specDownTemp = Convert.ToDouble(INI.GetValue("Specs", "TPH_Over_Temp_Spec_Low ", ConfigFile));

                adcVal = value.d2a(10, 3.3).ToString("F2");

                /*
                if (!(Convert.ToDouble(adcVal) <= specUpTemp && Convert.ToDouble(adcVal) > specUpTemp))
                {
                    value = "TPH_TEMP_OUT_OF_SPEC";
                    rlt = false;
                    goto End;
                }
                */

                regAddr = "0x020A4000";
                regVal = "0x00001333";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4004";
                regVal = "0x1E7F6307";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4000";
                regVal = "0x00001331";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4004";
                regVal = "0x1E7F6307";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
                Delay(1000);
                value = Convert.ToString(RelayCard1.MeasureVoltage(5) * 2);
                RelayCard2.SetRelayOff(5);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

            rlt1 = ((Convert.ToDouble(adcVal) <= specUpTemp && Convert.ToDouble(adcVal) > specUpTemp));
            value = adcVal;

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_SOC_TP191(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard1.SetRelayOn(0);
            Delay(500);

            string regAddr = "0x020A0000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 7, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rlt = !nib.check_bit_0_or_1(BitPosition.first);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

            RelayCard1.SetRelayOff(0);
            Delay(500);
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_SOC_TP193(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard1.SetRelayOn(4);
            Delay(500);

            string regAddr = "0x020A0000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 7, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rlt = !nib.check_bit_0_or_1(BitPosition.zeroth);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

            RelayCard1.SetRelayOff(4);
            Delay(500);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_SOC_TP197(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard1.SetRelayOn(5);
            Delay(500);

            string regAddr = "0x020A0000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 7, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rlt = !nib.check_bit_0_or_1(BitPosition.third);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

            RelayCard1.SetRelayOff(5);
            Delay(500);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_SOC_TP199(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard1.SetRelayOn(6);
            Delay(500);

            string regAddr = "0x0209C000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 6, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rlt = !nib.check_bit_0_or_1(BitPosition.third);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

            RelayCard1.SetRelayOff(6);
            Delay(500);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_SOC_TP200(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard1.SetRelayOn(7);
            Delay(500);

            string regAddr = "0x020A0000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 7, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rlt = !nib.check_bit_0_or_1(BitPosition.second);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

            RelayCard1.SetRelayOff(7);
            Delay(500);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_SOC_TP201(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard2.SetRelayOn(0);
            Delay(500);

            string regAddr = "0x020A0000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            dut.get_Memtool_Register_Char_Value(regVal, 6, ref cVal);
            int nib = cVal.ascii_2_nibble();

            rlt = !nib.check_bit_0_or_1(BitPosition.zeroth);
            value = Convert.ToString(nib, 2).PadLeft(4, '0');

            RelayCard2.SetRelayOff(0);
            Delay(500);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_MCU_TP191(int index, TestItem item)
        {
            /*
           TP191 (pwr 28)
           TP193 (menu 30)
           TP197 (down 33)
           TP199 (entry 31)
           TP200 (feed 29)
           TP201 (up 32)
            */

            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            DI_Status specDI = DI_Status.Low;

            RelayCard3.SetRelayOn(4);
            Delay(1000);
            string sCmd = PA45_FCT_Commands.Get_MCU_Pin_DI(28);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);

            if (specDI == DI_Status.High)
            {
                rlt = Convert.ToInt32(value) == 1;
            }

            if (specDI == DI_Status.Low)
            {
                rlt = Convert.ToInt32(value) == 0;
            }
            RelayCard3.SetRelayOff(4);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;

        }

        private bool Test_MB_FCT_Key_MCU_TP193(int index, TestItem item)
        {
            /*
          TP191 (pwr 28)
          TP193 (menu 30)
          TP197 (down 33)
          TP199 (entry 31)
          TP200 (feed 29)
          TP201 (up 32)
           */

            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            DI_Status specDI = DI_Status.Low;

            RelayCard1.SetRelayOn(4);
            Delay(1000);
            string sCmd = PA45_FCT_Commands.Get_MCU_Pin_DI(30);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);

            if (specDI == DI_Status.High)
            {
                rlt = Convert.ToInt32(value) == 1;
            }
            if (specDI == DI_Status.Low)
            {
                rlt = Convert.ToInt32(value) == 0;
            }
            RelayCard1.SetRelayOff(4);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_MCU_TP197(int index, TestItem item)
        {
            /*
          TP191 (pwr 28)
          TP193 (menu 30)
          TP197 (down 33)
          TP199 (entry 31)
          TP200 (feed 29)
          TP201 (up 32)
           */


            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            DI_Status specDI = DI_Status.Low;

            RelayCard1.SetRelayOn(5);
            Delay(1000);
            string sCmd = PA45_FCT_Commands.Get_MCU_Pin_DI(33);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);

            if (specDI == DI_Status.High)
            {
                rlt = Convert.ToInt32(value) == 1;
            }
            if (specDI == DI_Status.Low)
            {
                rlt = Convert.ToInt32(value) == 0;
            }

            RelayCard1.SetRelayOff(5);


        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_MCU_TP199(int index, TestItem item)
        {
            /*
        TP191 (pwr 28)
        TP193 (menu 30)
        TP197 (down 33)
        TP199 (entry 31)
        TP200 (feed 29)
        TP201 (up 32)
         */

            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            DI_Status specDI = DI_Status.Low;

            RelayCard1.SetRelayOn(6);
            Delay(1000);
            string sCmd = PA45_FCT_Commands.Get_MCU_Pin_DI(31);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);

            if (specDI == DI_Status.High)
            {
                rlt = Convert.ToInt32(value) == 1;
            }
            if (specDI == DI_Status.Low)
            {
                rlt = Convert.ToInt32(value) == 0;
            }

            RelayCard1.SetRelayOff(6);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_MCU_TP200(int index, TestItem item)
        {
            /*
        TP191 (pwr 28)
        TP193 (menu 30)
        TP197 (down 33)
        TP199 (entry 31)
        TP200 (feed 29)
        TP201 (up 32)
         */

            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            DI_Status specDI = DI_Status.Low;

            RelayCard1.SetRelayOn(7);
            Delay(1000);
            string sCmd = PA45_FCT_Commands.Get_MCU_Pin_DI(29);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);

            if (specDI == DI_Status.High)
            {
                rlt = Convert.ToInt32(value) == 1;
            }
            if (specDI == DI_Status.Low)
            {
                rlt = Convert.ToInt32(value) == 0;
            }

            RelayCard1.SetRelayOff(7);


        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_Key_MCU_TP201(int index, TestItem item)
        {
            /*
        TP191 (pwr 28)
        TP193 (menu 30)
        TP197 (down 33)
        TP199 (entry 31)
        TP200 (feed 29)
        TP201 (up 32)
         */

            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";
            DI_Status specDI = DI_Status.Low;

            RelayCard2.SetRelayOn(0);
            Delay(1000);
            string sCmd = PA45_FCT_Commands.Get_MCU_Pin_DI(32);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);

            if (specDI == DI_Status.High)
            {
                rlt = Convert.ToInt32(value) == 1;
            }
            if (specDI == DI_Status.Low)
            {
                rlt = Convert.ToInt32(value) == 0;
            }

            RelayCard2.SetRelayOff(0);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_RFID_Power(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";
            if (item.Name.ToLower().Contains("high"))
            {
                regAddr = "0x020A4000";
                regVal = "0x00001339";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020A4004";
                regVal = "0x1E7F630F";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

            }
            else if (item.Name.ToLower().Contains("low"))
            {
                regAddr = "0x020A4004";
                regVal = "0x1E7F630F";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);
            }
            else
            {
                value = "ITEM_NAME_ERR";
                rlt = false;
                goto End;
            }

            value = Convert.ToString(RelayCard1.MeasureVoltage(4) * 2);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_J22(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string regAddr = "", regVal = "", sCmd = "";

            if (item.Name.ToLower().Contains("high"))
            {
                regAddr = "0x020E00BC";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C000";
                regVal = "0x7000619C";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C004";
                regVal = "0x40080233";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020E00C0";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                char cVal = '\0';
                int sIndex = 0;

                dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
                int nib = cVal.ascii_2_nibble();

                rlt = nib.check_bit_0_or_1(BitPosition.third);
                value = Convert.ToString(nib, 2).PadLeft(4, '0');
            }
            else if (item.Name.ToLower().Contains("low"))
            {
                regAddr = "0x020E00BC";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C000";
                regVal = "0x3000619C";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C004";
                regVal = "0x40080233";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x020E00C0";
                regVal = "0x00000005";
                sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
                SeriPortWriteLine(spUART, sCmd);

                regAddr = "0x0209C000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                char cVal = '\0';
                int sIndex = 0;

                dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
                int nib = cVal.ascii_2_nibble();

                rlt = !nib.check_bit_0_or_1(BitPosition.third);
                value = Convert.ToString(nib, 2).PadLeft(4, '0');
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }



        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_TPH_ResiMeas(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "", sCmd = "";
            string regAddr = "0x020A4000", regVal = "0x00001335";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            regAddr = "0x020A4004";
            regVal = "0x1E7F6307";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);

            Delay(500);

            sCmd = PA45_FCT_Commands.Get_MCU_Pin_ADC(6);
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_uart_mcu_cat(tmpValue, ref value);
            double refVoltage = 3.3;
            double analogVal = value.d2a(10, refVoltage);
            analogVal = analogVal * 4.32;
            value = Convert.ToString(analogVal);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_MotorTest(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "", sCmd = "";
            string zh_cn = "", en_us = "", thai = "";

            RelayCard3.SetRelayOn(7);
            Keithy2306LAN.Connect();
            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 7.4);

            RelayCard3.SetRelayOn(2);
            Delay(1500);

            sCmd = PA45_FCT_Commands.motor_cleanfeed;

            zh_cn = "按下yes之后马达会跑。";
            en_us = "";
            thai = "";
            bool OpStep = GetOperatorSelection(zh_cn, en_us, thai);

            if (!OpStep)
            {
                value = "OP_STEP_FAIL";
                rlt = false;
                goto End;
            }

            SeriPortWriteLine(spUART, sCmd);
            zh_cn = "请观察马达是否有转动。";
            en_us = "";
            thai = "";
            OpStep = GetOperatorSelection(zh_cn, en_us, thai);

            rlt = OpStep;
            value = rlt ? "PASS" : "FAIL";
            Delay(5000);

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V24P0_MOTOR(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "", sCmd = "";

            /*
            string regAddr = "0x020A4000", regVal = "0x00003331";
            sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);
            RelayCard1.MeasureVoltage(6);
            value = Convert.ToString(RelayCard1.MeasureVoltage(6));
            */
            sCmd = PA45_FCT_Commands.motor_cleanfeed;
            Delay(100);
            //value = Keithy2306LAN.Measure_scpi(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1);
            value = RelayCard1.MeasureVoltage(6).ToString();
            Delay(2000);
        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_PLATEN_MOTOR(int index, TestItem item)
        {
            bool rlt = false;

            string value = "", tmpValue = "", sCmd = "";
            int relayIndex = -1;
            if (item.Name.Contains("J14"))
            {
                relayIndex = 7;
            }
            else
            {
                relayIndex = 6;
            }

            RelayCard2.SetRelayOn(relayIndex);
            Delay(500);
            sCmd = PA45_FCT_Commands.functest_motor_cleanfeed_1000;
            SeriPortWriteLine(spUART, sCmd);
            value = Keithy2306LAN.Measure(Electrical_Signal_Type.CURR, KEITHLEY_2306_Channel.CH1);
            RelayCard2.SetRelayOff(relayIndex);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_STM_VREF(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            string regAddr = "0x0209C000", regVal = "0x300061BF";

            string sCmd = PA45_FCT_Commands.SetMemoryAddressValue(regAddr, regVal);
            SeriPortWriteLine(spUART, sCmd);
            Delay(1000);
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

            string sCmd = PA45_FCT_Commands.GetSensorValue;
            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            string sensorVal = "", tempVal = "";
            dut.get_printer_sensor_templture(tmpValue, PA45_Sensor.MPCB, ref tempVal);
            value = tempVal;

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_3AxisMotion(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = PA45_FCT_Commands.functest_motion;

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            rlt = tmpValue.Contains("PASS");
            value = rlt ? "PASS" : "FAIL";

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_3AxisMotion_INT(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string sCmd = PA45_FCT_Commands.functest_motion;

            tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                rlt = false;
                goto End;
            }

            dut.get_MOTION(tmpValue, ref value);

            rlt = tmpValue.Contains("PASS") && value.ToUpper().Contains("0X");

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
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

        private bool Test_MB_FCT_V2P7_CAP(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(RelayCard2.MeasureVoltage(4) * 2);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VBATTPowerOff(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            //1. DC_IN Power Supply off (2306 ch2 关掉)
            Keithy2306LAN.SourceOff(KEITHLEY_2306_Channel.CH2);
            //2. VBUS_IN Power Supply off
            RelayCard1.SetRelayOff(2);
            Delay(500);

            //3. 電池需有接上 (VABTT) 2306 Channel-1 7.4V
            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 7.4);
            Delay(500);

            value = Convert.ToString(RelayCard1.MeasureVoltage(0) * 2);

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_VBUS_IN(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (item.Name.ToUpper().Contains("CC1O"))
            {
                RelayCard3.SetRelayOn(0);
                Delay(500);
                value = Convert.ToString(RelayCard2.MeasureVoltage(0) * 2);
                RelayCard3.SetRelayOff(0);
            }
            else if (item.Name.ToUpper().Contains("CC2O"))
            {
                RelayCard3.SetRelayOn(1);
                Delay(500);
                value = Convert.ToString(RelayCard2.MeasureVoltage(0) * 2);
                RelayCard3.SetRelayOff(1);
            }
            else
            {
                value = "ITEM_NAME_ERROR";
                rlt = false;
                goto End;
            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LOWPWR_IRQ_N(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "", adcVal = "";
            string regAddr = "", regVal = "", sCmd = "";
            char cVal = '\0';

            if (item.Name.Contains(">6.5"))
            {
                /*
                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 2.5);
                Delay(1000);
                RelayCard3.SetRelayOn(6);
                */

                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 7.0);

                regAddr = "0x0209C000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                if (string.IsNullOrWhiteSpace(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                dut.get_Memtool_Register_Char_Value(regVal, 7, ref cVal);
                int nib = cVal.ascii_2_nibble();

                if (!nib.check_bit_0_or_1(BitPosition.second))
                {
                    value = "CHECK_IMX_GPIO_IO02_HIGH_NG";
                    rlt = false;
                    goto End;
                }
                Delay(500);
                value = Convert.ToString(RelayCard2.MeasureVoltage(1) * 2);
                //RelayCard3.SetRelayOff(6);

            }
            else if (item.Name.Contains("<6.5"))
            {
                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 2.5);
                Delay(1000);
                RelayCard3.SetRelayOn(6);

                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 6);

                regAddr = "0x0209C000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);

                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                if (string.IsNullOrWhiteSpace(tmpValue))
                {
                    value = "TimeOut";
                    rlt = false;
                    goto End;
                }

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                dut.get_Memtool_Register_Char_Value(regVal, 7, ref cVal);
                int nib = cVal.ascii_2_nibble();

                if (nib.check_bit_0_or_1(BitPosition.second))
                {
                    value = "LOWPWR_IRQ_N_FAIL";
                    rlt = false;
                    goto End;
                }
                Delay(500);
                value = Convert.ToString(RelayCard2.MeasureVoltage(1) * 2);
                RelayCard3.SetRelayOn(6);

            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_LOWPWR_IRQ_N_DI(int index, TestItem item)
        {
            bool rlt = false, bTimeOut = true;
            string value = "", tmpValue = "";

            string regAddr = "0x0209C000", regVal = "";
            string sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
            SeriPortWriteLine(spUART, sCmd);

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                tmpValue = GetUARTStringFlagA(sCmd);
                if (!string.IsNullOrWhiteSpace(tmpValue))
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

            dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
            char cVal = '\0';
            int sIndex = 7;

            //0<4>5FA400  ( 0x<010>0 )
            dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
            int nib = cVal.ascii_2_nibble();

            AppendRuntimeLog($"Nibble = {nib}, 4bit binary = {Convert.ToString(nib, 2).PadLeft(4, '0')}");

            bool is_bit2_0 = (nib & 0x4) == 0;
            value = (nib & 0x4).ToString();
            if (item.Name.Contains("=6.5"))
            {
                rlt = is_bit2_0 == false;
            }
            else
            {
                rlt = is_bit2_0 == true;
            }

        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_BatteryPowerSupply(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            if (item.Name.Contains("6.0"))
            {
                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 2.5);
                Delay(1000);
                RelayCard3.SetRelayOn(6);
                Delay(1000);
                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH1, 4.5);
            }

            value = Convert.ToString(RelayCard2.MeasureVoltage(3) * 2);

            if (item.Name.Contains("6.0"))
            {
                RelayCard3.SetRelayOff(6);
            }

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

        private bool Test_MB_FCT_SuperCapLowDetect(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";
            string regAddr = "", regVal = "", sCmd = "";

            Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 2.5);
            Delay(1000);
            RelayCard3.SetRelayOn(6);

            if (item.Name.ToLower().Contains("high"))
            {
                value = Convert.ToString(RelayCard2.MeasureVoltage(3));
                //step1, 
                Keithy2306LAN.Connect();
                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 3);

                Delay(1000);
                RelayCard3.SetRelayOn(6);

                regAddr = "0x020A4000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                char cVal = '\0';
                int sIndex = 6;

                dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
                int nib = cVal.ascii_2_nibble();

                rlt = !nib.check_bit_0_or_1(BitPosition.first);
                value = Convert.ToString(nib, 2).PadLeft(4, '0');
                RelayCard3.SetRelayOff(6);
            }
            else
            {
                value = Convert.ToString(RelayCard2.MeasureVoltage(3));
                //step1, 
                Keithy2306LAN.Connect();
                Keithy2306LAN.Source(Electrical_Signal_Type.VOLT, KEITHLEY_2306_Channel.CH2, 1.5);
                Delay(1000);
                RelayCard3.SetRelayOn(6);
                regAddr = "0x020A4000";
                sCmd = PA45_FCT_Commands.GetMemoryAddressValue(regAddr);
                tmpValue = Write_Read_SeriPort(Communicate_Type.UART, sCmd);

                dut.get_Memtool_Register_Hex_Value(tmpValue, regAddr, ref regVal);
                char cVal = '\0';
                int sIndex = 6;

                dut.get_Memtool_Register_Char_Value(regVal, sIndex, ref cVal);
                int nib = cVal.ascii_2_nibble();

                rlt = nib.check_bit_0_or_1(BitPosition.first);
                value = Convert.ToString(nib, 2).PadLeft(4, '0');
                RelayCard3.SetRelayOff(6);
            }

            RelayCard3.SetRelayOff(6);
        End:
            item.Value = value;
            //rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_V5P0_MCU_PowerOff(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            value = Convert.ToString(CaptureCard1.MeasureVoltage(2));

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        private bool Test_MB_FCT_PowerButtonTurnOff(int index, TestItem item)
        {
            bool rlt = false;
            string value = "", tmpValue = "";

            RelayCard3.SetRelayOn(4);

            Delay(6000);
            RelayCard3.SetRelayOff(4);

            for (int i = 0; i < 30; i++)
            {
                value = Convert.ToString(CaptureCard1.MeasureVoltage(2));
                item.Value = value;
                rlt = item.IsValueInRange<double>();
                AppendRuntimeLog($"times:{i}, voltage: {value}");

                if (rlt)
                {
                    return rlt;
                }
                Delay(1000);
            }

        End:
            item.Value = value;
            rlt = item.IsValueInRange<double>();
            return rlt;
        }

        /*
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
    */
        private bool Test_MB_FCT_MCU_FW_Version(int index, TestItem item)
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
            //try
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
                    m_type.Name == "LowSensor" ||
                    m_type.Name == "LTSSensor" ||
                    m_type.Name == "PA45_PowerBoard" ||
                    (m_type.Name == "PA45" && productConfig.STATION_CODE == "Print_MAC_label_Program_NFC"))
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

                        if (ckbFailStop.Checked) { if (!dut.TestItems[i].Result) { break; } }
                    }
                }
                else
                {
                    UpdateErrorUI(Test_Proc_Error_Code.NoSeriPort);
                }

                Invoke(new Action(() => { lblFinishTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); }));
            }

            /*
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
            */

            //finally
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
                    DataAccess.SaveCsvLogByDay(dut, MainboardPartNumber);
                }));

                if (productConfig.STATION_CODE.Contains("03"))
                {
                    this.Invoke(new Action(() => { ckbSFCS.Checked = true; }));
                }

                if (ckbSFCS.Checked)
                {
                    Prod.PA45 m_PA45 = product as Prod.PA45;
                    string m_sn = "", m_sn1 = "";
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
                    string valueList = $"{dut.Product.ProductConfig.LINE_CODE},{dut.Product.ProductConfig.STATION_CODE},{dut.Product.ProductConfig.STATION_CODE},{MainBarcode},{lblResultWin.Text},{""},{dut.Product.ProductConfig.MODEL_NO},{Dns.GetHostName()}";

                    for (int i = 0; i < dut.TestItems.Count; i++)
                    {
                        columnList = columnList + "," + dut.TestItems[i].Name;
                        valueList = valueList + "," + dut.TestItems[i].Value;
                    }

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
                }

                WIFI_MAC_ADDRESS = "";
                BLUETOOTH_MAC_ADDRESS = "";
                PrinterPartNumber = "";
                PrinterSeriNumber = "";
                MainboardPartNumber = "";
                MainboardSeriNumber = "";
                have_send_wifi_script = false;

                spTester.Close();
                spUART.Close();


                if (Keithy2306LAN != null && productConfig.STATION_CODE == "FCT")
                {
                    Keithy2306LAN.SourceOff(KEITHLEY_2306_Channel.CH1);
                    Keithy2306LAN.SourceOff(KEITHLEY_2306_Channel.CH2);
                }

                CloseAllRelays();

                product.RunTimeLogUpdated -= OnRunTimeLogUpdated;
                product = null;

                dut.Product.RunTimeLogUpdated -= OnRunTimeLogUpdated;
                dut.Product = null;
                dut.RunTimeLogUpdated -= OnRunTimeLogUpdated;
                dut = null;

                _trigger = null;
                ClearUI();
                SetTestEntery(true);
                this.Invoke(new Action(() => { txtSN.Focus(); }));
                GC.Collect();

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

                case "LSS Sensor 230":
                    item.Result = Test_Printer_LssSensor_230(index, item);
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

                /*
                case "Wifi(Ping Router)":
                    item.Result = Test_Printer_PingWifiRouter(index, item);
                    break;
                    */

                case "WIFI Ping Test":
                    item.Result = Test_Printer_PingWifiRouter(index, item);
                    break;

                case "Find Device(BT)":
                    item.Result = Test_Printer_FindBluetoothDevice(index, item);
                    break;

                case "Find Printer(Bluetooth)":
                    item.Result = Test_Printer_FindBluetoothDevice(index, item);
                    break;

                case "RFID":
                    item.Result = Test_Printer_RFID(index, item);
                    break;

                case "Set RFID Country":
                    item.Result = Test_Printer_SetRFIDRegionCode(index, item);
                    break;

                case "Check RFID Country":
                    item.Result = Test_Printer_CheckRFIDRegionCode(index, item);
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

                case "Coin Battery":
                    item.Result = Test_MB_FCT_CoinBattery(index, item);
                    break;

                case "Auto PowerOn(5V)":
                    item.Result = Test_MB_FCT_AutoPowerOn5V(index, item);
                    break;

                case "Auto PowerOn(12V)":
                    item.Result = Test_MB_FCT_AutoPowerOn12V(index, item);
                    break;

                case "PowerOff Curr(7.4V)":
                    item.Result = Test_MB_FCT_PowerOn7p4V(index, item);
                    break;

                case "PowerKey Press":
                    item.Result = Test_MB_FCT_PowerKeyPress(index, item);
                    break;

                case "PowerOn Curr(7.4V)":
                    item.Result = Test_MB_FCT_PowerOn7p4V_battery(index, item);
                    break;

                /*
            case "FCT Mode":
                item.Result = Test_MB_FCT_GotoFctMode(index, item);
                break;
                */

                /*
            case "Power On(MB FCT)":
                item.Result = Test_MB_FCT_PowerOn(index, item);
                break;
                */

                case "Comm J1":
                    item.Result = Test_MB_FCT_CommJ1(index, item);
                    break;

                case "System Ready(MB FCT)":
                    item.Result = Test_MB_FCT_SystemReady(index, item);
                    break;

                case "FW Version(MB FCT)":
                    item.Result = Test_MB_FCT_FwVersion(index, item);
                    break;



                case "Boot Mode":
                    item.Result = Test_MB_FCT_BootMode(index, item);
                    break;

                case "Config HW":
                    item.Result = Test_MB_FCT_ConfigHW(index, item);
                    break;

                case "RTC Battery(coin)":
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

                case "V1P35_SOC(Normal)":
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

                case "V3P3_AUX(high)":
                    item.Result = Test_MB_FCT_V3P3_AUX(index, item);
                    break;

                case "V3P3_AUX(low)":
                    item.Result = Test_MB_FCT_V3P3_AUX(index, item);
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

                case "V3P3_WIFI(high)":
                    item.Result = Test_MB_FCT_V3P3_WIFI(index, item);
                    break;

                case "V3P3_WIFI(low)":
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

                /*
            case "LED1(SOC_DEBUG_LED)":
                item.Result = Test_MB_FCT_LED1(index, item);
                break;

            case "LED2(MCU P13)":
                item.Result = Test_MB_FCT_LED2(index, item);
                break;
                */

                case "LED3(Power LED)":
                    item.Result = Test_MB_FCT_PowerLED(index, item);
                    break;

                case "Battery Charger Current":
                    item.Result = Test_MB_FCT_DC_IN_ChargerCurrent(index, item);
                    break;

                case "LED4(charging)":
                    item.Result = Test_MB_FCT_LED4_Charging(index, item);
                    break;

                case "LED4(full charged)":
                    item.Result = Test_MB_FCT_LED4_FullCharged(index, item);
                    break;

                case "Set RTC(MB FCT)":
                    item.Result = Test_MB_FCT_SetRtc(index, item);
                    break;

                case "TP130 short TP110(high)":
                    item.Result = Test_MB_FCT_Tp130ShortTp110_H(index, item);
                    break;

                case "TP130 short TP110(low)":
                    item.Result = Test_MB_FCT_Tp130ShortTp110_1_L(index, item);
                    break;

                case "TP128 short TP112(high)":
                    item.Result = Test_MB_FCT_Tp128ShortTp112_H(index, item);
                    break;

                case "TP128 short TP112(low)":
                    item.Result = Test_MB_FCT_Tp128ShortTp112_L(index, item);
                    break;

                case "TP111(high)":
                    item.Result = Test_MB_FCT_Tp111_H(index, item);
                    break;

                case "TP111(low)":
                    item.Result = Test_MB_FCT_Tp111_L(index, item);
                    break;

                case "TP122 short TP126(high)":
                    item.Result = Test_MB_FCT_Tp122ShortTp126_H(index, item);
                    break;

                case "TP122 short TP126(low)":
                    item.Result = Test_MB_FCT_Tp122ShortTp126_L(index, item);
                    break;

                case "TP116 short TP114(high)":
                    item.Result = Test_MB_FCT_Tp116ShortTp114_H(index, item);
                    break;

                case "TP116 short TP114(low)":
                    item.Result = Test_MB_FCT_Tp116ShortTp114_L(index, item);
                    break;

                case "TP115 short TP113(high)":
                    item.Result = Test_MB_FCT_Tp115ShortTp113_H(index, item);
                    break;

                case "TP115 short TP113(low)":
                    item.Result = Test_MB_FCT_Tp115ShortTp113_L(index, item);
                    break;

                case "TP117 short TP118(high)":
                    item.Result = Test_MB_FCT_Tp117ShortTp118_H(index, item);
                    break;

                case "TP117 short TP118(low)":
                    item.Result = Test_MB_FCT_Tp117ShortTp118_L(index, item);
                    break;

                case "TP121 short TP123(high)":
                    item.Result = Test_MB_FCT_Tp121ShortTp123_H(index, item);
                    break;

                case "TP121 short TP123(low)":
                    item.Result = Test_MB_FCT_Tp121ShortTp123_L(index, item);
                    break;

                case "TP127 short TP129(high)":
                    item.Result = Test_MB_FCT_Tp127ShortTp129H(index, item);
                    break;

                case "TP127 short TP129(low)":
                    item.Result = Test_MB_FCT_Tp127ShortTp129_L(index, item);
                    break;

                case "UP_IRQ_N(high)":
                    item.Result = Test_MB_FCT_UP_IRQ_N(index, item);
                    break;

                case "UP_IRQ_N(low)":
                    item.Result = Test_MB_FCT_UP_IRQ_N(index, item);
                    break;

                case "Top LSS SENSOR J15":
                    item.Result = Test_MB_FCT_LssSensorADC(index, item);
                    break;

                case "Low LSS SENSOR J18":
                    item.Result = Test_MB_FCT_LssSensorADC(index, item);
                    break;

                case "Top LSS SENSOR J15(GPIO)":
                    item.Result = Test_MB_FCT_LssSensorDI(index, item);
                    break;

                case "Low LSS SENSOR J18(GPIO)":
                    item.Result = Test_MB_FCT_LssSensorDI(index, item);
                    break;

                case "COVER SENSOR":
                    item.Result = Test_MB_FCT_COVER_SENSOR(index, item);
                    break;

                case "SELF STRIP SENSOR(high)":
                    item.Result = Test_MB_FCT_SelfStripSensor_DI(index, item);
                    break;

                case "SELF STRIP SENSOR(low)":
                    item.Result = Test_MB_FCT_SelfStripSensor_DI(index, item);
                    break;

                case "SELF STRIP SENSOR I2C(ADC)":
                    item.Result = Test_MB_FCT_SelfStripSensor(index, item);
                    break;

                case "LCD J19 TX(high)":
                    item.Result = Test_MB_FCT_LCD_TX(index, item);
                    break;

                case "LCD J19 CLK(low)":
                    item.Result = Test_MB_FCT_LCD_CLK(index, item);
                    break;

                case "LCD J19 TX(low)":
                    item.Result = Test_MB_FCT_LCD_TX(index, item);
                    break;

                case "LCD J19 CLK(high)":
                    item.Result = Test_MB_FCT_LCD_CLK(index, item);
                    break;

                case "TPH TP151(low)":
                    item.Result = Test_MB_FCT_TPH_TP151(index, item);
                    break;

                case "TPH TP152(low)":
                    item.Result = Test_MB_FCT_TPH_TP152(index, item);
                    break;

                case "TPH TP153(low)":
                    item.Result = Test_MB_FCT_TPH_TP153(index, item);
                    break;

                case "TPH TP157(low)":
                    item.Result = Test_MB_FCT_TPH_TP157(index, item);
                    break;

                case "TPH TP158(low)":
                    item.Result = Test_MB_FCT_TPH_TP158(index, item);
                    break;

                case "TPH TP159(low)":
                    item.Result = Test_MB_FCT_TPH_TP159(index, item);
                    break;

                case "TPH TP160(low)":
                    item.Result = Test_MB_FCT_TPH_TP160(index, item);
                    break;

                case "TPH TP161(low)":
                    item.Result = Test_MB_FCT_TPH_TP161(index, item);
                    break;

                case "TPH TP151(high)":
                    item.Result = Test_MB_FCT_TPH_TP151(index, item);
                    break;

                case "TPH TP152(high)":
                    item.Result = Test_MB_FCT_TPH_TP152(index, item);
                    break;

                case "TPH TP153(high)":
                    item.Result = Test_MB_FCT_TPH_TP153(index, item);
                    break;

                case "TPH TP157(high)":
                    item.Result = Test_MB_FCT_TPH_TP157(index, item);
                    break;

                case "TPH TP158(high)":
                    item.Result = Test_MB_FCT_TPH_TP158(index, item);
                    break;

                case "TPH TP159(high)":
                    item.Result = Test_MB_FCT_TPH_TP159(index, item);
                    break;

                case "TPH TP160(high)":
                    item.Result = Test_MB_FCT_TPH_TP160(index, item);
                    break;

                case "TPH TP161(high)":
                    item.Result = Test_MB_FCT_TPH_TP161(index, item);
                    break;

                case "VBATT1 SENSE":
                    item.Result = Test_MB_FCT_VBATT1_SENSE(index, item);
                    break;

                case "3-AXIS MOTION SENSOR":
                    item.Result = Test_MB_FCT_3AxisMotion(index, item);
                    break;

                case "3-AXIS MOTION SENSOR(INT1)":
                    item.Result = Test_MB_FCT_3AxisMotion_INT(index, item);
                    break;

                case "3-AXIS MOTION SENSOR(INT2)":
                    item.Result = Test_MB_FCT_3AxisMotion_INT(index, item);
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

                case "LCD(J19)(LEDA)(high)":
                    item.Result = Test_MB_FCT_J19_LCD_LEDA(index, item);
                    break;

                case "LCD(J19)(LEDA)(low)":
                    item.Result = Test_MB_FCT_J19_LCD_LEDA(index, item);
                    break;

                case "Buzzer Control SOC(high)":
                    item.Result = Test_MB_FCT_BuzzerControlSOC(index, item);
                    break;

                case "Buzzer Control SOC(low)":
                    item.Result = Test_MB_FCT_BuzzerControlSOC(index, item);
                    break;

                case "Buzzer Control MCU(high)":
                    item.Result = Test_MB_FCT_BuzzerControlSOC(index, item);
                    break;

                case "Buzzer Control MCU(low)":
                    item.Result = Test_MB_FCT_BuzzerControlSOC(index, item);
                    break;

                case "RTC Test(Read)":
                    item.Result = Test_MB_FCT_RctSet(index, item);
                    break;

                case "Key SOC(TP191)":
                    item.Result = Test_MB_FCT_Key_SOC_TP191(index, item);
                    break;

                case "Key SOC(TP193)":
                    item.Result = Test_MB_FCT_Key_SOC_TP193(index, item);
                    break;

                case "Key SOC(TP197)":
                    item.Result = Test_MB_FCT_Key_SOC_TP197(index, item);
                    break;

                case "Key SOC(TP199)":
                    item.Result = Test_MB_FCT_Key_SOC_TP199(index, item);
                    break;

                case "Key SOC(TP200)":
                    item.Result = Test_MB_FCT_Key_SOC_TP200(index, item);
                    break;

                case "Key SOC(TP201)":
                    item.Result = Test_MB_FCT_Key_SOC_TP201(index, item);
                    break;

                case "Key MCU":
                    item.Result = Test_Printer_KeyButton(index, item);
                    break;

                case "Key MCU(TP191)":
                    item.Result = Test_MB_FCT_Key_MCU_TP191(index, item);
                    break;

                case "Key MCU(TP193)":
                    item.Result = Test_MB_FCT_Key_MCU_TP193(index, item);
                    break;

                case "Key MCU(TP197)":
                    item.Result = Test_MB_FCT_Key_MCU_TP197(index, item);
                    break;

                case "Key MCU(TP199)":
                    item.Result = Test_MB_FCT_Key_MCU_TP199(index, item);
                    break;

                case "Key MCU(TP200)":
                    item.Result = Test_MB_FCT_Key_MCU_TP200(index, item);
                    break;

                case "Key MCU(TP201)":
                    item.Result = Test_MB_FCT_Key_MCU_TP201(index, item);
                    break;

                case "RFID Power(high)":
                    item.Result = Test_MB_FCT_RFID_Power(index, item);
                    break;

                case "RFID Power(low)":
                    item.Result = Test_MB_FCT_RFID_Power(index, item);
                    break;

                case "J22 GPIO(high)":
                    item.Result = Test_MB_FCT_J22(index, item);
                    break;

                case "J22 GPIO(low)":
                    item.Result = Test_MB_FCT_J22(index, item);
                    break;

                case "Cellular Modem":
                    item.Result = Test_MB_FCT_Cellular_Modem(index, item);
                    break;

                case "USB HUB":
                    item.Result = Test_MB_FCT_USB_HUB(index, item);
                    break;

                case "USB HUB(HOST)(CC2)":
                    item.Result = Test_MB_FCT_USB_HUB_HOST_J5(index, item);
                    break;

                case "USB HUB(HOST)(CC1)":
                    item.Result = Test_MB_FCT_USB_HUB_HOST_J5(index, item);
                    break;

                case "MFI Chip":

                    if (dut.GetType().Name == "PA45")
                    {
                        item.Result = Test_Printer_MFI_Detect(index, item);
                    }
                    else
                    {
                        item.Result = Test_MB_FCT_MFI_Chip(index, item);
                    }

                    break;

                case "VEHICLE_MOUNT_DET":
                    item.Result = Test_MB_FCT_VEHICLE_MOUNT_DET(index, item);
                    break;

                case "Security IC":
                    item.Result = Test_MB_FCT_SecurityIC(index, item);
                    break;

                case "Charger IC":
                    item.Result = Test_MB_FCT_ChargerIC(index, item);
                    break;


                case "TPH Resistance Measurement":
                    item.Result = Test_MB_FCT_TPH_ResiMeas(index, item);
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

                case "V2P7_CAP":
                    item.Result = Test_MB_FCT_V2P7_CAP(index, item);
                    break;

                case "VBATT PowerOff":
                    item.Result = Test_MB_FCT_VBATTPowerOff(index, item);
                    break;

                case "VBUS_IN(CC1O)":
                    item.Result = Test_MB_FCT_VBUS_IN(index, item);
                    break;

                case "VBUS_IN(CC2O)":
                    item.Result = Test_MB_FCT_VBUS_IN(index, item);
                    break;

                case "LOWPWR_IRQ_N(>6.5V)":
                    item.Result = Test_MB_FCT_LOWPWR_IRQ_N(index, item);
                    break;

                case "LOWPWR_IRQ_N(<6.5V)":
                    item.Result = Test_MB_FCT_LOWPWR_IRQ_N(index, item);
                    break;

                case "LTPPWR(TPH)(ON)":
                    item.Result = Test_MB_FCT_LTPPWR_TPH(index, item);
                    break;

                case "LTPPWR(TPH)(OFF)":
                    item.Result = Test_MB_FCT_LTPPWR_TPH(index, item);
                    break;

                case "VSYS(battery 6.7)":
                    item.Result = Test_MB_FCT_BatteryPowerSupply(index, item);
                    break;

                case "VSYS(battery 6.0)":
                    item.Result = Test_MB_FCT_BatteryPowerSupply(index, item);
                    break;

                /*
            case "SUPER CAP(VSYS)":
                item.Result = Test_MB_FCT_SUPER_CAP(index, item);
                break;
                */

                /*
                case "Hotswap(V5P2_CAP)":
                    item.Result = Test_MB_FCT_HOTSWAP_V5P2_CAP(index, item);
                    break;
                    */

                case "Motor Test":
                    item.Result = Test_MB_FCT_MotorTest(index, item);
                    break;

                case "Motor Test -1":
                    item.Result = Test_MB_FCT_MotorTest(index, item);
                    break;

                case "Super CAP Low Voltage Detector(high)":
                    item.Result = Test_MB_FCT_SuperCapLowDetect(index, item);
                    break;

                case "Super CAP Low Voltage Detector(low)":
                    item.Result = Test_MB_FCT_SuperCapLowDetect(index, item);
                    break;

                case "V5P0 MCU Power Off":
                    item.Result = Test_MB_FCT_V5P0_MCU_PowerOff(index, item);
                    break;

                case "Power Button Turn Off Test":
                    item.Result = Test_MB_FCT_PowerButtonTurnOff(index, item);
                    break;

                case "Uboot Version(MB FCT)":
                    item.Result = Test_Printer_FwVersion(index, item);
                    break;

                case "LTS Sensor Function":
                    item.Result = Test_Printer_LTS(index, item);
                    break;

                case "Battery Function":
                    item.Result = Test_Printer_Battery(index, item);
                    break;

                /*
            case "Battery Function":
                item.Result = Test_Printer_Battery(index, item);
                break;
                */

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

                /*
            case "BM Sensor ADC Value(MB EOL)":
                item.Result = Test_MB_FCT_BMSensor(index, item);
                break;

            case "BM Sensor ADC Value(Front)(MB EOL)":
                item.Result = Test_MB_FCT_BMSensor(index, item);
                break;
                */

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

                case "USB_CC_INT(high)":
                    item.Result = Test_Powerboard_USB_CC_INT(index, item);
                    break;

                case "USB_CC_INT(low)":
                    item.Result = Test_Powerboard_USB_CC_INT(index, item);
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

                case "USB2_DOCK_DP(high)":
                    item.Result = Test_Powerboard_USB2_DOCK_DP(index, item);
                    break;

                case "USB2_DOCK_DP(low)":
                    item.Result = Test_Powerboard_USB2_DOCK_DP(index, item);
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

                case "Check Jig Air Pressure":
                    item.Result = Test_Keyboard_JigAirPressure(index, item);
                    break;

                case "UI_SW1-OFF":
                    item.Result = Test_Keyboard_SW1_UiSw1(index, item);
                    break;

                case "UI_SW1-ON":
                    item.Result = Test_Keyboard_SW1_UiSw1(index, item);
                    break;

                case "UI_SW3-OFF":
                    item.Result = Test_Keyboard_SW3_UiMenuKey(index, item);
                    break;

                case "UI_SW3-ON":
                    item.Result = Test_Keyboard_SW3_UiMenuKey(index, item);
                    break;

                case "UI_SW5-OFF":
                    item.Result = Test_Keyboard_SW5_DownKey(index, item);
                    break;

                case "UI_SW5-ON":
                    item.Result = Test_Keyboard_SW5_DownKey(index, item);
                    break;

                case "UI_SW2-OFF":
                    item.Result = Test_Keyboard_SW2_EntryKey(index, item);
                    break;

                case "UI_SW2-ON":
                    item.Result = Test_Keyboard_SW2_EntryKey(index, item);
                    break;

                case "UI_SW4-OFF":
                    item.Result = Test_Keyboard_SW4_FeedSw2(index, item);
                    break;

                case "UI_SW4-ON":
                    item.Result = Test_Keyboard_SW4_FeedSw2(index, item);
                    break;

                case "UI_SW6-OFF":
                    item.Result = Test_Keyboard_SW6_UpKey(index, item);
                    break;

                case "UI_SW6-ON":
                    item.Result = Test_Keyboard_SW6_UpKey(index, item);
                    break;

                case "LED_POWER_RED-ON":
                    item.Result = Test_Keyboard_LedPowerOn(index, item);
                    break;

                case "LED_POWER_GRN-ON":
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

                case "RePower On":
                    item.Result = Test_WiFiBtModule_RePowerOn(index, item);
                    break;

                case "BT_Module Write(BT)":
                    item.Result = Test_BtModule_WriteMAC(index, item);
                    break;

                case "BT_Module Read(BT)":
                    item.Result = Test_WiFiBtModule_BtWifiRead(index, item);
                    break;

                case "WIFI MAC":
                    item.Result = Test_WiFiBtModule_BtWifiRead(index, item);
                    break;

                case "BT MAC":

                    if (dut.GetType().Name == "BT_Module")
                    {
                        item.Result = Test_BtModule_WriteMAC(index, item);
                    }
                    else
                    {
                        item.Result = Test_WiFiBtModule_BtWifiRead(index, item);
                    }

                    break;

                case "Print MAC(BT)":
                    string mDUT_Type = dut.GetType().Name;
                    if (mDUT_Type == "PA45")
                        item.Result = Test_Printer_PrintMACLabel(index, item);
                    else
                        item.Result = Test_WiFiBtModule_PrintMAC(index, item);
                    break;

                case "Print MAC(Wifi)":
                    string mDUT_Type1 = dut.GetType().Name;
                    if (mDUT_Type1 == "PA45")
                        item.Result = Test_Printer_PrintMACLabel(index, item);
                    else
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
                if (item.Description.ToLower().Contains("force"))
                {
                    item.Result = true;
                    item.Value = item.Value.Replace("FAIL", "PASS");
                }

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

        private string Write_Read_SeriPort(Communicate_Type cType, string text, int milScd = 500)
        {
            string tmpValue = "";
            string m_tag = Guid.NewGuid().ToString("N");
            if (cType == Communicate_Type.VirtualComPort)
            {
                AppendRuntimeLog(m_tag);
                SerialPortWriteLineOnce(text);
            }

            if (cType == Communicate_Type.UART)
            {
                AppendSeriPortLog(m_tag);
                SeriPortWriteLine(spUART, text);
            }

            Delay(1000);

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                if (cType == Communicate_Type.VirtualComPort)
                {
                    tmpValue = GetRuntimeStringFlagA(m_tag);
                }

                if (cType == Communicate_Type.UART)
                {
                    tmpValue = GetUARTStringFlagA(m_tag);
                }

                tmpValue = tmpValue.Replace(m_tag, "");
                tmpValue = tmpValue.Replace(text, "");
                tmpValue = tmpValue.Replace("root@(none) ~$", "");

                if (!string.IsNullOrWhiteSpace(tmpValue))
                {
                    return tmpValue;
                }

                Delay(500);
            }

            if (cType == Communicate_Type.UART)
            {
                AppendRuntimeLog(tmpValue);
            }


            return tmpValue;
        }

        private string Write_Read_SeriPort_Delay(Communicate_Type cType, string text, int milScd = 3000)
        {
            string tmpValue = "";
            string m_tag = Guid.NewGuid().ToString("N");
            if (cType == Communicate_Type.VirtualComPort)
            {
                AppendRuntimeLog(m_tag);
                SerialPortWriteLineOnce(text);
            }

            if (cType == Communicate_Type.UART)
            {
                AppendSeriPortLog(m_tag);
                SeriPortWriteLine(spUART, text);
            }

            Delay(milScd);

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                if (cType == Communicate_Type.VirtualComPort)
                {
                    tmpValue = GetRuntimeStringFlagA(m_tag);
                }

                if (cType == Communicate_Type.UART)
                {
                    tmpValue = GetUARTStringFlagA(m_tag);
                }

                tmpValue = tmpValue.Replace(m_tag, "");
                tmpValue = tmpValue.Replace(text, "");
                tmpValue = tmpValue.Replace("root@(none) ~$", "");

                if (!string.IsNullOrWhiteSpace(tmpValue))
                {
                    return tmpValue;
                }

                Delay(500);
            }

            if (cType == Communicate_Type.UART)
            {
                AppendRuntimeLog(tmpValue);
            }


            return tmpValue;
        }

        private string Write_Read_SeriPort_6000(Communicate_Type cType, string text, int milScd = 6000)
        {
            string tmpValue = "";
            string m_tag = Guid.NewGuid().ToString("N");
            if (cType == Communicate_Type.VirtualComPort)
            {
                AppendRuntimeLog(m_tag);
                SerialPortWriteLineOnce(text, milScd);
            }

            if (cType == Communicate_Type.UART)
            {
                AppendSeriPortLog(m_tag);
                SeriPortWriteLine(spUART, text);
            }

            Delay(1000);

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                if (cType == Communicate_Type.VirtualComPort)
                {
                    tmpValue = GetRuntimeStringFlagA(m_tag);
                }

                if (cType == Communicate_Type.UART)
                {
                    tmpValue = GetUARTStringFlagA(m_tag);
                }

                tmpValue = tmpValue.Replace(m_tag, "");
                tmpValue = tmpValue.Replace(text, "");
                tmpValue = tmpValue.Replace("root@(none) ~$", "");

                if (!string.IsNullOrWhiteSpace(tmpValue))
                {
                    return tmpValue;
                }

                Delay(500);
            }

            if (cType == Communicate_Type.UART)
            {
                AppendRuntimeLog(tmpValue);
            }
            
            return tmpValue;
        }

        private bool test_feed_1(string pic_path = "")
        {
            SerialPortWriteLineOnce(PrinterCommands.testfeed_1);
            string zh_cn = "请确认testfeed -1 是否完成。";
            string en_us = "please check testfeed -1 have finished yet.";
            string thai = "thai tips (unknown)";

            if (string.IsNullOrWhiteSpace(pic_path))
            {
                pic_path = Path.Combine(Global.ASSEMBLY_PATH, "Res", "testfeed_gap.jpg");
            }

            return GetOperatorSelection(zh_cn, en_us, thai, pic_path);
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

        private Media_Type GetMediaType()
        {
            Media_Type type = Media_Type.Error;
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
                if (!string.IsNullOrWhiteSpace(tmpValue))
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

            if (media_type == "Media With Gaps")
            {
                type = Media_Type.Gaps;
            }

            if (media_type == "Black Mark")
            {
                type = Media_Type.BlackMark;
            }

            if (media_type == "Continuous Var Len")
            {
                type = Media_Type.ContinuousVarLen;
            }

            CurrentMediaType = type;
        End:
            return type;
        }

        private string GetLTSSensorValue(Communicate_Type cType)
        {
            string value = "", tmpValue = "", sCmd = "", m_tag = Guid.NewGuid().ToString("N");

            if (cType == Communicate_Type.UART) { sCmd = PA45_FCT_Commands.functest(SUB_COMMAND.LTS); }

            if (cType == Communicate_Type.VirtualComPort) { sCmd = PrinterCommands.LTS; }

            tmpValue = Write_Read_SeriPort(cType, sCmd);

            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = "TimeOut";
                goto End;
            }

            if (tmpValue.Contains($"{Printer_Error_Code.HW_NOT_INSTALL}"))
            {
                value = $"{Printer_Error_Code.HW_NOT_INSTALL}";
                goto End;
            }

            dut.get_LTS(tmpValue, ref value);
        End:
            return value;
        }

        private bool check_unlock_os_cmds()
        {

            SerialPortWriteLineOnce(PrinterCommands.LoginRoot);

            bool bTimeOut = true;
            string tmpValue = "", m_tag = Guid.NewGuid().ToString("N");
            AppendRuntimeLog(m_tag);

            SerialPortWriteLineOnce(PrinterCommands.unlock_os_cmds);

            for (int i = 0; i < RegularTimeOutCount; i++)
            {
                tmpValue = GetRuntimeStringFlagA(m_tag);
                tmpValue = tmpValue.Replace(PrinterCommands.unlock_os_cmds, "");
                if (!string.IsNullOrWhiteSpace(tmpValue))
                {
                    bTimeOut = false;
                    break;
                }
                Delay(500);
            }
            IEnumerable<TestItem> ping_test_flags = dut.TestItems
                  .Where(x => x.Name.ToLower().Contains("wifi") && x.Name.ToLower().Contains("ping")).ToList();

            string dutType = dut.GetType().Name;

            if (!have_send_wifi_script)
            {
                if (ping_test_flags != null)
                {
                    if (dutType == "WiFi_BT_Module")
                    {
                        ssid = INI.GetValue("Wifi_Router", "WifiBT_SSID", ConfigFile);
                        pwd = INI.GetValue("Wifi_Router", "WifiBT_PWD ", ConfigFile);
                        router_ip = INI.GetValue("Wifi_Router", "WifiBT_IP", ConfigFile);
                    }
                    else if (dutType == "PA45")
                    {
                        Prod.PA45 m_PA45 = product as Prod.PA45;

                        if (m_PA45 == null ||
                            Prod.PA45.CommHWOpt == CommHWOpt.BLUETOOTH_ONLY ||
                            Prod.PA45.CommHWOpt == CommHWOpt.USB_ONLY
                            )
                        {
                            goto End;
                        }

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
                    }
                    else
                    {
                        goto End;
                    }

                    INI.GetValue("Wifi_Router", "IP", ConfigFile);

                    string wifi_setting_script = "", wifi_setting_script_path = "";

                    wifi_setting_script_path = Path.Combine(Global.ASSEMBLY_PATH, "wifi_script.txt");

                    if (!File.Exists(wifi_setting_script_path))
                    {
                        AppendRuntimeLog($"can't find {wifi_setting_script_path}.");
                        goto End;
                    }

                    using (StreamReader sw = new StreamReader(wifi_setting_script_path)) { wifi_setting_script = sw.ReadToEnd(); }

                    if (!wifi_setting_script.Contains("NOMAD_WIFI_ROUTER_SSID") || !wifi_setting_script.Contains("NOMAD_WIFI_ROUTER_KEY"))
                    {
                        AppendRuntimeLog($"can't find {wifi_setting_script_path}.");
                        goto End;
                    }

                    wifi_setting_script = wifi_setting_script.Replace("NOMAD_WIFI_ROUTER_SSID", ssid);
                    wifi_setting_script = wifi_setting_script.Replace("NOMAD_WIFI_ROUTER_KEY", pwd);

                    SerialPortWriteLineOnce(wifi_setting_script);
                    have_send_wifi_script = true;
                }
            }


        End:
            return (!bTimeOut) && tmpValue.Contains(PrinterCommands.unlock_os_cmds_ok);
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

        private string ReadSerialportDataReceived() { return Convert.ToString(Invoke(new Func<string>(() => { return rtbSerialport.Text; }))); }

        private void ckbSFCS_CheckedChanged(object sender, EventArgs e) { MessageBox.Show("ckbSFCS_CheckedChanged"); }

        private void ckbSFCS_Click(object sender, EventArgs e) { MessageBox.Show("ckbSFCS_Click"); }

        #region serialport
        private bool InitializeSerialport(string vid, string pid, SerialPort serialport)
        {
            bool rlt = false;
            try
            {
                if (serialport.IsOpen) { serialport.Close(); }

                int temp_seri_baudreta = 115200;

                Type m_type = dut.GetType();

                if (m_type.Name == "QBC" || m_type.Name == "USB2ETH") { temp_seri_baudreta = 57600; }

                if (m_type.Name == "PA45" ||
                    m_type.Name == "BT_Module" ||
                    m_type.Name == "WiFi_BT_Module" ||
                    m_type.Name == "PA45_MainBoard" ||
                    m_type.Name == "LTSSensor")
                {
                    temp_seri_baudreta = 115200;
                }

                if (!string.IsNullOrWhiteSpace(seri_form.SeriBoudRete))
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

        private bool InitializeSerialport2(string vid, string pid, SerialPort serialport)
        {
            bool rlt = false;
            try
            {
                if (serialport.IsOpen) { serialport.Close(); }

                int temp_seri_baudreta = 115200;


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
                if (serialport.IsOpen) { spTester.Close(); }

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
            if (!spTester.IsOpen) { spTester.Open(); }

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

        private void SerialPortWriteLineOnce(string text, int milliSecond = 1500)
        {
            if (!spTester.IsOpen) spTester.Open();

            spTester.WriteLine(text);

            Invoke(new Action(() =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    rtbLog.AppendText("\n" + text + "\n");
                    rtbLog.Select(rtbLog.TextLength, 0);
                    rtbLog.ScrollToCaret();
                }
            }));

            Delay(milliSecond);
        }

        private void SeriPortWriteLine(SerialPort sPort, string text, int milliSecond = 200)
        {
            if (!sPort.IsOpen) { sPort.Open(); }

            sPort.WriteLine(text);

            Invoke(new Action(() =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    rtbLog.AppendText("\n" + text + "\n");
                    rtbLog.Select(rtbSerialport.TextLength, 0);
                    rtbLog.ScrollToCaret();
                }

                if (!string.IsNullOrEmpty(text))
                {
                    rtbSerialport.AppendText("\n" + text + "\n");
                    rtbSerialport.Select(rtbSerialport.TextLength, 0);
                    rtbSerialport.ScrollToCaret();
                }
            }));

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
            Invoke(new Action(() => { rtbLog.AppendText(recv); }));
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
            int count = spUART.BytesToRead;
            byte[] buffer = new byte[count];
            spUART.Read(buffer, 0, count);
            string recv = Encoding.GetEncoding("ASCII").GetString(buffer);
            Invoke(new Action(() => { rtbSerialport.AppendText(recv); }));
        }

        private void spDutDebugPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            int count = spUART.BytesToRead;
            byte[] buffer = new byte[count];
            spUART.Read(buffer, 0, count);
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
            int count = spUART.BytesToRead;
            byte[] buffer = new byte[count];
            spUART.Read(buffer, 0, count);
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

        private void releaseNoteToolStripMenuItem_Click(object sender, EventArgs e) { }

        private void serialPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialog_result = seri_form.ShowDialog();
            if (dialog_result == DialogResult.Yes) { }
        }

        private void MenuItem_Testitem_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(productConfig.TEST_ITEMS))
            {
                MessageBox.Show($"Please select a production config.", Global.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            //ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", MainBarcode + ";910100114400_BT_board");
            string snS = File.ReadAllText(Path.Combine(Global.ASSEMBLY_PATH, "SN.txt"));

            List<string> sn_List = snS.Split('\n').ToList();

            for (int i = 0; i < sn_List.Count; i++)
            {
                string tmpVal = _ssg_sfc.GetSpecialValue("Scanner", "GET_SN_MODEL", sn_List[i].Trim());
                string tmp = $"{tmpVal}_Lower_case";
                string barcode = _ssg_sfc.GetSpecialValue("Scanner", "GET_BARCODE_PART", sn_List[i].Trim() + $";{tmp}");


                AppendUSBLog1($"{sn_List[i].Trim()}{'\t'}{tmpVal}{'\t'}{barcode}");
            }

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

        private void sFCGetSpecialValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (get_sfc_data_form == null)
            {
                get_sfc_data_form = new frmSFC_Get_Special();
            }

            if (get_sfc_data_form != null)
            {
                get_sfc_data_form.Show();
            }


        }

        /// <summary>
        /// 遍历 AutomationElement 树
        /// </summary>
        /// <param name="element">起始元素</param>
        /// <param name="maxDepth">最大递归深度，防止卡死</param>
        /// <param name="currentDepth">当前深度</param>
        /// <param name="filterControlType">只打印某种 ControlType，传 null 打印全部</param>
        static void TraverseTree(AutomationElement element, int maxDepth, int currentDepth = 0, ControlType filterControlType = null)
        {
            if (element == null || currentDepth > maxDepth)
                return;

            try
            {
                // 获取元素属性（放在 try 里，防止元素中途失效）
                string name = element.Current.Name ?? "(empty)";
                string controlType = element.Current.ControlType?.ProgrammaticName ?? "(unknown)";
                string automationId = element.Current.AutomationId ?? "(empty)";
                string className = element.Current.ClassName ?? "(empty)";
                string frameworkId = element.Current.FrameworkId ?? "(empty)";
                bool isEnabled = element.Current.IsEnabled;
                bool isOffscreen = element.Current.IsOffscreen;

                // 如果指定了过滤条件，且当前元素不匹配，则跳过打印（但仍继续遍历子元素）
                if (filterControlType == null || element.Current.ControlType == filterControlType)
                {
                    string indent = new string(' ', currentDepth * 4);
                    Console.WriteLine($"{indent}[{controlType}]");
                    Console.WriteLine($"{indent}  Name         = {name}");
                    Console.WriteLine($"{indent}  AutomationId = {automationId}");
                    Console.WriteLine($"{indent}  ClassName    = {className}");
                    Console.WriteLine($"{indent}  FrameworkId  = {frameworkId}");
                    Console.WriteLine($"{indent}  IsEnabled    = {isEnabled}, IsOffscreen = {isOffscreen}");
                    Console.WriteLine();
                }
            }
            catch (ElementNotAvailableException)
            {
                // 元素在遍历过程中被销毁或不可用
                string indent = new string(' ', currentDepth * 4);
                Console.WriteLine($"{indent}[ElementNotAvailable]");
                return;
            }

            // 递归遍历子元素
            AutomationElement child = TreeWalker.RawViewWalker.GetFirstChild(element);
            while (child != null)
            {
                TraverseTree(child, maxDepth, currentDepth + 1, filterControlType);
                child = TreeWalker.RawViewWalker.GetNextSibling(child);
            }
        }

        private void btn2306_Click(object sender, EventArgs e)
        {
            string ipAddress = "192.168.0.3"; // 替换为仪器实际IP
            int port = 5025;                    // Keithley LAN 默认 Raw Socket 端口

            if (Keithy2306LAN == null) { Keithy2306LAN = new KEITHLEY_2306_LAN(ipAddress, port); }

            Keithy2306LAN.Connect();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
