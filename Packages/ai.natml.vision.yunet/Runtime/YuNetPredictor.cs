/* 
*   YuNet
*   Copyright (c) 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Vision {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEngine;
    using NatML.Features;
    using NatML.Internal;
    using NatML.Types;

    /// <summary>
    /// High speed face detection from libfacedetection.
    /// This predictor accepts an image feature and produces a list of face rectangles.
    /// Face rectangles are always specified in normalized coordinates.
    /// </summary>
    public sealed class YuNetPredictor : IMLPredictor<Rect[]> {

        #region --Client API--
        /// <summary>
        /// YuNet predictor tag.
        /// </summary>
        public const string Tag = @"@natsuite/yunet";

        /// <summary>
        /// Detect faces in an image.
        /// </summary>
        /// <param name="inputs">Input image.</param>
        /// <returns>Detected faces.</returns>
        public Rect[] Predict (params MLFeature[] inputs) {
            // Check
            if (inputs.Length != 1)
                throw new ArgumentException(@"YuNet predictor expects a single feature", nameof(inputs));
            // Check type
            var input = inputs[0];
            var imageType = MLImageType.FromType(input.type);
            var imageFeature = input as MLImageFeature;
            if (!imageType)
                throw new ArgumentException(@"YuNet predictor expects an an array or image feature", nameof(inputs));
            // Apply normalization
            if (imageFeature != null) {
                (imageFeature.mean, imageFeature.std) = model.normalization;
                imageFeature.aspectMode = model.aspectMode;
            }
            // Predict
            using var inputFeature = (input as IMLEdgeFeature).Create(inputType);
            using var outputFeatures = model.Predict(inputFeature);
            // Marshal
            var offsets = new MLArrayFeature<float>(outputFeatures[0]);             // (P,14)
            var confidenceScores = new MLArrayFeature<float>(outputFeatures[1]);    // (P,2)
            var iouScores = new MLArrayFeature<float>(outputFeatures[2]);           // (P,1)
            var candidateBoxes = new List<Rect>();
            var candidateScores = new List<float>();
            for (int i = 0, ilen = offsets.shape[0]; i < ilen; ++i) {
                // Check
                var confidenceScore = confidenceScores[i,1];
                var iouScore = iouScores[i,0];
                var score = Mathf.Sqrt(confidenceScore * Mathf.Clamp01(iouScore));
                if (score < minScore)
                    continue;
                // Decode
                var offset0 = new Vector2(offsets[i,0], offsets[i,1]);
                var offset1 = new Vector2(offsets[i,2], offsets[i,3]);
                var offset2 = Vector2.Scale(offset1, AnchorVariance);
                var offset2e = new Vector2(Mathf.Exp(offset2.x), Mathf.Exp(offset2.y));
                var min = anchors[i].min + AnchorVariance[0] * Vector2.Scale(offset0, anchors[i].size);
                var minInv = new Vector2(min.x, 1f - min.y);
                var size = Vector2.Scale(anchors[i].size, offset2e);
                var rawBox = new Rect(minInv - size / 2f, size);
                var box = imageFeature?.TransformRect(rawBox, inputType) ?? rawBox;
                // Add
                candidateBoxes.Add(box);
                candidateScores.Add(score);
            }
            var keepIdx = MLImageFeature.NonMaxSuppression(candidateBoxes, candidateScores, maxIoU);
            var result = keepIdx.Select(i => candidateBoxes[i]).ToArray();
            // Return
            return result;
        }

        /// <summary>
        /// Dispose the predictor and release resources.
        /// </summary>
        public void Dispose () => model.Dispose();

        /// <summary>
        /// Create the YuNet face predictor.
        /// </summary>
        /// <param name="minScore">Minimum candidate score.</param>
        /// <param name="maxIoU">Maximum intersection-over-union score for overlap removal.</param>
        /// <param name="configuration">Edge model configuration.</param>
        /// <param name="accessKey">NatML access key.</param>
        public static async Task<YuNetPredictor> Create (
            float minScore = 0.5f,
            float maxIoU = 0.5f,
            MLEdgeModel.Configuration configuration = null,
            string accessKey = null
        ) {
            var model = await MLEdgeModel.Create(Tag, configuration, accessKey);
            var predictor = new YuNetPredictor(model, minScore, maxIoU);
            return predictor;
        }
        #endregion


        #region --Operations--
        private readonly MLEdgeModel model;
        private readonly float minScore;
        private readonly float maxIoU;
        private readonly MLImageType inputType;
        private readonly Rect[] anchors;
        private static readonly int[][] MinAnchorSizes = new [] {
            new [] { 10, 16, 24 },
            new [] { 32, 48 },
            new [] { 64, 96 },
            new [] { 128, 192, 256 }
        };
        private static readonly int[] AnchorStrides = new [] { 8, 16, 32, 64 };
        private static readonly Vector2 AnchorVariance = new Vector2(0.1f, 0.2f);

        private YuNetPredictor (MLEdgeModel model, float minScore = 0.5f, float maxIoU = 0.5f) {
            this.model = model;
            this.minScore = minScore;
            this.maxIoU = maxIoU;
            this.inputType = model.inputs[0] as MLImageType;
            this.anchors = GenerateAnchors(inputType.width, inputType.height);
        }

        private static Rect[] GenerateAnchors (int width, int height) {
            // Compute strides
            var maps = new List<(int w, int h)> { ((width + 1) >> 2, (height + 1) >> 2) };
            for (var i = 0; i < 4; ++i)
                maps.Add((maps[i].w >> 1, maps[i].h >> 1));
            // Generate anchors
            var anchors = new List<Rect>();
            for (var i = 0; i < MinAnchorSizes.Length; ++i) {
                var minSizes = MinAnchorSizes[i];
                var stride = AnchorStrides[i];
                var (w, h) = maps[i + 1];
                for (var y = 0; y < h; ++y)
                    for (var x = 0; x < w; ++x)
                        foreach (var s in minSizes) {
                            var sizeX = (float)s / width;
                            var sizeY = (float)s / height;
                            var centerX = (x + 0.5f) * (float)stride / width;
                            var centerY = (y + 0.5f) * (float)stride / height;
                            var anchor = new Rect(centerX, centerY, sizeX, sizeY);
                            anchors.Add(anchor);
                        }
            }
            return anchors.ToArray();
        }
        #endregion
    }
}