using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Utilities
{
    public static class WinDeviceHelper
    {
        /*
         * ✔ AdapterTypeId = 0 → 以太网（Ethernet）技术类型
         */

        /// <summary>
        /// 精准查找：物理 Ethernet 以太网网卡（有线网卡）
        /// AdapterTypeId = 0 表示 标准以太网
        /// </summary>
        /// <summary>
        /// 获取电脑所有 物理Ethernet有线网卡
        /// </summary>
        private static List<EthernetInfo> GetPhysicalEthernetAdapters()
        {
            List<EthernetInfo> list = new List<EthernetInfo>();

            try
            {
                // 直接查询 Ethernet 网卡（最精准）
                string query = "SELECT * FROM Win32_NetworkAdapter WHERE AdapterTypeId = 0 AND PhysicalAdapter = True";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject mo in searcher.Get())
                {
                    try
                    {
                        EthernetInfo info = new EthernetInfo
                        {
                            Name = mo["Name"]?.ToString() ?? "",
                            DeviceID = mo["DeviceID"]?.ToString() ?? "",
                            MAC = mo["MACAddress"]?.ToString() ?? "",
                            AdapterType = mo["AdapterType"]?.ToString() ?? "",
                            NetConnectionID = mo["NetConnectionID"]?.ToString() ?? "", // 本地连接名称
                            IsEnabled = mo["NetEnabled"] != null && (bool)mo["NetEnabled"]
                        };

                        list.Add(info);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("查找以太网网卡失败: " + ex.Message);
            }

            return list;
        }

        public static bool IsPhysicalEthernetAdapters(string deviceName)
        {
            bool rlt = false;
            try
            {
                // 直接查询 Ethernet 网卡（最精准）
                string query = "SELECT * FROM Win32_NetworkAdapter WHERE AdapterTypeId = 0 AND PhysicalAdapter = True";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                string currName = "";
                foreach (ManagementObject mo in searcher.Get())
                {
                    currName = mo["Name"]?.ToString() ?? "";
                    
                    if(currName.Contains(deviceName))
                    {
                        rlt = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                rlt = false;
                throw ex;
            }

            return rlt;
        }

        public static string GetPhysicalEthernetAdaptersMac(string deviceName)
        {
            string macAddr = "";
            string currName = deviceName;
            try
            {
                // 直接查询 Ethernet 网卡（最精准）
                string query = "SELECT * FROM Win32_NetworkAdapter WHERE AdapterTypeId = 0 AND PhysicalAdapter = True";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject mo in searcher.Get())
                {
                    currName = mo["Name"]?.ToString() ?? "";

                    if (currName.Contains(deviceName))
                    {
                        macAddr = mo["MACAddress"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(macAddr))
                        {
                            return macAddr;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return macAddr;
        }

        public static string GetPhysicalEthernetAdaptersIpAddr(string deviceName)
        {
            string macAddr = "";
            string currName = deviceName;
            try
            {
                // 直接查询 Ethernet 网卡（最精准）
                string query = "SELECT * FROM Win32_NetworkAdapter WHERE AdapterTypeId = 0 AND PhysicalAdapter = True";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject mo in searcher.Get())
                {
                    currName = mo["Name"]?.ToString() ?? "";

                    if (currName.Contains(deviceName))
                    {
                        macAddr = mo["MACAddress"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(macAddr))
                        {
                            return macAddr;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return macAddr;
        }

        public static string GetSerialportComPort(string vid, string pid)
        {
            string value = "";

            using (var searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
            {
                ManagementObjectCollection devices = searcher.Get();

                if (devices.Count == 0) goto Exit;

                foreach (ManagementObject device in searcher.Get())
                {
                    string name = device["Name"]?.ToString() ?? "";
                    string pnpId = device["PNPDeviceID"]?.ToString() ?? "";

                    if (pnpId.Contains(vid) /*&& pnpId.Contains(pid)*/)
                    {
                        //USB 序列裝置 (COM16) 
                        int index_start = name.IndexOf("(");
                        int index_end = name.IndexOf(")");

                        value = name.Substring(index_start + 1, index_end - index_start - 1);
                        break;
                    }
                }
            }
        Exit:
            return value;
        }

        /// <summary>
        /// 列出所有已连接或可用的网卡名称和硬件描述
        /// </summary>
        public static void ListNetworkAdapters()
        {
            // 查询 Win32_NetworkAdapter，过滤掉没有 NetConnectionID (即未在控制面板显示) 的虚拟网卡
            string query = "SELECT NetConnectionID, Description FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", query))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine($"控制面板名称: {queryObj["NetConnectionID"]} | 硬件描述: {queryObj["Description"]}");
                }
            }
        }

        /// <summary>
        /// 列出所有已连接或可用的网卡名称和硬件描述
        /// </summary>
        public static string GetNetworkAdapters(string Description)
        {
            // 查询 Win32_NetworkAdapter，过滤掉没有 NetConnectionID (即未在控制面板显示) 的虚拟网卡
            string query = "SELECT NetConnectionID, Description FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", query))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine($"控制面板名称: {queryObj["NetConnectionID"]} | 硬件描述: {queryObj["Description"]}");
                    if (queryObj["Description"].ToString().Contains(Description))
                    {
                        return queryObj["NetConnectionID"].ToString();
                    }

                }
            }
            return "";
        }


        /// <summary>
        /// 设置指定网卡的IP、子网掩码、网关和DNS
        /// </summary>
        public static void SetNetworkAdapterIP(string NetConnectionID, string ipAddress, string subnetMask, string[] gateways, string[] dnsServers)
        {
            int adapterIndex = -1;

            // 步骤 1: 通过“控制面板名称”查找网卡对应的 Index (索引)
            string queryAdapter = $"SELECT Index FROM Win32_NetworkAdapter WHERE NetConnectionID = '{NetConnectionID}'";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", queryAdapter))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    adapterIndex = Convert.ToInt32(queryObj["Index"]);
                    break;
                }
            }

            if (adapterIndex == -1)
            {
                throw new Exception($"未找到名为 '{NetConnectionID}' 的网卡。请运行 ListNetworkAdapters() 确认名称是否完全一致。");
            }

            // 步骤 2: 通过 Index 获取该网卡的配置对象 (Win32_NetworkAdapterConfiguration)
            string queryConfig = $"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE Index = {adapterIndex}";
            using (ManagementObjectSearcher configSearcher = new ManagementObjectSearcher("root\\CIMV2", queryConfig))
            {
                foreach (ManagementObject config in configSearcher.Get())
                {
                    // 1. 设置 IP 和 子网掩码 (EnableStatic 方法会自动关闭 DHCP)
                    ManagementBaseObject inParams = config.GetMethodParameters("EnableStatic");
                    inParams["IPAddress"] = new string[] { ipAddress };
                    inParams["SubnetMask"] = new string[] { subnetMask };

                    ManagementBaseObject outParams = config.InvokeMethod("EnableStatic", inParams, null);
                    CheckReturnValue(outParams, "设置IP和子网掩码");

                    /*
                    // 2. 设置 默认网关 (可选)
                    if (gateways != null && gateways.Length > 0)
                    {
                        inParams = config.GetMethodParameters("SetGateways");
                        inParams["DefaultIPGateway"] = gateways;
                        inParams["GatewayCostMetric"] = new ushort[] { 1 }; // 跃点数

                        outParams = config.InvokeMethod("SetGateways", inParams, null);
                        CheckReturnValue(outParams, "设置网关");
                    }

                    // 3. 设置 DNS 服务器 (可选)
                    if (dnsServers != null && dnsServers.Length > 0)
                    {
                        inParams = config.GetMethodParameters("SetDNSServerSearchOrder");
                        inParams["DNSServerSearchOrder"] = dnsServers;

                        outParams = config.InvokeMethod("SetDNSServerSearchOrder", inParams, null);
                        CheckReturnValue(outParams, "设置DNS");
                    }
                    */

                    break; // 找到对应的配置并设置后退出循环
                }
            }
        }


        /// <summary>
        /// 检查 WMI 方法调用的返回值
        /// </summary>
        private static void CheckReturnValue(ManagementBaseObject outParams, string operationName)
        {
            uint returnVal = Convert.ToUInt32(outParams["ReturnValue"]);
            if (returnVal == 0)
            {
                // 成功
            }
            else if (returnVal == 1)
            {
                // 成功，但需要重启计算机或重启网卡才能生效
                Console.WriteLine($"[提示] {operationName} 成功，但可能需要重启网络适配器才能生效。");
            }
            else
            {
                // 失败 (常见的错误码: 70=拒绝访问/非管理员, 93=其他错误等)
                throw new Exception($"{operationName} 失败，WMI 返回错误代码: {returnVal}。请确保以【管理员身份】运行程序！");
            }
        }

    }

    /// <summary>
    /// 以太网网卡信息
    /// </summary>
    public class EthernetInfo
    {
        public string Name { get; set; }
        public string DeviceID { get; set; }
        public string MAC { get; set; }
        public string AdapterType { get; set; }
        public string NetConnectionID { get; set; }
        public bool IsEnabled { get; set; }
    }
}
