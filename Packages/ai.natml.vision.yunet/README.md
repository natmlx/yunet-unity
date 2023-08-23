# YuNet
[YuNet](https://github.com/ShiqiYu/libfacedetection) high performance face detection from `libfacedetection`.

## Installing YuNet
Add the following items to your Unity project's `Packages/manifest.json`:
```json
{
  "scopedRegistries": [
    {
      "name": "NatML",
      "url": "https://registry.npmjs.com",
      "scopes": ["ai.natml"]
    }
  ],
  "dependencies": {
    "ai.natml.vision.yunet": "1.0.3"
  }
}
```

## Detecting Faces in an Image
First, create the YuNet predictor:
```csharp
// Create the YuNet predictor
var predictor = await YuNetPredictor.Create();
```

Then detect faces in the image:
```csharp
// Create image feature
Texture2D image = ...;
// Detect faces
Rect[] faces = predictor.Predict(image);
```

___

## Requirements
- Unity 2022.3+

## Quick Tips
- Join the [NatML community on Discord](https://hub.natml.ai/community).
- Discover more ML models on [NatML Hub](https://hub.natml.ai).
- See the [NatML documentation](https://docs.natml.ai/unity).
- Contact us at [hi@natml.ai](mailto:hi@natml.ai).

Thank you very much!