namespace GimnasioSolid.Scanners
{
    public sealed class QrCodeScanner : IAccessScanner
    {
        public string Scan(string presentedData)
        {
            return presentedData.Trim();
        }
    }
}
