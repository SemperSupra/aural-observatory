/* R15 independent adverse tab/STOP/zoom qualification, no actual sound.
 * Source-backed localhost host; tests cannot prove physical audio route silence.
 */
'use strict';
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
const {spawn}=require('node:child_process');
const {chromium,firefox,webkit}=require('playwright');
const root=path.resolve(__dirname,'../..');
const out=path.join(root,'artifacts','ui','r15-adverse-a11y');
fs.mkdirSync(out,{recursive:true});
const host=spawn('dotnet',['run','--no-launch-profile','--project',
'src/AuralObservatory.SearchDesktop/AuralObservatory.SearchDesktop.csproj',
'-c','Release','--','--no-browser'],{
cwd:root,env:{...process.env,AURAL_NO_BROWSER:'1'},stdio:['ignore','pipe','pipe']});
let output='',address='';
for(const stream of [host.stdout,host.stderr])stream.on('data',c=>{
 output+=String(c);const m=output.match(/Aural Observatory local (?:search|session):\s*(http:\/\/127\.0\.0\.1:\d+)/);
 if(m)address=m[1];
});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
async function ready(){
 for(let i=0;i<400;i++){
  if(host.exitCode!==null)throw Error('host quit '+output);
  if(address){try{if((await fetch(address)).ok)return;}catch{}}
  await sleep(200);
 }throw Error('localhost host never became ready '+output);
}
const engineName=process.env.AURAL_R15_ADVERSE_BROWSER||'chromium';
const lib={chromium,firefox,webkit}[engineName];
if(!lib)throw Error('invalid browser '+engineName);
const profiles=[{name:'phone320',width:320,height:568,touch:true},
 {name:'landscape',width:844,height:390,touch:true},
 {name:'desktop',width:1280,height:800,touch:false}];
