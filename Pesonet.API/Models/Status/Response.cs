namespace Pesonet.API.Models.Status
{
    internal class Response
    {
        public GrpHdr GrpHdr { get; set; } = new();
        public List<TxInfAndSts> TxInfAndSts { get; set; } = [];
    }
    internal class GrpHdr
    {
        public string MsgId { get; set; } = string.Empty;
        public long CreDtTm { get; set; } = 0;
        public InstgAgt InstgAgt { get; set; } = new();
        public InstdAgt InstdAgt { get; set; } = new();
        public OrgnlGrpInfAndSts OrgnlGrpInfAndSts { get; set; } = new();
    }

    #region GrpHdr

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
        public string BICFI { get; set; } = string.Empty;
    }

    internal class OrgnlGrpInfAndSts
    {
        public string OrgnlMsgId { get; set; } = string.Empty;
        public string OrgnlMsgNmId { get; set; } = string.Empty;
        public string GrpSts { get; set; } = string.Empty;
    }

    #endregion

    #region TxInfAndSts

    internal class TxInfAndSts
    {
        public OrgnlGrpInf OrgnlGrpInf { get; set; } = new();
        public string OrgnlEndToEndId { get; set; } = string.Empty;
        public string OrgnlTxId { get; set; } = string.Empty;
        public string TxSts { get; set; } = string.Empty;
        public StsRsnInf StsRsnInf { get; set; } = new();
        public long? AccptncDtTm { get; set; } = 0;
        public OrgnlTxRef OrgnlTxRef { get; set; } = new();

    }

    internal class OrgnlGrpInf
    {
        public string OrgnlMsgId { get; set; } = string.Empty;
        public string OrgnlMsgNmId { get; set; } = string.Empty;
    }

    internal class StsRsnInf
    {
        public string AddtlInf { get; set; } = string.Empty;
    }

    internal class OrgnlTxRef
    {
        public Amt Amt { get; set; } = new();
        public Cdtr Cdtr { get; set; } = new();
        public CdtrAcct CdtrAcct { get; set; } = new();
        public CdtrAgt CdtrAgt { get; set; } = new();
    }

    internal class Amt
    {
        public EqvtAmt EqvtAmt { get; set; } = new();
    }

    internal class EqvtAmt
    {
        public string Amt { get; set; } = "0";
        public string? CcyOfTrf { get; set; } = string.Empty;
    }

    internal class Cdtr
    {
        public string Nm { get; set; } = string.Empty;
    }

    internal class CdtrAcct
    {
        public Id Id { get; set; } = new();
    }

    internal class Id
    {
        public Othr Othr { get; set; } = new();
    }

    internal class Othr
    {
        public string? Id { get; set; } = string.Empty;
    }

    internal class CdtrAgt
    {
        public FinInstnId FinInstnId { get; set; } = new();
    }

    #endregion

}
