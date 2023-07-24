using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lucky44.Placer
{
    public class LuckyPlacer
    {
        #region Singleton
        private static LuckyPlacer instance = null;
        private static readonly object padlock = new object();

        public static LuckyPlacer Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                        instance = new LuckyPlacer();
                    return instance;
                }
            }
        }
        #endregion

        public Dictionary<string, string[]> categories;

        public GameObject selected;
        public GameObject previewModel;

        public LuckyPlacer()
        {
            categories = new Dictionary<string, string[]>();
        }

        public void setSelected(GameObject g)
        {
            if (previewModel != null)
                Object.DestroyImmediate(previewModel);

            selected = g;
            previewModel = GameObject.Instantiate(selected);
            previewModel.name = "LUCKYPLACRPREVIEW";
            previewModel.hideFlags = HideFlags.HideInHierarchy;
            List<Collider> colliders = new List<Collider>();
            colliders.AddRange(previewModel.GetComponents<Collider>());
            colliders.AddRange(previewModel.GetComponentsInChildren<Collider>());

            colliders.ForEach(coll => Object.DestroyImmediate(coll));
        }
    }
}
