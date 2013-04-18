using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Bugzilla
{
    [Serializable]
    internal sealed class Bug : Issue
    {
        private readonly bool isClosed;

        public Bug(string id, string status, string title, string description, string release, bool isClosed)
            : base(id, status, title, description, release)
        {
            this.isClosed = isClosed;
        }

        public bool IsClosed
        {
            get { return this.isClosed; }
        }
    }
}
