using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altairis.BlobCdn.Configuration {

    public class BlobCdnSection : ConfigurationSection {

        [ConfigurationProperty("storage")]
        public StorageElement Storage {
            get { return (StorageElement)this["storage"]; }
            set { this["storage"] = value; }
        }

        [ConfigurationProperty("caching")]
        public CachingElement Caching {
            get { return (CachingElement)this["caching"]; }
            set { this["caching"] = value; }
        }

    }

}
