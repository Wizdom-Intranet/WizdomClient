using System;

namespace Wizdom.Client
{
    public class Environment
    {
        public string spHostURL { get; set; }
        public string appUrl { get; set; }
        public string blobUrl { get; set; }
        public string clientId { get; set; }
        public Version wizdomVersion { get; set; }
        public Principal currentPrincipal { get; set; }
    }

    public class Version
    {
        public int major { get; set; }
        public int minor { get; set; }
        public int build { get; set; }
        public int revision { get; set; }
        public override string ToString()
        {
            return this.version.ToString();
        }
        public System.Version version
        {
            get
            {
                return new System.Version(major, minor, build, revision);
            }
        }
    }

}
