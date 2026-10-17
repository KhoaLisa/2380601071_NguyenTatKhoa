const { chromium } = require('../.tools/browser/node_modules/playwright-core');
const fs = require('node:fs');
const path = require('node:path');

(async () => {
  const evidenceDir = path.resolve(__dirname, '../docs/testing/evidence');
  fs.mkdirSync(evidenceDir, {recursive: true});
  const browser = await chromium.launch({
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    headless: true,
    args: ['--disable-dev-shm-usage', '--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'],
  });
  try {
  const context = await browser.newContext({viewport: {width:1440, height:1000}});
  const page = await context.newPage();
  const failures = [];
  page.on('pageerror', error => failures.push(error.message));
  await page.goto('http://127.0.0.1:5000', {waitUntil:'networkidle'});
  if ((await page.content()).includes('name="Password"')) throw new Error('Lecturer page incorrectly shows login.');
  await page.screenshot({path:path.join(evidenceDir,'mvc-dashboard.png'),fullPage:true});
  console.log('MVC root', await page.title(), 'heading:', await page.locator('h1').innerText());
  await page.goto('http://127.0.0.1:5000/Exams/Import');
  await page.locator('input[type=file]').setInputFiles(path.resolve(__dirname,'../docs/source/TOEIC_Reading_v07_co_tag.docx'));
  await Promise.all([page.waitForLoadState('networkidle'), page.getByRole('button',{name:'Kiểm tra tệp',exact:true}).click()]);
  await page.getByRole('status').filter({hasText:'hợp lệ'}).waitFor({timeout:30000});
  console.log('MVC real Word validate success');
  await page.screenshot({path:path.join(evidenceDir,'mvc-word-validated.png'),fullPage:true});
  await page.locator('input[type=file]').setInputFiles(path.resolve(__dirname,'../docs/source/TOEIC_Reading_v07_co_tag.docx'));
  await Promise.all([page.waitForURL(/\/Exams\/Details\//,{timeout:45000}),page.getByRole('button',{name:'Nhập thành bản nháp',exact:false}).click()]);
  const examId = page.url().split('/').pop().split('?')[0];
  console.log('MVC import/preview',examId);
  await page.waitForLoadState('networkidle');
  await page.locator('.passage-paper img').evaluateAll(images => images.forEach(img=>img.loading='eager'));
  await page.waitForFunction(() => [...document.querySelectorAll('.passage-paper img')].every(img=>img.complete && img.naturalWidth>0), {timeout:30000});
  const brokenImages = await page.locator('.passage-paper img').evaluateAll(images => images.filter(img=>!img.complete || img.naturalWidth===0).length);
  if (brokenImages) throw new Error(`${brokenImages} passage images failed to load.`);
  await page.screenshot({path:path.join(evidenceDir,'mvc-preview.png')});
  const stateFile=path.join(evidenceDir,'browser-state.json');
  const previous=fs.existsSync(stateFile)?JSON.parse(fs.readFileSync(stateFile,'utf8')):{};
  fs.writeFileSync(stateFile,JSON.stringify({...previous,examId,failures},null,2));
  if (failures.length) throw new Error(failures.join('\n'));
  await context.close();
  } finally {
  await browser.close();
  }
})().catch(error=>{console.error(error);process.exit(1);});
