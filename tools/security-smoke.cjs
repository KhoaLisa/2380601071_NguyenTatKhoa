const { request } = require('../.tools/browser/node_modules/playwright-core');
const { randomBytes } = require('node:crypto');
(async () => {
  const client = await request.newContext({maxRedirects:0});
  try {
    const production = 'http://127.0.0.1:5002';
    let ready = false;
    for(let i=0;i<90;i++) {
      try { await client.get(production,{timeout:1000}); ready=true; break; }
      catch { await new Promise(resolve=>setTimeout(resolve,500)); }
    }
    if(!ready) throw new Error('Production smoke server did not start.');
    const development = 'http://127.0.0.1:5000';
    const session = await client.post(development+'/api/demo/student-session',{data:{installationId:randomBytes(32).toString('hex')}});
    const token = (await session.json()).result.accessToken;
    const disabled = await client.post(production+'/api/demo/student-session',{data:{installationId:randomBytes(32).toString('hex')}});
    if(disabled.status()!==404) throw new Error('Production demo bootstrap was not disabled: '+disabled.status());
    const denied = await client.get(production+'/api/services/app/StudentExam/GetPublishedExams',{headers:{Authorization:'Bearer '+token}});
    if(denied.status()!==403) throw new Error('Production accepted a demo session: '+denied.status());
    console.log('PASS: Production disables demo bootstrap even with DemoAccess enabled.');
    console.log('PASS: Production rejects a valid Development demo JWT.');
  } finally { await client.dispose(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
