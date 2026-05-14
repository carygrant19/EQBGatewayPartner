namespace Pesonet.API.Models.Transaction
{
    internal class Request
    {
        public FIToFICstmrCdtTrf FIToFICstmrCdtTrf { get; set; } = default!;
    }

    internal class FIToFICstmrCdtTrf
    {
        public GrpHdr GrpHdr { get; set; } = new();
        public List<CdtTrfTxInf> CdtTrfTxInf { get; set; } = [];
    }
    internal class GrpHdr
    {
        public string? MsgId { get; set; } = string.Empty;
        public string? CreDtTm { get; set; } = string.Empty;
        public int NbOfTxs { get; set; } = 1;
        public TtlIntrBkSttlmAmt TtlIntrBkSttlmAmt { get; set; } = new();
        public string? IntrBkSttlmDt { get; set; } = null;
        public SttlmInf SttlmInf { get; set; } = new();
        public InstgAgt InstgAgt { get; set; } = new();
        public InstdAgt InstdAgt { get; set; } = new();
        public GPmtTpInf PmtTpInf { get; set; } = new();
    }

    internal class TtlIntrBkSttlmAmt
    {
        public string Ccy { get; set; } = "PHP";
        public string value { get; set; } = "1000";
    }
    internal class SttlmInf
    {
        public string SttlmMtd { get; set; } = "CLRG";
    }
    internal class InstgAgt
    {
        public FinInstnId FinInstnId { get; set; } = new();
    }
    internal class InstdAgt
    {
        public FinInstnId FinInstnId { get; set; } = new();
    }

    internal class FinInstnId
    {
        public string BICFI { get; set; } = string.Empty; //get from cnfig
    }

    internal class GPmtTpInf
    {
        public LclInstrm LclInstrm { get; set; } = new();
    }
    internal class LclInstrm
    {
        public string Prtry { get; set; } = "20231012120143";
    }

    internal class CdtTrfTxInf
    {
        public PmtId PmtId { get; set; } = new();
        public PmtTpInf PmtTpInf { get; set; } = new();
        public IntrBkSttlmAmt IntrBkSttlmAmt { get; set; } = new();

        public string ChrgBr = "SLEV";
        public Dbtr Dbtr { get; set; } = new();
        public DbtrAcct DbtrAcct { get; set; } = new();
        public DbtrAgt DbtrAgt { get; set; } = new();
        public Cdtr Cdtr { get; set; } = new();
        public CdtrAcct CdtrAcct { get; set; } = new();
        public CdtrAgt CdtrAgt { get; set; } = new();
        public RmtInf RmtInf { get; set; } = new();
    }

    internal class PmtId
    {
        public int? EndToEndId { get; set; } = null;
        public string TxId { get; set; } = string.Empty;
    }

    internal class PmtTpInf
    {
        public SvcLvl SvcLvl { get; set; } = new();
        public CtgyPurp CtgyPurp { get; set; } = new();
    }

    internal class SvcLvl
    {
        public string Prtry { get; set; } = "NURG";
    }

    internal class CtgyPurp
    {
        public string Cd { get; set; } = "CASH";
    }

    internal class IntrBkSttlmAmt
    {
        public string Ccy { get; set; } = "PHP";
        public string value { get; set; } = "1000";

    }

    internal class Dbtr
    {
        public string Nm { get; set; } = "Juan Dela Cruz";
        public string[] PstlAdr { get; set; } = ["PH"];

    }

    internal class DbtrAcct
    {
        public Id Id { get; set; } = new();
    }

    internal class Id
    {
        public Othr Othr { get; set; } = new();
    }

    internal class Othr
    {
        public string Id { get; set; } = "";
    }

    internal class DbtrAgt
    {
        public FinInstnId FinInstnId = new();
    }

    internal class Cdtr
    {
        public string Nm { get; set; } = "Juana Sanchez";
        public string[] PstlAdr { get; set; } = ["PH"];

    }
    internal class CdtrAcct
    {
        public Id Id { get; set; } = new();
    }

    internal class CdtrAgt
    {
        public FinInstnId FinInstnId = new();
    }

    internal class RmtInf
    {
        public Ustrd Ustrd { get; set; } = new();
    }

    internal class Ustrd
    {
        public string? rfi_reference_number { get; set; } = null;
        public string? ofi_customer_reference_number { get; set; } = null;
        public string? rfi_customer_reference_number { get; set; } = null;
    }
}
