using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links.UrlBuilders;
using Sitecore.Resources.Media;
using Sitecore.Shell.Applications.Dialogs;
using Sitecore.Shell.Framework;
using Sitecore.StringExtensions;
using Sitecore.Utils;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.XA.Foundation.Multisite.Controls;
using Sitecore.Xml;
using System;
using System.Xml;

namespace Foundation.SitecoreExtensions.Extensions
{
    public class GeneralLinkForm : LinkForm
    {
        /// <summary>The aria-label </summary>
        protected Edit AriaLabel;
        /// <summary>The class.</summary>
        protected Edit Class;
        /// <summary>The custom</summary>
        protected Literal Custom;
        /// <summary>The custom target.</summary>
        protected Edit CustomTarget;
        /// <summary>The internal link data context.</summary>
        protected DataContext InternalLinkDataContext;
        /// <summary>Internal link Treeview</summary>
        protected TreeviewEx InternalLinkTreeview;
        /// <summary>The Treeview Container</summary>
        protected Border InternalLinkTreeviewContainer;
        /// <summary>The JavaScriptCode;</summary>
        protected Memo JavascriptCode;
        /// <summary>The anchor.</summary>
        protected Edit LinkAnchor;
        /// <summary>Mailt to Container</summary>
        protected Border MailToContainer;
        /// <summary>The mail to</summary>
        protected Edit MailToLink;
        /// <summary>The media link data context.</summary>
        protected DataContext MediaLinkDataContext;
        /// <summary>Media link Treeview</summary>
        protected TreeviewEx MediaLinkTreeview;
        /// <summary>The Treeview Container</summary>
        protected Border MediaLinkTreeviewContainer;
        /// <summary>The preview.</summary>
        protected Border MediaPreview;
        /// <summary>The modes</summary>
        protected Border Modes;
        /// <summary>The querystring.</summary>
        protected Edit Querystring;
        /// <summary>The section header</summary>
        protected Literal SectionHeader;
        /// <summary>The target.</summary>
        protected Combobox Target;
        /// <summary>The text.</summary>
        protected Edit Text;
        /// <summary>The title.</summary>
        protected Edit Title;
        /// <summary>The Treeview Container</summary>
        protected Scrollbox TreeviewContainer;
        /// <summary>The upload media</summary>
        protected Button UploadMedia;
        /// <summary>The url</summary>
        protected Edit Url;
        /// <summary>The url container</summary>
        protected Border UrlContainer;

        /// <summary>Gets or sets CurrentMode.</summary>
        /// <value>The current mode.</value>
        private string CurrentMode
        {
            get
            {
                string serverProperty = this.ServerProperties["current_mode"] as string;
                return !string.IsNullOrEmpty(serverProperty) ? serverProperty : "internal";
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.ServerProperties["current_mode"] = (object)value;
            }
        }

        /// <summary>Handles the message.</summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull((object)message, nameof(message));
            if (this.CurrentMode != "media")
            {
                base.HandleMessage(message);
            }
            else
            {
                Item obj = (Item)null;
                if (message.Arguments.Count > 0 && ID.IsID(message.Arguments["id"]))
                {
                    IDataView dataView = this.MediaLinkTreeview.GetDataView();
                    if (dataView != null)
                        obj = dataView.GetItem(message.Arguments["id"]);
                }
                if (obj == null)
                    obj = this.MediaLinkTreeview.GetSelectionItem();
                Dispatcher.Dispatch(message, obj);
            }
        }

        /// <summary>Called when the listbox has changed.</summary>
        protected void OnListboxChanged()
        {
            if (this.Target.Value == "Custom")
            {
                this.CustomTarget.Disabled = false;
                this.Custom.Class = string.Empty;
            }
            else
            {
                this.CustomTarget.Value = string.Empty;
                this.CustomTarget.Disabled = true;
                this.Custom.Class = "disabled";
            }
        }

        /// <summary>The on load.</summary>
        /// <param name="e">The e.</param>
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
            this.CurrentMode = this.LinkType ?? string.Empty;

            this.ParseLinkExtended(this.GetLink());

