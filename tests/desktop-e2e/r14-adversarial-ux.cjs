/* R14 adverse human UX journeys in real Playwright browser engines.
 * Not hardware, screenreader or calibrated audio qualification.
 */
'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');
const { spawn } = require('node:child_process');
const { chromium, firefox, webkit } = require('playwright');
const AxeBuilder = require('@axe-core/playwright').default;

const repo=path.resolve(__dirname,'../..');
const host=spawn('dotnet',['run','--no-launch-profile','--project',
  'src/AuralObservatory.SearchDesktop/AuralObservatory.SearchDesktop.csproj',
  '-c','Release','--','--no-browser'],
 {cwd:repo,env:{...process.env,AURAL_NO_BROWSER:'1'},stdio:['ignore','pipe','pipe']});
let log='',url='';
for(const stream of [host.stdout,host.stderr])
 stream.on('data',b=>{
  log+=String(b);
  const m=log.match(/Aural Observatory local (?:search|session):\s*(http:\/\/127\.0\.0\.1:\d+)/);
  if(m)url=m[1];
 });
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
async function ready(){
 const deadline=Date.now()+90000;
 while(Date.now()<deadline){
  if(host.exitCode!==null)throw Error('Native source-backed host exited: '+log);
  if(url){try{let r=await fetch(url);if(r.ok)return}catch{}}
  await sleep(200);
 }
 throw Error('Could not launch local C# audio host: '+log);
}
const cases=[
 {name:'desktop-keyboard',width:1280,height:800,touch:false},
 {name:'laptop-zoom-200',width:640,height:360,touch:false},
 {name:'phone-portrait',width:390,height:844,touch:true},
 {name:'phone-small',width:320,height:568,touch:true},
 {name:'phone-landscape',width:844,height:390,touch:true},
 {name:'tablet-landscape',width:1024,height:768,touch:true}
];
const report=[];
function compact(x){return JSON.stringify(x);}
async function scenario(browser,name,profile){
 const context=await browser.newContext({
  viewport:{width:profile.width,height:profile.height},
  // Playwright Firefox supports touch but not isMobile viewport meta emulation.
  // Explicitly record that this is viewport+touch coverage, not mobile UA HIL.
  hasTouch:profile.touch, isMobile:profile.touch && name!=='firefox',
  deviceScaleFactor:profile.touch?2:1,
  reducedMotion:'reduce',colorScheme:'dark',forcedColors:'none',
 });
 const page=await context.newPage();
 const exceptions=[];page.on('pageerror',e=>exceptions.push(e.message));
 await page.goto(url,{waitUntil:'domcontentloaded'});
 await page.waitForFunction(()=>document.getElementById('prefs-status')?.textContent.includes('loaded'),null,{timeout:20000});
 // Failure conditions must be actively probed, not just a screenshot.
 assert.equal(await page.locator('html').getAttribute('lang'),'en','current English locale explicit');
 assert.equal(await page.locator('#aural-touch-overlay').isHidden(),true);
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true);
 const token=await page.evaluate(()=>document.querySelector('meta[name=aural-token]')?.content||'');
 const denial=await page.evaluate(async()=>{
  const r=await fetch('/api/sweep-stop',{method:'POST',headers:{'X-Aural-Token':'forged'}});
  return r.status;
 });
 assert.equal(denial,403,'agent/automation cannot forge local controller token');
 await page.locator('#deck-tab-spatial').click();
 await page.locator('#touch-controller-open').click();
 assert.equal(await page.locator('#aural-touch-overlay').isVisible(),true);
 assert.equal(await page.locator('main').evaluate(x=>x.inert),true);
 // Standalone dialog role is not proof of compliant focus management: check.
 const accessibleName=await page.locator('#aural-touch-overlay').getAttribute('aria-labelledby');
 assert.equal(accessibleName,'touch-heading');
 const go=await page.evaluate(()=>{
  const d=document.querySelector('#aural-touch-overlay');
  const buttons=['touch-emergency-stop','touch-close','touch-mark','touch-stop','touch-center','touch-play'];
  const rr=d.getBoundingClientRect();
  return{rect:{left:rr.left,right:rr.right,top:rr.top,bottom:rr.bottom},
    overflow:document.documentElement.scrollWidth-window.innerWidth,
    controls:buttons.map(id=>{
      const r=document.getElementById(id).getBoundingClientRect();
      return{id,x:r.left,y:r.top,w:r.width,h:r.height,
        inViewport:r.left>=-2&&r.right<=window.innerWidth+2&&
                   r.top>=-2&&r.bottom<=window.innerHeight+2}
    })};
 });
 assert.ok(go.overflow<=2,'unexpected lateral overflow: '+compact(go));
 for(const c of go.controls){
  assert.ok(c.inViewport,'control clipped in '+profile.name+': '+compact(c));
  assert.ok(c.w>=40&&c.h>=40,'inadequate important mobile action target: '+compact(c));
 }
 // Real keyboard path for equivalent single-pointer operation.
 await page.locator('#touch-left-zone').focus();
 const before=Number(await page.locator('#spatial-azimuth').inputValue());
 await page.keyboard.press('ArrowRight');
 const after=Number(await page.locator('#spatial-azimuth').inputValue());
 assert.equal(after,before+5,'keyboard/one-finger alternative for drag');
 await page.locator('#touch-right-zone').focus();
 await page.keyboard.press('ArrowUp');
 assert.ok(Number(await page.locator('#spatial-elevation').inputValue())>0,'height keyboard alternative');
 await page.locator('#touch-center').click();
 assert.equal(await page.locator('#spatial-azimuth').inputValue(),'0');
 // Keyboard modal focus must never escape to background until dismissed.
 await page.locator('#touch-emergency-stop').focus();
 let focusLeaks=0;
 for(let i=0;i<14;i++){
  await page.keyboard.press('Tab');
  const current=await page.evaluate(()=>
   document.querySelector('#aural-touch-overlay').contains(document.activeElement));
  if(!current)focusLeaks++;
 }
 assert.equal(focusLeaks,0,'modal Tab focus escaped visible controller');
 for(let i=0;i<10;i++){
  await page.keyboard.press('Shift+Tab');
  const contained=await page.evaluate(()=>
   document.querySelector('#aural-touch-overlay').contains(document.activeElement));
  if(!contained)focusLeaks++;
 }
 assert.equal(focusLeaks,0,'Shift+Tab escaped the modal');
 // Negative workflow: Play prepared from a fresh session must stay silent.
 await page.locator('#touch-play').click();
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,
   'unprepared Play started sound unexpectedly');
 assert.equal(await page.locator('#sound').evaluate(x=>x.paused),true,
   'unprepared Play started randomized trial unexpectedly');
 // Dialog should respond to Escape by stopping and returning focus.
 await page.locator('#touch-close').focus();
 await page.keyboard.press('Escape');
 assert.equal(await page.locator('#aural-touch-overlay').isHidden(),true);
 assert.equal(await page.locator('#touch-controller-open').evaluate(x=>x===document.activeElement),true,
    'closing overlay must restore opening control focus');
 assert.equal(await page.locator('main').evaluate(x=>x.inert),false);
 await page.locator('#touch-controller-open').click();
 // Automated WCAG 2.2 AA-ish smoke; axe cannot replace screenreader/physical checks.
 const audit=await new AxeBuilder({page})
   .withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa']).analyze();
 const severe=audit.violations.filter(v=>['critical','serious'].includes(v.impact));
 assert.equal(severe.length,0,'serious axe issues: '+compact(severe.map(x=>({id:x.id,impact:x.impact}))));
 assert.deepEqual(exceptions,[],'uncaught browser errors');
 const folder=path.join(repo,'artifacts','evaluation','r14','browser');
 fs.mkdirSync(folder,{recursive:true});
 const fname=name+'-'+profile.name+'.png';
 await page.locator('#aural-touch-overlay').screenshot({
  path:path.join(folder,fname),animations:'disabled'
 });
 await page.locator('#touch-close').click();
 await context.close();
 return{engine:name,form_factor:profile.name,size:[profile.width,profile.height],
   checks:{token_denial_403:true,target_min40:true,no_clip:true,drag_keyboard_equivalent:true,
     focus_containment:true,escape_focus_restore:true,no_unprepared_play:true,
     no_trial_side_effect:true,axe_severe_zero:true,html_language:'en'},
   evidence_level:'real_browser_emulated_form_factor_only',
   limitations:['no actual smartphone','no real screen reader','no device STOP or SPL','no localized UI'],
   screenshot:fname};
}
async function main(){
 await ready();
 const name=process.env.AURAL_R14_BROWSER||'chromium';
 const engine=({chromium,firefox,webkit})[name];
 if(!engine)throw Error('unsupported engine: '+name);
 const browser=await engine.launch({
  headless:true,args:name==='chromium'?['--no-sandbox']:[]
 });
 try{for(const profile of cases){
  const r=await scenario(browser,name,profile);
  report.push(r);console.log('PASS R14 '+name+' '+profile.name);
 }}finally{await browser.close()}
 const folder=path.join(repo,'artifacts','evaluation','r14','browser');
 fs.mkdirSync(folder,{recursive:true});
 fs.writeFileSync(path.join(folder,name+'-adversarial.json'),JSON.stringify({
  schema_version:'r14.ux.adversarial.v1',source_ref:'R13-Q test parent',
  engine:name,results:report,hardware:false,screenreader:false,release_authorized:false
 },null,2)+'\n');
 console.log('PASS R14 '+name+' '+report.length+' actual-browser adverse journeys');
}
main().catch(e=>{console.error('R14 qualification failure:',e.stack||e);process.exitCode=1})
.finally(()=>{if(host.exitCode===null)host.kill('SIGTERM')});
