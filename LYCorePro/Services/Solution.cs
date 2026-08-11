using LYCorePro.Common.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYCorePro.Services
{
    public class Solution : NotifyPropertyBase
    {
        #region 单例模式
        private static Solution? _Instance = null;

        public Solution() { }

        public static Solution Ins
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new Solution();
                }
                return _Instance;
            }
            set { _Instance = value; }
        }
        #endregion

    }
}
