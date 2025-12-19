using UnityEngine;
using CMS.CMSData;

namespace CMS.Loaders
{
    public abstract class CMSLoader<T> : CMSBaseLoader where T : CMSRootData
    {
        public T Load(LoadContext ctx)
        {
            var data = Resources.Load<TextAsset>($"{ctx.RootPath}/{ctx.DataName}");
            var rootData = JsonUtility.FromJson<T>(data.text);
            return rootData;
        }

        public override CMSRootData LoadBase(LoadContext ctx)
        {
            return Load(ctx);
        }
    }
}