using System;
using System.Collections.Generic;

namespace CMS.Loaders
{
    [Serializable]
    public class ManifestDataEntry
    {
        public string Key;
        public string Loader;
    }

    [Serializable]
    public class ManifestDto
    {
        public string Id;
        public string Path;
        public List<ManifestDataEntry> Data;
    }
}