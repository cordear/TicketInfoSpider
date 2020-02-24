using System.Collections.Generic;

namespace TicketInfoSpider
{
    public class Ext
    {
    }

    public class Ext2
    {
    }

    public class FpmxList
    {
        public string requestId { get; set; }
        public Ext2 ext { get; set; }
        public object id { get; set; }
        public string mxid { get; set; }
        public object kprq { get; set; }
        public object fpdm { get; set; }
        public object fphm { get; set; }
        public string fpmxxh { get; set; }
        public string xsdjbh { get; set; }
        public string fphxz { get; set; }
        public double je { get; set; }
        public double sl { get; set; }
        public double se { get; set; }
        public string spmc { get; set; }
        public string spsm { get; set; }
        public string ggxh { get; set; }
        public string dw { get; set; }
        public string spsl { get; set; }
        public string spdj { get; set; }
        public string hsbz { get; set; }
        public string spbm { get; set; }
        public string djmxxh { get; set; }
        public object bb { get; set; }
        public string yhzcbs { get; set; }
        public string lslbs { get; set; }
        public string zzstsgl { get; set; }
        public string zxbm { get; set; }
        public object jshj { get; set; }
        public string zhdyhh { get; set; }
        public object kysl { get; set; }
    }

    public class RtnData
    {
        public string requestId { get; set; }
        public Ext ext { get; set; }
        public string id { get; set; }
        public object cxlx { get; set; }
        public string fplxdm { get; set; }
        public string tspz { get; set; }
        public object tsfs { get; set; }
        public string kprq { get; set; }
        public object ssnf { get; set; }
        public string ssyf { get; set; }
        public string zsfs { get; set; }
        public string fpdm { get; set; }
        public string fphm { get; set; }
        public string ghdwdm { get; set; }
        public string ghdwmc { get; set; }
        public string ghdwdzdh { get; set; }
        public string ghdwyhzh { get; set; }
        public string skr { get; set; }
        public object kprbm { get; set; }
        public string kpr { get; set; }
        public string fhr { get; set; }
        public string bz { get; set; }
        public string czydm { get; set; }
        public string kplx { get; set; }
        public string zfrdm { get; set; }
        public string zfr { get; set; }
        public object zfrq { get; set; }
        public string hzxxb { get; set; }
        public object hzxxId { get; set; }
        public string qdbz { get; set; }
        public double hjje { get; set; }
        public double se { get; set; }
        public double jshj { get; set; }
        public string hsbz { get; set; }
        public string skm { get; set; }
        public string jym { get; set; }
        public string ewm { get; set; }
        public string fpzt { get; set; }
        public string xhdwmc { get; set; }
        public string xhdwdm { get; set; }
        public string xhdwdzdh { get; set; }
        public string xhdwyhzh { get; set; }
        public object dkdwmc { get; set; }
        public object dkdwdm { get; set; }
        public string swjgdm { get; set; }
        public string swjgmc { get; set; }
        public object wspzhm { get; set; }
        public string tzdh { get; set; }
        public string kpjh { get; set; }
        public string yfpdm { get; set; }
        public string yfphm { get; set; }
        public string scbz { get; set; }
        public object dqsj { get; set; }
        public object zfdqsj { get; set; }
        public string bbh { get; set; }
        public string kpddm { get; set; }
        public object kpdmc { get; set; }
        public string fpcbh { get; set; }
        public string by1 { get; set; }
        public string by2 { get; set; }
        public string jqbh { get; set; }
        public string zyspmc { get; set; }
        public string spsm { get; set; }
        public double zhsl { get; set; }
        public bool? zkbz { get; set; }
        public double? hjzkje { get; set; }
        public double? hjzkse { get; set; }
        public string zfyy { get; set; }
        public string qmbz { get; set; }
        public string qmz { get; set; }
        public string yqbz { get; set; }
        public string qmcs { get; set; }
        public object zhbm { get; set; }
        public object zzbm { get; set; }
        public object fpbz { get; set; }
        public string sblx { get; set; }
        public string lybz { get; set; }
        public object xtid { get; set; }
        public object xtmc { get; set; }
        public object bsbz { get; set; }
        public object tsbz { get; set; }
        public object lrrdm { get; set; }
        public object lrrmc { get; set; }
        public string ddbh { get; set; }
        public string ywfpqqlsh { get; set; }
        public object order_date { get; set; }
        public string htbh { get; set; }
        public object hth { get; set; }
        public string gfkhzjlx { get; set; }
        public string gfkhzjhm { get; set; }
        public object czbz { get; set; }
        public object orderTime { get; set; }
        public List<FpmxList> fpmxList { get; set; }
        public object jdcfpmx { get; set; }
        public object zzCode { get; set; }
        public object businessid { get; set; }
        public string gfkhyx { get; set; }
        public string gfkhdh { get; set; }
        public double? kce { get; set; }
        public string fpqqlsh { get; set; }
        public object skpbh { get; set; }
        public object skpkl { get; set; }
        public object keypwd { get; set; }
        public string pdfurl { get; set; }
        public bool dzp { get; set; }
        public bool cezs { get; set; }
        public bool zp { get; set; }
        public bool pp { get; set; }
        public bool ptzs { get; set; }
    }

    public class TicketDataCollection
    {
        public List<RtnData> rtnData { get; set; }
        public string rtnMsg { get; set; }
        public string rtnCode { get; set; }
    }
}