import base64, json, sys
import mediapipe as mp
import numpy as np

BaseOptions = mp.tasks.BaseOptions
PoseLandmarker = mp.tasks.vision.PoseLandmarker
Options = mp.tasks.vision.PoseLandmarkerOptions
RunningMode = mp.tasks.vision.RunningMode

options = Options(base_options=BaseOptions(model_asset_path=sys.argv[1]), running_mode=RunningMode.IMAGE,
                  num_poses=4, min_pose_detection_confidence=.5, min_pose_presence_confidence=.5,
                  min_tracking_confidence=.5, output_segmentation_masks=False)

with PoseLandmarker.create_from_options(options) as detector:
    print(json.dumps({"ready": True}), flush=True)
    for line in sys.stdin:
        request = {"id": -1}
        try:
            request = json.loads(line)
            pixels = np.frombuffer(base64.b64decode(request["rgb"]), dtype=np.uint8)
            pixels = pixels.reshape((request["height"], request["width"], 3))
            result = detector.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=pixels))
            poses = [[{"x": p.x, "y": p.y, "visibility": p.visibility} for p in pose]
                     for pose in result.pose_landmarks]
            response = {"id": request["id"], "poses": poses}
        except Exception as error:
            response = {"id": request.get("id", -1), "error": str(error), "poses": []}
        print(json.dumps(response), flush=True)
