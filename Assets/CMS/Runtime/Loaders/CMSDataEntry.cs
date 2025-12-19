using System.Linq;
using UnityEngine;
using CMS;

namespace CMS.Loaders
{
    public class CMSDataEntry
    {
        public static string RootPath = "CMS";
        public static string ManifestName = "manifest";
        
        public void Load()
        {
            var allTextAssets = Resources.LoadAll<TextAsset>(RootPath);
            var manifests = allTextAssets
                .Where(x => ManifestName.Equals(x.name))
                .Select(x => JsonUtility.FromJson<ManifestDto>(x.text));
            
            var loadContext = new LoadContext();
            
            foreach (var manifest in manifests)
            {
                foreach (var dataLoaders in manifest.Data)
                {
                    loadContext.RootPath = $"{RootPath}/{manifest.Path}";
                    loadContext.DataName = dataLoaders.Key;
                    var loaders = CMSLoadersFactory.Instance(dataLoaders.Loader);
                    var result = loaders.LoadBase(loadContext);
                    CMSEntry.CMSDataCollection.AddTo(result.GetType(), result);
                }
            }
        }
    }
}