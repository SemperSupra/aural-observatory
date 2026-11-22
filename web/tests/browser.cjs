/* R14 visual-only Pages projection browser acceptance. No audio, personal data or devices. */
"use strict";
const assert = require("node:assert/strict");
const path = require("node:path");
const fs = require("node:fs");
const http = require("node:http");
const { chromium, firefox, webkit } = require("../../tests/desktop-e2e/node_modules/playwright");
const AxeBuilder = require("../../tests/desktop-e2e/node_modules/@axe-core/playwright").default;
const root = path.resolve(__dirname, "..");
const prefix = "/aural-observatory/";
const evidenceDir = path.resolve(__dirname, "../evidence");
fs.mkdirSync(evidenceDir,{recursive:true});
const contentTypes={".html":"text/html; charset=utf-8",".css":"text/css",".js":"text/javascript",".svg":"image/svg+xml",".json":"application/json",".webmanifest":"application/manifest+json"};
const server=http.createServer((req,res)=>{
  const pathname=new URL(req.url,"http://127.0.0.1").pathname;
  if(!pathname.startsWith(prefix)){res.writeHead(404);res.end("not found");return;}
  const name=pathname.slice(prefix.length)||"index.html";
  const file=path.resolve(root,name);
  if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end("forbidden");return;}
  try{
    const data=fs.readFileSync(file);
    res.writeHead(200,{"Content-Type":contentTypes[path.extname(file)]||"text/plain","Cache-Control":"no-store"});
    res.end(data);
  }catch{res.writeHead(404);res.end("not found");}
});
const cases=[
  {name:"phone-small",width:320,height:568,touch:true},
  {name:"phone-landscape",width:844,height:390,touch:true},
  {name:"tablet",width:768,height:1024,touch:true},
  {name:"desktop",width:1280,height:800,touch:false}
];
const report=[];
(async()=>{
  await new Promise(resolve=>server.listen(0,"127.0.0.1",resolve));
  const address=server.address();
  const url="http://127.0.0.1:"+address.port+prefix;
  try{
    for(const [engineName,engine] of Object.entries({chromium,firefox,webkit})){
      const browser=await engine.launch({headless:true});
      try{
        for(const profile of cases){
          const context=await browser.newContext({viewport:{width:profile.width,height:profile.height},hasTouch:profile.touch,isMobile:profile.touch && engineName !== "firefox",deviceScaleFactor:profile.touch?2:1,reducedMotion:"reduce",colorScheme:"light"});
          const page=await context.newPage();
          const errors=[];
          page.on("pageerror",e=>errors.push(String(e)));
          page.on("request",r=>{if(!r.url().startsWith("http://127.0.0.1:"+address.port))errors.push("Unexpected remote resource: "+r.url());});
          await page.goto(url,{waitUntil:"networkidle"});
          assert.equal(await page.locator("html").getAttribute("lang"),"en");
          assert.equal(await page.locator("h1").count(),1);
          assert.equal(await page.locator("audio").count(),0);
          assert.equal(await page.locator("video").count(),0);
          assert.ok((await page.locator("body").innerText()).includes("Visual preview only"));
          const overflow=await page.evaluate(()=>document.documentElement.scrollWidth-innerWidth);
          assert.ok(overflow<=2,engineName+"/"+profile.name+" overflow "+overflow);
          const stop=await page.locator("#reset").boundingBox();
          assert.ok(stop&&stop.height>=44&&stop.width>=44,"STOP/RESET target size");
          assert.ok(stop.x>=0&&stop.x+stop.width<=profile.width+2,"STOP/RESET clipped");
          await page.locator("#azimuth").evaluate(el => { el.value = "90"; el.dispatchEvent(new Event("input",{bubbles:true})); });
          assert.equal(await page.locator("#azimuth-value").textContent(),"90°");
          await page.locator("#reset").click();
          assert.equal(await page.locator("#azimuth-value").textContent(),"0°");
          assert.match(await page.locator("#feedback").textContent(),/No sound was playing/);
          await page.getByRole("radio",{name:"Dark"}).check();
          assert.equal(await page.locator("html").getAttribute("data-theme"),"dark");
          await page.reload({waitUntil:"networkidle"});
          assert.equal(await page.locator("html").getAttribute("data-theme"),"dark");
          await page.getByRole("radio",{name:"System"}).check();
          const axe=await new AxeBuilder({page}).withTags(["wcag2a","wcag2aa","wcag21a","wcag21aa"]).analyze();
          const serious=axe.violations.filter(v=>["serious","critical"].includes(v.impact));
          assert.equal(serious.length,0,engineName+"/"+profile.name+" axe "+JSON.stringify(serious.map(v=>({id:v.id,impact:v.impact}))));
          assert.deepEqual(errors,[]);
          const png=path.join(evidenceDir,engineName+"-"+profile.name+".png");
          await page.screenshot({path:png,fullPage:true});
          report.push({engine:engineName,profile:profile.name,viewport:[profile.width,profile.height],touch:profile.touch,overflow,axeViolations:axe.violations.map(v=>({id:v.id,impact:v.impact})),screenshot:path.basename(png)});
          await context.close();
        }
      }finally{await browser.close();}
    }
    const context=await chromium.launch({headless:true});
    const nojs=await context.newContext({javaScriptEnabled:false});
    const p=await nojs.newPage();
    await p.goto(url);
    assert.ok((await p.locator("#scope").innerText()).includes("Web / PWA"));
    await nojs.close();await context.close();
    fs.writeFileSync(path.join(evidenceDir,"r14-pwa-browser-receipt.json"),JSON.stringify({source:"R14 public visual preview",soundOutput:false,profiles:report},null,2));
    console.log("PASS "+report.length+" real-browser profiles. No audio or user data.");
  }finally{await new Promise(resolve=>server.close(resolve));}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});