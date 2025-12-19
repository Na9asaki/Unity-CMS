namespace Source.CMS.CMSData
{
    public abstract class CMSRootData
    {
        public T As<T>() where T : CMSRootData
        {
            return (T)this;
        }
    }
}