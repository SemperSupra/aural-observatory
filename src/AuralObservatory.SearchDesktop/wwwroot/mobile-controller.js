/* R13 contextual mobile/touch overlay.
 * Based on general interface patterns established in Open Space Duel, not
 * copied source. Aural native DSP, localhost token and experiment remain owners.
 * Virtual pads are input ONLY: never call a sound-start endpoint from motion.
 */
(() => {
  'use strict';
  const get = id => document.getElementById(id);
  const root = get('spatial-controller');
  if (!root || !get('spatial-azimuth') || !get('spatial-secondary-pad')) return;
  const open = document.createElement('button');
  open.type = 'button';
  open.id = 'touch-controller-open';
  open.textContent = 'Open full-screen touch controller';
  open.setAttribute('aria-haspopup','dialog');
  open.setAttribute('aria-controls','aural-touch-overlay');
  root.querySelector('.spatial-layout').before(open);

  const overlay = document.createElement('div');
  overlay.id = 'aural-touch-overlay';
  overlay.hidden = true;
  overlay.setAttribute('role','dialog');
  overlay.setAttribute('aria-modal','true');
  overlay.setAttribute('aria-labelledby','touch-heading');
  overlay.innerHTML = `
    <header class="aural-touch-header">
      <strong id="touch-heading">Spatial sound · Touch controller</strong>
      <span class="aural-touch-live" id="touch-coordinates" aria-live="off">Front · 0° / 0° / 1.0×</span>
      <button type="button" id="touch-emergency-stop" class="aural-touch-stop">STOP</button>
      <button type="button" id="touch-close">Close + STOP</button>
    </header>
    <div class="aural-touch-stage">
      <div class="aural-touch-head" aria-hidden="true">FRONT<span>HEAD</span>BACK</div>
      <div id="touch-left-zone" class="aural-touch-zone" role="group" tabindex="0"
        aria-label="Left virtual analog stick: touch anywhere then drag to position sound around head; arrow keys adjust azimuth">
        <span class="aural-touch-zone-label">LEFT · DIRECTION</span>
        <div id="touch-left-base" class="aural-touch-base" aria-hidden="true">
          <div id="touch-left-knob" class="aural-touch-knob"></div>
        </div>
      </div>
      <div id="touch-right-zone" class="aural-touch-zone" role="group" tabindex="0"
        aria-label="Right virtual analog stick: drag up or down for elevation, right for distance; arrow keys adjust">
        <span class="aural-touch-zone-label">RIGHT · HEIGHT / DISTANCE</span>
        <div id="touch-right-base" class="aural-touch-base" aria-hidden="true">
          <div id="touch-right-knob" class="aural-touch-knob"></div>
        </div>
      </div>
    </div>
    <footer class="aural-touch-footer">
      <div class="aural-touch-actions">
        <button type="button" id="touch-mark">A · Mark</button>
        <button type="button" id="touch-stop" class="aural-touch-stop">B · STOP</button>
        <button type="button" id="touch-center">X · Center</button>
        <button type="button" id="touch-play">Y · Play prepared</button>
      </div>
      <label class="aural-touch-enable"><input type="checkbox" id="touch-hrtf">
        Enable 3D HRTF for bilateral reference-tone preview</label>
      <div id="touch-status" role="status" aria-live="polite">
        Movement changes position only. Prepare sound manually before Y can play it.
      </div>
    </footer>`;
  document.body.appendChild(overlay);
  const main = document.querySelector('main');
  const header = document.querySelector('body > header');
  const saved = {overflow:'', mainInert:false, headerInert:false};
  const panners = {
    left: {id:null,originX:0,originY:0,radius:60},
    right:{id:null,originX:0,originY:0,radius:60}
  };
  let visible=false;
  const clamp=(x,min,max)=>Math.min(max,Math.max(min,Number.isFinite(x)?x:min));
  const text=message=>{get('touch-status').textContent=message};
  const rangeValue=id=>Number(get(id).value);
  const sample=()=>({
    azimuth:rangeValue('spatial-azimuth'),
    elevation:rangeValue('spatial-elevation'),
    distance:rangeValue('spatial-distance')
  });
  function report(){
    const s=sample();
    get('touch-coordinates').textContent='Azimuth '+Math.round(s.azimuth)+
      '° · Elevation '+Math.round(s.elevation)+'° · Distance '+s.distance.toFixed(1)+'×';
  }
  function setRange(id,x){
    const node=get(id);
    node.value=String(clamp(x,Number(node.min),Number(node.max)));
    node.dispatchEvent(new Event('input',{bubbles:true}));
    report();
  }
  function stopIfActive(){
    const manual=get('manual-sound');
    const trial=get('sound');
    // These are existing user-gesture STOP controls. No direct host claim or
    // route to audio start is added; physical silence remains unverified.
    if (!manual.paused || manual.getAttribute('src') || get('manual-start').disabled) {
      get('manual-stop').click();
    }
    if (!trial.paused && !trial.ended) get('stop').click();
  }
  function endPad(side){
    const p=panners[side];
    p.id=null;
    get('touch-'+side+'-base').hidden=true;
  }
  function cancelAll(){
    endPad('left');
    endPad('right');
  }
  function close(){
    if(!visible)return;
    // Discard gestures, and stop before re-exposing the main app. Closing a
    // controller may NEVER silently leave an active preview unattended.
    cancelAll();stopIfActive();
    visible=false;overlay.hidden=true;
    document.body.style.overflow=saved.overflow;
    if(main)main.inert=saved.mainInert;
    if(header)header.inert=saved.headerInert;
    open.focus({preventScroll:true});
  }
  function launch(){
    if(visible)return;
    saved.overflow=document.body.style.overflow;
    saved.mainInert=!!main?.inert;
    saved.headerInert=!!header?.inert;
    document.body.style.overflow='hidden';
    if(main)main.inert=true;
    if(header)header.inert=true;
    visible=true;
    overlay.hidden=false;
    get('touch-hrtf').checked=get('spatial-enable').checked;
    report();
    text('Two-touch movement enabled. Motion cannot start audio; STOP and Close remain available.');
    get('touch-emergency-stop').focus({preventScroll:true});
  }
  open.addEventListener('click',launch);
  get('touch-close').addEventListener('click',close);
  for(const id of ['touch-emergency-stop','touch-stop']){
    get(id).addEventListener('click',()=>{
      stopIfActive();
      text('STOP requested; physical host/device silence is not measured.');
    });
  }
  get('touch-mark').addEventListener('click',()=>get('spatial-virtual-mark').click());
  get('touch-center').addEventListener('click',()=>{
    get('spatial-virtual-center').click();report();text('Spatial position re-centered.');
  });
  get('touch-play').addEventListener('click',()=>{
    // Only an actual explicit button click may invoke the existing
    // 'Play prepared' button; never on touch movement, attach, or tilt.
    get('spatial-virtual-play').click();
    text('Play-prepared requested by user. No sound if no preview is prepared.');
  });
  get('touch-hrtf').addEventListener('change',()=>{
    const canonical=get('spatial-enable');
    if(canonical.checked!==get('touch-hrtf').checked)canonical.click();
    get('touch-hrtf').checked=canonical.checked;
    text(canonical.checked ?
      '3D rendering enabled for eligible manual bilateral reference tone only.' :
      '3D rendering disabled or unavailable for current ear/mode.');
  });
  function displace(side,event){
    const p=panners[side];
    const dx=event.clientX-p.originX;
    const dy=event.clientY-p.originY;
    const dist=Math.hypot(dx,dy);
    const radius=p.radius;
    const scalar=dist>radius?radius/dist:1;
    const boundedX=dx*scalar, boundedY=dy*scalar;
    const knob=get('touch-'+side+'-knob');
    knob.style.transform='translate(-50%, -50%) translate('+boundedX+'px,'+boundedY+'px)';
    // A nontrivial deadzone prevents involuntary drift and noisy touch start.
    if(dist<radius*.14)return;
    if(side==='left'){
      setRange('spatial-azimuth',Math.atan2(dx,-dy)*180/Math.PI);
    }else{
      setRange('spatial-elevation',-boundedY/radius*45);
      setRange('spatial-distance',1+Math.max(0,boundedX/radius)*2);
    }
  }
  for(const side of ['left','right']){
    const node=get('touch-'+side+'-zone'),base=get('touch-'+side+'-base');
    node.addEventListener('pointerdown',event=>{
      const p=panners[side];
      if(!visible || p.id!==null || !event.isPrimary && event.pointerType==='mouse')return;
      if(event.cancelable)event.preventDefault();
      p.id=event.pointerId;
      p.originX=event.clientX;p.originY=event.clientY;
      const rect=node.getBoundingClientRect();
      // Dynamic floating center bounded to the current zone; no fixed
      // tap coordinate and no interference between the two active touches.
      base.style.left=(event.clientX-rect.left)+'px';
      base.style.top=(event.clientY-rect.top)+'px';
      p.radius=clamp(Math.min(rect.width,rect.height)*.32,40,76);
      base.style.setProperty('--touch-radius',p.radius+'px');
      base.hidden=false;
      get('touch-'+side+'-knob').style.transform='translate(-50%, -50%)';
      try {node.setPointerCapture(event.pointerId)}catch{}
    });
    node.addEventListener('pointermove',event=>{
      if(!visible||panners[side].id!==event.pointerId)return;
      if(event.cancelable)event.preventDefault();
      displace(side,event);
    });
    for(const name of ['pointerup','pointercancel','lostpointercapture']){
      node.addEventListener(name,event=>{
        if(panners[side].id===event.pointerId)endPad(side);
      });
    }
    node.addEventListener('keydown',event=>{
      const s=sample();
      if(event.key==='Home'){
        event.preventDefault();
        if(side==='left')setRange('spatial-azimuth',0);
        else{setRange('spatial-elevation',0);setRange('spatial-distance',1)}
        return;
      }
      const horiz=event.key==='ArrowLeft'?-1:event.key==='ArrowRight'?1:0;
      const vert=event.key==='ArrowUp'?1:event.key==='ArrowDown'?-1:0;
      if(!horiz&&!vert)return;
      event.preventDefault();
      if(side==='left')setRange('spatial-azimuth',s.azimuth+horiz*5);
      else {
        setRange('spatial-elevation',s.elevation+vert*5);
        setRange('spatial-distance',s.distance+horiz*.1);
      }
    });
  }
  // Losing input always clears both touch IDs. Do not leave an active
  // hidden overlay or inadvertently start/keep audio on a suspended page.
  window.addEventListener('blur',()=>{
    cancelAll();
    if(visible)stopIfActive();
  });
  document.addEventListener('visibilitychange',()=>{
    if(document.hidden && visible)close();
  });
  window.addEventListener('pagehide',()=>{if(visible)close()});
  window.addEventListener('resize',cancelAll);
  window.addEventListener('orientationchange',cancelAll);
  overlay.addEventListener('keydown',e=>{
    if(e.key==='Escape'){
      e.preventDefault();e.stopPropagation();
      close();
    }
  });
})();
