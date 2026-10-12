import time
import json
from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
res = c.call_tool("run_tests", {"mode": "PlayMode", "group_names": ["CayTheGioi.Tests.WeatherSceneTests"], "include_failed_tests": True, "init_timeout": 120000})
job_id = res.get("data", {}).get("job_id")
print("Job ID:", job_id)
for _ in range(40):
    time.sleep(3)
    job_res = c.call_tool("get_test_job", {"job_id": job_id})
    status = job_res.get("data", {}).get("status")
    progress = job_res.get("data", {}).get("progress", {})
    print(f"Status: {status}, progress: {progress.get('completed')}/{progress.get('total')}")
    if status in ["succeeded", "failed"]:
        print("Final:\n", json.dumps(job_res, indent=2))
        break
