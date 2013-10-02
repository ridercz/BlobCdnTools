using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Altairis.BlobCdn.Configuration {
    public class StorageElement : ConfigurationElement {

        [ConfigurationProperty("connectionStringName", IsRequired = false, DefaultValue = "UseDevelopmentStorage=true")]
        public string ConnectionStringName {
            get { return (string)this["connectionStringName"]; }
            set { this["connectionStringName"] = value; }
        }

        [ConfigurationProperty("containerName", IsRequired = false, DefaultValue = "cdn")]
        public string ContainerName {
            get { return (string)this["containerName"]; }
            set { this["containerName"] = value; }
        }

    }
}
