namespace TRM.Web.Services.HtmlToPdf
{
    public interface ITrmHtmlToPdf
    {
        byte[] ConvertHtmlToPdf(string htmlString, string footerText);
    }
}