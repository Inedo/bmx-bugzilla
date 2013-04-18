using System;
using System.Collections.Generic;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Bugzilla
{
    /// <summary>
    /// Represents a Bugzilla product.
    /// </summary>
    [Serializable]
    internal sealed class Product : CategoryBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Product"/> class.
        /// </summary>
        /// <param name="args">The return values from Bugzilla.Product.get.</param>
        public Product(Dictionary<string, object> args)
            : base((args["name"] ?? string.Empty).ToString(), (args["name"] ?? string.Empty).ToString(), null)
        {
        }
    }
}
