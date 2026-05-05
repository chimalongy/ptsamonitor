namespace ptsamonitor.Models
{
    public class DashboardTransaction
    {
        public long Id { get; set; }
        public string TransId { get; set; }
        public string TranNumber { get; set; }
        public DateTime Time { get; set; }
        public string Source { get; set; }
        public string Source1 { get; set; }
        public string Destination { get; set; }
        public int RespCode { get; set; }
        public string RespCodeDescription { get; set; }
        public string TermName { get; set; }
        public string TermRetailerName { get; set; }
        public string Inst { get; set; }
        public string Pan { get; set; }
        public string MaskedPan { get; set; }
        public string AuthFiName { get; set; }
        public decimal Amount { get; set; }
        public string InstitutionCode { get; set; }
    }
}
