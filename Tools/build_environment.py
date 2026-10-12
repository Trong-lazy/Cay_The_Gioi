from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
code = """
CayTheGioi.Editor.TropicalEnvironmentSetup.SetupEnvironment();
return "SETUP_COMPLETED_SUCCESSFULLY";
"""
print("Running TropicalEnvironmentSetup in Unity Editor...")
res = c.call_tool('execute_code', {'action': 'execute', 'code': code, 'safety_checks': False})
print("Result:", res)
