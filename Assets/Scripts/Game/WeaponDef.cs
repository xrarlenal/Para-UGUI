using System.Collections.Generic;
using UnityEngine;

namespace GlassUI.Game
{
    [System.Serializable]
    public class WeaponDef
    {
        public string name = "武器";
        public string kind = "突击步枪";
        public int damage = 18;
        public float fireRate = 0.11f;
        public int magSize = 30;
        public float reloadTime = 1.4f;
        public float spread = 0.6f;
        public int pellets = 1;
        public bool automatic = true;
        public Color accent = new Color(0.28f, 0.55f, 1f);

        // 程序化枪械外观参数
        public float bodyLength = 0.42f;
        public float bodyHeight = 0.09f;
        public float barrelLength = 0.34f;
        public float stockScale = 1f;
    }
    /// <summary>
    /// 武器系统：三把程序化拼出来的枪、切换动画、开火、后坐、换弹、命中反馈。
    /// 第一人称视角下枪挂在相机下面。
    /// </summary>
}
