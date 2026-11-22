/* R15 OFFLINE HRTF sensor: synthetically renders Web Audio PannerNode to an
 * OfflineAudioContext. There is NO connection to real output hardware.
 * Evidence is transfer-function response, NOT human localization or SPL.
 */
'use strict';
const {chromium,webkit}=require('../desktop-e2e/node_modules/playwright');
const fs=require('node:fs');const path=require('node:path');
const assert=require('node:assert/strict');
const repo=path.resolve(__dirname,'../..'), out=path.join(repo,'artifacts','audio','r15');
fs.mkdirSync(out,{recursive:true});
const scenarios=[
 {name:'front',yaw:0,elevation:0,distance:1},
 {name:'left',yaw:-75,elevation:0,distance:1},
 {name:'right',yaw:75,elevation:0,distance:1},
 {name:'rear',yaw:180,elevation:0,distance:1},
 {name:'above',yaw:0,elevation:35,distance:1},
 {name:'far-right',yaw:75,elevation:0,distance:2}
];
const results=[];
(async()=>{
 for(const [name,api] of Object.entries({chromium,webkit})){
  const browser=await api.launch({headless:true});
  try{
   const page=await browser.newPage();
   for(const scenario of scenarios){
    const metrics=await page.evaluate(async(cfg)=>{
      const ctx=new OfflineAudioContext(2,48000,48000);
      const input=ctx.createBuffer(1,48000,48000);
      const samples=input.getChannelData(0);
      let state=0x9e3779b9;
      // Deterministic unit test broadband noise, NOT treatment or product audio.
      for(let n=0;n<samples.length;n++){
        state ^= state<<13;state ^= state>>>17;state ^= state<<5;
        const fade=Math.min(1,n/480,(samples.length-1-n)/480);
        samples[n]=((state>>>0)/4294967296*2-1)*0.0125*Math.max(0,fade);
      }
      const src=ctx.createBufferSource();src.buffer=input;
      const p=ctx.createPanner();p.panningModel='HRTF';
      p.distanceModel='inverse';p.refDistance=1;p.rolloffFactor=1;p.maxDistance=3;
      const a=cfg.yaw*Math.PI/180, e=cfg.elevation*Math.PI/180;
      p.positionX.value=Math.sin(a)*Math.cos(e)*cfg.distance;
      p.positionY.value=Math.sin(e)*cfg.distance;
      p.positionZ.value=-Math.cos(a)*Math.cos(e)*cfg.distance;
      src.connect(p);p.connect(ctx.destination);
      src.start(0);
      const rendered=await ctx.startRendering();
      const l=rendered.getChannelData(0),r=rendered.getChannelData(1);
      let L=0,R=0,peak=0,lr=0,n=0,finite=true;
      for(let i=2400;i<l.length-2400;i++){
        const a=l[i],b=r[i];if(!Number.isFinite(a)||!Number.isFinite(b))finite=false;
        L+=a*a;R+=b*b;lr+=a*b;n++;peak=Math.max(peak,Math.abs(a),Math.abs(b));
      }
      const rmsL=Math.sqrt(L/n),rmsR=Math.sqrt(R/n);
      return {channels:rendered.numberOfChannels,samples:rendered.length,sampleRate:rendered.sampleRate,
        rmsL,rmsR,channelRatioDb:20*Math.log10((rmsL+1e-12)/(rmsR+1e-12)),
        zeroLagCorrelation:lr/Math.sqrt(Math.max(1e-20,L*R)),peak,finite};
    },scenario);
    assert.equal(metrics.channels,2);
    assert.equal(metrics.samples,48000);
    assert.equal(metrics.sampleRate,48000);
    assert.ok(metrics.finite && metrics.peak<0.2 && metrics.rmsL>0 && metrics.rmsR>0,
      'No silence, invalid samples or digital overload '+JSON.stringify({name,scenario,metrics}));
    results.push({engine:name,scenario:scenario.name,...metrics});
   }
   await page.close();
  }finally{await browser.close();}
 }
 const receipt={schemaVersion:1,mode:'OFFLINE_ONLY',source:'synthetic deterministic 48k mono noise',engines:['chromium','webkit'],
   physicalHeadphonesQualified:false,loudspeakerCrosstalkQualified:false,perceptualLocalizationQualified:false,
   calibratedSPL:false,audibleOutput:false,results};
 fs.writeFileSync(path.join(out,'offline-hrtf.json'),JSON.stringify(receipt,null,2)+'\n');
 console.log(JSON.stringify({status:'PASS/OFFLINE-ONLY',rows:results.length,
  channelRatioDb:results.map(x=>({engine:x.engine,scenario:x.scenario,delta:x.channelRatioDb.toFixed(2)}))}));
})().catch(e=>{console.error(e);process.exitCode=1;});
