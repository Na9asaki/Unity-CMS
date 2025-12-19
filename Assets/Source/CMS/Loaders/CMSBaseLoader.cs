using Source.CMS.CMSData;

namespace CMS.Loaders
{
    public abstract class CMSBaseLoader
    {
        public abstract CMSRootData LoadBase(LoadContext ctx);
    }
}