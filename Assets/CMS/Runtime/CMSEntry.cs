using CMS.Loaders;
using CMS;

namespace CMS
{
    public static class CMSEntry
    {
        public static CMSDataCollection CMSDataCollection = new CMSDataCollection();

        public static void Launch()
        {
            var dataEntry = new CMSDataEntry();
            dataEntry.Load();
        }
    }
}
