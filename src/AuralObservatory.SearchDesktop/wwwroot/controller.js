/* R12 exploratory spatial controller: browser input/platform adapter only.
 * Original C# generates bounded manual WAV; this module synthesizes NO audio.
 * HRTF positioning is restricted to unscored bilateral reference-tone previews.
 */
(() => {
  'use strict';
  const byId = id => document.getElementById(id);
  const root = byId('spatial-controller');
  if (!root) return;
  const manual = byId('manual-sound');
  const enable = byId('spatial-enable');
  const status = byId('spatial-status');
  const axes = {
    yaw: byId('spatial-axis-yaw'), elevation: byId('spatial-axis-elevation'),
    distance: byId('spatial-axis-distance')
  };
  const ranges = {azimuth: byId('spatial-azimuth'), elevation: byId('spatial-elevation'),
    distance: byId('spatial-distance')};
  const pad = byId('spatial-pad');
  const dot = byId('spatial-emitter');
  const secondaryPad = byId('spatial-secondary-pad');
  const secondaryDot = byId('spatial-secondary-emitter');
  const gamepadToggle = byId('spatial-use-gamepad');
  const gamepadPicker = byId('spatial-gamepads');
  const deviceStatus = byId('spatial-gamepad-status');
  const diagnostics = byId('spatial-gamepad-diagnostics');
  const invert = {
    yaw: byId('spatial-invert-yaw'),
    elevation: byId('spatial-invert-elevation'),
    distance: byId('spatial-invert-distance')
  };
  const gpStop = byId('spatial-button-stop');
  const gpMark = byId('spatial-button-mark');
  const gpCenter = byId('spatial-button-center');
  const clamp = (value, low, high) => Math.min(high, Math.max(low, Number.isFinite(value) ? value : 0));
  const position = { azimuth: 0, elevation: 0, distance: 1 };
  let audioContext = null, mediaSource = null, panner = null, outputGain = null;
  let frameTime = 0, lastGamepadId = '', lastButtons = [], lastDiagnosticTime = 0;
  const activeManual = () => !manual.paused && !manual.ended;
  function eligible() {
    return byId('manual-mode').value === 'ReferenceTone' && byId('manual-ear').value === 'Both';
  }
  function announce(message) { status.textContent = message; }
  function updatePosition() {
    const radians = position.azimuth * Math.PI / 180;
    const altitude = position.elevation * Math.PI / 180;
    const horizontal = Math.cos(altitude) * position.distance;
    const x = Math.sin(radians) * horizontal;
    const y = Math.sin(altitude) * position.distance;
    const z = -Math.cos(radians) * horizontal; // listener faces -Z; +Z is behind.
    for (const [key,value] of Object.entries(position)) {
      const node = ranges[key];
      node.value = String(value);
      byId('spatial-value-' + key).textContent =
        key === 'distance' ? value.toFixed(1) + '×' : Math.round(value) + '°';
    }
    const normalizedAzimuth = position.azimuth * Math.PI / 180;
    const normalizedRadius = Math.min(1, Math.abs(position.azimuth) / 180);
    // A top-view map shows angle around the head; center is the listening head.
    dot.style.left = (50 + Math.sin(normalizedAzimuth) * (12 + 35 * normalizedRadius)) + '%';
    dot.style.top = (50 - Math.cos(normalizedAzimuth) * (12 + 35 * normalizedRadius)) + '%';
    // Second on-screen stick is a direct, bounded height/distance control.
    secondaryDot.style.left = (20 + 60 * ((position.distance - 1) / 2)) + '%';
    secondaryDot.style.top = (50 - 30 * (position.elevation / 45)) + '%';
    if (panner && enable.checked && eligible() && audioContext) {
      const now = audioContext.currentTime;
      panner.positionX.setTargetAtTime(x, now, .025);
      panner.positionY.setTargetAtTime(y, now, .025);
      panner.positionZ.setTargetAtTime(z, now, .025);
    }
  }
  function positionAt(azimuth, elevation, distance) {
    position.azimuth = clamp(azimuth, -180, 180);
    position.elevation = clamp(elevation, -45, 45);
    position.distance = clamp(distance, 1, 3);
    updatePosition();
  }
  function wire(spatial) {
    if (!mediaSource || !outputGain || !audioContext) return;
    mediaSource.disconnect();
    if (panner) panner.disconnect();
    outputGain.disconnect();
    if (spatial && panner) {
      mediaSource.connect(panner);
      panner.connect(outputGain);
    } else {
      mediaSource.connect(outputGain);
    }
    outputGain.connect(audioContext.destination);
  }
  async function setupAudio() {
    if (!eligible()) {
      enable.checked = false;
      announce('3D positioning requires the unscored manual Reference tone in Both ears. The randomized trial remains unchanged.');
      wire(false);
      return;
    }
    if (!enable.checked) {
      wire(false);
      announce('3D positioning off. Manual WAV remains attenuated; no automatic playback.');
      return;
    }
    const AudioContextClass = window.AudioContext || window.webkitAudioContext;
    if (!AudioContextClass) {
      enable.checked = false;
      announce('This browser does not provide Web Audio 3D positioning.');
      return;
    }
    try {
      if (!audioContext) {
        audioContext = new AudioContextClass();
        // PannerNode is a platform HRTF spatializer, not a competing tone/DSP generator.
        mediaSource = audioContext.createMediaElementSource(manual);
        panner = audioContext.createPanner();
        panner.panningModel = 'HRTF';
        panner.distanceModel = 'inverse';
        panner.refDistance = 1;
        panner.maxDistance = 3;
        panner.rolloffFactor = 1;
        outputGain = audioContext.createGain();
        // Conservative attenuation across mono downmix/HRTF; NOT calibrated dB SPL.
        outputGain.gain.value = .25;
      }
      wire(true);
      updatePosition();
      await audioContext.resume(); // Only called from explicit UI gesture.
      announce('3D HRTF positioning enabled for manual bilateral tone previews only. Start sound explicitly; device volume stays low.');
    } catch (error) {
      enable.checked = false;
      wire(false);
      announce('Spatial routing unavailable: ' + error.message);
    }
  }
  enable.addEventListener('change', () => { void setupAudio(); });
  for (const id of ['manual-mode', 'manual-ear']) {
    byId(id).addEventListener('change', () => {
      if (!eligible() && enable.checked) {
        enable.checked = false;
        wire(false);
        announce('3D positioning disabled: only bilateral reference tone can be spatialized.');
      }
    });
  }
  Object.entries(ranges).forEach(([key,node]) => node.addEventListener('input', () =>
    positionAt(key === 'azimuth' ? Number(node.value) : position.azimuth,
      key === 'elevation' ? Number(node.value) : position.elevation,
      key === 'distance' ? Number(node.value) : position.distance)));
  function movePointer(e) {
    const r = pad.getBoundingClientRect();
    const x = clamp(((e.clientX - r.left) / r.width - .5) * 2, -1, 1);
    const y = clamp(((e.clientY - r.top) / r.height - .5) * 2, -1, 1);
    const azimuth = Math.abs(x) < .04 && Math.abs(y) < .04
      ? 0 : Math.atan2(x, -y) * 180 / Math.PI;
    positionAt(azimuth, position.elevation, position.distance);
  }
  let pointer = null;
  pad.addEventListener('pointerdown', e => {
    pointer = e.pointerId;
    pad.setPointerCapture(e.pointerId);
    movePointer(e);
  });
  pad.addEventListener('pointermove', e => { if (e.pointerId === pointer) movePointer(e); });
  const releasePointer = e => { if (e.pointerId === pointer) pointer = null; };
  pad.addEventListener('pointerup', releasePointer);
  pad.addEventListener('pointercancel', releasePointer);
  pad.addEventListener('keydown', e => {
    let yaw = 0, height = 0;
    if (e.key === 'ArrowLeft') yaw = -5;
    else if (e.key === 'ArrowRight') yaw = 5;
    else if (e.key === 'ArrowUp') height = 5;
    else if (e.key === 'ArrowDown') height = -5;
    else if (e.key === 'Home') { positionAt(0, 0, 1); e.preventDefault(); return; }
    else return;
    e.preventDefault();
    positionAt(position.azimuth + yaw, position.elevation + height, position.distance);
  });
  function moveSecondary(e) {
    const r = secondaryPad.getBoundingClientRect();
    const across = clamp((e.clientX - r.left) / r.width, 0, 1);
    const upwards = clamp((e.clientY - r.top) / r.height, 0, 1);
    positionAt(position.azimuth, (0.5 - upwards) * 90, 1 + 2 * across);
  }
  let secondaryPointer = null;
  secondaryPad.addEventListener('pointerdown', e => {
    secondaryPointer = e.pointerId;
    secondaryPad.setPointerCapture(e.pointerId);
    moveSecondary(e);
  });
  secondaryPad.addEventListener('pointermove', e => {
    if (e.pointerId === secondaryPointer) moveSecondary(e);
  });
  for (const eventName of ['pointerup','pointercancel']) {
    secondaryPad.addEventListener(eventName, e => {
      if (e.pointerId === secondaryPointer) secondaryPointer = null;
    });
  }
  secondaryPad.addEventListener('keydown', e => {
    let height=0, distance=0;
    if (e.key === 'ArrowLeft') distance=-0.1;
    else if (e.key === 'ArrowRight') distance=0.1;
    else if (e.key === 'ArrowUp') height=5;
    else if (e.key === 'ArrowDown') height=-5;
    else if (e.key === 'Home') {
      positionAt(position.azimuth,0,1);
      e.preventDefault();
      return;
    } else return;
    e.preventDefault();
    positionAt(position.azimuth,position.elevation+height,position.distance+distance);
  });
  byId('spatial-center').addEventListener('click', () => positionAt(0, 0, 1));
  byId('spatial-virtual-center').addEventListener('click', () => positionAt(0, 0, 1));
  byId('spatial-virtual-mark').addEventListener('click', () => {
    if (activeManual()) byId('manual-mark').click();
    else announce('A / Mark is available only while a manual tone is playing.');
  });
  byId('spatial-virtual-play').addEventListener('click', () => {
    if (!byId('manual-resume').disabled) byId('manual-resume').click();
    else announce('Prepare a manual reference tone first, then click Y / Play prepared.');
  });
  // Escape is an emergency *UI* stop. It cancels the manual element and
  // requests the existing host cleanup; does not claim physical silence.
  function stopPlayback() {
    const manualPending = byId('manual-start').disabled || !!manual.getAttribute('src');
    (manualPending ? byId('manual-stop') : byId('stop')).click();
  }
  byId('spatial-virtual-stop').addEventListener('click', () => {
    stopPlayback();
    announce('Virtual B / STOP requested. Actual host/device silence is not measured.');
  });
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape' && !e.repeat) {
      stopPlayback();
      announce('STOP requested locally; actual host/device silence is not measured.');
    }
  });
  function stick(name, padData) {
    const i = Number(axes[name].value);
    const value = Number(padData.axes[i]);
    if (!Number.isFinite(value)) return 0;
    const mapped = invert[name].checked ? -value : value;
    return Math.abs(mapped) < .18 ? 0 : clamp(mapped, -1, 1);
  }
  function buttonEdge(index, gp) {
    const value = gp.buttons[Number(index.value)];
    const pressed = !!(value && (value.pressed || Number(value.value) > .6));
    const key = String(index.id);
    const previous = lastButtons[key] || false;
    lastButtons[key] = pressed;
    return pressed && !previous;
  }
  function updateGamepadList() {
    const list = typeof navigator.getGamepads === 'function'
      ? Array.from(navigator.getGamepads()).filter(Boolean) : [];
    const selected = gamepadPicker.value;
    gamepadPicker.replaceChildren(new Option('Choose connected controller', ''));
    for (const gp of list) gamepadPicker.add(new Option(gp.id || ('Controller ' + gp.index), String(gp.index)));
    if (list.some(g => String(g.index) === selected)) gamepadPicker.value = selected;
    else if (list.length === 1) gamepadPicker.value = String(list[0].index);
    lastGamepadId = '';
    lastButtons = [];
    diagnostics.textContent = list.length ? 'Awaiting controller movement.' : 'No controller samples available.';
    deviceStatus.textContent = list.length
      ? list.length + ' gamepad(s) detected. Enable use, then move a stick or press a button. Nonstandard controllers may need axis/button remapping.'
      : 'No browser-accessible gamepad detected. USB/DirectInput support varies; use the virtual controller or keyboard.';
  }
  gamepadToggle.addEventListener('change', () => {
    updateGamepadList();
    if (!gamepadToggle.checked) announce('Physical controller input disabled; pointer and keyboard remain available.');
  });
  gamepadPicker.addEventListener('change', () => { lastGamepadId = ''; lastButtons = []; });
  window.addEventListener('gamepadconnected', updateGamepadList);
  window.addEventListener('gamepaddisconnected', updateGamepadList);
  function poll(time) {
    const elapsed = Math.min(.05, frameTime ? (time - frameTime) / 1000 : 0);
    frameTime = time;
    if (!document.hidden && gamepadToggle.checked && typeof navigator.getGamepads === 'function') {
      const gp = Array.from(navigator.getGamepads()).find(g => g && String(g.index) === gamepadPicker.value);
      if (gp) {
        if (lastGamepadId !== gp.id) {
          lastGamepadId = gp.id;
          lastButtons = [];
          deviceStatus.textContent = gp.id + ' (' + (gp.mapping || 'nonstandard') +
            ' mapping); ' + gp.axes.length + ' axes / ' + gp.buttons.length +
            ' buttons. STOP button remains available.';
        }
        // Axis motion changes position only. It CANNOT start audio.
        if (time - lastDiagnosticTime > 250) {
          lastDiagnosticTime = time;
          const visibleAxes = Array.from(gp.axes).slice(0,8)
            .map((value,i) => i + ':' + (Number.isFinite(value) ? value.toFixed(2) : '?')).join('  ');
          const pressed = gp.buttons.flatMap((button,i) =>
            button && (button.pressed || button.value > .6) ? [String(i)] : []);
          diagnostics.textContent = 'Raw axes ' + visibleAxes + ' | pressed buttons: ' +
            (pressed.join(', ') || 'none') + '.';
        }
        const yaw = stick('yaw', gp);
        const elevate = stick('elevation', gp);
        const depth = stick('distance', gp);
        if (yaw || elevate || depth)
          positionAt(position.azimuth + yaw * 125 * elapsed,
            position.elevation - elevate * 70 * elapsed,
            position.distance + depth * 1.2 * elapsed);
        if (buttonEdge(gpStop, gp)) {
          stopPlayback();
          announce('Gamepad STOP requested. Audio host acknowledgment and headphone SPL remain unverified.');
        }
        if (buttonEdge(gpMark, gp) && activeManual()) byId('manual-mark').click();
        if (buttonEdge(gpCenter, gp)) positionAt(0, 0, 1);
      }
    } else { lastButtons = []; }
    window.requestAnimationFrame(poll);
  }
  updatePosition();
  updateGamepadList();
  window.requestAnimationFrame(poll);
})();
