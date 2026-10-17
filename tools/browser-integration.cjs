const { chromium } = require('../.tools/browser/node_modules/playwright-core');
const fs = require('node:fs');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const evidence = path.resolve(__dirname, '../docs/testing/evidence');
const statePath = path.join(evidence, 'browser-state.json');
const state = JSON.parse(fs.readFileSync(statePath, 'utf8'));
const origin = 'http://127.0.0.1:5000';
function requireTrue(value, message) {
  if (!value) throw new Error(message);
  console.log('PASS:', message);
}
async function go(page, target) { await page.goto(origin + target, {waitUntil:'networkidle'}); }
async function submit(page, button) {
  await Promise.all([page.waitForNavigation({waitUntil:'networkidle'}), button.click()]);
}
(async () => {
  const browser = await chromium.launch({executablePath:'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',headless:true,
    args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  let activeStudent;
  const errors = [];
  const trace = [];
  try {
    const context = await browser.newContext({viewport:{width:1440,height:1000}});
    const page = await context.newPage();
    page.on('pageerror',error=>errors.push(error.message));
    await go(page, '/Exams/Import');
    await page.locator('input[type=file]').setInputFiles({name:'invalid.docx',mimeType:'application/vnd.openxmlformats-officedocument.wordprocessingml.document',buffer:Buffer.from([1,2,3])});
    await submit(page,page.getByRole('button',{name:'Kiểm tra tệp',exact:true}));
    await page.getByRole('heading',{name:'Cần điều chỉnh tài liệu'}).waitFor();
    requireTrue(true,'invalid Word displays validation errors');
    await page.screenshot({path:path.join(evidence,'mvc-invalid-word.png')});
    if (!state.uiExamId) {
      await go(page, '/Exams/Details/'+state.examId);
      await page.locator('details.draft-copy summary').click();
      state.uiCode = 'E2E-UI-'+randomUUID().replaceAll('-','');
      await page.locator('#copy-code').fill(state.uiCode);
      await page.locator('#copy-title').fill('E2E Browser — Reading v07');
      await submit(page,page.getByRole('button',{name:'Tạo bản sao Draft',exact:true}));
      state.uiExamId = page.url().split('/').pop();
      fs.writeFileSync(statePath,JSON.stringify(state,null,2));
    }
    await go(page,'/Exams/Edit/'+state.uiExamId);
    const detail = async () => (await (await page.request.get(origin+'/api/services/app/ExamManagement/Get?id='+state.uiExamId)).json()).result;
    let exam = await detail();
    if (exam.status === 0) {
      await page.locator('#exam-title').fill('E2E Browser — Reading v07 (edited)');
      await submit(page,page.getByRole('button',{name:'Lưu thông tin',exact:true}));
      await page.locator('#edit-question-101 summary').click();
      const qForm = page.locator('#edit-question-101 form');
      await qForm.locator('[name=Hint]').fill('E2E browser test hint — isolated copy only.');
      await qForm.locator('input[type=radio]').nth(1).check();
      await submit(page,page.getByRole('button',{name:'Lưu câu 101',exact:true}));
      exam = await detail();
      requireTrue(exam.questions.find(q=>q.originalNumber===101).hint.startsWith('E2E browser'), 'MVC question edit is persisted in SQL');
      const firstImage = exam.groups.flatMap(g=>g.contentBlocks).find(b=>b.type===2);
      const imageResponse = await page.request.get(origin+firstImage.contentUrl);
      requireTrue(imageResponse.status()===200,'lecturer can read source image');
      const imageForm = page.locator('form[action="/Exams/ReplaceImage"]').filter({has:page.locator(`input[value="${firstImage.id}"]`)});
      await imageForm.locator('input[type=file]').setInputFiles({name:'browser-verified.png',mimeType:'image/png',buffer:await imageResponse.body()});
      await imageForm.locator('..').evaluate(node=>{if(node.tagName==='DETAILS')node.open=true; else node.closest('details').open=true;});
      await submit(page,imageForm.getByRole('button',{name:'Lưu ảnh thay thế'}));
      const updatedImage = (await detail()).groups.flatMap(g=>g.contentBlocks).find(b=>b.id===firstImage.id);
      requireTrue(updatedImage.fileName==='browser-verified.png' && updatedImage.sourceBlockId===firstImage.sourceBlockId,'MVC image replacement preserves source links');
      await page.screenshot({path:path.join(evidence,'mvc-editor.png')});
      await go(page,'/Exams/Publish/'+state.uiExamId);
      await page.getByText('Cấu trúc đề đã vượt qua kiểm tra.').waitFor();
      await page.screenshot({path:path.join(evidence,'mvc-publish-confirmation.png')});
      await submit(page,page.getByRole('button',{name:'Xác nhận xuất bản',exact:false}));
    }
    requireTrue((await detail()).status===1,'MVC publishes the isolated UI test exam');
    await go(page,'/Exams/Statistics/'+state.uiExamId);
    await page.screenshot({path:path.join(evidence,'mvc-statistics-before.png')});
    const mobile = await browser.newContext({viewport:{width:390,height:844}});
    const student = await mobile.newPage();
    activeStudent = student;
    student.on('pageerror',error=>errors.push(error.message));
    student.on('response',r=>{if(r.url().includes('/StudentExam/Start')) {
      console.log('START HTTP',r.status(),r.request().method());
      r.json().then(body=>{if(body.result?.attemptId) {
        state.uiAttemptIds = [...new Set([...(state.uiAttemptIds || []),body.result.attemptId])];
        fs.writeFileSync(statePath,JSON.stringify(state,null,2));
      }}).catch(()=>{});
    }});
    student.on('console',message=>trace.push(message.type()+': '+message.text()));
    const sessionPromise = student.waitForResponse(r=>r.url().endsWith('/api/demo/student-session') && r.request().method()==='POST');
    await student.goto('http://127.0.0.1:5200');
    const placeholder = student.locator('flt-semantics-placeholder');
    await placeholder.waitFor({state:'attached',timeout:60000});
    await placeholder.evaluate(node=>node.click());
    await student.getByText('TOEICFlow',{exact:true}).waitFor({timeout:60000});
    await student.screenshot({path:path.join(evidence,'flutter-exam-list.png')});
    const session = (await (await sessionPromise).json()).result;
    state.uiUserIds = [...new Set([...(state.uiUserIds || []), session.userId])];
    fs.writeFileSync(statePath,JSON.stringify(state,null,2));
    const studentHeaders = {Authorization:'Bearer '+session.accessToken};
    const forbidden = await student.request.get(origin+'/api/services/app/ExamManagement/GetAll',{headers:studentHeaders});
    requireTrue(forbidden.status()===403,'student session cannot read lecturer answers');
    const deniedUsers = await student.request.get(origin+'/api/services/app/User/GetAll',{headers:studentHeaders});
    requireTrue(deniedUsers.status()===403,'student session cannot access ABP user administration');
    const deniedOrigin = await student.request.post(origin+'/api/demo/student-session',{headers:{Origin:'https://untrusted.example'},data:{installationId:'a'.repeat(64)}});
    requireTrue(deniedOrigin.status()===403,'untrusted browser origin cannot bootstrap demo sessions');
    const cors = await student.request.fetch(origin+'/api/demo/student-session',{method:'OPTIONS',headers:{Origin:'http://127.0.0.1:5200','Access-Control-Request-Method':'POST','Access-Control-Request-Headers':'content-type'}});
    requireTrue(cors.headers()['access-control-allow-origin']==='http://127.0.0.1:5200','CORS allows the configured Flutter origin only');
    await student.getByRole('group',{name:new RegExp(state.uiCode)}).getByRole('button',{name:'Xem đề thi',exact:true}).click();
    await student.getByText('Thông tin bài thi',{exact:true}).waitFor();
    await student.screenshot({path:path.join(evidence,'flutter-instructions.png')});
    const [startResponse, attemptResponse] = await Promise.all([
      student.waitForResponse(r=>r.url().endsWith('/StudentExam/Start') && r.request().method()==='POST',{timeout:75000}),
      student.waitForResponse(r=>r.url().includes('/StudentExam/GetAttempt') && r.request().method()==='GET',{timeout:75000}),
      student.getByRole('button',{name:'Bắt đầu / tiếp tục bài thi',exact:true}).click()
    ]);
    state.attemptId = (await startResponse.json()).result.attemptId;
    fs.writeFileSync(statePath,JSON.stringify(state,null,2));
    const attempt = (await attemptResponse.json()).result;
    fs.writeFileSync(path.join(evidence,'browser-current-attempt.json'),JSON.stringify(attempt,null,2));
    state.uiAttemptIds = [...new Set([...(state.uiAttemptIds || []), state.attemptId])];
    fs.writeFileSync(statePath,JSON.stringify(state,null,2));
    requireTrue(attempt.questions.length===100 && attempt.groups.length===19,'Flutter loads the actual 100-question exam from SQL');
    requireTrue(!JSON.stringify(attempt).match(/"(hint|isCorrect|correctOptionId)"/i),'active student response contains no answer key or hints');
    await student.getByRole('button',{name:'Nộp bài',exact:true}).waitFor();
    await student.screenshot({path:path.join(evidence,'flutter-part5.png')});
    const save = async action => {
      const response = student.waitForResponse(r=>r.url().endsWith('/StudentExam/SaveAnswer'));
      await action();
      requireTrue((await (await response).json()).result.accepted,'answer action is accepted by SQL-backed API');
    };
    await save(()=>student.getByRole('checkbox').nth(0).click());
    await save(()=>student.getByRole('checkbox').nth(1).click());
    await save(()=>student.getByRole('button',{name:'Hủy lựa chọn',exact:true}).click());
    await save(()=>student.getByRole('checkbox').nth(2).click());
    const savedAttempt = (await (await student.request.get(origin+'/api/services/app/StudentExam/GetAttempt?attemptId='+state.attemptId,{headers:studentHeaders})).json()).result;
    requireTrue(savedAttempt.questions[0].selectedOptionId===attempt.questions[0].options[2].id,'save/change/clear/select persists the final answer');
    const safeImage = attempt.groups.flatMap(g=>g.contentBlocks).find(b=>b.type===2);
    const safeImageResponse = await student.request.get(origin+safeImage.contentUrl,{headers:studentHeaders});
    requireTrue(safeImageResponse.status()===200 && safeImageResponse.headers()['content-type'].startsWith('image/png') &&
      safeImageResponse.headers()['x-content-type-options']==='nosniff' &&
      safeImageResponse.headers()['content-security-policy'].includes('sandbox'),
      'authenticated source image is served with verified raster MIME, nosniff and sandbox CSP');
    const otherSession = (await (await student.request.post(origin+'/api/demo/student-session',{data:{installationId:randomUUID().replaceAll('-','')+randomUUID().replaceAll('-','')}})).json()).result;
    const otherRead = await student.request.get(origin+'/api/services/app/StudentExam/GetAttempt?attemptId='+state.attemptId,{headers:{Authorization:'Bearer '+otherSession.accessToken}});
    requireTrue(otherRead.status()===403,'another learner cannot read this attempt');
    await student.reload();
    await student.locator('flt-semantics-placeholder').waitFor({state:'attached'});
    await student.locator('flt-semantics-placeholder').evaluate(node=>node.click());
    await student.getByRole('tab',{name:'Đang làm',exact:true}).click();
    const restoredResponse = student.waitForResponse(r=>r.url().includes('/StudentExam/GetAttempt'));
    await student.getByRole('button',{name:'Tiếp tục làm bài',exact:true}).click();
    const restored = (await (await restoredResponse).json()).result;
    requireTrue(restored.attemptId===attempt.attemptId && restored.dueAtUtc===attempt.dueAtUtc && restored.questions[0].selectedOptionId===savedAttempt.questions[0].selectedOptionId,'refresh restores identity, answer, order and unchanged deadline');
    await student.getByRole('checkbox').nth(2).waitFor();
    const jump = async number => {
      await student.getByRole('button',{name:'Bản đồ câu hỏi',exact:true}).click();
      await student.getByRole('button',{name:String(number),exact:true}).click();
    };
    await jump(131);
    await student.getByText('PART 6',{exact:true}).waitFor();
    await student.getByRole('button',{name:'Phóng to ảnh',exact:true}).first().waitFor();
    await student.screenshot({path:path.join(evidence,'flutter-part6.png')});
    await jump(200);
    await student.getByText('PART 7',{exact:true}).waitFor();
    await student.getByRole('button',{name:'Phóng to ảnh',exact:true}).first().waitFor();
    await student.screenshot({path:path.join(evidence,'flutter-part7-mobile.png')});
    await student.setViewportSize({width:1440,height:1000});
    await student.screenshot({path:path.join(evidence,'flutter-part7-desktop.png')});
    await student.setViewportSize({width:390,height:844});
    await student.getByRole('button',{name:'Nộp bài',exact:true}).click();
    await student.getByRole('button',{name:'Tiếp tục làm',exact:true}).click();
    await student.getByRole('button',{name:'Nộp bài',exact:true}).click();
    const resultResponse = student.waitForResponse(r=>r.url().endsWith('/StudentExam/Submit'));
    await student.getByRole('button',{name:'Xác nhận nộp',exact:true}).click();
    const result = (await (await resultResponse).json()).result;
    requireTrue(result.correctCount+result.incorrectCount===1 && result.skippedCount===99,'Flutter submission stores a correctly partitioned result');
    await student.getByText('Kết quả bài thi',{exact:true}).waitFor();
    await student.screenshot({path:path.join(evidence,'flutter-result.png')});
    const duplicate = (await (await student.request.post(origin+'/api/services/app/StudentExam/Submit',{headers:studentHeaders,data:{attemptId:state.attemptId}})).json()).result;
    requireTrue(duplicate.submittedAtUtc===result.submittedAtUtc,'repeated submit returns the exact stored result');
    await go(page,'/Exams/Statistics/'+state.uiExamId);
    const statistics = (await (await page.request.get(origin+'/api/services/app/ExamManagement/GetStatistics?id='+state.uiExamId)).json()).result;
    requireTrue(statistics.completedAttemptCount>=1,'lecturer statistics include the Flutter submission');
    await page.screenshot({path:path.join(evidence,'mvc-statistics-after.png')});
    state.browserFlowPassed = true;
    state.browserTestResult = result;
    fs.writeFileSync(statePath,JSON.stringify(state,null,2));
    fs.writeFileSync(path.join(evidence,'browser-integration-state.json'),JSON.stringify({state,errors},null,2));
    requireTrue(errors.length===0,'browser has no uncaught page errors');
  } catch(error) {
    if(activeStudent && !activeStudent.isClosed()) {
      await activeStudent.screenshot({path:path.join(evidence,'browser-failure.png')}).catch(()=>{});
      const aria=await activeStudent.locator('body').ariaSnapshot().catch(()=>'<no semantics>');
      fs.writeFileSync(path.join(evidence,'browser-failure-aria.txt'),aria);
      console.error('FAILURE ARIA',aria);
    }
    console.error('PAGE ERRORS',errors,'CONSOLE',trace.slice(-20));
    throw error;
  } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
