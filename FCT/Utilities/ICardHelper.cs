using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FCT.Utilities
{
    public static class ICardHelper
    {
        //常量定义
        //淘宝：https://item.taobao.com/item.htm?spm=a1z10.5-c.w4002-17663462238.11.5a94789eFyjn2z&id=615391857885

        public const byte BLOCK0_EN = 0x01;//操作第0块
        public const byte BLOCK1_EN = 0x02;//操作第1块
        public const byte BLOCK2_EN = 0x04;//操作第2块
        public const byte NEEDSERIAL = 0x08;//仅对指定序列号的卡操作
        public const byte EXTERNKEY = 0x10;
        public const byte NEEDHALT = 0x20;//读卡或写卡后顺便休眠该卡，休眠后，卡必须拿离开感应区，再放回感应区，才能进行第二次操作。

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //外部函数声明：让设备发出声响
        [DllImport("OUR_MIFARE.dll", EntryPoint = "pcdbeep", CallingConvention = CallingConvention.StdCall)]
        public static extern byte pcdbeep(UInt32 xms);//xms单位为毫秒 


        //------------------------------------------------------------------------------------------------------------------------------------------------------    
        //只读卡号
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccrequest", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccrequest(byte[] serial);//devicenumber用于返回编号 

        //------------------------------------------------------------------------------------------------------------------------------------------------------    
        //读取设备编号，可做为软件加密狗用,也可以根据此编号在公司网站上查询保修期限
        [DllImport("OUR_MIFARE.dll", EntryPoint = "pcdgetdevicenumber", CallingConvention = CallingConvention.StdCall)]
        public static extern byte pcdgetdevicenumber(byte[] devicenumber);//devicenumber用于返回编号 


        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //轻松读卡
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccreadex", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccreadex(byte ctrlword, byte[] serial, byte area, byte keyA1B0, byte[] picckey, byte[] piccdata0_2);
        //参数：说明
        //ctrlword：控制字
        //serial：卡序列号数组，用于指定或返回卡序列号
        //area：指定读卡区号
        //keyA1B0：指定用A或B密码认证,一般是用A密码，只有特殊用途下才用B密码，在这不做详细解释。
        //picckey：指定卡密码，6个字节，卡出厂时的初始密码为6个0xff
        //piccdata0_2：用于返回卡该区第0块到第2块的数据，共48个字节.


        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //轻松写卡
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccwriteex", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccwriteex(byte ctrlword, byte[] serial, byte area, byte keyA1B0, byte[] picckey, byte[] piccdata0_2);
        //参数：说明
        //ctrlword：控制字
        //serial：卡序列号数组，用于指定或返回卡序列号
        //area：指定读卡区号
        //keyA1B0：指定用A或B密码认证,一般是用A密码，只有特殊用途下才用B密码，在这不做详细解释。
        //picckey：指定卡密码，6个字节，卡出厂时的初始密码为6个0xff
        //piccdata0_2：用于返回卡该区第0块到第2块的数据，共48个字节.


        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //密码认证方式1，用外部密码认证，必须指定外部密码。本函数必须在piccrequest或piccrequestex函数执行之后运行，并且要紧接着调用，中途不能调用其他函数。
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccauthkey1", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccauthkey1(byte[] serial, byte area, byte keyA1B0, byte[] picckey);
        //参数：说明
        //serial：卡序列号数组，用于指定或返回卡序列号
        //area：指定读卡区号
        //keyA1B0：指定用A或B密码认证,一般是用A密码，只有特殊用途下才用B密码，在这不做详细解释。
        //picckey：指定卡密码，6个字节，卡出厂时的初始密码为6个0xff


        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //读出一块的数据，也就是16个字节。必须在执行piccrequest或 Piccrequestex函数，接着执行piccauthkey1或 piccauthkey2函数，然后执行piccread才能成功读出一块的数据。
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccread", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccread(byte block, byte[] blockdata);
        //参数：说明
        //block：指定读卡块号
        //blockdata：用于返回卡该块数据，共16个字节.

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //写一块的数据，也就是16个字节。必须在执行piccrequest或 Piccrequestex函数，接着执行piccauthkey1或 piccauthkey2函数，然后执行piccread才能成功读出一块的数据。
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccwrite", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccwrite(byte block, byte[] blockdata);
        //参数：说明
        //block：指定写卡块号
        //blockdata：写块数据缓冲，共16个字节.


        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //修改卡单区的密码
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccchangesinglekey", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccchangesinglekey(byte ctrlword, byte[] serial, byte area, byte keyA1B0, byte[] piccoldkey, byte[] piccnewkey);
        //参数：说明
        //ctrlword：控制字
        //serial：卡序列号数组，用于指定或返回卡序列号
        //area：指定读卡区号
        //keyA1B0：指定用A或B密码认证,一般是用A密码，只有特殊用途下才用B密码，在这不做详细解释。
        //piccoldkey：//旧密码
        //piccnewkey：//新密码.

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //修改卡A/B密码及控制字
        [DllImport("OUR_MIFARE.dll", EntryPoint = "piccchangesinglekeyex", CallingConvention = CallingConvention.StdCall)]
        public static extern byte piccchangesinglekeyex(byte ctrlword, byte[] serial, byte area, byte keyA1B0, byte[] piccoldkey, byte[] piccnewkey);
        //参数：说明
        //ctrlword：控制字
        //serial：卡序列号数组，用于指定或返回卡序列号
        //area：指定读卡区号
        //keyA1B0：指定用A或B密码认证,一般是用A密码，只有特殊用途下才用B密码，在这不做详细解释。
        //piccoldkey：//旧密码
        //piccnewkey：//新密码.

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //发送显示内容到读卡器
        [DllImport("OUR_MIFARE.dll", EntryPoint = "lcddispfull", CallingConvention = CallingConvention.StdCall)]
        public static extern byte lcddispfull(string lcdstr);
        //参数：说明
        //lcdstr：显示内容


        //------------------------------------------------------------------------------------------------------------------------------------------------------    
        //写设备存储区1
        [DllImport("OUR_MIFARE.dll", EntryPoint = "pcdsetcustomizedata1", CallingConvention = CallingConvention.StdCall)]
        public static extern byte pcdsetcustomizedata1(byte[] readerdata);//devicenumber用于传递写数据 

        //------------------------------------------------------------------------------------------------------------------------------------------------------    
        //读设备存储区1
        [DllImport("OUR_MIFARE.dll", EntryPoint = "pcdgetcustomizedata1", CallingConvention = CallingConvention.StdCall)]
        public static extern byte pcdgetcustomizedata1(byte[] readerdata);//devicenumber用于传递写数据 

        //------------------------------------------------------------------------------------------------------------------------------------------------------    
        //写设备存储区2
        [DllImport("OUR_MIFARE.dll", EntryPoint = "pcdsetcustomizedata2", CallingConvention = CallingConvention.StdCall)]
        public static extern byte pcdsetcustomizedata2(byte[] readerdata);//devicenumber用于传递写数据 

        //------------------------------------------------------------------------------------------------------------------------------------------------------    
        //读设备存储区2
        [DllImport("OUR_MIFARE.dll", EntryPoint = "pcdgetcustomizedata2", CallingConvention = CallingConvention.StdCall)]
        public static extern byte pcdgetcustomizedata2(byte[] readerdata, byte[] devno);//devicenumber用于传递写数据 


        public static string ReadNFCTag(int sectorIndex)
        {
            string value = string.Empty;

            byte status;//存放返回值
            byte myareano;//区号
            byte authmode;//密码类型，用A密码或B密码
            byte myctrlword;//控制字
            byte[] mypicckey = new byte[6];//密码
            byte[] mypiccserial = new byte[4];//卡序列号
            byte[] mypiccdata = new byte[48]; //卡数据缓冲
            //控制字指定,控制字的含义请查看本公司网站提供的动态库说明
            myctrlword = BLOCK0_EN + BLOCK1_EN + BLOCK2_EN + EXTERNKEY;

            //指定区号
            myareano = Convert.ToByte(sectorIndex);//指定为第8区
            //批定密码模式
            authmode = 1;//大于0表示用A密码认证，推荐用A密码认证

            //指定密码
            mypicckey[0] = 0xff;
            mypicckey[1] = 0xff;
            mypicckey[2] = 0xff;
            mypicckey[3] = 0xff;
            mypicckey[4] = 0xff;
            mypicckey[5] = 0xff;

            //status = piccreadex(myctrlword, mypiccserial, myareano, authmode, mypicckey, mypiccdata);
            status = piccreadex(myctrlword, mypiccserial, myareano, authmode, Key(2), mypiccdata);
            //在下面设定断点，然后查看mypiccserial、mypiccdata，
            //调用完 piccreadex函数可读出卡序列号到 mypiccserial，读出卡数据到mypiccdata，
            //开发人员根据自己的需要处理mypiccserial、mypiccdata 中的数据了。
            //处理返回函数
            switch (status)
            {
                case 0:
                    pcdbeep(20);
                    string readinf = "";
                    for (int i = 0; i < 48; i++)
                    {
                        readinf = readinf + mypiccdata[i].ToString("X2");
                    }
                    //textBox7.Text = readinf;
                    value = readinf;
                    MessageBox.Show("操作成功,数据已显示在右框栏中！", "示例程序");
                    break;
                case 8:
                    MessageBox.Show("未寻到卡，请将卡放在感应区！", "示例提示");
                    break;
                case 9:
                    MessageBox.Show("读序列码错误！", "示例提示");
                    break;
                case 10:
                    MessageBox.Show("选卡错误！", "示例提示");
                    break;
                case 11:
                    MessageBox.Show("装载密码错误！", "示例提示");
                    break;
                case 12:
                    MessageBox.Show("卡密码认证错误！", "示例提示");
                    break;
                case 13:
                    MessageBox.Show("读卡错误！", "示例提示");
                    break;
                case 14:
                    MessageBox.Show("写卡错误！", "示例提示");
                    break;
                case 21:
                    MessageBox.Show("没有动态库！", "示例提示");
                    break;
                case 22:
                    MessageBox.Show("动态库或驱动程序异常！", "示例提示");
                    break;
                case 23:
                    MessageBox.Show("驱动程序错误或尚未安装！", "示例提示");
                    break;
                case 24:
                    MessageBox.Show("操作超时，一般是动态库没有反映！", "示例提示");
                    break;
                case 25:
                    MessageBox.Show("发送字数不够！", "示例提示");
                    break;
                case 26:
                    MessageBox.Show("发送的CRC错！", "示例提示");
                    break;
                case 27:
                    MessageBox.Show("接收的字数不够！", "示例提示");
                    break;
                case 28:
                    MessageBox.Show("接收的CRC错！", "示例提示");
                    break;
                default:
                    MessageBox.Show("返回码(对应的说明请看例子中的注释):" + status, "示例提示");
                    break;
            }

            //返回解释
            /*
            REQUEST 8//寻卡错误
            READSERIAL 9//读序列吗错误
            SELECTCARD 10//选卡错误
            LOADKEY 11//装载密码错误
            AUTHKEY 12//密码认证错误
            READ 13//读卡错误
            WRITE 14//写卡错误

            NONEDLL 21//没有动态库
            DRIVERORDLL 22//动态库或驱动程序异常
            DRIVERNULL 23//驱动程序错误或尚未安装
            TIMEOUT 24//操作超时，一般是动态库没有反映
            TXSIZE 25//发送字数不够
            TXCRC 26//发送的CRC错
            RXSIZE 27//接收的字数不够
            RXCRC 28//接收的CRC错
            */

            return value;
        }


        public static readonly byte[] Key_Default = new byte[6] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        public static readonly byte[] Key_A = new byte[6] { 0xa0, 0xa1, 0xa2, 0xa3, 0xa4, 0xa5 };
        public static readonly byte[] Key_B = new byte[6] { 0xd3, 0xf7, 0xd3, 0xf7, 0xd3, 0xf7 };

        public static byte[] Key(int i)
        {
            byte[] secret = new byte[6];//密码
            switch (i)
            {
                case 0:
                    secret[0] = 0xff;
                    secret[1] = 0xff;
                    secret[2] = 0xff;
                    secret[3] = 0xff;
                    secret[4] = 0xff;
                    secret[5] = 0xff;
                    break;
                case 1:
                    secret[0] = 0xa0;
                    secret[1] = 0xa1;
                    secret[2] = 0xa2;
                    secret[3] = 0xa3;
                    secret[4] = 0xa4;
                    secret[5] = 0xa5;
                    break;
                case 2:
                    secret[0] = 0xd3;
                    secret[1] = 0xf7;
                    secret[2] = 0xd3;
                    secret[3] = 0xf7;
                    secret[4] = 0xd3;
                    secret[5] = 0xf7;
                    break;

            }
            return secret;
        }

        public static byte[] NDEF_FORMAT_DATA(int i)
        {
            byte[] data = new byte[16];
            switch (i)
            {
                case 0:
                    byte[] array1 = { 0x14, 0x01, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1 };
                    data = array1;
                    break;
                case 1:
                    byte[] array2 = { 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1, 0x03, 0xE1 };
                    data = array2;
                    break;
                case 2:
                    byte[] array3 = { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x78, 0x77, 0x88, 0xC1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                    data = array3;
                    break;
                case 3:
                    byte[] array4 = { 0x03, 0x04, 0xD8, 0x00, 0x00, 0x00, 0xFE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    data = array4;
                    break;
                case 4:
                    byte[] array5 = { 0xD3, 0xF7, 0xD3, 0xF7, 0xD3, 0xF7, 0x7F, 0x07, 0x88, 0x40, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                    data = array5;
                    break;
            }

            return data;
        }

        public static bool WriteCardSectorBlock(byte[] data, int block, byte sector, byte[] mypicckey, ref string statusCode)
        {
            bool result = false;

            byte status;//存放返回值
            byte myareano;//区号
            byte myblock; //块号
            byte authmode;//密码类型，用A密码或B密码
            byte myctrlword;//控制字

            byte[] mypiccserial = new byte[4];//卡序列号
            byte[] mypiccdata = new byte[48]; //卡数据缓冲
            byte[] myblockdata = new byte[16];  //块数据缓冲
            //控制字指定,控制字的含义请查看本公司网站提供的动态库说明
            myctrlword = BLOCK0_EN + BLOCK1_EN + BLOCK2_EN + EXTERNKEY;

            //指定区号
            myareano = sector;//指定为第8区
            myblock = (byte)(myareano * 4 + block); ;//(byte)(myareano * 4 + 0);   //块号为区号*4+0、1、2、3，其中第3块为密码控制块
            //批定密码模式
            authmode = 1;//大于0表示用A密码认证，推荐用A密码认证


            //for (byte i = 0; i < 16; i++)
            //{
            //    myblockdata[i] = Convert.ToByte(Convert.ToInt32(data.Substring(i * 2, 2), 16));
            //}
            myblockdata = data;


            //status = piccchangesinglekeyex(myctrlword, mypiccserial, myareano, authmode, mypicckey, mypiccdata);
            status = piccrequest(mypiccserial);
            if (status == 0)
            {
                status = piccauthkey1(mypiccserial, myareano, authmode, mypicckey);
                if (status == 0)
                {
                    status = piccwrite(myblock, myblockdata);
                }
            }
            string readinf = "";

            switch (status)
            {
                case 0:
                    pcdbeep(20);
                    //for (int i = 0; i < 48; i++)
                    //{
                    //    readinf = readinf + mypiccdata[i].ToString("X2");
                    //}
                    statusCode = "OK";
                    break;
                case 8:
                    statusCode = "未寻到卡，请将卡放在感应区！";
                    break;
                case 9:
                    statusCode = "读序列码错误！";
                    break;
                case 10:
                    statusCode = "选卡错误！";
                    break;
                case 11:
                    statusCode = "装载密码错误！";
                    break;
                case 12:
                    statusCode = "卡密码认证错误！";
                    break;
                case 13:
                    statusCode = "读卡错误！";
                    break;
                case 14:
                    statusCode = "写卡错误！";
                    break;
                case 21:
                    statusCode = "没有动态库！";
                    break;
                case 22:
                    statusCode = "动态库或驱动程序异常！";
                    break;
                case 23:
                    statusCode = "驱动程序错误或尚未安装！";
                    break;
                case 24:
                    statusCode = "操作超时，一般是动态库没有反映！";
                    break;
                case 25:
                    statusCode = "发送字数不够！";
                    break;
                case 26:
                    statusCode = "发送的CRC错！";
                    break;
                case 27:
                    statusCode = "接收的字数不够！";
                    break;
                case 28:
                    statusCode = "接收的CRC错！";
                    break;
                default:
                    MessageBox.Show("返回码(对应的说明请看例子中的注释):" + status, "示例提示");
                    break;
            }

            if (status == 0)
            {
                result = true;
            }
            return result;
        }

        public static string WriteCardSector(string data, byte sector, byte[] mypicckey, ref string statusCode)
        {
            bool result = false;
            string code = "";

            byte status;//存放返回值
            byte myareano;//区号
            byte authmode;//密码类型，用A密码或B密码
            byte myctrlword;//控制字

            byte[] mypiccserial = new byte[4];//卡序列号
            byte[] mypiccdata = new byte[48]; //卡数据缓冲
            //控制字指定,控制字的含义请查看本公司网站提供的动态库说明
            myctrlword = BLOCK0_EN + BLOCK1_EN + BLOCK2_EN + EXTERNKEY;

            //指定区号
            myareano = sector;//指定为第8区
            //批定密码模式
            authmode = 1;//大于0表示用A密码认证，推荐用A密码认证

            ////指定密码
            //mypicckey[0] = 0xff;
            //mypicckey[1] = 0xff;
            //mypicckey[2] = 0xff;
            //mypicckey[3] = 0xff;
            //mypicckey[4] = 0xff;
            //mypicckey[5] = 0xff;

            for (byte i = 0; i < 48; i++)
            {
                mypiccdata[i] = Convert.ToByte(Convert.ToInt32(data.Substring(i * 2, 2), 16));
            }


            status = piccwriteex(myctrlword, mypiccserial, myareano, authmode, mypicckey, mypiccdata);
            //在下面设定断点，然后查看mypiccserial、mypiccdata，
            //调用完 piccreadex函数可读出卡序列号到 mypiccserial，读出卡数据到mypiccdata，
            //开发人员根据自己的需要处理mypiccserial、mypiccdata 中的数据了。
            //处理返回函数
            string readinf = "";

            switch (status)
            {
                case 0:
                    pcdbeep(20);
                    for (int i = 0; i < 48; i++)
                    {
                        readinf = readinf + mypiccdata[i].ToString("X2");
                    }
                    statusCode = "OK";
                    break;
                case 8:
                    statusCode = "未寻到卡，请将卡放在感应区！";
                    break;
                case 9:
                    statusCode = "读序列码错误！";
                    break;
                case 10:
                    statusCode = "选卡错误！";
                    break;
                case 11:
                    statusCode = "装载密码错误！";
                    break;
                case 12:
                    statusCode = "卡密码认证错误！";
                    break;
                case 13:
                    statusCode = "读卡错误！";
                    break;
                case 14:
                    statusCode = "写卡错误！";
                    break;
                case 21:
                    statusCode = "没有动态库！";
                    break;
                case 22:
                    statusCode = "动态库或驱动程序异常！";
                    break;
                case 23:
                    statusCode = "驱动程序错误或尚未安装！";
                    break;
                case 24:
                    statusCode = "操作超时，一般是动态库没有反映！";
                    break;
                case 25:
                    statusCode = "发送字数不够！";
                    break;
                case 26:
                    statusCode = "发送的CRC错！";
                    break;
                case 27:
                    statusCode = "接收的字数不够！";
                    break;
                case 28:
                    statusCode = "接收的CRC错！";
                    break;
                default:
                    MessageBox.Show("返回码(对应的说明请看例子中的注释):" + status, "示例提示");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(readinf))
            {
                code = readinf;
            }
            else
            {
                code = status.ToString();
            }
            /*
            if (status == 0)
            {
                result = true;
            }
            */
            return code;

        }

        public static string ReadCard(byte sector, byte[] mypicckey, ref string statusCode)
        {
            string code = "";


            byte status;//存放返回值
            byte myareano;//区号
            byte authmode;//密码类型，用A密码或B密码
            byte myctrlword;//控制字

            byte[] mypiccserial = new byte[4];//卡序列号
            byte[] mypiccdata = new byte[48]; //卡数据缓冲
            //控制字指定,控制字的含义请查看本公司网站提供的动态库说明
            myctrlword = BLOCK0_EN + BLOCK1_EN + BLOCK2_EN + EXTERNKEY;

            //指定区号
            myareano = sector;//指定为第8区
            //批定密码模式
            authmode = 1;//大于0表示用A密码认证，推荐用A密码认证

            ////指定密码
            //mypicckey[0] = 0xff;
            //mypicckey[1] = 0xff;
            //mypicckey[2] = 0xff;
            //mypicckey[3] = 0xff;
            //mypicckey[4] = 0xff;
            //mypicckey[5] = 0xff;

            status = piccreadex(myctrlword, mypiccserial, myareano, authmode, mypicckey, mypiccdata);
            //在下面设定断点，然后查看mypiccserial、mypiccdata，
            //调用完 piccreadex函数可读出卡序列号到 mypiccserial，读出卡数据到mypiccdata，
            //开发人员根据自己的需要处理mypiccserial、mypiccdata 中的数据了。
            //处理返回函数
            string readinf = "";

            switch (status)
            {
                case 0:
                    pcdbeep(20);
                    for (int i = 0; i < 48; i++)
                    {
                        readinf = readinf + mypiccdata[i].ToString("X2");
                    }
                    statusCode = "OK";
                    break;
                case 8:
                    statusCode = "未寻到卡，请将卡放在感应区！";
                    break;
                case 9:
                    statusCode = "读序列码错误！";
                    break;
                case 10:
                    statusCode = "选卡错误！";
                    break;
                case 11:
                    statusCode = "装载密码错误！";
                    break;
                case 12:
                    statusCode = "卡密码认证错误！";
                    break;
                case 13:
                    statusCode = "读卡错误！";
                    break;
                case 14:
                    statusCode = "写卡错误！";
                    break;
                case 21:
                    statusCode = "没有动态库！";
                    break;
                case 22:
                    statusCode = "动态库或驱动程序异常！";
                    break;
                case 23:
                    statusCode = "驱动程序错误或尚未安装！";
                    break;
                case 24:
                    statusCode = "操作超时，一般是动态库没有反映！";
                    break;
                case 25:
                    statusCode = "发送字数不够！";
                    break;
                case 26:
                    statusCode = "发送的CRC错！";
                    break;
                case 27:
                    statusCode = "接收的字数不够！";
                    break;
                case 28:
                    statusCode = "接收的CRC错！";
                    break;
                default:
                    MessageBox.Show("返回码(对应的说明请看例子中的注释):" + status, "示例提示");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(readinf))
            {
                code = readinf;
            }
            else
            {
                code = status.ToString();
            }


            //返回解释
            /*
            REQUEST 8//寻卡错误
            READSERIAL 9//读序列吗错误
            SELECTCARD 10//选卡错误
            LOADKEY 11//装载密码错误
            AUTHKEY 12//密码认证错误
            READ 13//读卡错误
            WRITE 14//写卡错误

            NONEDLL 21//没有动态库
            DRIVERORDLL 22//动态库或驱动程序异常
            DRIVERNULL 23//驱动程序错误或尚未安装
            TIMEOUT 24//操作超时，一般是动态库没有反映
            TXSIZE 25//发送字数不够
            TXCRC 26//发送的CRC错
            RXSIZE 27//接收的字数不够
            RXCRC 28//接收的CRC错
            */
            return code;
        }
        
        public static string NFC_Data(int sector, string mac)
        {
            string data = "";
            string macWrite = "";
            if (!string.IsNullOrWhiteSpace(mac))
            {
                string[] macSplit = mac.Split(':');
                Array.Reverse(macSplit);
                foreach (string temp in macSplit)
                {
                    macWrite += temp;
                }
            }

            switch (sector)
            {
                case 0:
                    data = "14 01 03 E1 03 E1 03 E1 03 E1 03 E1 03 E1 03 E1";
                    break;
                case 1:
                    data =
                        "03 3E DA 20 19 01 61 70 70 6C 69 63 61 74 69 6F" +
                        "6E 2F 76 6E 64 2E 62 6C 75 65 74 6F 6F 74 68 2E" +
                        "65 70 2E 6F 6F 62 30 19 00 " + macWrite + " 05";
                    break;
                case 2:
                    data =
                        "09 50 41 34 35 04 0D 80 06 04 05 03 1E 11 0B 11" +
                        "FE 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00" +
                        "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00";
                    break;
                case 3:
                    data = "03 E1 03 E1 03 E1 03 E1 03 E1 03 E1 03 E1 03 E1";
                    break;

            }

            return data.Replace(" ", "");
        }
    }
}
