using Microsoft.Extensions.DependencyInjection;
using Sitecore.Collections;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Links;
using Sitecore.Links.UrlBuilders;
using Sitecore.Sites;
using Sitecore.Web;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Multisite.LinkManagers;
using Sitecore.Xml.Xsl;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Sitecore.Configuration;

namespace Foundation.SitecoreExtensions.Extensions
{
    public class SxaLinkRenderer : LinkRenderer
    {
        private readonly char[] _delimiter = new char[2]
        {
          '=',
          '&'
        };

        public SxaLinkRenderer(Item item)
          : base(item)
        {
        }

        protected ISiteInfoResolver SiteInfoResolver { get; } = ServiceProviderServiceExtensions.GetService<ISiteInfoResolver>(ServiceLocator.ServiceProvider);

        protected virtual string GetUrl(XmlField field) => field != null ? new LinkItem(((CustomField)field).Value).TargetUrl : LinkManager.GetItemUrl(this.Item, this.GetUrlOptions(this.Item));

        protected ItemUrlBuilderOptions GetUrlOptions(Item item)
        {
            ItemUrlBuilderOptions urlBuilderOptions = LinkManager.GetDefaultUrlBuilderOptions();
            SiteInfo siteInfo = this.SiteInfoResolver.GetSiteInfo(item);
            urlBuilderOptions.SiteResolving = new bool?(Settings.Rendering.SiteResolving);
            urlBuilderOptions.Site = new SiteContext(siteInfo);
            return urlBuilderOptions;
        }

        /// <summary>Renders this instance.</summary>
        /// <returns>The render.</returns>
        public override RenderFieldResult Render()
        {
            SafeDictionary<string> attributes = new SafeDictionary<string>();
            attributes.AddRange((SafeDictionary<string, string>)this.Parameters);
            if (MainUtil.GetBool(attributes["endlink"], false))
                return RenderFieldResult.EndLink;
            Set<string> set = Set<string>.Create("field", "select", "text", "haschildren", "before", "after", "enclosingtag", "fieldname", "disable-web-editing");
            LinkField linkField = this.LinkField;
            if (linkField != null)
            {
                attributes["title"] = HttpUtility.HtmlAttributeEncode(StringUtil.GetString(attributes["title"], linkField.Title));
                attributes["target"] = StringUtil.GetString(attributes["target"], linkField.Target);
                attributes["class"] = StringUtil.GetString(attributes["class"], linkField.Class);
                attributes["aria-label"] = StringUtil.GetString(attributes["aria-label"], linkField.GetAttribute("aria-label"));
                this.SetRelAttribute(attributes, linkField);
            }
            string str1 = string.Empty;
            string rawParameters = this.RawParameters;
            if (!string.IsNullOrEmpty(rawParameters) && rawParameters.IndexOfAny(this._delimiter) < 0)
                str1 = rawParameters;
            if (string.IsNullOrEmpty(str1))
            {
                Item targetItem = this.TargetItem;
                string str2 = targetItem != null ? targetItem.DisplayName : string.Empty;
                string str3 = HttpUtility.HtmlEncode(linkField != null ? linkField.Text : string.Empty);
                str1 = StringUtil.GetString(str1, attributes["text"], str3, str2);
            }
            string url = this.GetUrl((XmlField)linkField);
            if (this.LinkType == "javascript")
            {
                attributes["href"] = "#";
                attributes["onclick"] = StringUtil.GetString(attributes["onclick"], url);
            }
            else
                attributes["href"] = HttpUtility.HtmlEncode(StringUtil.GetString(attributes["href"], url));
            StringBuilder tag = new StringBuilder("<a", 47);
            foreach (KeyValuePair<string, string> keyValuePair in (SafeDictionary<string, string>)attributes)
            {
                string key = keyValuePair.Key;
                string str4 = keyValuePair.Value;
                if (!set.Contains(key.ToLowerInvariant()))
                    FieldRendererBase.AddAttribute(tag, key, str4);
            }
            tag.Append('>');
            if (!MainUtil.GetBool(attributes["haschildren"], false))
            {
                if (string.IsNullOrEmpty(str1))
                    str1 = url;
                if (string.IsNullOrEmpty(str1))
                    return RenderFieldResult.Empty;
                tag.Append(str1);
            }
            return new RenderFieldResult()
            {
                FirstPart = tag.ToString(),
                LastPart = "</a>"
            };
        }
    }
}
