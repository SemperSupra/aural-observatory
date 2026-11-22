/* R15: identical-source, scripted A/B geometry and interaction RDTE.
 * Runs actual localhost C# application and same source DOM in two layouts:
 * A = original full vertical sections; B = proposed R15 tabbed workspaces.
 * Uses deterministic target IDs (known to harness), NOT human discovery.
 * Never clicks PLAY, starts random trials, emits audio, or accesses medical data.
 */
'use strict';
const assert=require('node:assert/strict');
const {spawn}=require('node:child_process');
const fs=require('node:fs'),path=require('node:path');
const {chromium,firefox,webkit}=require('playwright');
const AxeBuilder=require('@axe-core/playwright').default;
const repo=path.resolve(__dirname,'../..');
const out=path.join(repo,'artifacts','ui','r15-ab-rdte');
fs.mkdirSync(out,{recursive:true});
const host=spawn('dotnet',['run','--no-launch-profile','--project',
  'src/AuralObservatory.SearchDesktop/AuralObservatory.SearchDesktop.csproj',
  '-c','Release','--','--no-browser'],{
   cwd:repo,env:{...process.env,AURAL_NO_BROWSER:'1'},stdio:['ignore','pipe','pipe']});
let log='',url='';
for(const stream of [host.stdout,host.stderr])stream.on('data',chunk=>{
 log+=String(chunk);
 const m=log.match(/Aural Observatory local (?:search|session):\s*(http:\/\/127\.0\.0\.1:\d+)/);
 if(m)url=m[1];
});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
async function awaitHost(){
 for(let n=0;n<450;n++){
  if(host.exitCode!==null)throw Error('host exited: '+log);
  if(url){try{if((await fetch(url)).ok)return url;}catch{}}
  await sleep(200);
 }
 throw Error('localhost C# host not ready: '+log);
}
const profiles=[
 {name:'phone320',width:320,height:568,touch:true},
 {name:'phoneLandscape',width:844,height:390,touch:true},
 {name:'tablet',width:768,height:1024,touch:true},
 {name:'desktop',width:1280,height:800,touch:false}
];
// All tasks are non-acoustic. The oracle knows the target ID in advance;
// this biases "navigation" positively and makes it unsuitable as human success time.
const tasks=[
 {key:'ear-left',id:'left-hz',workspace:'sound',anchor:'configuration'},
 {key:'manual-preview',id:'manual-mode',workspace:'spatial',anchor:null},
 {key:'spatial-pad',id:'spatial-azimuth',workspace:'spatial',anchor:'spatial-controller'},
 {key:'quiet-search',id:'quiet',workspace:'search',anchor:'experiment'},
 {key:'appearance',id:'theme',workspace:'settings',anchor:'appearance'},
 {key:'manual-ear',id:'manual-ear',workspace:'spatial',anchor:null},
 {key:'experiment-stop',id:'stop',workspace:'search',anchor:'experiment'},
 {key:'audio-save',id:'save-audio',workspace:'sound',anchor:'configuration'},
 {key:'presets-export',id:'export-prefs',workspace:'settings',anchor:'appearance'}
];
const engineName=process.env.AURAL_R15_AB_BROWSER||'chromium';
const browserLib={chromium,firefox,webkit}[engineName];
if(!browserLib)throw Error('Invalid AURAL_R15_AB_BROWSER '+engineName);

