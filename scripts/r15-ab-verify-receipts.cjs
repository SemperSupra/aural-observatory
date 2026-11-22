#!/usr/bin/env node
// R15 durable AB evidence validator. Stdlib only, fail-closed on drift or misleading promotion.
'use strict';
const fs=require('node:fs'),path=require('node:path');
const filename=process.argv[2]||path.resolve(__dirname,'../docs/evaluation/r15/AB-VERIFIED-RECEIPTS.json');
const assert=require('node:assert/strict');
function check(data){
 assert.equal(data.schemaVersion,1);
 assert.equal(data.sourceRepository,'SemperSupra/aural-observatory');
 assert.match(data.sourceSha,/^[0-9a-f]{40}$/);
 assert.equal(data.sourceSha,data.firstFailure.correctiveCommit);
 assert.equal(data.workflowRunId,45033080315,'exact historical run must be cited');
 assert.equal(data.workflowConclusion,'success');
 assert.equal(data.humanParticipants,0);
 assert.equal(data.actualAcousticOutput,false);
 assert.equal(data.physicalStopQualified,false);
 assert.equal(data.humanPreferenceQualified,false);
 assert.equal(data.originalCssWidthConfound,true);
 assert.ok(data.importantCaveat.includes('not demonstrated human efficiency'));
 const expectedEngines=['chromium','firefox','webkit'];
 const expectedProfiles=['phone320','phoneLandscape','tablet','desktop'];
 assert.equal(data.browserArtifacts.length,3);
 for(const [i,a] of data.browserArtifacts.entries()){
  assert.equal(a.engine,expectedEngines[i]);
  assert.ok(Number.isInteger(a.artifactId)&&a.artifactId>0);
  assert.match(a.sha256,/^[0-9a-f]{64}$/);
 }
 assert.equal(data.results.engines,3);
 assert.equal(data.results.profilesPerEngine,4);
 assert.equal(data.results.conditions,2);
 assert.equal(data.results.taskCountPerView,9);
 assert.equal(data.results.pairedComparisons,12);
 assert.equal(data.results.browserViews,24);
 assert.equal(data.results.scriptedTaskEncounters,216);
 assert.equal(data.pairs.length,12);
 const seen=new Set();
 let reduction=0,sumLong=0,sumCard=0;
 for(const p of data.pairs){
  assert.ok(expectedEngines.includes(p.engine)&&expectedProfiles.includes(p.profile));
  const key=p.engine+'/'+p.profile;
  assert.ok(!seen.has(key),'duplicate comparison '+key);seen.add(key);
  for(const k of ['longHeight','cardHeight','longScroll','cardScroll','longActions','cardActions',
   'longZoomOverflow','cardZoomOverflow']){
   assert.ok(Number.isInteger(p[k])&&p[k]>=0,key+' invalid '+k);
  }
  assert.ok(p.longHeight>p.cardHeight,key+' card must not be taller in this evidence');
  assert.ok(p.longScroll>p.cardScroll&&p.cardScroll>0,key+' scroll regression or invalid evidence');
  assert.equal(p.longActions,p.cardActions,key+' navigation actions changed');
  assert.equal(p.longActions,7,key+' task script steps drift');
  assert.ok(p.longZoomOverflow<=2&&p.cardZoomOverflow<=2,key+' zoom overflow');
  reduction+=1-p.cardScroll/p.longScroll;
  sumCard+=p.cardScroll;sumLong+=p.longScroll;
 }
 assert.equal(seen.size,12);
 assert.equal(data.results.meanPerPairScrollReductionPercent,
  Number((reduction/12*100).toFixed(3)),'reported mean scroll metric drift');
 assert.equal(data.results.totalDistanceWeightedScrollReductionPercent,
  Number(((1-sumCard/sumLong)*100).toFixed(3)),'reported weighted scroll metric drift');
 assert.equal(data.firstFailure.observedRunId,45032815655);
 assert.equal(data.firstFailure.inheritedRunId,45032832028);
 assert.ok(data.firstFailure.exactHeadPasses.includes(data.workflowRunId));
 return {pairs:12,views:24,tasks:216,meanPercent:data.results.meanPerPairScrollReductionPercent,
  weightedPercent:data.results.totalDistanceWeightedScrollReductionPercent};
}
try{
 const report=check(JSON.parse(fs.readFileSync(filename,'utf8')));
 console.log('PASS R15 synthetic evidence integrity '+JSON.stringify(report));
}catch(e){console.error('FAIL R15 evidence contract: '+e.stack);process.exitCode=1;}
