/* R13 public-safe Playwright browser verification for contextual touch overlay.
 * Real browser interaction; device hardware and acoustic sound remain untested.
 */
'use strict';
const assert = require('node:assert/strict');
const {spawn} = require('node:child_process');
const {chromium,webkit} = require('playwright');
const AxeBuilder = require('@axe-core/playwright').default;
const fs = require('node:fs');
const path = require('node:path');

const repo=path.resolve(__dirname,'../..');
const project='src/AuralObservatory.SearchDesktop/AuralObservatory.SearchDesktop.csproj';
const host=spawn('dotnet',['run','--no-launch-profile','--project',project,'-c','Release','--','--no-browser'],{
  cwd:repo, env:{...process.env,AURAL_NO_BROWSER:'1'}, stdio:['ignore','pipe','pipe']
});
let output='',address=null;
host.stdout.on('data',buf=>{output+=String(buf);const m=output.match(/Aural Observatory local (?:search|session):\s*(http:\/\/127\.0\.0\.1:\d+)/);if(m)address=m[1]});
host.stderr.on('data',buf=>{output+=String(buf)});
const delay=ms=>new Promise(r=>setTimeout(r,ms));
async function ready(){
  const deadline=Date.now()+90000;
  while(Date.now()<deadline){
    if(host.exitCode!==null)throw Error('Native desktop host exited: '+output);
    if(address){try{const result=await fetch(address);if(result.ok)return address}catch{}}
    await delay(250);
  }
  throw Error('Native desktop loopback host timeout: '+output);
}
const positions=async page=>page.evaluate(()=>({
  az:Number(document.querySelector('#spatial-azimuth').value),
  el:Number(document.querySelector('#spatial-elevation').value),
  dist:Number(document.querySelector('#spatial-distance').value)
}));
async function pointer(page,side,type,id,x,y,isPrimary=true){
  await page.evaluate(({side,type,id,x,y,isPrimary})=>{
    const node=document.getElementById('touch-'+side+'-zone');
    const event=new PointerEvent(type,{bubbles:true,cancelable:true,
      pointerId:id,pointerType:'touch',isPrimary,clientX:x,clientY:y});
    node.dispatchEvent(event);
  },{side,type,id,x,y,isPrimary});
}
async function runCase(browser,engine,device){
 const context=await browser.newContext({
  viewport:device.size, isMobile:true, hasTouch:true,
  reducedMotion:'reduce',colorScheme:'light',
  deviceScaleFactor:device.dpr||2
 });
 const page=await context.newPage();
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(address,{waitUntil:'domcontentloaded'});
 await page.waitForFunction(()=>document.getElementById('prefs-status').textContent.includes('loaded'),null,{timeout:15000});
 assert.equal(await page.locator('#aural-touch-overlay').isHidden(),true,'opt-in modal hidden by default');
 assert.equal(await page.locator('#sound').evaluate(el=>el.paused),true,'no randomized trial starts on mobile');
 assert.equal(await page.locator('#manual-sound').evaluate(el=>el.paused),true,'no manual preview starts on mobile');
 await page.locator('#deck-tab-spatial').click();
 await page.locator('#touch-controller-open').click();
 assert.equal(await page.locator('#aural-touch-overlay').isVisible(),true);
 assert.equal(await page.locator('main').evaluate(x=>x.inert),true,'background content must be inert');
 assert.equal(await page.locator('#touch-emergency-stop').isVisible(),true);
 assert.equal(await page.locator('#touch-close').isVisible(),true);
 assert.equal(await page.locator('#touch-hrtf').isChecked(),false);
 const left=await page.locator('#touch-left-zone').boundingBox();
 const right=await page.locator('#touch-right-zone').boundingBox();
 assert.ok(left&&right&&left.width>=100&&right.width>=100,
   'independent responsive touch regions exist');
 const lx=left.x+left.width*.48,ly=left.y+left.height*.5;
 const rx=right.x+right.width*.48,ry=right.y+right.height*.55;
 await pointer(page,'left','pointerdown',41,lx,ly,true);
 await pointer(page,'right','pointerdown',42,rx,ry,false);
 assert.equal(await page.locator('#touch-left-base').isVisible(),true,'floating left base follows touch');
 assert.equal(await page.locator('#touch-right-base').isVisible(),true,'second simultaneous touch preserved');
 await pointer(page,'left','pointermove',41,lx+40,ly-40,true);
 await pointer(page,'right','pointermove',42,rx+40,ry-35,false);
 const moved=await positions(page);
 assert.ok(moved.az>10&&moved.az<90,JSON.stringify({device, moved,message:'left azimuth bounded'}));
 assert.ok(moved.el>3&&moved.dist>1,JSON.stringify({device,moved,message:'right controls changed'}));
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'two-touch control cannot start audio');
 assert.equal(await page.locator('#sound').evaluate(x=>x.paused),true,'two-touch control cannot start randomized trial');
 // Capture the real interactive geometry WHILE both independent pads are held.
 // Later idle screenshots intentionally show neutral/inactive touch rings.
 const pictureDir=path.join(repo,'artifacts','ui','r13-touch');
 fs.mkdirSync(pictureDir,{recursive:true});
 await page.locator('#aural-touch-overlay').screenshot({
   path:path.join(pictureDir,engine+'-'+device.name+'-active-dual-touch.png'),
   animations:'disabled'
 });
 // Pointer cancellation only affects the owning touch identity.
 await pointer(page,'left','pointercancel',41,lx+40,ly-40);
 assert.equal(await page.locator('#touch-left-base').isHidden(),true,'left cancel drops left touch');
 assert.equal(await page.locator('#touch-right-base').isVisible(),true,'right touch survives unrelated cancel');
 await pointer(page,'right','pointermove',42,rx+50,ry-45,false);
 const second=await positions(page);
 assert.ok(second.el>=moved.el,'right touch remained operational after left cancel');
 await pointer(page,'right','pointerup',42,rx+50,ry-45,false);
 assert.equal(await page.locator('#touch-right-base').isHidden(),true,'touch release clears right');
 const stoppedPosition=await positions(page);
 await pointer(page,'right','pointermove',42,rx+70,ry-50,false);
 assert.deepEqual(await positions(page),stoppedPosition,'stale released pointer cannot act');
 await page.locator('#touch-left-zone').focus();
 await page.keyboard.press('ArrowLeft');
 assert.ok((await positions(page)).az<stoppedPosition.az,'keyboard arrow changes azimuth');
 await page.locator('#touch-center').click();
 const centered=await positions(page);
 assert.equal(centered.az,0);assert.equal(centered.el,0);assert.equal(centered.dist,1);
 await page.locator('#touch-play').click();
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'unprepared Y never causes autoplay');
 await page.locator('#touch-stop').click();
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'B stops even without active sound');
 // Simulate orientation change without a full page reload.
 const changed= device.size.width<device.size.height ?
   {width:Math.max(660,device.size.height),height:Math.max(350,device.size.width)} :
   {width:390,height:844};
 await page.setViewportSize(changed);
 await page.evaluate(()=>window.dispatchEvent(new Event('orientationchange')));
 await page.waitForTimeout(90);
 assert.equal(await page.locator('#touch-left-base').isHidden(),true,'orientation invalidates active touches');
 const view=await page.evaluate(()=>({
  width:window.innerWidth,scroll:document.documentElement.scrollWidth,
  rect:(()=>{const r=document.querySelector('#aural-touch-overlay').getBoundingClientRect();return{x:r.x,y:r.y,w:r.width,h:r.height}})(),
  stop:(()=>{const r=document.querySelector('#touch-emergency-stop').getBoundingClientRect();return{w:r.width,h:r.height}})()
 }));
 assert.ok(view.scroll<=view.width+2,'orientation creates horizontal overflow: '+JSON.stringify(view));
 assert.ok(view.stop.w>=40&&view.stop.h>=40,'STOP target remains tappable');
 await page.locator('#touch-close').click();
 assert.equal(await page.locator('#aural-touch-overlay').isHidden(),true);
 assert.equal(await page.locator('main').evaluate(x=>x.inert),false,'close restores base UI');
 assert.equal(await page.locator('#sound').evaluate(x=>x.paused),true);
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true);
 // Reopen and verify a recorded actual browser screenshot + accessible modal.
 await page.locator('#touch-controller-open').click();
 const axe=new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa']);
 const check=await axe.analyze();
 const critical=check.violations.filter(v=>v.impact==='critical'||v.impact==='serious');
 assert.deepEqual(critical.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.length})),[],
   'no automated serious/critical accessibility defects');
 assert.deepEqual(errors,[],'no page script errors');
 const dir=path.join(repo,'artifacts','ui','r13-touch');
 fs.mkdirSync(dir,{recursive:true});
 const filename=engine+'-'+device.name+'.png';
 await page.locator('#aural-touch-overlay').screenshot({
   path:path.join(dir,filename),animations:'disabled'
 });
 await page.locator('#touch-close').click();
 await context.close();
 return{engine,device:device.name,viewport:device.size,
   touchIndependent:true,orientationResponsive:true,physicalAudioVerified:false,
   automatedViolations:check.violations.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.length})),
   screenshot:filename};
}
async function main(){
 await ready();
 const engine=process.env.AURAL_TOUCH_BROWSER||'chromium';
 const kind=({chromium,webkit})[engine];
 if(!kind)throw Error('Unsupported browser '+engine);
 const browser=await kind.launch({headless:true,args:engine==='chromium'?['--no-sandbox']:[]});
 const profiles=[
   {name:'phone-portrait',size:{width:390,height:844}},
   {name:'phone-landscape',size:{width:844,height:390}},
   {name:'tablet-portrait',size:{width:768,height:1024}},
   {name:'minimum-width',size:{width:320,height:700}}
 ];
 const results=[];
 try {for (const device of profiles)results.push(await runCase(browser,engine,device))}
 finally{await browser.close()}
 const dir=path.join(repo,'artifacts','ui','r13-touch');
 fs.mkdirSync(dir,{recursive:true});
 fs.writeFileSync(path.join(dir,engine+'-evidence.json'),
   JSON.stringify({schemaVersion:1,reference:'OSD private source inspected, original Aural adaptation',
     engine,source:'synthetic CI only',results,realDeviceTested:false,
     headphoneSPLMeasured:false,androidNativeQualified:false},null,2)+'\n');
 console.log('PASS R13 '+engine+' phone/tablet touch overlays; '+results.length+' real-browser profiles');
}
main().catch(e=>{console.error(e.stack||e);process.exitCode=1})
  .finally(()=>{if(host.exitCode===null)host.kill('SIGTERM')});
