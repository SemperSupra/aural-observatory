/* R15 accessible, OSD-inspired compact cards.
 * Existing audio and trial controls are MOVED, never cloned or reimplemented.
 * A separate always-visible STOP delegates to the existing host-authorized buttons.
 */
(() => {
  'use strict';
  function init() {
    const main = document.querySelector('main#content');
    if (!main || document.getElementById('aural-workspace-deck')) return;
    const groups = [
      {key:'sound',label:'Sound setup',sections:['configuration','manual-sweep']},
      {key:'spatial',label:'Spatial control',sections:['spatial-controller']},
      {key:'search',label:'Quiet search',sections:['experiment','response','feedback']},
      {key:'settings',label:'Preferences',sections:['appearance']}
    ];
    const response = document.getElementById('response-title')?.closest('section');
    if (response && !response.id) response.id = 'response';
    // A missing requirement must not silently produce a partial or inaccessible deck.
    const sections = new Map(groups.flatMap(group => group.sections.map(id => [id,document.getElementById(id)])));
    if ([...sections.values()].some(node=>!node)) {
      console.warn('Aural card deck: missing source sections; preserving classic layout.');
      return;
    }

    const deck = document.createElement('div');
    deck.id = 'aural-workspace-deck';
    const toolbar = document.createElement('div');
    toolbar.className = 'deck-toolbar';
    toolbar.innerHTML = '<div class="deck-nav"><button type="button" class="deck-step" id="deck-prev" aria-label="Previous card">‹</button><div class="deck-tabs" role="tablist" aria-label="Aural workspaces"></div><button type="button" class="deck-step" id="deck-next" aria-label="Next card">›</button></div><div class="deck-tools"><span class="deck-count" id="deck-count" aria-live="polite"></span><button type="button" class="deck-global-stop" id="deck-stop">STOP SOUND</button></div><p class="deck-alert" id="deck-alert" role="status" aria-live="polite"></p>';
    deck.appendChild(toolbar);
    const tabs = toolbar.querySelector('.deck-tabs');
    const alert = toolbar.querySelector('#deck-alert');
    const count = toolbar.querySelector('#deck-count');
    const cards = [];
    for (const [index,group] of groups.entries()) {
      const tab = document.createElement('button');
      tab.type = 'button';
      tab.className = 'deck-tab';
      tab.id = 'deck-tab-' + group.key;
      tab.setAttribute('role','tab');
      tab.setAttribute('aria-controls','deck-panel-'+group.key);
      tab.textContent = group.label;
      tabs.appendChild(tab);

      const panel = document.createElement('div');
      panel.className = 'deck-panel';
      panel.id = 'deck-panel-' + group.key;
      panel.setAttribute('role','tabpanel');
      panel.setAttribute('aria-labelledby',tab.id);
      panel.setAttribute('tabindex','0');
      panel.dataset.index = String(index);
      for (const id of group.sections) panel.appendChild(sections.get(id));
      deck.appendChild(panel);
      cards.push({tab,panel,key:group.key});
    }
    // The safety introduction remains visible above the deck.
    main.appendChild(deck);
    // Keep future source additions visible rather than hiding them silently.
    let current = 0;
    function playing() {
      const trial = document.getElementById('sound');
      const manual = document.getElementById('manual-sound');
      return Boolean((trial && !trial.paused && !trial.ended) ||
                     (manual && !manual.paused && !manual.ended));
    }
    function announce(message,isError=false) {
      alert.textContent=message;
      alert.dataset.alert=isError?'error':'info';
    }
    function select(index,focus=false) {
      if (index<0||index>=cards.length) return false;
      if (current!==index && playing()) {
        announce('Stop the current sound before changing cards. STOP SOUND remains available here.',true);
        return false;
      }
      current=index;
      cards.forEach((card,i)=>{
        const active=i===index;
        card.tab.setAttribute('aria-selected',String(active));
        card.tab.tabIndex=active?0:-1;
        card.panel.hidden=!active;
        card.panel.inert=!active;
      });
      toolbar.querySelector('#deck-prev').disabled=current===0;
      toolbar.querySelector('#deck-next').disabled=current===cards.length-1;
      count.textContent='Card '+(current+1)+' of '+cards.length+' · '+groups[current].label;
      announce('');
      try{window.sessionStorage.setItem('aural-card',groups[current].key);}catch{}
      const selected=cards[current].tab;
      selected.scrollIntoView({block:'nearest',inline:'nearest',behavior:'auto'});
      if (focus) selected.focus({preventScroll:true});
      return true;
    }
    cards.forEach((card,i)=>{
      card.tab.addEventListener('click',()=>select(i,true));
      card.tab.addEventListener('keydown',event=>{
        if (!['ArrowRight','ArrowLeft','Home','End'].includes(event.key)) return;
        event.preventDefault();
        const next=event.key==='Home'?0:event.key==='End'?cards.length-1:
          event.key==='ArrowRight'?Math.min(cards.length-1,current+1):Math.max(0,current-1);
        select(next,true);
      });
    });
    toolbar.querySelector('#deck-prev').addEventListener('click',()=>select(current-1));
    toolbar.querySelector('#deck-next').addEventListener('click',()=>select(current+1));
    toolbar.querySelector('#deck-stop').addEventListener('click',()=>{
      const trial=document.getElementById('sound');
      const manual=document.getElementById('manual-sound');
      let sent=false;
      if (trial && !trial.paused && !trial.ended) {
        document.getElementById('stop')?.click();
        sent=true;
      }
      if (manual && !manual.paused && !manual.ended) {
        document.getElementById('manual-stop')?.click();
        sent=true;
      }
      announce(sent?
        'STOP requested through existing playback controls. Verify actual device silence; browser confirmation is not acoustic proof.':
        'No active browser audio detected. Physical device silence is not independently verified.');
    });
    // Within-page links can select their owning workspace without hidden-target traps.
    const owner = new Map(groups.flatMap((group,index)=>group.sections.map(id=>[id,index])));
    document.querySelectorAll('header nav a[href^="#"]').forEach(link=>{
      link.addEventListener('click',()=>{
        const key=link.getAttribute('href').slice(1);
        if (owner.has(key)) {
          if(!select(owner.get(key),false)) return;
        }
      });
    });
    // Persist only UI layout choice; do not store personal audio or health data.
    let start=0;
    try{const stored=window.sessionStorage.getItem('aural-card');const idx=groups.findIndex(g=>g.key===stored);if(idx>=0)start=idx;}catch{}
    select(start);
  }
  if (document.readyState==='loading') document.addEventListener('DOMContentLoaded',init,{once:true});
  else init();
})();