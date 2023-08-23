/* 
*   YuNet
*   Copyright (c) 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using System.Threading.Tasks;
    using UnityEngine;
    using NatML.Vision;
    using NatML.Visualizers;
    using VideoKit;

    public sealed class YuNetSample : MonoBehaviour {

        [Header(@"Camera")]
        public VideoKitCameraManager cameraManager;

        [Header(@"UI")]
        public YuNetVisualizer visualizer;

        private YuNetPredictor predictor;

        private async void Start () {
            // Create predictor
            predictor = await YuNetPredictor.Create();
            // Listen for frames
            cameraManager.OnCameraFrame.AddListener(OnCameraFrame);
        }

        private void OnCameraFrame (CameraFrame frame) {
            // Predict faces
            var faces = predictor.Predict(frame);
            // Visualize
            visualizer.Render(faces);
        }

        void OnDisable () {
            // Stop listening for frames
            cameraManager.OnCameraFrame.RemoveListener(OnCameraFrame);
            // Dispose predictor
            predictor.Dispose();
        }
    }
}