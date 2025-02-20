using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
	public static class MuByteHelper
    {
		public static unsafe void WriteTo(this byte[] bytes, int offset, ulong num)
		{
			byte* bPoint = (byte*)&num;
			for (int i = 0; i < sizeof(long); ++i)
			{
				bytes[offset + i] = bPoint[i];
			}
		}
	}
}
