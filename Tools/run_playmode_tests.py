import time
import json
from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
print("Starting PlayMode test run...")
res = c.call_tool("run_tests", {"mode": "PlayMode", "include_failed_tests": True, "init_timeout": 120000})
print("Start result:", res)

job_id = None
if isinstance(res, dict) and "data" in res and res["data"] and "job_id" in res["data"]:
    job_id = res["data"]["job_id"]
elif isinstance(res, dict) and "result" in res and "data" in res["result"] and res["result"]["data"] and "job_id" in res["result"]["data"]:
    job_id = res["result"]["data"]["job_id"]

if job_id:
    print("Job ID:", job_id)
    for _ in range(30):
        time.sleep(3)
        job_res = c.call_tool("get_test_job", {"job_id": job_id})
        status = job_res.get("data", {}).get("status") if isinstance(job_res, dict) else None
        print("Polling PlayMode job status:", status)
        if status in ["succeeded", "failed"]:
            print("Final PlayMode test job result:\n", json.dumps(job_res, indent=2))
            break
