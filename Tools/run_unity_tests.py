import time
from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
print("Starting EditMode test run...")
res = c.call_tool("run_tests", {"mode": "EditMode", "include_failed_tests": True})
print("Start result:", res)

job_id = None
if isinstance(res, dict) and "data" in res and res["data"] and "job_id" in res["data"]:
    job_id = res["data"]["job_id"]
elif isinstance(res, dict) and "result" in res and "data" in res["result"] and res["result"]["data"] and "job_id" in res["result"]["data"]:
    job_id = res["result"]["data"]["job_id"]

if job_id:
    print("Job ID:", job_id)
    for _ in range(30):
        time.sleep(2)
        job_res = c.call_tool("get_test_job", {"job_id": job_id})
        status = job_res.get("data", {}).get("status") if isinstance(job_res, dict) else None
        print("Polling job status:", status)
        if status in ["Completed", "Failed"]:
            print("Final test job result:", job_res)
            break
else:
    print("Could not get job_id from response:", res)