async function layoutMetrics(page){
 return page.evaluate(()=>{
  const stop=document.querySelector('#deck-stop');
  const r=stop?.getBoundingClientRect();
  return {
   docHeight:document.documentElement.scrollHeight,
   scrollHeight:document.body.scrollHeight,
   viewportHeight:innerHeight,
   viewportWidth:innerWidth,
   overflowX:Math.max(0,document.documentElement.scrollWidth-innerWidth),
   visibleTabs:[...document.querySelectorAll('[role=tab]')].filter(x=>!x.hidden && x.getBoundingClientRect().width>0).length,
   stopVisible:!!r && r.top>=0 && r.bottom<=innerHeight && r.width>=44 && r.height>=44,
   visibleSectionCount:[...document.querySelectorAll('main#content section')].filter(e=>e.getClientRects().length>0).length
  };
 });
}
async function originalLongScroll(page){
 const status=await page.evaluate(()=>{
  const main=document.querySelector('main#content');
  const deck=document.getElementById('aural-workspace-deck');
  const sequence=['configuration','manual-sweep','spatial-controller','experiment','response','feedback','appearance'];
  if(!deck) return {ok:false,reason:'no deck to transform'};
  for(const id of sequence){
   const node=document.getElementById(id);
   if(!node) return {ok:false,reason:'missing '+id};
   main.appendChild(node);
  }
  deck.remove();
  const old=document.querySelector('body > header nav');
  if(old)old.hidden=false;
  document.body.style.maxWidth='780px'; // matches original pre-deck CSS
  document.documentElement.scrollTop=0;
  return {ok:true,restored:sequence,oldNavVisible:old&&!old.hidden};
 });
 assert.equal(status.ok,true,JSON.stringify(status));
}
async function scrollToTarget(page,id){
 return page.evaluate((id)=>{
  const x=document.getElementById(id);
  if(!x)return {found:false};
  const before=window.scrollY;
  x.scrollIntoView({block:'center',inline:'nearest',behavior:'instant'});
  const r=x.getBoundingClientRect();
  const after=window.scrollY;
  return {found:true,delta:Math.abs(after-before),scrollY:after,inViewport:r.top>=-2&&r.bottom<=innerHeight+2,
    box:{x:r.x,y:r.y,width:r.width,height:r.height}};
 },id);
}
async function taskSequence(page,layout){
 let lastWorkspace=layout==='cards'?'sound':null;
 const record=[];
 for(const task of tasks){
  let clicks=0,anchorUsed=false,navScroll=0;
  if(layout==='cards' && task.workspace!==lastWorkspace){
    const before=await page.evaluate(()=>window.scrollY);
    await page.locator('#deck-tab-'+task.workspace).click();
    const after=await page.evaluate(()=>window.scrollY);
    navScroll=Math.abs(after-before);
    clicks=1;
    lastWorkspace=task.workspace;
    assert.equal(await page.locator('#deck-tab-'+task.workspace).getAttribute('aria-selected'),'true');
  }else if(layout==='long-scroll' && task.anchor){
    const link=page.locator('body > header nav a[href="#'+task.anchor+'"]');
    if(await link.count()){
      const before=await page.evaluate(()=>window.scrollY);
      await link.click();
      await page.waitForTimeout(80);
      const after=await page.evaluate(()=>window.scrollY);
      navScroll=Math.abs(after-before);
      clicks=1;anchorUsed=true;
    }
  }
  const result=await scrollToTarget(page,task.id);
  assert.equal(result.found,true,'missing target '+task.id);
  assert.equal(result.inViewport,true,'unreachable target '+task.id+' on '+layout);
  const node=page.locator('#'+task.id);
  assert.equal(await node.isVisible(),true,'hidden task control '+task.id+' '+layout);
  record.push({task:task.key,target:task.id,explicitNavigationActions:clicks,headerAnchorUsed:anchorUsed,
    verticalDisplacement:Math.round(navScroll+result.delta),scrollY:Math.round(result.scrollY),
    targetHeight:Math.round(result.box.height)});
  // Verify audio remains unused and sample layout interactions cannot accidentally START.
  assert.equal(await page.locator('#sound').evaluate(e=>e.paused),true);
  assert.equal(await page.locator('#manual-sound').evaluate(e=>e.paused),true);
 }
 return record;
}
async function testProfile(browser,profile,layout){
 const context=await browser.newContext({
  viewport:{width:profile.width,height:profile.height},
  reducedMotion:'reduce',colorScheme:'light',hasTouch:profile.touch,
  isMobile:profile.touch&&engineName!=='firefox'
 });
 const page=await context.newPage();
 const errors=[],requests=[];
 page.on('pageerror',e=>errors.push(String(e)));
 page.on('request',req=>{if(!req.url().startsWith('http://127.0.0.1:'))requests.push(req.url());});
 try{
  await page.goto(url,{waitUntil:'domcontentloaded'});
  await page.waitForFunction(()=>document.getElementById('deck-tab-sound')!==null);
  await page.waitForFunction(()=>document.getElementById('prefs-status')?.textContent.includes('loaded'),null,{timeout:20000});
  if(layout==='long-scroll')await originalLongScroll(page);
  else {
   assert.equal(await page.locator('[role=tab]').count(),4);
   assert.equal(await page.locator('[role=tabpanel]:visible').count(),1);
  }
  const initial=await layoutMetrics(page);
  await page.screenshot({path:path.join(out,engineName+'-'+profile.name+'-'+layout+'-start.png'),animations:'disabled',fullPage:false});
  const tasksRecord=await taskSequence(page,layout);
  // Verify reachability of STOP and no leakage after aggressive navigation.
  if(layout==='cards'){
   await page.locator('#deck-tab-spatial').click();
   await page.locator('#spatial-gamepad-diagnostics').scrollIntoViewIfNeeded();
   const stop=await page.locator('#deck-stop').boundingBox();
   const visible=stop && stop.y>=-2 && stop.y+stop.height<=profile.height+2;
   assert.ok(visible,'STOP cannot remain available deep in Spatial lab '+profile.name);
   assert.equal(await page.locator('#deck-stop').isVisible(),true);
  }
  const after=await layoutMetrics(page);
  // The original app's 150% text setting must also remain operable.
  await page.evaluate(()=>{document.documentElement.style.fontSize='150%';});
  if(layout==='cards')await page.locator('#deck-tab-spatial').click();
  const zoom=await layoutMetrics(page);
  const overflowItems=await page.evaluate(()=>{
    const limit=window.innerWidth;
    return [...document.querySelectorAll('body *')].map(e=>({e,r:e.getBoundingClientRect()}))
      .filter(({e,r})=>r.width>0 && r.right>limit+2 && getComputedStyle(e).display!=='none')
      .slice(0,12).map(({e,r})=>({tag:e.tagName,id:e.id,cls:String(e.className).slice(0,60),
        right:Math.round(r.right),left:Math.round(r.left),width:Math.round(r.width)}));
  });
  const axe=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa']).analyze();
  const severe=axe.violations.filter(v=>['serious','critical'].includes(v.impact));
  const receipt={
   engine:engineName,profile:profile.name,layout,source:'identical localhost C# DOM; long-scroll is in-browser restoration',
   synthetic:true,humanParticipants:0,userPreferenceMeasured:false,actualAudioOutput:false,
   initial,tasks:tasksRecord,after,zoom,overflowItems,
   totalVerticalDisplacement:tasksRecord.reduce((n,x)=>n+x.verticalDisplacement,0),
   totalNavigationActions:tasksRecord.reduce((n,x)=>n+x.explicitNavigationActions,0),
   severeAxe:severe.map(x=>({id:x.id,impact:x.impact,nodes:x.nodes.length})),
   otherAxe:axe.violations.filter(v=>!['serious','critical'].includes(v.impact)).map(x=>({id:x.id,impact:x.impact})),
   pageErrors:errors,externalRequests:requests
  };
  assert.equal(severe.length,0,'serious/critical axe: '+JSON.stringify(receipt.severeAxe));
  assert.deepEqual(errors,[],'JS exceptions');
  assert.deepEqual(requests,[],'external requests');
  return receipt;
 }finally{await context.close();}
}
(async()=>{
 await awaitHost();
 const browser=await browserLib.launch({headless:true});
 const results=[];
 try{
  for(const profile of profiles){
   for(const variant of ['long-scroll','cards']){
    const receipt=await testProfile(browser,profile,variant);
    results.push(receipt);
    console.log(JSON.stringify({engine:engineName,profile:profile.name,layout:variant,
      docHeight:receipt.initial.docHeight,
      scroll:receipt.totalVerticalDisplacement,
      actions:receipt.totalNavigationActions,
      zoomOverflow:receipt.zoom.overflowX,
      offenders:receipt.overflowItems.slice(0,3)}));
   }
  }
 }finally{await browser.close();}
 const pairs=profiles.map(profile=>{
  const long=results.find(x=>x.profile===profile.name&&x.layout==='long-scroll');
  const cards=results.find(x=>x.profile===profile.name&&x.layout==='cards');
  return {profile:profile.name,engine:engineName,
   longHeight:long.initial.docHeight,cardHeight:cards.initial.docHeight,
   longScroll:long.totalVerticalDisplacement,cardScroll:cards.totalVerticalDisplacement,
   longActions:long.totalNavigationActions,cardActions:cards.totalNavigationActions,
   longZoomOverflow:long.zoom.overflowX,cardZoomOverflow:cards.zoom.overflowX};
 });
 const report={schemaVersion:1,scope:'SOURCE-EQUIVALENT SCRIPTED COMPARISON; NOT HUMAN USABILITY',
  engine:engineName,hardwareSPLQualified:false,hiddenSourceOracles:true,
  limitation:'Agent knows exact DOM IDs; timings/scroll distances do not measure human discovery or preference.',
  results,pairs};
 fs.writeFileSync(path.join(out,engineName+'.json'),JSON.stringify(report,null,2)+'\n');
 const bad=results.filter(x=>x.zoom.overflowX>2||x.initial.overflowX>2||x.after.overflowX>2);
 if(bad.length){
  console.error('FAIL R15 overflow: '+JSON.stringify(bad.map(x=>({
   profile:x.profile,layout:x.layout,zoomOverflow:x.zoom.overflowX,offenders:x.overflowItems.slice(0,4)}))));
  process.exitCode=1;
 }else console.log('PASS '+engineName+' '+results.length+' views; '+pairs.length+' paired scripted cases');
})().catch(e=>{console.error(e.stack||e);process.exitCode=1;}).finally(()=>{
 host.kill('SIGTERM');
 setTimeout(()=>{if(host.exitCode===null)host.kill('SIGKILL')},1000).unref();
});
