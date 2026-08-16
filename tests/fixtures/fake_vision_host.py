import json, sys, time

time.sleep(.6)
print(json.dumps({"ready": True}), flush=True)

for line in sys.stdin:
    request = json.loads(line)
    print(json.dumps({"id": request["id"], "poses": [[{"x": .3, "y": .4, "visibility": .9}]]}), flush=True)
