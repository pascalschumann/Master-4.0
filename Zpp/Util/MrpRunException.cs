using System;

namespace Zpp.Util
{
    public class MrpRunException : SystemException
    {
        public MrpRunException(string message) : base(message)
        {
            DebuggingTools.PrintStateToFiles(ZppConfiguration.CacheManager.GetDbTransactionData(),
                true);
        }
    }
}