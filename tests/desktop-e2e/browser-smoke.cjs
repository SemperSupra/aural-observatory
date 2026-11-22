/* Aural Observatory desktop Playwright UI qualification; synthetic data only. */
'use strict';
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const { chromium } = require('playwright');
const AxeBuilder = require('@axe-core/playwright').default;
const fs = require('node:fs');
const path = require('node:path');

const repo=path.resolve(__dirname,'../..');
const env={...process.env,AURAL_NO_BROWSER:'1'};
const project='src/AuralObservatory.SearchDesktop/AuralObservatory.SearchDesktop.csproj';
let server=spawn('dotnet',['run','--no-launch-profile','--project',project,'-c','Release','--','--no-browser'],
  {cwd:repo,env,stdio:['ignore','pipe','pipe']});
let output='', url=null, browser;
server.stdout.on('data',data=>{output+=data.toString(); const m=output.match(/Aural Observatory local (?:search|session):\s*(http:\/\/127\.0\.0\.1:\d+)/);
 if(m)url=m[1];});
server.stderr.on('data',data=>{output+=data.toString();});
function wait(ms){return new Promise(resolve=>setTimeout(resolve,ms));}
async function ready(){
 const deadline=Date.now()+90000;
 while(Date.now()<deadline){
  if(server.exitCode!==null)throw Error('Desktop host exited early:\n'+output);
  if(url){try{const r=await fetch(url);if(r.ok)return url;}catch{}}
  await wait(250);
 }
 throw Error('Timed out starting desktop host:\n'+output);
}
async function run(){
 const address=await ready();
 browser=await chromium.launch({headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1200,height:800},colorScheme:'light',reducedMotion:'reduce'});
 const page=await context.newPage();
 const fatal=[];
 page.on('pageerror',e=>fatal.push(e.message));
 await page.goto(address,{waitUntil:'domcontentloaded'});
 await page.waitForFunction(()=>document.querySelector('#prefs-status').textContent.includes('Settings loaded'),null,{timeout:10000});
 assert.match(await page.locator('body').innerText(),/not medical treatment/i);
 await page.locator('#left-hz').fill('4300');
 await page.locator('#right-hz').fill('6100');
 await page.locator('#left-colour').selectOption('5');
 await page.locator('#right-colour').selectOption('6');
 await page.locator('#left-hz').fill('15000');
 await page.locator('#save-audio').click();
 await page.waitForFunction(()=>document.querySelector('#audio-config-status').textContent.includes('Cannot save'),null,{timeout:5000});
 assert.equal(await page.locator('#left-hz').getAttribute('aria-invalid'),'true');
 await page.locator('#left-hz').fill('4300');
 await page.locator('#save-audio').click();
 await page.waitForFunction(()=>document.querySelector('#audio-config-status').textContent.includes('Profile saved'),null,{timeout:5000});
 await page.reload();
 await page.waitForFunction(()=>document.querySelector('#prefs-status').textContent.includes('loaded'),null,{timeout:10000});
 assert.equal(await page.locator('#left-hz').inputValue(),'4300');
 assert.equal(await page.locator('#right-hz').inputValue(),'6100');
 assert.equal(await page.locator('#left-colour').inputValue(),'5');
 assert.equal(await page.locator('#right-colour').inputValue(),'6');
 assert.equal(await page.locator('#sound').evaluate(x=>x.autoplay),false);
 await page.locator('#deck-tab-settings').click();
 await page.locator('#theme').selectOption('2');
 assert.equal(await page.locator('html').getAttribute('data-theme'),'dark');
 await page.locator('#font-size').evaluate(x=>{x.value='150';x.dispatchEvent(new Event('change',{bubbles:true}));});
 assert.equal(await page.locator('html').evaluate(x=>x.style.fontSize),'150%');
 await page.locator('#high-contrast').check();
 assert.equal(await page.locator('html').getAttribute('data-contrast'),'high');
 await page.locator('#save-prefs').click();
 await page.waitForFunction(()=>document.querySelector('#prefs-status').textContent.includes('saved'),null,{timeout:5000});
 await page.reload();
 await page.waitForFunction(()=>document.querySelector('#prefs-status').textContent.includes('loaded'),null,{timeout:10000});
 assert.equal(await page.locator('#theme').inputValue(),'2');
 assert.equal(await page.locator('#font-size').inputValue(),'150');
 assert.equal(await page.locator('#high-contrast').isChecked(),true);
 await page.locator('#preset-name').fill('CI Synthetic Profile');
 await page.locator('#save-preset').click();
 await page.waitForFunction(()=>Array.from(document.querySelector('#presets').options).some(o=>o.text==='CI Synthetic Profile'),null,{timeout:5000});
 await page.locator('#deck-tab-sound').click();
 await page.locator('#left-hz').fill('5200');
 await page.locator('#save-audio').click();
 await page.waitForFunction(()=>document.querySelector('#audio-config-status').textContent.includes('Profile saved'),null,{timeout:5000});
 await page.locator('#deck-tab-settings').click();
 await page.locator('#presets').selectOption({label:'CI Synthetic Profile'});
 await page.locator('#load-preset').click();
 await page.waitForFunction(()=>document.querySelector('#left-hz').value==='4300',null,{timeout:5000});
 assert.equal(await page.locator('#left-hz').inputValue(),'4300');
 await page.locator('#deck-tab-spatial').click();
 // R10: real C# WAV generation through authenticated API; no browser DSP, no study-score mutation.
 await page.locator('#manual-seconds').fill('120');
 await page.locator('#manual-start').click();
 await page.waitForFunction(()=>document.querySelector('#manual-sweep-status').textContent.includes('Sweep rejected'),null,{timeout:10000});
 await page.locator('#manual-seconds').fill('8');
 await page.locator('#manual-mode').selectOption('ReferenceTone');
 await page.locator('#manual-ear').selectOption('Left');
 await page.locator('#manual-start').click();
 await page.waitForFunction(()=>document.querySelector('#manual-sound').src.startsWith('blob:'),null,{timeout:30000});
 const sweepSrc=await page.locator('#manual-sound').getAttribute('src');
 assert.ok(sweepSrc.startsWith('blob:'),'Manual sweep served as a token-authenticated local Blob URL');
 assert.ok(!sweepSrc.includes('token='),'Manual media URL contains no query credential');
 assert.equal(await page.locator('#manual-mark').isDisabled(),false,'Subjective frequency-mark control becomes available when prepared');
 assert.equal(await page.locator('#sound').getAttribute('src'),null,'Randomized quiet-search player remains untouched');
 await page.locator('#manual-stop').click();
 await page.waitForFunction(()=>document.querySelector('#manual-sweep-status').textContent.includes('stopped'),null,{timeout:10000});
 assert.equal(await page.locator('#manual-start').isDisabled(),false,'Manual sweep available after stop');
 // R12: real Chromium controller UI + local Web Audio spatial adapter; synthetic data only.
 assert.equal(await page.locator('#spatial-enable').isChecked(),false,'3D sound movement never starts automatically');
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'spatial UI never autoplays');
 await page.locator('#manual-ear').selectOption('Left');
 // The app must undo a disallowed opt-in; Playwright .check() would fail
 // because it expects the checkbox to remain checked after the click.
 await page.locator('#spatial-enable').click();
 await page.waitForFunction(()=>document.querySelector('#spatial-status').textContent.includes('requires the unscored'),null,{timeout:5000});
 assert.equal(await page.locator('#spatial-enable').isChecked(),false,'ear-isolated trial cannot enter binaural panner');
 await page.locator('#manual-ear').selectOption('Both');
 await page.locator('#spatial-enable').check();
 await page.waitForFunction(()=>document.querySelector('#spatial-status').textContent.includes('enabled for manual'),null,{timeout:5000});
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'enabling panner cannot start playback');
 await page.locator('#spatial-azimuth').evaluate(x=>{x.value='90';x.dispatchEvent(new Event('input',{bubbles:true}));});
 assert.equal(await page.locator('#spatial-value-azimuth').innerText(),'90°');
 await page.locator('#spatial-pad').focus();
 await page.keyboard.press('ArrowLeft');
 assert.equal(await page.locator('#spatial-value-azimuth').innerText(),'85°','keyboard changes position');
 await page.locator('#spatial-center').click();
 assert.equal(await page.locator('#spatial-value-azimuth').innerText(),'0°','center restores front');
 await page.locator('#spatial-secondary-pad').focus();
 await page.keyboard.press('ArrowUp');
 assert.equal(await page.locator('#spatial-value-elevation').innerText(),'5°','second stick keyboard controls elevation');
 await page.locator('#spatial-virtual-center').click();
 assert.equal(await page.locator('#spatial-value-elevation').innerText(),'0°','virtual X button restores elevation');
 await page.locator('#spatial-virtual-mark').click();
 await page.waitForFunction(()=>document.querySelector('#spatial-status').textContent.includes('Mark is available only'),null,{timeout:5000});
 await page.locator('#spatial-virtual-play').click();
 await page.waitForFunction(()=>document.querySelector('#spatial-status').textContent.includes('Prepare a manual reference tone'),null,{timeout:5000});
 // CDP browser API shim is only a synthetic input fixture; NO real USB claim.
 await page.evaluate(()=>{
   window.__syntheticGamepad={index:0,id:'Synthetic Xbox controller',mapping:'standard',
     axes:[0,0,0,0],buttons:Array.from({length:17},()=>({pressed:false,value:0}))};
   Object.defineProperty(navigator,'getGamepads',{configurable:true,value:()=>[window.__syntheticGamepad]});
   window.dispatchEvent(new Event('gamepadconnected'));
 });
 await page.locator('#spatial-use-gamepad').check();
 await page.waitForFunction(()=>document.querySelector('#spatial-gamepad-status').textContent.includes('Synthetic Xbox controller'),null,{timeout:5000});
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'gamepad discovery cannot start playback');
 await page.evaluate(()=>{window.__syntheticGamepad.axes[0]=0.8;});
 await page.waitForFunction(()=>Number(document.querySelector('#spatial-azimuth').value)>5,null,{timeout:5000});
 await page.waitForFunction(()=>document.querySelector('#spatial-gamepad-diagnostics').textContent.includes('0:0.80'),null,{timeout:5000});
 const beforeInversion=Number(await page.locator('#spatial-azimuth').inputValue());
 await page.locator('#spatial-invert-yaw').check();
 await page.waitForFunction(before=>Number(document.querySelector('#spatial-azimuth').value)<before-2,beforeInversion,{timeout:5000});
 await page.evaluate(()=>{window.__syntheticGamepad.axes[0]=0;window.__syntheticGamepad.buttons[1]={pressed:true,value:1};});
 await page.waitForFunction(()=>document.querySelector('#spatial-status').textContent.includes('Gamepad STOP requested'),null,{timeout:5000});
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'gamepad STOP leaves manual player paused');
 await page.locator('#spatial-use-gamepad').uncheck();
 // Spatial preview still uses the original C#-generated WAV, never randomized study scoring.
 await page.locator('#manual-start').click();
 await page.waitForFunction(()=>document.querySelector('#manual-sound').src.startsWith('blob:'),null,{timeout:30000});
 assert.equal(await page.locator('#sound').getAttribute('src'),null,'spatial preview never writes randomized trial audio');
 await page.locator('#spatial-virtual-stop').click();
 await page.waitForFunction(()=>document.querySelector('#manual-sweep-status').textContent.includes('stopped'),null,{timeout:10000});
 assert.equal(await page.locator('#manual-sound').evaluate(x=>x.paused),true,'virtual B STOP cancels manual audio');
 const visualDir=path.join(repo,'artifacts','ui');
 fs.mkdirSync(visualDir,{recursive:true});
 await page.locator('#spatial-controller').screenshot({path:path.join(visualDir,'r12-spatial-controller.png'),animations:'disabled'});
 await page.setViewportSize({width:320,height:700});
 const bounds=await page.evaluate(()=>({vw:window.innerWidth,sw:document.documentElement.scrollWidth}));
 assert.ok(bounds.sw<=bounds.vw+2,'No horizontal scrolling at 320 CSS px: '+JSON.stringify(bounds));
 await page.locator('#deck-tab-settings').click();
 await page.locator('#theme').selectOption('1');
 await page.locator('#high-contrast').uncheck();
 await page.locator('#save-prefs').click();
 const axe=new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa']);
 const report=await axe.analyze();
 const critical=report.violations.filter(v=>v.impact==='critical'||v.impact==='serious');
 assert.deepEqual(critical.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.length})),[],'No serious/critical automated accessibility violations');
 assert.deepEqual(fatal,[],'No uncaught page errors');
 const evidence={schemaVersion:1,scope:'synthetic desktop browser',browser:'Chromium headless',
   platform:process.platform,at:new Date().toISOString(),checks:['ear-editor','input-bounds','corrected-noise-selection','theme','scale','contrast','persist','presets','manual-sweep-bounds','authenticated-WAV-blob','manual-stop','spatial-virtual-dual-sticks','spatial-actions-A-B-X-Y','spatial-HRTF-opt-in','spatial-ear-isolation','spatial-gamepad-synthetic','spatial-legacy-diagnostics-inversion','spatial-no-autoplay','320px-reflow','axe-automated-WCAG21'],
   automatedViolations:report.violations.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.length})),
   manualScreenReaderVerified:false,physicalAudioVerified:false};
 const target=path.join(repo,'artifacts','ui');
 fs.mkdirSync(target,{recursive:true});
 fs.writeFileSync(path.join(target,'desktop-browser-smoke.json'),JSON.stringify(evidence,null,2)+'\n');
 console.log('PASS: desktop browser smoke; saved '+path.join(target,'desktop-browser-smoke.json'));
}
(async()=>{try{await run();}catch(e){console.error(e.stack||e);process.exitCode=1;}
finally{try{await browser?.close();}catch{} if(server&&server.exitCode===null)server.kill('SIGTERM');}})();
