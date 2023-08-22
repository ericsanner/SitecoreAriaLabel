using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.WebControls;
using Sitecore.Xml;
using System;
using Sitecore.Shell.Applications.Dialogs;
using Sitecore;
using System.Xml;

namespace Foundation.SitecoreExtensions.Extensions
{
    /// <summary>Represents a InternalLinkForm.</summary>
    public class InternalLinkForm : LinkForm
    {
        /// <summary>The aria-label.</summary>
        protected Edit AriaLabel;
        /// <summary>The anchor.</summary>
        protected Edit Anchor;
        /// <summary>The class.</summary>
        protected Edit Class;
        /// <summary>The custom label.</summary>
        protected Panel CustomLabel;
        /// <summary>The custom target.</summary>
        protected Edit CustomTarget;
        /// <summary>The internal link data context.</summary>
        protected DataContext InternalLinkDataContext;
        /// <summary>The querystring.</summary>
        protected Edit Querystring;
        /// <summary>The target.</summary>
        protected Combobox Target;
        /// <summary>The text.</summary>
        protected Edit Text;
        /// <summary>The title.</summary>
        protected Edit Title;
        /// <summary>The treeview.</summary>
        protected TreeviewEx Treeview;

        /// <summary>Called when the listbox has changed.</summary>
        protected void OnListboxChanged()
        {
            if (this.Target.Value == "Custom")
            {
                this.CustomTarget.Disabled = false;
                this.CustomLabel.Disabled = false;
            }
            else
            {
                this.CustomTarget.Value = string.Empty;
                this.CustomTarget.Disabled = true;
                this.CustomLabel.Disabled = true;
            }
        }

        /// <summary>Raises the load event.</summary>
        /// <param name="e">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client postback,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull((object)e, nameof(e));
            base.OnLoad(e);

            this.ParseLinkExtended(this.GetLink());

            if (Context.ClientPage.IsEvent)
                return;
            this.InternalLinkDataContext.GetFromQueryString();
            this.CustomTarget.Disabled = true;
            this.CustomLabel.Disabled = true;
            string queryString = WebUtil.GetQueryString("ro");
            string linkAttribute1 = this.LinkAttributes["url"];
            string str = string.Empty;
            string linkAttribute2 = this.LinkAttributes["target"];
            string linkTargetValue = LinkForm.GetLinkTargetValue(linkAttribute2);
            if (linkTargetValue == "Custom")
            {
                str = linkAttribute2;
                this.CustomTarget.Disabled = false;
                this.CustomLabel.Disabled = false;
                this.CustomTarget.Background = "window";
            }
            this.Text.Value = this.LinkAttributes["text"];
            this.Anchor.Value = this.LinkAttributes["anchor"];
            this.Target.Value = linkTargetValue;
            this.CustomTarget.Value = str;
            this.Class.Value = this.LinkAttributes["class"];
            this.Querystring.Value = this.LinkAttributes["querystring"];
            this.Title.Value = this.LinkAttributes["title"];
            this.AriaLabel.Value = this.LinkAttributes["aria-label"];
            string linkAttribute3 = this.LinkAttributes["id"];
            if (string.IsNullOrEmpty(linkAttribute3) || !ID.IsID(linkAttribute3))
            {
                this.SetFolderFromUrl(linkAttribute1);
            }
            else
            {
                ID id = new ID(linkAttribute3);
                if (Sitecore.Client.ContentDatabase.GetItem(id, this.InternalLinkDataContext.Language) == null && !string.IsNullOrWhiteSpace(linkAttribute1))
                    this.SetFolderFromUrl(linkAttribute1);
                else
                    this.InternalLinkDataContext.SetFolder(new ItemUri(id, this.InternalLinkDataContext.Language, Sitecore.Client.ContentDatabase));
            }
            if (queryString.Length <= 0)
                return;
            this.InternalLinkDataContext.Root = queryString;
        }

        /// <summary>Handles a click on the OK button.</summary>
        /// <param name="sender">
        /// </param>
        /// <param name="args">
        /// </param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)args, nameof(args));
            Item selectionItem = this.Treeview.GetSelectionItem();
            if (selectionItem == null)
            {
                Context.ClientPage.ClientResponse.Alert("Select an item.");
            }
            else
            {
                string attributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
                string str = this.Querystring.Value;
                if (str.StartsWith("?", StringComparison.InvariantCulture))
                    str = str.Substring(1);
                Packet packet = new Packet("link", Array.Empty<string>());
                LinkForm.SetAttribute(packet, "text", (Control)this.Text);
                LinkForm.SetAttribute(packet, "linktype", "internal");
                LinkForm.SetAttribute(packet, "anchor", (Control)this.Anchor);
                LinkForm.SetAttribute(packet, "querystring", (Control)this.Anchor);
                LinkForm.SetAttribute(packet, "title", (Control)this.Title);
                LinkForm.SetAttribute(packet, "class", (Control)this.Class);
                LinkForm.SetAttribute(packet, "aria-label", (Control)this.AriaLabel);
                LinkForm.SetAttribute(packet, "querystring", str);
                LinkForm.SetAttribute(packet, "target", attributeFromValue);
                LinkForm.SetAttribute(packet, "id", selectionItem.ID.ToString());
                Assert.IsTrue(!string.IsNullOrEmpty(selectionItem.ID.ToString()) && ID.IsID(selectionItem.ID.ToString()), "ID doesn't exist.");
                Context.ClientPage.ClientResponse.SetDialogValue(packet.OuterXml);
                base.OnOK(sender, args);
            }
        }

        /// <summary>The set folder from url.</summary>
        /// <param name="url">The url.</param>
        private void SetFolderFromUrl(string url)
        {
            Assert.ArgumentNotNull((object)url, nameof(url));
            if (this.LinkType != "internal")
                url = "/sitecore/content" + Settings.DefaultItem;
            if (url.Length == 0)
                url = "/sitecore/content";
            if (!url.StartsWith("/sitecore", StringComparison.InvariantCulture))
                url = "/sitecore/content" + url;
            this.InternalLinkDataContext.Folder = url;
        }

        private void ParseLinkExtended(string link)
        {
            Assert.ArgumentNotNull((object)link, nameof(link));
            XmlDocument xmlDocument = XmlUtil.LoadXml(link);
            if (xmlDocument == null)
                return;
            XmlNode node = xmlDocument.SelectSingleNode("/link");
            if (node == null)
                return;
            LinkAttributes["aria-label"] = XmlUtil.GetAttribute("aria-label", node);
        }
    }    
}
