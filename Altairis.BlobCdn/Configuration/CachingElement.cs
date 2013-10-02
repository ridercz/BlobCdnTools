using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Altairis.BlobCdn.Configuration {
    public class CachingElement : ConfigurationElement {

        [ConfigurationProperty("expireDays", IsRequired = false, DefaultValue = 365)]
        [IntegerValidator(MinValue = 0, MaxValue = int.MaxValue)]
        public int ExpireDays {
            get { return (int)this["expireDays"]; }
            set { this["expireDays"] = value; }
        }

    }
}
