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

        // 将十六进制字符串转换为字节数组的函数
        public static byte[] FormatBytes(this string hexString)
        {
            string[] hexValues = hexString.Split(' ');
            byte[] bytes = new byte[hexValues.Length];
            for (int i = 0; i < hexValues.Length; i++)
            {
                var v = hexValues[i];
                if (v.Length == 0) continue;
                bytes[i] = Convert.ToByte(v, 16); // 16 表示十六进制
            }
            return bytes;
        }
    }
}
