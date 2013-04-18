using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Bugzilla
{
    public sealed class BugzillaProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUserName;
        private ValidatingTextBox txtBaseUrl;
        private PasswordTextBox txtPassword;
        private TextBox txtReleaseField;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollabNetTrackerProviderEditor"/> class.
        /// </summary>
        public BugzillaProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            EnsureChildControls();

            var provider = (BugzillaProvider)extension;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtReleaseField.Text = provider.ReleaseField ?? string.Empty;
        }

        public override ProviderBase CreateFromForm()
        {
            EnsureChildControls();

            return new BugzillaProvider()
            {
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                BaseUrl = this.txtBaseUrl.Text,
                ReleaseField = this.txtReleaseField.Text
            };
        }

        protected override void CreateChildControls()
        {
            txtUserName = new ValidatingTextBox();
            txtBaseUrl = new ValidatingTextBox()
            {
                Width = 300
            };
            
            txtPassword = new PasswordTextBox();
            txtReleaseField = new TextBox()
            {
                Text = "cf_release"
            };

            CUtil.Add(this,
                new FormFieldGroup("Bugzilla Server URL",
                    "The URL of the Bugzilla XML-RPC server, for example: http://bugzilla/xmlrpc.cgi",
                    false,
                    new StandardFormField(
                        "Server URL:",
                        txtBaseUrl)
                    ),
                new FormFieldGroup("Authentication",
                    "Provide a username and password to connect to the Bugzilla service.",
                    false,
                    new StandardFormField(
                        "User Name:",
                        txtUserName),
                    new StandardFormField(
                        "Password:",
                        txtPassword)
                    ),
                new FormFieldGroup("Configuration",
                    "Additional configuration options.",
                    false,
                    new StandardFormField(
                        "Bug Release Field:",
                        txtReleaseField)
                    )
            );

            base.CreateChildControls();
        }
    }
}
