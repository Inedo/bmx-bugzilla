using System;
using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Bugzilla
{
    /// <summary>
    /// Bugzilla issue tracker provider.
    /// </summary>
    [ProviderProperties(
      "Bugzilla",
      "Supports Bugzilla 3.6 and later.")]
    [CustomEditor(typeof(BugzillaProviderEditor))]
    public sealed class BugzillaProvider : IssueTrackingProviderBase, ICategoryFilterable, IUpdatingProvider
    {
        private XmlRpc rpc;

        /// <summary>
        /// Initializes a new instance of the <see cref="BugzillaProvider"/> class.
        /// </summary>
        public BugzillaProvider()
        {
        }

        /// <summary>
        /// Gets or sets the URL of the Bugzilla server.
        /// </summary>
        [Persistent]
        public string BaseUrl { get; set; }
        /// <summary>
        /// Gets or sets the user name to use when connecting to the Bugzilla server.
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the password which corresponds to the UserName property.
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the name of the field used to track releases in Bugzilla.
        /// </summary>
        [Persistent]
        public string ReleaseField { get; set; }

        public bool CanAppendIssueDescriptions
        {
            get { return true; }
        }
        public bool CanChangeIssueStatuses
        {
            get { return true; }
        }
        public bool CanCloseIssues
        {
            get { return true; }
        }
        public string[] CategoryIdFilter { get; set; }
        public string[] CategoryTypeNames
        {
            get { return new[] { "Product" }; }
        }

        /// <summary>
        /// Gets the XML-RPC proxy object.
        /// </summary>
        private XmlRpc RPC
        {
            get { return this.rpc ?? (this.rpc = new XmlRpc(new Uri(this.BaseUrl))); }
        }

        /// <summary>
        /// Gets a URL to the specified issue.
        /// </summary>
        /// <param name="issue">The issue whose URL is returned.</param>
        /// <returns>
        /// The URL of the specified issue if applicable; otherwise null.
        /// </returns>
        public override string GetIssueUrl(Issue issue)
        {
            var url = this.BaseUrl.Substring(0, this.BaseUrl.LastIndexOf('/'));
            return string.Format("{0}/show_bug.cgi?id={1}", url, issue.IssueId);
        }
        public override Issue[] GetIssues(string releaseNumber)
        {
            Login();
            try
            {
                var issues = new List<Bug>();

                var searchArgs = new Dictionary<string, object>();

                if(this.CategoryIdFilter != null && this.CategoryIdFilter.Length > 0)
                    searchArgs.Add("product", this.CategoryIdFilter[0]);

                if(!string.IsNullOrEmpty(this.ReleaseField))
                    searchArgs.Add(this.ReleaseField, releaseNumber);

                var bugs = this.RPC.Invoke("Bug.search", searchArgs);
                if (bugs == null)
                    throw new InvalidOperationException("The Bug.search XML-RPC method is not available.");

                var bugIdList = new List<int>();
                foreach (Dictionary<string, object> bug in (System.Collections.IEnumerable)bugs["bugs"])
                    bugIdList.Add((int)bug["id"]);

                Dictionary<string, object> comments;

                try
                {
                    comments = (Dictionary<string, object>)this.RPC.Invoke("Bug.comments", new Dictionary<string, object>()
                    {
                        { "ids", bugIdList }
                    })["bugs"];
                }
                catch
                {
                    comments = null;
                }

                foreach (Dictionary<string, object> bug in (System.Collections.IEnumerable)bugs["bugs"])
                {
                    var id = bug["id"].ToString();
                    var status = bug["status"].ToString();
                    var title = (string)bug["summary"] ?? string.Empty;
                    var description = string.Empty;
                    if (comments != null)
                        description = CombineComments(comments[id]);
                    var isClosed = object.Equals(bug["is_open"], "0");
                    issues.Add(new Bug(id, status, title, description, releaseNumber, isClosed));
                }

                return issues.ToArray();
            }
            finally
            {
                Logout();
            }
        }
        public override bool IsIssueClosed(Issue issue)
        {
            return ((Bug)issue).IsClosed;
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            Dictionary<string, object> result;
            try
            {
                result = this.RPC.Invoke("Bugzilla.version");
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }

            if (result == null || !result.ContainsKey("version"))
                throw new NotAvailableException("Unexpected response from Bugzilla.version method.");
        }
        public override string ToString()
        {
            return "Connects to the Bugzilla issue tracking system.";
        }
        public CategoryBase[] GetCategories()
        {
            Login();
            try
            {
                var ids = this.RPC.Invoke("Product.get_accessible_products");
                var products = this.RPC.Invoke("Product.get", ids);

                var categories = new List<Product>();
                foreach (Dictionary<string, object> product in (System.Collections.IEnumerable)products["products"])
                    categories.Add(new Product(product));

                return categories.ToArray();
            }
            catch
            {
                return new CategoryBase[0];
            }
            finally
            {
                Logout();
            }
        }

        public void AppendIssueDescription(string issueId, string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;

            Login();
            try
            {
                var result = this.RPC.Invoke("Bug.add_comment", new Dictionary<string, object>()
                {
                    { "id", int.Parse(issueId) },
                    { "comment", textToAppend }
                });
            }
            finally
            {
                Logout();
            }
        }
        public void ChangeIssueStatus(string issueId, string newStatus)
        {
            Login();
            try
            {
                this.RPC.Invoke("Bug.update", new Dictionary<string, object>()
                {
                    { "status", newStatus }
                });
            }
            finally
            {
                Logout();
            }
        }
        public void CloseIssue(string issueId)
        {
            ChangeIssueStatus("issueId", "closed");
        }

        private void Login()
        {
            this.RPC.Invoke("User.login", new Dictionary<string, object>() 
            {
                { "login", this.UserName },
                { "password", this.Password },
                { "remember", false }
            });
        }
        private void Logout()
        {
            this.RPC.Invoke("User.logout");
        }
        private string CombineComments(object commentDict)
        {
            var comments = (System.Collections.IEnumerable)((Dictionary<string, object>)commentDict)["comments"];

            var commentTextList = new List<string>();
            foreach (Dictionary<string, object> comment in comments)
            {
                var text = (string)comment["text"];
                if (!string.IsNullOrEmpty(text))
                    commentTextList.Add(text);
            }

            return string.Join(Environment.NewLine, commentTextList.ToArray());
        }
    }
}
