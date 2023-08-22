using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Xml;
using System;
using Sitecore.Shell.Applications.Dialogs;
using Sitecore;
using System.Xml;

namespace Foundation.SitecoreExtensions.Extensions
{
    /// <summary>Represents a ExternalLinkForm.</summary>
    public class ExternalLinkForm : LinkForm
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
        /// <summary>The target.</summary>
        protected Combobox Target;
        /// <summary>The text.</summary>
        protected Edit Text;
        /// <summary>The title.</summary>
        protected Edit Title;
        /// <summary>The url.</summary>
        protected Edit Url;
        /// <summary> The test button </summary>
        protected Button Test;

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
            if (Context.ClientPage.IsEvent)
                return;

            this.ParseLinkExtended(this.GetLink());

            string str1 = this.LinkAttributes["url"];
            if (this.LinkType != "external")
                str1 = string.Empty;
            string str2 = string.Empty;
            string linkAttribute = this.LinkAttributes["target"];
            string linkTargetValue = LinkForm.GetLinkTargetValue(linkAttribute);
            if (linkTargetValue == "Custom")
            {
                str2 = linkAttribute;
                this.CustomTarget.Disabled = false;
                this.CustomLabel.Disabled = false;
            }
            this.Text.Value = this.LinkAttributes["text"];
            this.Url.Value = str1;
            this.Target.Value = linkTargetValue;
            this.CustomTarget.Value = str2;
            this.Class.Value = this.LinkAttributes["class"];
            this.Title.Value = this.LinkAttributes["title"];
            this.AriaLabel.Value = this.LinkAttributes["aria-label"];
            this.Test.ToolTip = Translate.Text("Open the specified URL in a browser.");
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
            string path = this.GetPath();
            string attributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
            Packet packet = new Packet("link", Array.Empty<string>());
            LinkForm.SetAttribute(packet, "text", (Control)this.Text);
            LinkForm.SetAttribute(packet, "linktype", "external");
            LinkForm.SetAttribute(packet, "url", path);
            LinkForm.SetAttribute(packet, "anchor", string.Empty);
            LinkForm.SetAttribute(packet, "title", (Control)this.Title);
            LinkForm.SetAttribute(packet, "class", (Control)this.Class);
            LinkForm.SetAttribute(packet, "target", attributeFromValue);
            LinkForm.SetAttribute(packet, "aria-label", (Control)this.AriaLabel);
            Context.ClientPage.ClientResponse.SetDialogValue(packet.OuterXml);
            base.OnOK(sender, args);
        }

        /// <summary>Called when this instance has test.</summary>
        protected void OnTest()
        {
            string path = this.GetPath();
            if (path.Length <= 0)
                return;
            Context.ClientPage.ClientResponse.Eval("try {window.open('" + path + "', '_blank') } catch(e) { alert('" + Translate.Text("An error occured:") + " ' + e.description) }");
        }

        /// <summary>Gets the path.</summary>
        /// <returns>The path.</returns>
        /// <contract>
        ///   <ensures condition="not null" />
        /// </contract>
        private string GetPath()
        {
            string path = this.Url.Value;
            if (path.Length > 0 && path.IndexOf("://", StringComparison.InvariantCulture) < 0 && !path.StartsWith("/", StringComparison.InvariantCulture))
                path = "http://" + path;
            return path;
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
