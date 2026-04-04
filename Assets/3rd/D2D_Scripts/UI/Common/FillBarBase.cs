using UnityEngine;
using UnityEngine.UI;

namespace D2D.UI
{
    public abstract class FillBarBase: MonoBehaviour
    {
        [SerializeField] private Image _image;

        protected virtual void Update()
        {
            _image.fillAmount = Calculate();
        }

        protected abstract float Calculate();

        /// <summary>
        /// 返回填充图片当前填充量终点的世界坐标（水平 Left→Right 填充模式）。
        /// 用于 XP 特效飞往经验条当前进度位置。
        /// </summary>
        public Vector3 GetFillEndWorldPosition()
        {
            if (_image == null) return transform.position;

            RectTransform rt = _image.rectTransform;
            Rect rect = rt.rect;
            float localX = rect.xMin + _image.fillAmount * rect.width;
            float localY = rect.center.y;
            return rt.TransformPoint(new Vector3(localX, localY, 0f));
        }
    }
}