using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Models.SFC
{
    public class SFCS_Setting
    {
        private string _product;
        private string _partNumber;
        private string _station;
        private string _barcode;
        private string _line = "Line1";
        private string _testVersion = "v1.0.0.1";

        public string Product { get; set; }
        public string PartNumber { get; set; }
        public string Station { get; set; }
        public string Barcode { get; set; }
        //public string Line { get; set; }
        //public string testVersion { get; set; }

        public string Line
        {
            get => _line;
            set => _line = value;
        }

        public string testVersion
        {
            get => _testVersion;
            set => _testVersion = value;
        }

        //CheckStationPass(string Product, string Station, string Barcode, string LineNo, string Version);
        public SFCS_Setting(string product, string station, string lineNo, string version)
        {
            Product = product;
            Station = station;
            Line = lineNo;
            testVersion = version;
        }

    }
}
