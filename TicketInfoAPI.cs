using System;
using System.Net.Http;
using System.Text;

namespace TicketInfoSpider
{
    public static class TicketInfoApi
    {
        public static Uri ValidateCode =>
            new Uri("http://www.baiwang.com/cloudapi/cterminal/api/service/fpcyvalidateCode");

        public static Uri AcquireTicketData =>
            new Uri("http://www.baiwang.com/cloudapi/cterminal/api/service/queryfpbybd");
    }

    public class TicketDataRequestMessage : HttpRequestMessage
    {
        public TicketDataRequestMessage(string ticketId, string validCode, string price)
        {
            RequestUri = TicketInfoApi.AcquireTicketData;
            Method = HttpMethod.Post;
            Content = new StringContent(
                $"bdbh={ticketId}&danHao=01&yzm={validCode}&cxlx=1&sfzjlx=01&zjhm=&fpje={price}&jesq=1",Encoding.UTF8, 
                "application/x-www-form-urlencoded");
            this.Headers.Add("Origin","http://www.baiwang.com");
            this.Headers.Add("Referer","http://www.baiwang.com/cloudpages/cterminal-res/cterminal/fpcy.html");
            this.Headers.Add("Accept","application/json, text/javascript, */*; q=0.01");
            this.Headers.Add("X-Requested-With","XMLHttpRequest");
            this.Headers.Add("Proxy-Connection","keep-alive");
        }
    }
}