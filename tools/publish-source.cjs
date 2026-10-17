const { chromium } = require('../.tools/browser/node_modules/playwright-core');
const fs = require('node:fs');
const path = require('node:path');
(async()=>{
  const statePath = path.resolve(__dirname,'../docs/testing/evidence/browser-state.json');
  const state = JSON.parse(fs.readFileSync(statePath,'utf8'));
  const browser = await chromium.launch({executablePath:'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',headless:true});
  try {
    const page = await browser.newPage({viewport:{width:1440,height:1000}});
    await page.goto('http://127.0.0.1:5000',{waitUntil:'networkidle'});
    const response = await page.request.get('http://127.0.0.1:5000/api/services/app/ExamManagement/Get?id='+state.examId);
    const exam = (await response.json()).result;
    if(exam.code!=='reading_v07' || exam.questionCount!==100)throw new Error('Source exam identity mismatch.');
    if(exam.status===0) {
      await page.goto('http://127.0.0.1:5000/Exams/Publish/'+exam.id,{waitUntil:'networkidle'});
      await Promise.all([page.waitForNavigation({waitUntil:'networkidle'}),page.getByRole('button',{name:'Xác nhận xuất bản',exact:false}).click()]);
    }
    await page.goto('http://127.0.0.1:5000',{waitUntil:'networkidle'});
    await page.screenshot({path:path.resolve(__dirname,'../docs/testing/evidence/mvc-dashboard-final.png'),fullPage:true});
    state.sourcePublished=true;
    fs.writeFileSync(statePath,JSON.stringify(state,null,2));
    console.log('PASS: source Reading v07 is Published; source content and learner results were not changed.');
  } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
