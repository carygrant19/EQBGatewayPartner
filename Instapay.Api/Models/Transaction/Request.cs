namespace Instapay.Api.Models.Transaction
{
    internal class Request
    {
        public string rqtag { get; set; } = string.Empty;
        public string claims { get; set; } = string.Empty!;
        public string srcacctname { get; set; } = string.Empty!;
        public string srcacctnum { get; set; } = string.Empty!;
        public string srcaccttype { get; set; } = string.Empty!;
        public string srcBirthDt { get; set; } = string.Empty!;
        public string srcCityOfBirth { get; set; } = string.Empty!;
        public string srcCtryOfBirth { get; set; } = string.Empty!;
        public string destname { get; set; } = string.Empty!;
        public string destbnkcode { get; set; } = string.Empty!;
        public string destacctno { get; set; } = string.Empty!;
        public string destMobile { get; set; } = string.Empty!;
        public string tottxnamt { get; set; } = string.Empty!;
        public string merchantid { get; set; } = string.Empty!;
    }
}
