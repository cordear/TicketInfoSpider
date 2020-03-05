using System.Data;
using System.IO;
using System.Text;

namespace TicketInfoSpider
{
    internal class CsvLoader
    {
        public static DataTable Csv2DataTable(string fileName)
        {
            var dt = new DataTable();
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var sr = new StreamReader(fs, new UnicodeEncoding());
            //记录每次读取的一行记录
            var strLine = "";
            //记录每行记录中的各字段内容
            //标示列数
            var columnCount = 0;
            //标示是否是读取的第一行
            var isFirst = true;

            //逐行读取CSV中的数据
            while ((strLine = sr.ReadLine()) != null)
            {
                var aryLine = strLine.Split(',');
                if (isFirst)
                {
                    isFirst = false;
                    columnCount = aryLine.Length;
                    //创建列
                    for (var i = 0; i < columnCount; i++)
                    {
                        var dc = new DataColumn(aryLine[i]);
                        dt.Columns.Add(dc);
                    }
                }
                else
                {
                    var dr = dt.NewRow();
                    for (var j = 0; j < columnCount; j++) dr[j] = aryLine[j];
                    dt.Rows.Add(dr);
                }
            }

            sr.Close();
            fs.Close();
            return dt;
        }
    }
}