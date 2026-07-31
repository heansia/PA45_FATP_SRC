using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.Instrument
{
    class draft
    {
        private int getCaDIvalue(byte[] byte_, int i)
        {

            string str = BitConverter.ToString(byte_);
            str = str.Replace("-", "");
            str = str.Remove(0, 8);
            if (i < 8)
            {
                str = str.Substring(i * 2 + 1, 1);
            }
            else
            {
                str = str.Remove(16, 2);
                str = str.Substring(i * 2 + 1, 1);
            }

            int mm = Convert.ToInt32(str);

            return mm;
        }

        private string getDIvalue(byte[] byte_, int i)
        {
            string str = BitConverter.ToString(byte_);
            str = str.Replace("-", "");
            str = str.Remove(0, 2);
            str = str.Substring(i * 2 + 1, 1);
            return str;
        }

        private string GetCaAdcValueBytes2DoubleStr(byte[] buffer)
        {
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();

            string strCharacter = asciiEncoding.GetString(buffer);

            strCharacter = strCharacter.Replace("\0", "");
            strCharacter = strCharacter.Remove(0, 4);
            strCharacter = strCharacter.Substring(0, 5);

            if (strCharacter.Substring(0, 1) == "0")
                strCharacter = strCharacter.Remove(0, 1);

            //int value = Convert.ToInt32(str, 16);

            int value = Convert.ToInt32(strCharacter);
            double b = Convert.ToDouble(value);
            b = b / 1000;

            return b.ToString("F3");
        }

        private string getRelayADCvalue(byte[] byte_, int i)
        {
            string test = "";
            int x = 0;
            foreach (byte ii in byte_)
            {
                x++;
                if (i == x)
                {
                    test = ii.ToString();
                    break;
                }
            }

            double b = Convert.ToDouble(test);

            b = b / 100 * 2;

            test = b.ToString("F2");

            return test;
        }

        private string getCaADCvalue(byte[] byte_)
        {
            try
            {
                string test = "";
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                string strCharacter = asciiEncoding.GetString(byte_);

                strCharacter = strCharacter.Replace("\0", "");

                strCharacter = strCharacter.Remove(0, 4);

                strCharacter = strCharacter.Substring(0, 5);

                if (strCharacter.Substring(0, 1) == "0")
                {

                    strCharacter = strCharacter.Remove(0, 1);
                }

                //int value = Convert.ToInt32(str, 16);

                int value = Convert.ToInt32(strCharacter);



                double b = Convert.ToDouble(value);

                b = b / 1000;

                test = b.ToString("F3");

                return test;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        private string getCaCurrvalue(byte[] byte_)
        {
            string str = System.Text.Encoding.Default.GetString(byte_);
            str = str.Replace("\0", "").Replace("CSA:", "");
            str = str.Split(' ')[0];
            str = (Convert.ToDouble(str) / 0.2 / 1000).ToString();

            return str;
        }

    }
}
