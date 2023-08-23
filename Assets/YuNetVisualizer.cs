/* 
*   YuNet
*   Copyright (c) 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Visualizers {

    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using VideoKit.UI;

    /// <summary>
    /// </summary>
    [RequireComponent(typeof(VideoKitCameraView))]
    public sealed class YuNetVisualizer : MonoBehaviour {

        #region --Inspector--
        public Image faceRect;
        #endregion


        #region --Client API--
        /// <summary>
        /// Render a set of detected faces.
        /// </summary>
        /// <param name="faces">Faces to render.</param>
        public void Render (params Rect[] faces) {
            // Delete current
            foreach (var rect in currentRects)
                GameObject.Destroy(rect.gameObject);
            currentRects.Clear();       
            // Render rects
            var imageRect = new Rect(0, 0, rawImage.texture.width, rawImage.texture.height);
            foreach (var face in faces) {
                var prefab = Instantiate(faceRect, transform);
                prefab.gameObject.SetActive(true);
                Render(prefab, face, imageRect);                
                currentRects.Add(prefab);
            }
        }
        #endregion


        #region --Operations--
        private RawImage rawImage;
        private AspectRatioFitter aspectFitter;
        private readonly List<Image> currentRects = new List<Image>();

        void Awake () {
            rawImage = GetComponent<RawImage>();
            aspectFitter = GetComponent<AspectRatioFitter>();
        }

        void Render (Image prefab, Rect faceRect, Rect frameRect) {
            var rectTransform = prefab.transform as RectTransform;
            var imageTransform = rawImage.transform as RectTransform;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.pivot = Vector2.zero;
            rectTransform.sizeDelta = Vector2.Scale(imageTransform.rect.size, faceRect.size);
            rectTransform.anchoredPosition = Rect.NormalizedToPoint(imageTransform.rect, faceRect.position);
        }
        #endregion
    }
}