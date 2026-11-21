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
 const page=await browser.newPage({viewport:{width:1200,height:800},colorScheme:'light',reducedMotion:'reduce'});
 const fatal=[];
 page.on('pageerror',e=>fatal.push(e.message));
 await page.goto(address,{waitUntil:'domcontentloaded'});
 await page.waitForFunction(()=>document.querySelector('#prefs-status').textContent.includes('Settings loaded'),null,{timeout:10000});
 assert.match(await page.locator('body').innerText(),/not medical treatment/i);
 assert.equal(await page.locator('audio').evaluate(x=>x.autoplay),false);
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
 await page.setViewportSize({width:320,height:700});
 const bounds=await page.evaluate(()=>({vw:window.innerWidth,sw:document.documentElement.scrollWidth}));
 assert.ok(bounds.sw<=bounds.vw+2,'No horizontal scrolling at 320 CSS px: '+JSON.stringify(bounds));
 await page.locator('#theme').selectOption('1');
 await page.locator('#high-contrast').uncheck();
 await page.locator('#save-prefs').click();
 const axe=new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa']);
 const report=await axe.analyze();
 const critical=report.violations.filter(v=>v.impact==='critical'||v.impact==='serious');
 assert.deepEqual(critical.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.length})),[],'No serious/critical automated accessibility violations');
 assert.deepEqual(fatal,[],'No uncaught page errors');
 const evidence={schemaVersion:1,scope:'synthetic desktop browser',browser:'Chromium headless',
   platform:process.platform,at:new Date().toISOString(),checks:['theme','scale','contrast','persist','presets','320px-reflow','axe-automated-WCAG21'],
   automatedViolations:report.violations.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.length})),
   manualScreenReaderVerified:false,physicalAudioVerified:false};
 const target=path.join(repo,'artifacts','ui');
 fs.mkdirSync(target,{recursive:true});
 fs.writeFileSync(path.join(target,'desktop-browser-smoke.json'),JSON.stringify(evidence,null,2)+'\n');
 console.log('PASS: desktop browser smoke; saved '+path.join(target,'desktop-browser-smoke.json'));
}
(async()=>{try{await run();}catch(e){console.error(e.stack||e);process.exitCode=1;}
finally{try{await browser?.close();}catch{} if(server&&server.exitCode===null)server.kill('SIGTERM');}})();
