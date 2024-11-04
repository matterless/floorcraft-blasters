using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class SkewedImage : Image
    {
        public float skewX;
        public float skewY;
        public bool mirrorX;
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
         
            var height = rectTransform.rect.height;
            var width = rectTransform.rect.width;
            var xskew = height * Mathf.Tan(Mathf.Deg2Rad * skewX);
            var yskew = width * Mathf.Tan(Mathf.Deg2Rad * skewY);

            var ymin = rectTransform.rect.yMin;
            var xmin = rectTransform.rect.xMin;
            
            UIVertex v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);

                if (mirrorX)
                {
                    if (v.position.x > 0)
                        v.position += new Vector3(Mathf.Lerp(0, xskew, (v.position.y - ymin) / height), Mathf.Lerp(0, yskew, (v.position.x - xmin) / width), 0);
                    else
                        v.position -= new Vector3(Mathf.Lerp(0, xskew, (v.position.y - ymin) / height), Mathf.Lerp(0, yskew, (v.position.x - xmin) / width), 0);    
                }
                else
                {
                    v.position += new Vector3(Mathf.Lerp(0, xskew, (v.position.y - ymin) / height), Mathf.Lerp(0, yskew, (v.position.x - xmin) / width), 0);
                }
                
                vh.SetUIVertex(v, i);
            }

        }
    }
}
