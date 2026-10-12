import json, subprocess, sys
from pathlib import Path
sys.stdout.reconfigure(encoding="utf-8")
method=sys.argv[1]; request=Path(sys.argv[2]); output=request.with_name(request.stem.replace('_request','')+'.json')
subprocess.run([sys.executable,'Evidence/UpgradeCurrency_20261009/rpc.py',method,'@'+str(request),str(output)],check=True,stdout=subprocess.DEVNULL)
d=json.loads(output.read_text(encoding='utf-8'))['result'];d=d.get('structuredContent',d);d=d.get('result',d)
if 'contents' in d: d=json.loads(d['contents'][0]['text'])
if 'data' in d and isinstance(d['data'],dict) and 'progress' in d['data']:
 data=d['data'];print(json.dumps({'status':data['status'],'progress':data['progress'],'summary':(data.get('result') or {}).get('summary'),'error':data.get('error')},ensure_ascii=False))
else: print(json.dumps(d,ensure_ascii=False)[:5000])