const ids=['sound','spatial','search','settings'];
(async()=>{
 await ready();const browser=await lib.launch({headless:true});
 const results=[];
 try{
  for(const p of profiles){
   const ctx=await browser.newContext({viewport:{width:p.width,height:p.height},
     hasTouch:p.touch,isMobile:p.touch&&engineName!=='firefox',
     reducedMotion:'reduce',colorScheme:'dark'});
   const page=await ctx.newPage();
   const jsErrors=[];page.on('pageerror',e=>jsErrors.push(String(e)));
   try{
    await page.goto(address,{waitUntil:'domcontentloaded'});
    await page.waitForFunction(()=>document.getElementById('deck-tab-sound')!==null);
    await page.waitForFunction(()=>document.getElementById('prefs-status')?.textContent.includes('loaded'),null,{timeout:20000});
    const structural=await page.evaluate(()=>{
     const keys=['sound','spatial','search','settings'];
     return keys.map(k=>{
      const tab=document.getElementById('deck-tab-'+k);
      const panel=document.getElementById('deck-panel-'+k);
      return {key:k,tabRole:tab?.getAttribute('role'),panelRole:panel?.getAttribute('role'),
        controls:tab?.getAttribute('aria-controls'),labelledby:panel?.getAttribute('aria-labelledby'),
        selected:tab?.getAttribute('aria-selected'),inert:panel?.inert,hidden:panel?.hidden};
     });
    });
    assert.equal(structural.length,4);
    for(const s of structural){
     assert.equal(s.tabRole,'tab');assert.equal(s.panelRole,'tabpanel');
     assert.equal(s.controls,'deck-panel-'+s.key);
     assert.equal(s.labelledby,'deck-tab-'+s.key);
    }
    assert.equal(structural.filter(s=>s.selected==='true').length,1);
    // No hidden panel should participate in the tab order.
    for(const key of ids){
     await page.locator('#deck-tab-'+key).click();
     const state=await page.evaluate((selected)=>[...document.querySelectorAll('[role=tabpanel]')].map(e=>({
      key:e.id,hidden:e.hidden,inert:e.inert,visible:e.getClientRects().length>0,
      active:e.id==='deck-panel-'+selected})),key);
     assert.equal(state.filter(x=>x.visible).length,1);
     for(const x of state){assert.equal(x.hidden,!x.active);assert.equal(x.inert,!x.active);}
    }
    // Roving keyboard, Home/End and wrap on all engines; no sound started.
    await page.locator('#deck-tab-settings').focus();
    await page.keyboard.press('ArrowRight');
    assert.equal(await page.locator('#deck-tab-sound').getAttribute('aria-selected'),'true');
    await page.keyboard.press('ArrowLeft');
    assert.equal(await page.locator('#deck-tab-settings').getAttribute('aria-selected'),'true');
    await page.keyboard.press('Home');
    assert.equal(await page.locator('#deck-tab-sound').getAttribute('aria-selected'),'true');
    await page.keyboard.press('End');
    assert.equal(await page.locator('#deck-tab-settings').getAttribute('aria-selected'),'true');

    // Adverse case: both manual PREPARING and active quiet-search observation
    // artificially signaled through disabled state. Intercept STOP in capture
    // to prove delegated control routing WITHOUT the existing host endpoint.
    await page.locator('#deck-tab-spatial').click();
    await page.evaluate(()=>{
     window.__r15StopWitness={manual:0,trial:0};
     for(const [id,key] of [['manual-stop','manual'],['stop','trial']]){
      document.getElementById(id).addEventListener('click',e=>{
       window.__r15StopWitness[key]++;e.stopImmediatePropagation();e.preventDefault();
      },{capture:true,once:false});
     }
     document.getElementById('manual-start').disabled=true;
     document.getElementById('mode').disabled=true;
    });
    await page.locator('#deck-tab-search').click();
    assert.equal(await page.locator('#deck-tab-spatial').getAttribute('aria-selected'),'true',
      'must not abandon active manual/study phase');
    assert.match(await page.locator('#deck-alert').textContent(),/STOP SOUND/);
    await page.locator('#deck-stop').click();
    const witness=await page.evaluate(()=>window.__r15StopWitness);
    assert.equal(witness.manual,1,'delegates to manual STOP');
    assert.equal(witness.trial,1,'delegates to search STOP');
    assert.match(await page.locator('#deck-alert').textContent(),/STOP requested/);
    await page.evaluate(()=>{
     document.getElementById('manual-start').disabled=false;
     document.getElementById('mode').disabled=false;
    });
    await page.locator('#deck-tab-search').click();
    assert.equal(await page.locator('#deck-tab-search').getAttribute('aria-selected'),'true');

    // 200% document text size is a source-backed difficult mobile condition.
    // Existing core has an explicit 80..200% control. We set the CSS root directly
    // to keep this input-only without saving a preference to disk.
    const zoom=await page.evaluate(()=>{
     document.documentElement.style.fontSize='200%';
     const viewport=innerWidth;
     const t=[...document.querySelectorAll('#aural-workspace-deck [role=tab]')].map(el=>{
      const r=el.getBoundingClientRect();
      return {label:el.textContent,left:r.left,right:r.right,width:r.width,height:r.height};
     });
     const stop=document.querySelector('#deck-stop').getBoundingClientRect();
     const offenders=[...document.querySelectorAll('body *')].filter(e=>{
      const r=e.getBoundingClientRect(),cs=getComputedStyle(e);
      return r.width>0&&r.right>viewport+2&&cs.display!=='none'&&cs.visibility!=='hidden';
     }).slice(0,25).map(e=>{
       const r=e.getBoundingClientRect();
       return {tag:e.tagName,id:e.id,cls:typeof e.className==='string'?e.className.slice(0,60):'',
         right:Math.round(r.right),left:Math.round(r.left),width:Math.round(r.width),
         scrollWidth:e.scrollWidth,clientWidth:e.clientWidth};
     });
     const scrollables=[document.documentElement,document.body,...document.querySelectorAll('body *')]
      .filter(e=>e.scrollWidth>e.clientWidth+2)
      .slice(0,30).map(e=>{
       const r=e.getBoundingClientRect(),style=getComputedStyle(e);
       return {tag:e.tagName,id:e.id,cls:typeof e.className==='string'?e.className.slice(0,60):'',
         scrollWidth:e.scrollWidth,clientWidth:e.clientWidth,offsetWidth:e.offsetWidth,
         right:Math.round(r.right),left:Math.round(r.left),
         overflowX:style.overflowX,whiteSpace:style.whiteSpace};
      });
     return {overflow:document.documentElement.scrollWidth-viewport,
       bodyOverflow:document.body.scrollWidth-viewport,
       viewport,tabCount:t.length,tabs:t,stopSize:{width:stop.width,height:stop.height},
       offenders,scrollables};
    });
    if(zoom.overflow>2){
     fs.writeFileSync(path.join(out,engineName+'-'+p.name+'-overflow.json'),JSON.stringify(zoom,null,2));
     await page.screenshot({path:path.join(out,engineName+'-'+p.name+'-overflow.png'),
       animations:'disabled',fullPage:true});
    }
    assert.ok(zoom.overflow<=2,'200% text horizontal overflow '+JSON.stringify(zoom));
    assert.ok(zoom.tabs.every(t=>t.width>=44&&t.height>=44),'tab target under 44px');
    if(p.width<=500)assert.ok(zoom.tabs.every(t=>t.left>=0&&t.right<=p.width+2),
      'visible 2x2 picker must not clip at 200% text');
    // Stop must stay in the visual viewport after deep scroll.
    await page.locator('#deck-tab-spatial').click();
    await page.locator('#spatial-gamepad-diagnostics').scrollIntoViewIfNeeded();
    const sticky=await page.locator('#deck-stop').boundingBox();
    assert.ok(sticky&&sticky.y>=-2&&sticky.y+sticky.height<=p.height+2,
      'STOP outside viewport under 200% text '+JSON.stringify(sticky));
    assert.equal(await page.locator('#sound').evaluate(x=>x.paused),true);
    assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true);
    assert.deepEqual(jsErrors,[]);
    const receipt={engine:engineName,profile:p.name,synthetic:true,actualAudio:false,
      realATQualified:false,zoom,stopDelegation:witness,
      stickyStop:{visible:true,width:sticky.width,height:sticky.height}};
    results.push(receipt);
    console.log('PASS '+engineName+' '+p.name+' adverse tab/STOP/200%');
   }finally{await ctx.close();}
  }
 }finally{await browser.close();}
 fs.writeFileSync(path.join(out,engineName+'.json'),JSON.stringify({results,acousticQualified:false},null,2));
})().catch(e=>{console.error(e.stack||e);process.exitCode=1}).finally(()=>{
 host.kill('SIGTERM');
 setTimeout(()=>{if(host.exitCode===null)host.kill('SIGKILL')},1000).unref();
});
