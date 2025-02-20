using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public static class MuTimeHelper
    {
        public static bool Check(ref long time,int Interval)
        {
            bool bRet = false;
            var nNow = TimeInfo.Instance.ClientFrameTime();
            if (time == nNow) return bRet;
            if ((nNow - time) > (long)Interval)
            {
                time = nNow;
                bRet = true;
            }
            return bRet;
        }
    }
}
