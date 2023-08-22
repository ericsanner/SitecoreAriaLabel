using Sitecore.Data.Items;
using Sitecore.Xml.Xsl;

namespace Foundation.SitecoreExtensions.Extensions
{
    public class GetLinkFieldValue : Sitecore.Pipelines.RenderField.GetLinkFieldValue
    {
        protected override LinkRenderer CreateRenderer(Item item) => (LinkRenderer)new SxaLinkRenderer(item);
    }
}