            this.InitControls();
            this.SetModeSpecificControls();
            RegisterScripts();
        }

        /// <summary>Called when the media has open.</summary>
        protected void OnMediaOpen()
        {
            Item selectionItem = this.MediaLinkTreeview.GetSelectionItem();
            if (selectionItem == null || !selectionItem.HasChildren)
                return;
            this.MediaLinkDataContext.SetFolder(selectionItem.Uri);
        }

        /// <summary>Called when the mode has change.</summary>
        /// <param name="mode">The mode.</param>
        protected void OnModeChange(string mode)
        {
            Assert.ArgumentNotNull((object)mode, nameof(mode));
            this.CurrentMode = mode;
            this.SetModeSpecificControls();
            if (UIUtil.IsIE())
                return;
            SheerResponse.Eval("scForm.browser.initializeFixsizeElements();");
        }

        /// <summary>Handles a click on the OK button.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        /// <exception cref="T:System.ArgumentException"><c>ArgumentException</c>.</exception>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)args, nameof(args));
            Packet packet = new Packet("link", Array.Empty<string>());
            this.SetCommonAttributes(packet);
            bool flag;
            switch (this.CurrentMode)
            {
                case "internal":
                    flag = this.SetInternalLinkAttributes(packet);
                    break;
                case "media":
                    flag = this.SetMediaLinkAttributes(packet);
                    break;
                case "external":
                    flag = this.SetExternalLinkAttributes(packet);
                    break;
                case "mailto":
                    flag = this.SetMailToLinkAttributes(packet);
                    break;
                case "anchor":
                    flag = this.SetAnchorLinkAttributes(packet);
                    break;
                case "javascript":
                    flag = this.SetJavascriptLinkAttributes(packet);
                    break;
                default:
                    throw new ArgumentException("Unsupported mode: " + this.CurrentMode);
            }
            if (!flag)
                return;
            SheerResponse.SetDialogValue(packet.OuterXml);
            base.OnOK(sender, args);
        }

        /// <summary>Selects the tree node.</summary>
        protected void SelectMediaTreeNode()
        {
            Item selectionItem = this.MediaLinkTreeview.GetSelectionItem();
            if (selectionItem == null)
                return;
            this.UpdateMediaPreview(selectionItem);
        }

        /// <summary>Uploads the image.</summary>
        protected void UploadImage()
        {
            Item selectionItem = this.MediaLinkTreeview.GetSelectionItem();
            if (selectionItem == null)
                return;
            if (!selectionItem.Access.CanCreate())
                SheerResponse.Alert("You do not have permission to create a new item here.");
            else
                Context.ClientPage.SendMessage((object)this, "media:upload(edit=1,load=1)");
        }

        /// <summary>The hide containing row.</summary>
        /// <param name="control">The control.</param>
        private static void HideContainingRow(Sitecore.Web.UI.HtmlControls.Control control)
        {
            Assert.ArgumentNotNull((object)control, nameof(control));
            if (!Context.ClientPage.IsEvent)
            {
                if (!(control.Parent is GridPanel parent))
                    return;
                parent.SetExtensibleProperty((System.Web.UI.Control)control, "row.style", "display:none");
            }
            else
                SheerResponse.SetStyle(control.ID + "Row", "display", "none");
        }

        /// <summary>The show containing row.</summary>
        /// <param name="control">The control.</param>
        private static void ShowContainingRow(Sitecore.Web.UI.HtmlControls.Control control)
        {
            Assert.ArgumentNotNull((object)control, nameof(control));
            if (!Context.ClientPage.IsEvent)
            {
                if (!(control.Parent is GridPanel parent))
                    return;
                parent.SetExtensibleProperty((System.Web.UI.Control)control, "row.style", string.Empty);
            }
            else
                SheerResponse.SetStyle(control.ID + "Row", "display", string.Empty);
        }

        /// <summary>The init controls.</summary>
        private void InitControls()
        {
            string str = string.Empty;
            string linkAttribute = this.LinkAttributes["target"];
            string linkTargetValue = LinkForm.GetLinkTargetValue(linkAttribute);
            if (linkTargetValue == "Custom")
            {
                str = linkAttribute;
                this.CustomTarget.Disabled = false;
                this.Custom.Class = string.Empty;
            }
            else
            {
                this.CustomTarget.Disabled = true;
                this.Custom.Class = "disabled";
            }
            this.Text.Value = this.LinkAttributes["text"];
            this.Target.Value = linkTargetValue;
            this.CustomTarget.Value = str;
            this.Class.Value = this.LinkAttributes["class"];
            this.Querystring.Value = this.LinkAttributes["querystring"];
            this.Title.Value = this.LinkAttributes["title"];
            this.AriaLabel.Value = LinkAttributes["aria-label"];
            this.InitMediaLinkDataContext();
            this.InitInternalLinkDataContext();
        }

        /// <summary>The init internal link data context.</summary>
        private void InitInternalLinkDataContext()
        {
            this.InternalLinkDataContext.GetFromQueryString();
            string queryString = WebUtil.GetQueryString("ro");
            string linkAttribute = this.LinkAttributes["id"];
            if (!string.IsNullOrEmpty(linkAttribute) && ID.IsID(linkAttribute))
                this.InternalLinkDataContext.SetFolder(new ItemUri(new ID(linkAttribute), Client.ContentDatabase));
            if (queryString.Length <= 0)
                return;
            this.InternalLinkDataContext.Root = queryString;
        }

        /// <summary>The init media link data context.</summary>
        private void InitMediaLinkDataContext()
        {
            this.MediaLinkDataContext.GetFromQueryString();
            string str = this.LinkAttributes["url"].IsNullOrEmpty() ? this.LinkAttributes["id"] : this.LinkAttributes["url"];
            if (this.CurrentMode != "media")
                str = string.Empty;
            if (str.Length == 0)
            {
                str = "/sitecore/media library";
            }
            else
            {
                if (!ID.IsID(str) && !str.StartsWith("/sitecore", StringComparison.InvariantCulture) && !str.StartsWith("/{11111111-1111-1111-1111-111111111111}", StringComparison.InvariantCulture))
                    str = "/sitecore/media library" + str;
                IDataView dataView = this.MediaLinkTreeview.GetDataView();
                if (dataView == null)
                    return;
                Item obj = dataView.GetItem(str);
                if (obj != null && obj.Parent != null)
                    this.MediaLinkDataContext.SetFolder(obj.Uri);
            }
            this.MediaLinkDataContext.AddSelected(new DataUri(str));
            this.MediaLinkDataContext.Root = "/sitecore/media library";
        }

        /// <summary>The register scripts.</summary>
        private static void RegisterScripts()
        {
            string script = "window.Texts = {{ ErrorOcurred: \"{0}\"}};".FormatWith((object)Translate.Text("An error occured:"));
            Context.ClientPage.ClientScript.RegisterClientScriptBlock(Context.ClientPage.GetType(), "translationsScript", script, true);
        }

        /// <summary>The set anchor link attributes.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The set anchor link attributes.</returns>
        private bool SetAnchorLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull((object)packet, nameof(packet));
            string str = this.LinkAnchor.Value;
            if (str.Length > 0 && str.StartsWith("#", StringComparison.InvariantCulture))
                str = str.Substring(1);
            LinkForm.SetAttribute(packet, "url", str);
            LinkForm.SetAttribute(packet, "anchor", str);
            return true;
        }

        /// <summary>The set anchor link controls.</summary>
        private void SetAnchorLinkControls()
        {
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.LinkAnchor);
            string str = this.LinkAttributes["anchor"];
            if (this.LinkType != "anchor" && string.IsNullOrEmpty(this.LinkAnchor.Value))
                str = string.Empty;
            if (!string.IsNullOrEmpty(str) && !str.StartsWith("#", StringComparison.InvariantCulture))
                str = "#" + str;
            this.LinkAnchor.Value = str ?? string.Empty;
            this.SectionHeader.Text = Translate.Text("Specify the name of the anchor, e.g. #header1, and any additional properties");
        }

        /// <summary>The set common attributes.</summary>
        /// <param name="packet">The packet.</param>
        private void SetCommonAttributes(Packet packet)
        {
            Assert.ArgumentNotNull((object)packet, nameof(packet));
            LinkForm.SetAttribute(packet, "linktype", this.CurrentMode);
            LinkForm.SetAttribute(packet, "text", (Sitecore.Web.UI.HtmlControls.Control)this.Text);
            LinkForm.SetAttribute(packet, "title", (Sitecore.Web.UI.HtmlControls.Control)this.Title);
            LinkForm.SetAttribute(packet, "class", (Sitecore.Web.UI.HtmlControls.Control)this.Class);
            LinkForm.SetAttribute(packet, "aria-label", (Sitecore.Web.UI.HtmlControls.Control)this.AriaLabel);
        }

        /// <summary>The set external link attributes.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The set external link attributes.</returns>
        private bool SetExternalLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull((object)packet, nameof(packet));
            string str = this.Url.Value;
            if (str.Length > 0 && str.IndexOf("://", StringComparison.InvariantCulture) < 0 && !str.StartsWith("/", StringComparison.InvariantCulture))
                str = "http://" + str;
            string attributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
            LinkForm.SetAttribute(packet, "url", str);
            LinkForm.SetAttribute(packet, "anchor", string.Empty);
            LinkForm.SetAttribute(packet, "target", attributeFromValue);
            return true;
        }

        /// <summary>The set external link controls.</summary>
        private void SetExternalLinkControls()
        {
            if (this.LinkType == "external" && string.IsNullOrEmpty(this.Url.Value))
                this.Url.Value = this.LinkAttributes["url"];
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.UrlContainer);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.Target);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.CustomTarget);
            this.SectionHeader.Text = Translate.Text("Specify the URL, e.g. http://www.sitecore.net and any additional properties.");
        }

        /// <summary>The set internal link attributes.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The set internal link attributes.</returns>
        private bool SetInternalLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull((object)packet, nameof(packet));
            Item selectionItem = this.InternalLinkTreeview.GetSelectionItem();
            if (selectionItem == null)
            {
                Context.ClientPage.ClientResponse.Alert("Select an item.");
                return false;
            }
            string attributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
            string str = this.Querystring.Value;
            if (str.StartsWith("?", StringComparison.InvariantCulture))
                str = str.Substring(1);
            LinkForm.SetAttribute(packet, "anchor", (Sitecore.Web.UI.HtmlControls.Control)this.LinkAnchor);
            LinkForm.SetAttribute(packet, "querystring", str);
            LinkForm.SetAttribute(packet, "target", attributeFromValue);
            LinkForm.SetAttribute(packet, "id", selectionItem.ID.ToString());
            return true;
        }

        /// <summary>The set internal link contols.</summary>
        private void SetInternalLinkContols()
        {
            this.LinkAnchor.Value = this.LinkAttributes["anchor"];
            this.InternalLinkTreeviewContainer.Visible = true;
            this.MediaLinkTreeviewContainer.Visible = false;
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.TreeviewContainer);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.Querystring);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.LinkAnchor);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.Target);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.CustomTarget);
            this.SectionHeader.Text = Translate.Text("Select the item that you want to create a link to and specify the appropriate properties.");
        }

        /// <summary>The set java script link controls.</summary>
        private void SetJavaScriptLinkControls()
        {
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.JavascriptCode);
            string str = this.LinkAttributes["url"];
            if (this.LinkType != "javascript" && string.IsNullOrEmpty(this.JavascriptCode.Value))
                str = string.Empty;
            this.JavascriptCode.Value = str;
            this.SectionHeader.Text = Translate.Text("Specify the JavaScript and any additional properties.");
        }

        /// <summary>The set javascript link attributes.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The set javascript link attributes.</returns>
        private bool SetJavascriptLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull((object)packet, nameof(packet));
            string str = this.JavascriptCode.Value;
            if (str.Length > 0 && str.IndexOf("javascript:", StringComparison.InvariantCulture) < 0)
                str = "javascript:" + str;
            LinkForm.SetAttribute(packet, "url", str);
            LinkForm.SetAttribute(packet, "anchor", string.Empty);
            return true;
        }

        /// <summary>The set mail link controls.</summary>
        private void SetMailLinkControls()
        {
            if (this.LinkType == "mailto" && string.IsNullOrEmpty(this.Url.Value))
                this.MailToLink.Value = this.LinkAttributes["url"];
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.MailToContainer);
            this.SectionHeader.Text = Translate.Text("Specify the email address and any additional properties. To send a test mail use the 'Send a test mail' button.");
        }

        /// <summary>The set mail to link attributes.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The set mail to link attributes.</returns>
        private bool SetMailToLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull((object)packet, nameof(packet));
            string str = this.MailToLink.Value;
            string email = StringUtil.GetLastPart(str, ':', str);
            if (!EmailUtility.IsValidEmailAddress(email))
            {
                SheerResponse.Alert("The e-mail address is invalid.");
                return false;
            }
            if (!string.IsNullOrEmpty(email))
                email = "mailto:" + email;
            LinkForm.SetAttribute(packet, "url", email ?? string.Empty);
            LinkForm.SetAttribute(packet, "anchor", string.Empty);
            return true;
        }

        /// <summary>The set media link attributes.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The set media link attributes.</returns>
        private bool SetMediaLinkAttributes(Packet packet)
        {
            Assert.ArgumentNotNull((object)packet, nameof(packet));
            Item selectionItem = this.MediaLinkTreeview.GetSelectionItem();
            if (selectionItem == null)
            {
                Context.ClientPage.ClientResponse.Alert("Select a media item.");
                return false;
            }
            string attributeFromValue = LinkForm.GetLinkTargetAttributeFromValue(this.Target.Value, this.CustomTarget.Value);
            LinkForm.SetAttribute(packet, "target", attributeFromValue);
            LinkForm.SetAttribute(packet, "id", selectionItem.ID.ToString());
            return true;
        }

        /// <summary>The set media link controls.</summary>
        private void SetMediaLinkControls()
        {
            this.InternalLinkTreeviewContainer.Visible = false;
            this.MediaLinkTreeviewContainer.Visible = true;
            this.MediaPreview.Visible = true;
            this.UploadMedia.Visible = true;
            Item folder = this.MediaLinkDataContext.GetFolder();
            if (folder != null)
                this.UpdateMediaPreview(folder);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.TreeviewContainer);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.Target);
            ShowContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.CustomTarget);
            this.SectionHeader.Text = Translate.Text("Select an item from the media library and specify any additional properties.");
        }

        /// <summary>The set mode specific controls.</summary>
        /// <exception cref="T:System.ArgumentException">
        /// </exception>
        private void SetModeSpecificControls()
        {
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.TreeviewContainer);
            this.MediaPreview.Visible = false;
            this.UploadMedia.Visible = false;
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.UrlContainer);
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.Querystring);
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.MailToContainer);
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.LinkAnchor);
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.JavascriptCode);
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.Target);
            HideContainingRow((Sitecore.Web.UI.HtmlControls.Control)this.CustomTarget);
            switch (this.CurrentMode)
            {
                case "internal":
                    this.SetInternalLinkContols();
                    break;
                case "media":
                    this.SetMediaLinkControls();
                    break;
                case "external":
                    this.SetExternalLinkControls();
                    break;
                case "mailto":
                    this.SetMailLinkControls();
                    break;
                case "anchor":
                    this.SetAnchorLinkControls();
                    break;
                case "javascript":
                    this.SetJavaScriptLinkControls();
                    break;
                default:
                    throw new ArgumentException("Unsupported mode: " + this.CurrentMode);
            }
            foreach (Border control in this.Modes.Controls)
            {
                if (control != null)
                    control.Class = control.ID.ToLowerInvariant() == this.CurrentMode ? "selected" : string.Empty;
            }
        }

        /// <summary>Updates the preview.</summary>
        /// <param name="item">The item.</param>
        private void UpdateMediaPreview(Item item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            MediaUrlBuilderOptions thumbnailOptions = MediaUrlBuilderOptions.GetThumbnailOptions((MediaItem)item);
            thumbnailOptions.UseDefaultIcon = new bool?(true);
            thumbnailOptions.Width = new int?(96);
            thumbnailOptions.Height = new int?(96);
            thumbnailOptions.Language = item.Language;
            thumbnailOptions.AllowStretch = new bool?(false);
            this.MediaPreview.InnerHtml = "<img src=\"" + MediaManager.GetMediaUrl((MediaItem)item, thumbnailOptions) + "\" width=\"96px\" height=\"96px\" border=\"0\" alt=\"\" />";
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
