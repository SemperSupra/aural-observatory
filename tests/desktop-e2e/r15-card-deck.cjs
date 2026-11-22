/* R15: actual browser/host card deck, keyboard, dimensions and fail-closed controls.
 * No physical audio, headphones, personal data, or unapproved playback.
 */
'use strict';
const assert=require('node:assert/strict');
const {spawn}=require('node:child_process');
const fs=require('node:fs');
const path=require('node:path');
const {chromium,firefox,webkit}=require('playwright');
const AxeBuilder=require('@axe-core/playwright').default;
const repo=path.resolve(__dirname,'../..');
const host=spawn('dotnet',['run','--no-launch-profile','--project','src/AuralObservatory.SearchDesktop/AuralObservatory.SearchDesktop.csproj','-c','Release','--','--no-browser'],
 {cwd:repo,env:{...process.env,AURAL_NO_BROWSER:'1'},stdio:['ignore','pipe','pipe']});
let output='',address='';
for(const stream of [host.stdout,host.stderr])stream.on('data',b=>{
 output+=String(b);
 const m=output.match(/Aural Observatory local (?:search|session):\s*(http:\/\/127\.0\.0\.1:\d+)/);
 if(m)address=m[1];
});
const pause=ms=>new Promise(r=>setTimeout(r,ms));
async function ready(){
 for(let i=0;i<450;i++){if(host.exitCode!==null)throw Error('Local app exited '+output);
  if(address){try{const r=await fetch(address);if(r.ok)return;}catch{}}
  await pause(200);
 }
 throw Error('Timed out waiting for C# local host '+output);
}
const cases=[
 {name:'phone320',width:320,height:568,touch:true},
 {name:'landscape',width:844,height:390,touch:true},
 {name:'tablet',width:768,height:1024,touch:true},
 {name:'desktop',width:1280,height:800,touch:false}
];
(async()=>{
 await ready();
 const engineName=process.env.AURAL_R15_BROWSER||'chromium';
 const engines={chromium,firefox,webkit};
 const engine=engines[engineName];
 if(!engine)throw Error('Invalid browser: '+engineName);
 const browser=await engine.launch({headless:true});
 const receipts=[],evidence=path.join(repo,'artifacts','ui','r15-card-deck');
 fs.mkdirSync(evidence,{recursive:true});
 try{
  for(const p of cases){
   const context=await browser.newContext({viewport:{width:p.width,height:p.height},hasTouch:p.touch,isMobile:p.touch&&engineName!=='firefox',colorScheme:'dark',reducedMotion:'reduce'});
   const page=await context.newPage();const errors=[];
   page.on('pageerror',err=>errors.push(String(err)));
   await page.goto(address,{waitUntil:'domcontentloaded'});
   await page.waitForFunction(()=>document.getElementById('deck-tab-sound')!==null);
   await page.waitForFunction(()=>document.getElementById('prefs-status').textContent.includes('loaded'),null,{timeout:20000});
   assert.equal(await page.locator('body > header nav').isHidden(),true,'no duplicate classic navigation when deck is active');
   assert.equal(await page.locator('[role=tab]').count(),4,'four separate workspaces');
   assert.equal(await page.locator('[role=tabpanel]:visible').count(),1,'only one card visible');
   assert.equal(await page.locator('#deck-tab-sound').getAttribute('aria-selected'),'true');
   assert.equal(await page.locator('#deck-stop').isVisible(),true);
   // User can reach each workspace without touching scroll; original control IDs stay bound.
   await page.locator('#deck-tab-spatial').click();
   assert.equal(await page.locator('#manual-start').isVisible(),true);
   assert.equal(await page.locator('#touch-controller-open').isVisible(),true);
   assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true);
   await page.locator('#deck-tab-search').click();
   assert.equal(await page.locator('#quiet').isVisible(),true);
   await page.locator('#deck-tab-settings').click();
   assert.equal(await page.locator('#theme').isVisible(),true);
   // Roving native tab keyboard and accessible selected state.
   await page.locator('#deck-tab-settings').focus();
   await page.keyboard.press('Home');
   assert.equal(await page.locator('#deck-tab-sound').getAttribute('aria-selected'),'true');
   await page.keyboard.press('ArrowRight');
   assert.equal(await page.locator('#deck-tab-spatial').getAttribute('aria-selected'),'true');
   // Simulated in-progress state: switching cards is forbidden before STOP.
   await page.locator('#manual-start').evaluate(x=>x.disabled=true);
   await page.locator('#deck-tab-search').click();
   assert.equal(await page.locator('#deck-tab-spatial').getAttribute('aria-selected'),'true');
   assert.match(await page.locator('#deck-alert').textContent(),/STOP SOUND/);
   await page.locator('#manual-start').evaluate(x=>x.disabled=false);
   await page.locator('#deck-tab-search').click();
   await page.locator('#deck-stop').click();
   assert.match(await page.locator('#deck-alert').textContent(),/No active browser audio/);
   assert.equal(await page.locator('#sound').evaluate(x=>x.paused),true,'deck never starts randomized playback');
   // Viewport/contrast guard after navigating each real card.
   const overflow=await page.evaluate(()=>document.documentElement.scrollWidth-innerWidth);
   assert.ok(overflow<=2,engineName+'/'+p.name+' horizontal overflow '+overflow);
   const stop=await page.locator('#deck-stop').boundingBox();
   assert.ok(stop && stop.width>=44 && stop.height>=44,'sticky STOP meets touch target size');
   assert.ok(stop.x>=0 && stop.x+stop.width<=p.width+2,'sticky STOP not clipped');
   const axe=await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa']).analyze();
   const severe=axe.violations.filter(v=>['serious','critical'].includes(v.impact));
   assert.equal(severe.length,0,JSON.stringify(severe.map(x=>({id:x.id,impact:x.impact}))));
   assert.deepEqual(errors,[]);
   const filename=engineName+'-'+p.name+'.png';
   await page.screenshot({path:path.join(evidence,filename),fullPage:true,animations:'disabled'});
   receipts.push({engine:engineName,profile:p.name,overflow,axe:axe.violations.map(x=>({id:x.id,impact:x.impact})),screenshot:filename});
   await context.close();
  }
  fs.writeFileSync(path.join(evidence,engineName+'.json'),JSON.stringify({status:'source-UI-only',audioPhysicalQualified:false,receipts},null,2));
  console.log('PASS '+engineName+' '+receipts.length+' source-backed card UX profiles');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;}).finally(()=>{
 host.kill('SIGTERM');
 setTimeout(()=>{if(host.exitCode===null)host.kill('SIGKILL')},1000).unref();
});